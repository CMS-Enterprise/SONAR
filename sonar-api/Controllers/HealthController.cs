using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;
using Enum = System.Enum;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[Authorize(Policy = "Admin")]
[Route("api/v{version:apiVersion}/health")]
public class HealthController : ControllerBase {
  private readonly PrometheusRemoteWriteClient _remoteWriteClient;
  private readonly ILogger<HealthController> _logger;
  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly HealthDataHelper _healthDataHelper;
  private readonly ServiceHealthCacheHelper _cacheHelper;
  private readonly String _sonarEnvironment;
  private readonly ValidationHelper _validationHelper;

  public HealthController(
    ServiceDataHelper serviceDataHelper,
    HealthDataHelper healthDataHelper,
    ServiceHealthCacheHelper cacheHelper,
    PrometheusRemoteWriteClient remoteWriteClient,
    IOptions<SonarHealthCheckConfiguration> sonarHealthConfig,
    ILogger<HealthController> logger,
    ValidationHelper validationHelper) {
    this._serviceDataHelper = serviceDataHelper;
    this._healthDataHelper = healthDataHelper;
    this._cacheHelper = cacheHelper;
    this._remoteWriteClient = remoteWriteClient;
    this._sonarEnvironment = sonarHealthConfig.Value.SonarEnvironment;
    this._logger = logger;
    this._validationHelper = validationHelper;
  }

