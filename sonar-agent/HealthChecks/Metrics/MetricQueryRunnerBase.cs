using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Query;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Agent.HealthChecks.Metrics;

public abstract class MetricQueryRunnerBase : IMetricQueryRunner {
  protected ILogger Logger { get; }

  protected MetricQueryRunnerBase(ILogger logger) {
    this.Logger = logger;
  }

  public async Task<IImmutableList<(DateTime Timestamp, Decimal Value)>?> QueryRangeAsync(
    String healthCheckName,
    String expression,
    DateTime start,
    DateTime end,
    CancellationToken cancellationToken) {

    var qrResult = await this.GetQueryResultsAsync(
      expression, start, end, cancellationToken
    );

    if (qrResult == null) {
      // An error occurred executing the query
      return null;
    }

    // Error handling
    if (qrResult.Data == null) {
      // No data, bad request
      this.Logger.LogWarning(message: "Returned nothing for health check: {HealthCheck}", healthCheckName);
      return null;
    } else if (qrResult.Data.Result.Count > 1) {
      // Bad config, multiple time series returned
      this.Logger.LogWarning(
        message: "Invalid configuration, multiple time series returned for health check: {HealthCheck}",
        healthCheckName);
      return null;
    } else if ((qrResult.Data.Result.Count == 0) ||
      (qrResult.Data.Result[0].Values == null) ||
      (qrResult.Data.Result[0].Values!.Count == 0)) {
      // No samples
      this.Logger.LogWarning(message: "Returned no samples for health check: {HealthCheck}", healthCheckName);
      return null;
    } else {
      // Successfully obtained samples for query, Convert from (Decimal, String) to (DateTime, Decimal)
      return qrResult.Data.Result[0].Values
        !.Select(sample => (
          Timestamp: DateTime.UnixEpoch.AddSeconds((Double)sample.Timestamp),
          Value: Decimal.Parse(sample.Value)
        ))
        .ToImmutableList();
    }
  }

  protected abstract Task<ResponseEnvelope<QueryResults>?> GetQueryResultsAsync(
    String query,
    DateTime start,
    DateTime end,
    CancellationToken cancellationToken);
}
