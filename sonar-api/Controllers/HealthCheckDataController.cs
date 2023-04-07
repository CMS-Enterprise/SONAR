using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Prometheus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

/// <summary>
/// API endpoints for dealing with granular health check time series data.
/// </summary>
[ApiController]
[ApiVersion(2)]
[Route("api/v{version:apiVersion}/health-check-data")]
public class HealthCheckDataController : ControllerBase {

  private readonly ILogger<HealthCheckDataController> _logger;
  private readonly IPrometheusService _prometheusService;
  private readonly ApiKeyDataHelper _apiKeyDataHelper;

  public HealthCheckDataController(
    ILogger<HealthCheckDataController> logger,
    IPrometheusService prometheusService,
    ApiKeyDataHelper apiKeyDataHelper) {
    this._logger = logger;
    this._prometheusService = prometheusService;
    this._apiKeyDataHelper = apiKeyDataHelper;
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

    // TODO: This is temporary pending the outcome of a spike to determine an annotation/middleware based
    // TODO (cont): approach for authorizing endpoints.
    await this._apiKeyDataHelper.ValidateTenantPermission(
      this.Request.Headers["ApiKey"].SingleOrDefault(),
      environment,
      tenant,
      activity: "record health check data",
      cancellationToken);

    if (data.HealthCheckSamples.Count == 0) {
      throw new BadRequestException($"No data provided.");
    }

    foreach (var (healthCheck, samples) in data.HealthCheckSamples) {
      if ((samples == null) || (samples.Count == 0)) {
        throw new BadRequestException($"No samples provided for '{healthCheck}'.");
      }
    }

    this._logger.LogDebug(
      message: $"Received {data.TotalSamples} samples from {data.TotalHealthChecks} health checks for" +
       $" environment = '{environment}', tenant = '{tenant}', service = '{service}': {data}");

    var recordedData = await this._prometheusService.WriteHealthCheckDataAsync(
      environment,
      tenant,
      service,
      data,
      cancellationToken);

    this._logger.LogDebug(
      message: $"Recorded {recordedData.TotalSamples} samples from {recordedData.TotalHealthChecks} health checks for" +
        $" environment = '{environment}', tenant = '{tenant}', service = '{service}'");

    return this.Ok(recordedData);
  }
}
