using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Prometheus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

/// <summary>
/// API endpoints for dealing with granular health check time series data.
/// </summary>
[ApiController]
[ApiVersion(2)]
[Authorize(Policy = "Admin")]
[Route("api/v{version:apiVersion}/health-check-data")]
public class HealthCheckDataController : ControllerBase {

  private readonly ILogger<HealthCheckDataController> _logger;
  private readonly IPrometheusService _prometheusService;
  private readonly ValidationHelper _validationHelper;

  public HealthCheckDataController(
    ILogger<HealthCheckDataController> logger,
    IPrometheusService prometheusService,
    ValidationHelper validationHelper) {
    this._logger = logger;
    this._prometheusService = prometheusService;
    this._validationHelper = validationHelper;
  }

  /// <summary>
  /// Record the given raw <see cref="ServiceHealthData"/> time series samples for the given environment,
  /// tenant, and service in Prometheus. Filters out stale and out-of-order samples prior to calling P8s.
  /// </summary>
  /// <param name="environment">The environment the data belongs to.</param>
  /// <param name="tenant">The tenant the data belongs to.</param>
  /// <param name="service">The service the data belongs to.</param>
  /// <param name="data">The health check data to be written.</param>
  /// <param name="cancellationToken">The cancellation token for the async operation.</param>
  /// <returns>
  /// A new <see cref="ServiceHealthData"/> containing the samples that were actually written;
  /// can be empty if no samples were written because they all failed the filtering criteria.
  /// </returns>
  /// <exception cref="BadRequestException">If the Prometheus request is invalid.</exception>
  /// <exception cref="InternalServerErrorException">If there's any other problem calling Prometheus.</exception>
  [HttpPost("{environment}/tenants/{tenant}/services/{service}", Name = "RecordHealthCheckData")]
  [Consumes(typeof(ServiceHealthData), contentType: "application/json")]
  [ProducesResponseType(typeof(ServiceHealthData), statusCode: (Int32)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: (Int32)HttpStatusCode.BadRequest)]
  [ProducesResponseType((Int32)HttpStatusCode.InternalServerError)]
  public async Task<IActionResult> RecordHealthCheckData(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String service,
    [FromBody] ServiceHealthData data,
    CancellationToken cancellationToken = default) {

    if (data.HealthCheckSamples.Count == 0) {
      throw new BadRequestException($"No data provided.");
    }

    foreach (var (healthCheck, samples) in data.HealthCheckSamples) {
      if ((samples == null) || (samples.Count == 0)) {
        throw new BadRequestException($"No samples provided for '{healthCheck}'.");
      }

      // validate sample timestamp
      foreach (var sample in samples) {
        ValidationHelper.ValidateTimestamp(sample.Timestamp);
      }
    }

    this._logger.LogDebug(
      message: "Received {TotalSamples} samples from {TotalHealthChecks} health checks for environment = '{Environment}', tenant = '{Tenant}', service = '{Service}': {Data}",
      data.TotalSamples,
      data.TotalHealthChecks,
      environment,
      tenant,
      service,
      data);

    var recordedData = await this._prometheusService.WriteHealthCheckDataAsync(
      environment,
      tenant,
      service,
      data,
      cancellationToken);

    this._logger.LogDebug(
      message: "Recorded {TotalSamples} samples from {TotalHealthChecks} health checks for environment = '{Environment}', tenant = '{Tenant}', service = '{Service}'",
      recordedData.TotalSamples,
      recordedData.TotalHealthChecks,
      environment,
      tenant,
      service);

    return this.Ok(recordedData);
  }

  /// <summary>
  /// Retrieves the given raw <see cref="ServiceHealthData"/> time series samples for the given environment,
  /// tenant, service, and health check in Prometheus. Filters out samples outside of the given start and end date time
  /// (or if those are not given, filters out samples from more than 10 minutes ago UTC) prior to calling P8s.
  /// </summary>
  /// <param name="environment">The environment to get timestamps for.</param>
  /// <param name="tenant">The tenant to get timestamps for.</param>
  /// <param name="service">The service to get timestamps for.</param>
  /// <param name="healthCheck">The name of the health check to get timestamps for.</param>
  /// <param name="queryEnd"></param>
  /// <param name="cancellationToken">The cancellation token for the async operation.</param>
  /// <param name="queryStart"></param>
  /// <exception cref="BadRequestException">If the Prometheus request is invalid.</exception>
  /// <exception cref="InternalServerErrorException">If there's any other problem calling Prometheus.</exception>
  [HttpGet("{environment}/tenants/{tenant}/services/{service}/health-check/{healthCheck}", Name = "GetHealthCheckData")]
  [ProducesResponseType(typeof(MetricDataCollection), statusCode: (Int32)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: (Int32)HttpStatusCode.BadRequest)]
  [AllowAnonymous]
  public async Task<IActionResult> GetHealthCheckData(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String service,
    [FromRoute] String healthCheck,
    [FromQuery] DateTime? queryStart, // Nullable so we don't get Unix epoch as a default if not provided in query; we'll provide our own defaults
    [FromQuery] DateTime? queryEnd,
    CancellationToken cancellationToken = default) {

    queryEnd ??= DateTime.UtcNow;
    queryStart ??= queryEnd.Value.Subtract(TimeSpan.FromMinutes(10));

    if (queryEnd <= queryStart) {
      throw new BadRequestException("Query end time must be after query start time.");
    }

    if ((queryEnd - queryStart) > TimeSpan.FromDays(7)) {
      throw new BadRequestException("Query time range is too large.");
    }

    var timestampedHealthCheckMetrics = await this._prometheusService.QuerySpecificHealthCheckDataAsync(
      environment,
      tenant,
      service,
      healthCheck,
      start: queryStart.Value,
      end: queryEnd.Value,
      cancellationToken);

    return this.Ok(new MetricDataCollection(timestampedHealthCheckMetrics));
  }
}
