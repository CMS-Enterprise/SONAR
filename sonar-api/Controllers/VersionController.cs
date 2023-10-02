using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Prometheus;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[Authorize(Policy = "Admin")]
[Route("api/v{version:apiVersion}/version")]
public class VersionController : ControllerBase {
  private readonly PrometheusRemoteWriteClient _remoteWriteClient;
  private readonly ILogger<HealthController> _logger;
  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly VersionDataHelper _versionDataHelper;
  private readonly ServiceVersionCacheHelper _versionCacheHelper;
  private readonly ValidationHelper _validationHelper;
  public VersionController(
    ServiceDataHelper serviceDataHelper,
    VersionDataHelper versionDataHelper,
    PrometheusRemoteWriteClient remoteWriteClient,
    ILogger<HealthController> logger,
    ServiceVersionCacheHelper versionCacheHelper,
    ValidationHelper validationHelper) {
    this._serviceDataHelper = serviceDataHelper;
    this._versionDataHelper = versionDataHelper;
    this._remoteWriteClient = remoteWriteClient;
    this._logger = logger;
    this._versionCacheHelper = versionCacheHelper;
    this._validationHelper = validationHelper;
  }

  /// <summary>
  ///   Records a single version for the specified service.
  /// </summary>
  /// <response code="204">The service version was successfully recorded.</response>
  /// <response code="400">The service version provided is not valid.</response>
  /// <response code="401">The API key in the header is not authorized for recording.</response>
  /// <response code="404">The specified environment, tenant, or service was not found.</response>
  /// <response code="500">An internal error occurred attempting to record the service version.</response>
  [HttpPost("{environment}/tenants/{tenant}/services/{service}", Name = "RecordServiceVersion")]
  [Consumes(typeof(ServiceVersion), contentType: "application/json")]
  [ProducesResponseType(statusCode: 204)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 500)]
  public async Task<IActionResult> RecordServiceVersion(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String service,
    [FromBody] ServiceVersion value,
    CancellationToken cancellationToken = default) {

    // version data validation
    await ValidateVersionData(environment, tenant, service, value, cancellationToken);

    // sample timestamp validation
    ValidationHelper.ValidateTimestamp(value.Timestamp);

    async Task CachingTaskExceptionHandling() {
      try {
        await this._versionCacheHelper
          .CreateUpdateVersionCache(
            environment,
            tenant,
            service,
            value,
            cancellationToken);
      } catch (Exception e) {
        this._logger.LogWarning(
          message: "Unexpected error occurred during version caching process: {Message}",
          e.Message
        );
      }
    }

    // construct write data
    var writeData =
      new WriteRequest {
        Metadata = {
            new MetricMetadata {
              Help = "The most recent version of a service.",
              Type = MetricMetadata.Types.MetricType.Stateset,
              MetricFamilyName = VersionDataHelper.ServiceVersionAggregateMetricName
            },
            new MetricMetadata {
              Help = "The version recorded for individual service version checks.",
              Type = MetricMetadata.Types.MetricType.Stateset,
              MetricFamilyName = VersionDataHelper.ServiceVersionCheckMetricName
            }
        }
      };

    writeData.Timeseries.AddRange(
      CreateVersionMetric(
        VersionDataHelper.ServiceVersionAggregateMetricName,
        value.Timestamp,
        value.VersionChecks,
        new Label { Name = HealthDataHelper.MetricLabelKeys.Environment, Value = environment },
        new Label { Name = HealthDataHelper.MetricLabelKeys.Tenant, Value = tenant },
        new Label { Name = HealthDataHelper.MetricLabelKeys.Service, Value = service }
        )
      );

    var cachingTask = CachingTaskExceptionHandling();
    var prometheusTask = this._remoteWriteClient.RemoteWriteRequest(writeData, cancellationToken);

    try {
      await Task.WhenAll(cachingTask, prometheusTask);
    } catch (Exception) {
      // ignore
    }

    // This will throw if the prometheus write has an issue
    try {
      var problem = await prometheusTask;

      if (problem == null) {
        return this.NoContent();
      }

      if (problem.Status == (Int32)HttpStatusCode.BadRequest) {
        problem.Type = ProblemTypes.InvalidData;
      }

      return this.StatusCode(problem.Status ?? (Int32)HttpStatusCode.InternalServerError, problem);
    } catch (Exception ex) {
      this._logger.LogError(
        ex,
        "An unhandled exception was raised by Prometheus while attempting to write service version: {Message}",
        ex.Message
      );

      // This type of failure should be invisible to the agent since we also cache the status
      return this.NoContent();
    }

  }

  [HttpGet("{environment}/tenants/{tenant}/services/{*servicePath}", Name = "GetSpecificServiceVersionDetails")]
  [ProducesResponseType(typeof(ServiceVersionDetails[]), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(500)]
  [AllowAnonymous]
  public async Task<IActionResult> GetSpecificServiceVersionDetails(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String servicePath,
    CancellationToken cancellationToken) {

    var (_, _, services) =
      await this._serviceDataHelper.FetchExistingConfiguration(environment, tenant, cancellationToken);
    var serviceChildIdsLookup = await this._serviceDataHelper.GetServiceChildIdsLookup(services, cancellationToken);

    // Validate specified service
    Service? existingService =
      await this._serviceDataHelper.GetSpecificService(environment, tenant, servicePath, serviceChildIdsLookup,
        cancellationToken);

    if (existingService == null) {
      throw new ResourceNotFoundException(nameof(Service), servicePath);
    }

    var queryResults = await this._versionDataHelper.GetVersionDetailsForService(
      environment,
      tenant,
      existingService.Name,
      cancellationToken);

    return this.Ok(queryResults.ToArray());
  }



  private static IEnumerable<TimeSeries> CreateVersionMetric(
    String metricName,
    DateTime timestamp,
    IReadOnlyDictionary<VersionCheckType, String> versionChecks,
    params Label[] labels) {

    var timestampMs = (Int64)timestamp.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMilliseconds;

    return versionChecks.Select(kvp => {
      var ts = new TimeSeries {
        Labels = {
          new Label { Name = "__name__", Value = metricName },
          new Label { Name = VersionDataHelper.ServiceVersionTypeLabelName, Value = kvp.Key.ToString() },
          new Label { Name = VersionDataHelper.ServiceVersionValueLabelName, Value = kvp.Value }
        },
        Samples = {
          new Sample {
            Timestamp = timestampMs,
            Value = 1
          }
        }
      };

      ts.Labels.AddRange(labels);

      return ts;
    });
  }

  private async Task ValidateVersionData(
    String environment, String tenant, String service, ServiceVersion value,
    CancellationToken cancellationToken) {

    // Ensure the specified service exists
    var existingService =
      await this._serviceDataHelper.FetchExistingService(environment, tenant, service, cancellationToken);

    var existingVersionChecks =
      await this._serviceDataHelper.FetchExistingVersionChecks(new[] { existingService.Id }, cancellationToken);

    var existingVersionCheckDictionary =
      existingVersionChecks.ToImmutableDictionary(vc => vc.VersionCheckType.ToString(), StringComparer.OrdinalIgnoreCase);

    foreach (var versionCheck in value.VersionChecks) {
      if (!existingVersionCheckDictionary.TryGetValue(versionCheck.Key.ToString(), out var val)) {
        throw new BadRequestException(
          message: "Version Check not present in Configuration",
          ProblemTypes.InconsistentData,
          new Dictionary<String, Object?> {
            { nameof(VersionCheck), versionCheck.Key }
          }
        );
      }
    }
  }
}
