using System;
using System.Collections.Immutable;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Exceptions;
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
[Route("api/v2/health-check-data")]
public class HealthCheckDataController : ControllerBase {

  private readonly ILogger<HealthCheckDataController> _logger;
  private readonly IPrometheusService _prometheusService;

  public HealthCheckDataController(
    ILogger<HealthCheckDataController> logger,
    IPrometheusService prometheusService) {
    this._logger = logger;
    this._prometheusService = prometheusService;
  }

  [HttpPost("{environment}/tenants/{tenant}/services/{service}", Name = "RecordMetrics")]
  [Consumes(typeof(ImmutableList<(DateTime Timestamp, Decimal Value)>), contentType: "application/json")]
  [ProducesResponseType(typeof(NoContentResult), statusCode: (Int32)HttpStatusCode.NoContent)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: (Int32)HttpStatusCode.BadRequest)]
  [ProducesResponseType((Int32)HttpStatusCode.InternalServerError)]
  public async Task<IActionResult> RecordMetrics(
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
        throw new BadRequestException($"No samples provided for {healthCheck}.");
      }
    }

    this._logger.LogDebug(
      message: "Received service health metrics for " +
        "environment = \"{environment}\", tenant = \"{tenant}\", service = \"{service}\": {metrics}",
      environment,
      tenant,
      service,
      data);

    await this._prometheusService.WriteHealthCheckDataAsync(
      environment,
      tenant,
      service,
      data,
      cancellationToken);

    return this.NoContent();
  }
}
