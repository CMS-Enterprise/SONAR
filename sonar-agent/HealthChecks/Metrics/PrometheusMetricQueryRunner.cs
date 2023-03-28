using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Prometheus;
using Cms.BatCave.Sonar.Query;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Agent.HealthChecks.Metrics;

public class PrometheusMetricQueryRunner : MetricQueryRunnerBase {
  private readonly IPrometheusClient _promClient;

  public PrometheusMetricQueryRunner(
    IPrometheusClient promClient,
    ILogger<PrometheusMetricQueryRunner> logger) :
    base(logger) {

    this._promClient = promClient;
  }

  protected override async Task<ResponseEnvelope<QueryResults>?> GetQueryResultsAsync(
    String query,
    DateTime start,
    DateTime end,
    CancellationToken cancellationToken) {

    ResponseEnvelope<QueryResults>? qrResult = null;
    try {
      qrResult = await this._promClient.QueryRangeAsync(
        query, start, end, step: TimeSpan.FromSeconds(1), timeout: null, cancellationToken
      );
    } catch (HttpRequestException e) {
      this.Logger.LogError(
        e,
        "HTTP error querying Prometheus ({StatusCode}): {Message}",
        e.StatusCode,
        e.Message
      );
    } catch (TaskCanceledException) {
      if (cancellationToken.IsCancellationRequested) {
        throw;
      } else {
        this.Logger.LogError("Prometheus query request timed out");
      }
    } catch (InvalidOperationException e) {
      this.Logger.LogError(e, "Unexpected error querying Prometheus: {Message}", e.Message);
    }

    return qrResult;
  }
}