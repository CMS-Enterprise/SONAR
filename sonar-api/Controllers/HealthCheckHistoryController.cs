using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Prometheus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PrometheusQuerySdk;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

/// <summary>
/// API endpoints for that returns historic time series data or individual service
/// </summary>
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
  /// Retrieves the instantaneous prometheus health status each healthcheck given the service.
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

    var timestamp = timeQuery ?? DateTime.UtcNow;
    if (timestamp.Kind != DateTimeKind.Utc) {
      return this.BadRequest("Invalid timestamp");
    }

    var result = await this._healthDataHelper.GetHealthCheckResultForService(
      environment,
      tenant,
      service,
      timestamp,
      cancellationToken);

    return this.Ok(result);
  }

  /// <summary>
  /// Retrieves prometheus health status time series for each healthcheck given the
  /// service. Filters out samples outside of the given start and end date time
  /// (or if those are not given, filters out samples from more than 10 minutes ago UTC) prior to calling P8s.
  /// </summary>
  /// <param name="environment">The environment to get timestamps for.</param>
  /// <param name="tenant">The tenant to get timestamps for.</param>
  /// <param name="service">The service to get timestamps for.</param>
  /// <param name="queryEnd"></param>
  /// <param name="cancellationToken">The cancellation token for the async operation.</param>
  /// <param name="queryStart"></param>
  /// <param name="step"></param>
  /// <exception cref="BadRequestException">If the Prometheus request is invalid.</exception>
  /// <exception cref="InternalServerErrorException">If there's any other problem calling Prometheus.</exception>
  [HttpGet("{environment}/tenants/{tenant}/services/{service}/health-check-results", Name = "GetHealthCheckResultsForService")]
  [ProducesResponseType(typeof(HealthCheckHistory), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  public async Task<IActionResult> GetHealthCheckHistoryData(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String service,
    [Optional] DateTime? queryStart, // Nullable so we don't get Unix epoch as a default if not provided in query; we'll provide our own defaults
    [Optional] DateTime? queryEnd,
    [Optional] Int32? step,
    CancellationToken cancellationToken = default) {

    var (startTime, endTime, stepIncrement) = this._prometheusQueryHelper.ValidateParameters(queryStart, queryEnd, step);

    // Get Services from configuration
    var (_, _, services) =
      await this._serviceDataHelper.FetchExistingConfiguration(environment, tenant, cancellationToken);

    var resultService = services.Where(svc => svc.Value.Name == service).ToImmutableDictionary();

    // Get List of Health Checks this is a list of all services
    var healthChecksByService = await this._healthDataHelper.GetHealthChecksByService(
      resultService,
      cancellationToken);

    var result = new List<Dictionary<String, List<(DateTime, HealthStatus)>>>();
      foreach (var healthCheck in healthChecksByService.FirstOrDefault()!) {
        var currentHealthCheckName = healthCheck.Name;
        var timestampedHealthCheckMetrics = await this._healthDataHelper.GetHealthCheckResultsForService(
          environment,
          tenant,
          service,
          healthCheck: currentHealthCheckName,
          startTime,
          endTime,
          stepIncrement,
          cancellationToken);

        result.Add(timestampedHealthCheckMetrics);
      }

    return this.Ok(new HealthCheckHistory(result));
  }
}
