using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
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
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/health-check-history")]
public class HealthCheckHistoryController : ControllerBase {
  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly HealthDataHelper _healthDataHelper;
  private readonly PrometheusQueryHelper _prometheusQueryHelper;

  public HealthCheckHistoryController(
    ServiceDataHelper serviceDataHelper,
    HealthDataHelper healthDataHelper,
    PrometheusQueryHelper prometheusQueryHelper
  ) {

    this._serviceDataHelper = serviceDataHelper;
    this._healthDataHelper = healthDataHelper;
    this._prometheusQueryHelper = prometheusQueryHelper;
  }

  /// <summary>
  /// Retrieves the instantaneous prometheus health status for each healthcheck given the service at a specific time.
  /// </summary>
  /// <param name="environment"></param>
  /// <param name="tenant"></param>
  /// <param name="service"></param>
  /// <param name="timeQuery"></param>
  /// <param name="cancellationToken"></param>
  [HttpGet("{environment}/tenants/{tenant}/services/{service}/health-check-result", Name = "GetHealthCheckResultForService")]
  [ProducesResponseType(typeof(Dictionary<String, (DateTime Timestamp, HealthStatus Status)>), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(500)]
  public async Task<IActionResult> GetHealthCheckResultForService(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String service,
    [FromQuery] DateTime? timeQuery,
    CancellationToken cancellationToken) {

    timeQuery ??= DateTime.UtcNow;
    ValidationHelper.ValidateTimestamp((DateTime)timeQuery);

    var result = await this._healthDataHelper.GetHealthCheckResultForService(
      environment,
      tenant,
      service,
      (DateTime)timeQuery,
      cancellationToken);

    return this.Ok(result);
  }

  /// <summary>
  /// Retrieves the prometheus health status time series for each healthcheck given the
  /// service. Filters out samples outside of the given start and end date time.
  /// </summary>
  /// <param name="environment">The environment to get timestamps for.</param>
  /// <param name="tenant">The tenant to get timestamps for.</param>
  /// <param name="service">The service to get timestamps for.</param>
  /// <param name="start"></param>
  /// <param name="end"></param>
  /// <param name="step"></param>
  /// <param name="cancellationToken">The cancellation token for the async operation.</param>
  /// <exception cref="BadRequestException">If the Prometheus request is invalid.</exception>
  /// <exception cref="InternalServerErrorException">If there's any other problem calling Prometheus.</exception>
  [HttpGet("{environment}/tenants/{tenant}/services/{service}/health-check-results", Name = "GetHealthCheckResultsForService")]
  [ProducesResponseType(typeof(HealthCheckHistory), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  public async Task<IActionResult> GetHealthCheckResultsForService(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String service,
    [Optional] DateTime? start, // Nullable so we don't get Unix epoch as a default if not provided in query; we'll provide our own defaults
    [Optional] DateTime? end,
    [Optional] Int32? step,
    CancellationToken cancellationToken = default) {

    var (startTime, endTime, stepIncrement) = this._prometheusQueryHelper.ValidateParameters(start, end, step);

    // Fetch existing service
    var existingService = await this._serviceDataHelper.FetchExistingService(environment, tenant, service, cancellationToken);
    var existingServiceDict = new Dictionary<Guid, Service>() { { existingService.Id, existingService } };

    // Extract list of health checks
    var healthChecksByService = await this._healthDataHelper.GetHealthChecksByService(
      existingServiceDict.ToImmutableDictionary(),
      cancellationToken);
    var healthChecks = healthChecksByService.SelectMany(hc => hc).ToList();

    var result = new Dictionary<String, List<(DateTime, HealthStatus)>>();
    foreach (var healthCheck in healthChecks) {
      var currentHealthCheckName = healthCheck.Name;
      var timestampedHealthCheckMetrics = await this._healthDataHelper.GetHealthCheckResultsForService(
        environment,
        tenant,
        service,
        currentHealthCheckName,
        startTime,
        endTime,
        stepIncrement,
        cancellationToken);

      result.Add(currentHealthCheckName, timestampedHealthCheckMetrics);
    }

    return this.Ok(new HealthCheckHistory(result));
  }
}
