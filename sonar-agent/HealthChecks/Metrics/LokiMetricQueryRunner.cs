using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Loki;
using Cms.BatCave.Sonar.Query;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Agent.HealthChecks.Metrics;

public class LokiMetricQueryRunner : MetricQueryRunnerBase {
  private readonly ILokiClient _lokiClient;

  public LokiMetricQueryRunner(
    ILokiClient lokiClient,
    ILogger<LokiMetricQueryRunner> logger) :
    base(logger) {
    this._lokiClient = lokiClient;
  }

  protected override async Task<ResponseEnvelope<QueryResults>?> GetQueryResultsAsync(
    String query,
    DateTime start,
    DateTime end,
    CancellationToken cancellationToken) {

    ResponseEnvelope<QueryResults>? qrResult = null;
    try {
      qrResult = await this._lokiClient.QueryRangeAsync(
        query, start, end, direction: Direction.Forward, cancellationToken: cancellationToken
      );
    } catch (HttpRequestException e) {
      this.Logger.LogError(e, "HTTP error querying Loki: {Message}", e.Message);
    } catch (TaskCanceledException) {
      if (cancellationToken.IsCancellationRequested) {
        throw;
      } else {
        this.Logger.LogError("Loki query request timed out");
      }
    } catch (InvalidOperationException e) {
      this.Logger.LogError(e, "Unexpected error querying Loki: {Message}", e.Message);
    }

    return qrResult;
  }
}
