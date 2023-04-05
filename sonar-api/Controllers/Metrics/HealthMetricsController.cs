using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Query;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Prometheus;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers.Metrics;

/// <summary>
/// API endpoints for dealing with granular health check metric time series data.
/// </summary>
[ApiController]
[Route("api/v2/health-metrics")]
public class HealthMetricsController : ControllerBase {

  private readonly ILogger<HealthMetricsController> _logger;
  private readonly PrometheusRemoteWriteClient _prometheusClient;

  public HealthMetricsController(
    ILogger<HealthMetricsController> logger,
    PrometheusRemoteWriteClient prometheusClient) {
    this._logger = logger;
    this._prometheusClient = prometheusClient;
  }

  [HttpPut("{environment}/tenants/{tenant}/services/{service}", Name = "RecordMetrics")]
  [Consumes(typeof(ImmutableList<(DateTime Timestamp, Decimal Value)>), contentType: "application/json")]
  [Produces(typeof(NoContentResult))]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  public async Task<IActionResult> RecordMetrics(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String service,
    [FromBody] ServiceHealthMetrics metrics,
    CancellationToken cancellationToken = default) {

    if (this._logger.IsEnabled(LogLevel.Information)) {
      StringBuilder debugLogMessageBuilder = new();

      debugLogMessageBuilder.Append(
        $"Received health metrics for Environment='{environment}' Tenant='{tenant}' Service='{service}':");

      foreach (var (healthCheck, samples) in metrics.HealthCheckSamples) {
        debugLogMessageBuilder.Append($"\n  HealthCheck='{healthCheck}':");
        foreach (var (timestamp, value) in samples) {
          debugLogMessageBuilder.Append($"\n    ({timestamp}, {value})");
        }
      }

      this._logger.LogInformation(message: "{logMessage}", debugLogMessageBuilder);
    }

    var prometheusWriteRequest = new WriteRequest();

    prometheusWriteRequest.Metadata.AddRange(
      new[] {
        new MetricMetadata {
          Help = "The value returned by a service health check metric query.",
          Type = MetricMetadata.Types.MetricType.Summary,
          MetricFamilyName = HealthDataHelper.ServiceHealthCheckDataMetricName
        }
      });

    prometheusWriteRequest.Timeseries.AddRange(
      metrics.HealthCheckSamples.Select(kvp => {
        var (healthCheck, samples) = kvp;
        var timeSeries = new TimeSeries();

        timeSeries.Labels.AddRange(
          new [] {
            new Label { Name = "__name__", Value = HealthDataHelper.ServiceHealthCheckDataMetricName },
            new Label { Name = HealthDataHelper.MetricLabelKeys.Environment, Value = environment },
            new Label { Name = HealthDataHelper.MetricLabelKeys.Tenant, Value = tenant },
            new Label { Name = HealthDataHelper.MetricLabelKeys.Service, Value = service },
            new Label { Name = HealthDataHelper.MetricLabelKeys.HealthCheck, Value = healthCheck },
          });

        timeSeries.Samples.AddRange(
          samples.Select(sample => new Sample {
            Timestamp = sample.Timestamp.MillisSinceUnixEpoch(),
            Value = sample.Value
          }));

        return timeSeries;
      }));

    var problem = await this._prometheusClient.RemoteWriteRequest(prometheusWriteRequest, cancellationToken);

    return problem == null
      ? this.NoContent()
      : this.StatusCode((Int32)HttpStatusCode.BadRequest, problem);
  }
}