  /// <summary>
  ///   Records a single health status for the specified service.
  /// </summary>
  /// <remarks>
  ///   Service health status information must be recorded in chronological order per-service, and cannot
  ///   be recorded for timestamps older than 2 hours. Timestamps greater than 2 hours will result in an
  ///   "out of bounds" error. Health status that is reported out of order will result in an "out of
  ///   order sample" error.
  /// </remarks>
  /// <response code="204">The service health status was successfully recorded.</response>
  /// <response code="400">The service health status provided is not valid.</response>
  /// <response code="401">The API key in the header is not authorized for recording.</response>
  /// <response code="404">The specified environment, tenant, or service was not found.</response>
  /// <response code="500">An internal error occurred attempting to record the service health status.</response>
  [HttpPost("{environment}/tenants/{tenant}/services/{service}", Name = "RecordStatus")]
  [Consumes(typeof(ServiceHealth), contentType: "application/json")]
  [ProducesResponseType(statusCode: 204)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 500)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 503)]
  public async Task<IActionResult> RecordStatus(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String service,
    [FromBody] ServiceHealth value,
    CancellationToken cancellationToken = default) {

    // health status validation
    var canonicalHeathStatusDictionary =
      await ValidateHealthStatus(environment, tenant, service, value, cancellationToken);

    // sample timestamp validation
    ValidationHelper.ValidateTimestamp(value.Timestamp);

    // local function to handle exception handling for caching
    async Task CachingTaskExceptionHandling() {
      try {
        await this._cacheHelper
          .CreateUpdateCache(
            environment,
            tenant,
            service,
            value,
            canonicalHeathStatusDictionary.ToImmutableDictionary(),
            cancellationToken);
      } catch (Exception e) {
        this._logger.LogWarning(
          message: "Unexpected error occurred during caching process: {Message}",
          e.Message
        );
      }
    }

    var writeData =
      new WriteRequest {
        Metadata = {
          new MetricMetadata {
            Help = "The aggregate health status of a service.",
            Type = MetricMetadata.Types.MetricType.Stateset,
            MetricFamilyName = HealthDataHelper.ServiceHealthAggregateMetricName
          },
          new MetricMetadata {
            Help = "The status of individual service health checks.",
            Type = MetricMetadata.Types.MetricType.Stateset,
            MetricFamilyName = HealthDataHelper.ServiceHealthCheckMetricName
          }
        }
      };

    writeData.Timeseries.AddRange(
      HealthController.CreateHealthStatusMetric(
        HealthDataHelper.ServiceHealthAggregateMetricName,
        value.Timestamp,
        value.AggregateStatus,
        new Label { Name = HealthDataHelper.MetricLabelKeys.Environment, Value = environment },
        new Label { Name = HealthDataHelper.MetricLabelKeys.Tenant, Value = tenant },
        new Label { Name = HealthDataHelper.MetricLabelKeys.Service, Value = service }
      )
    );

    writeData.Timeseries.AddRange(
      canonicalHeathStatusDictionary.SelectMany(kvp =>
        HealthController.CreateHealthStatusMetric(
          HealthDataHelper.ServiceHealthCheckMetricName,
          value.Timestamp,
          kvp.Value,
          new Label { Name = HealthDataHelper.MetricLabelKeys.Environment, Value = environment },
          new Label { Name = HealthDataHelper.MetricLabelKeys.Tenant, Value = tenant },
          new Label { Name = HealthDataHelper.MetricLabelKeys.Service, Value = service },
          new Label { Name = HealthDataHelper.MetricLabelKeys.HealthCheck, Value = kvp.Key }
        )
      )
    );

    // cache data in parallel with Prometheus request
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
        "An unhandled exception was raised by Prometheus while attempting to write service status: {Message}",
        ex.Message
      );

      // This type of failure should be invisible to the agent since we also cache the status
      return this.NoContent();
    }
  }

  [HttpGet("{environment}/tenants/sonar", Name = "GetSonarHealth")]
  [ProducesResponseType(typeof(ServiceHierarchyHealth[]), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 500)]
  [AllowAnonymous]
  public async Task<IActionResult> GetSonarHealth(
    [FromRoute] String environment,
    CancellationToken cancellationToken) {
    // Check if environment provided matches value in config.
    if (environment != this._sonarEnvironment) {
      return this.NotFound(new {
        Message = "Sonar environment not found."
      });
    }

    return this.Ok(await this._healthDataHelper.CheckSonarHealth(cancellationToken));
  }


  [HttpGet("{environment}/tenants/{tenant}", Name = "GetServiceHierarchyHealth")]
  [ProducesResponseType(typeof(ServiceHierarchyHealth[]), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(500)]
  [AllowAnonymous]
  public async Task<IActionResult> GetServiceHierarchyHealth(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    CancellationToken cancellationToken) {

    if (String.Equals(environment, this._sonarEnvironment, StringComparison.OrdinalIgnoreCase) &&
      String.Equals(tenant, TenantDataHelper.SonarTenantName, StringComparison.OrdinalIgnoreCase)) {
      return this.Ok(GetSonarHealth(environment, cancellationToken));
    }

    var (_, _, services) =
      await this._serviceDataHelper.FetchExistingConfiguration(environment, tenant, cancellationToken);

    var serviceStatuses = await this._healthDataHelper.GetServiceStatuses(
      environment, tenant, cancellationToken
    );
    var healthCheckStatus = await this._healthDataHelper.GetHealthCheckStatus(
      environment, tenant, cancellationToken
    );

    var serviceChildIdsLookup = await this._serviceDataHelper.GetServiceChildIdsLookup(services, cancellationToken);
    var healthChecksByService = await this._healthDataHelper.GetHealthChecksByService(services, cancellationToken);

    return this.Ok(
      services.Values.Where(svc => svc.IsRootService)
        .Select(svc => this._healthDataHelper.ToServiceHealth(
          svc, services, serviceStatuses, serviceChildIdsLookup, healthChecksByService, healthCheckStatus)
        )
        .ToArray()
    );
  }

  [HttpGet("{environment}/tenants/{tenant}/services/{*servicePath}", Name = "GetSpecificServiceHierarchyHealth")]
  [ProducesResponseType(typeof(ServiceHierarchyHealth[]), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(500)]
  [AllowAnonymous]
  public async Task<IActionResult> GetSpecificServiceHierarchyHealth(
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

    var serviceStatuses = await this._healthDataHelper.GetServiceStatuses(
      environment, tenant, cancellationToken
    );
    var healthCheckStatus = await this._healthDataHelper.GetHealthCheckStatus(
      environment, tenant, cancellationToken
    );
    var healthChecksByService = await this._healthDataHelper.GetHealthChecksByService(services, cancellationToken);

    return this.Ok(this._healthDataHelper.ToServiceHealth(
      services.Values.Single(svc => svc.Id == existingService.Id),
      services, serviceStatuses, serviceChildIdsLookup, healthChecksByService, healthCheckStatus)
    );
  }

  private static IEnumerable<TimeSeries> CreateHealthStatusMetric(
    String metricName,
    DateTime timestamp,
    HealthStatus currentStatus,
    params Label[] labels) {

    var timestampMs = (Int64)timestamp.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMilliseconds;

    return Enum.GetValues<HealthStatus>().Select(status => {
      var ts = new TimeSeries {
        Labels = {
          new Label { Name = "__name__", Value = metricName },
          new Label { Name = metricName, Value = status.ToString() }
        },
        Samples = {
          new Sample {
            Timestamp = timestampMs,
            Value = currentStatus == status ? 1 : 0
          }
        }
      };

      ts.Labels.AddRange(labels);

      return ts;
    });
  }

  private async Task<IDictionary<String, HealthStatus>> ValidateHealthStatus(
    String environment, String tenant, String service, ServiceHealth value,
    CancellationToken cancellationToken) {

    // Ensure the specified service exists
    var existingService =
      await this._serviceDataHelper.FetchExistingService(environment, tenant, service, cancellationToken);

    var existingHealthChecks =
      await this._serviceDataHelper.FetchExistingHealthChecks(new[] { existingService.Id }, cancellationToken);

    var existingHealthCheckDictionary =
      existingHealthChecks.ToImmutableDictionary(hc => hc.Name, StringComparer.OrdinalIgnoreCase);

    var newHealthStatusByName = new Dictionary<String, HealthStatus>(StringComparer.OrdinalIgnoreCase);
    foreach (var healthCheck in value.HealthChecks) {
      if (existingHealthCheckDictionary.TryGetValue(healthCheck.Key, out var existingHealthCheck)) {
        newHealthStatusByName.Add(existingHealthCheck.Name, healthCheck.Value);
      } else {
        throw new BadRequestException(
          message: "Health Check not present in Configuration",
          ProblemTypes.InconsistentData,
          new Dictionary<String, Object?> {
            { nameof(HealthCheck), healthCheck.Key }
          }
        );
      }
    }

    return newHealthStatusByName;
  }
}
