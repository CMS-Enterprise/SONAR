using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrometheusQuerySdk.Models;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Agent.HealthChecks.Metrics;

/// <summary>
///   A base class for implementing <see cref="IMetricQueryRunner" /> for any query execution backend
///   that can return results using the <see cref="ResponseEnvelope{QueryResults}" /> data model.
/// </summary>
/// <remarks>
///   <para>
///     The <see cref="ResponseEnvelope{QueryResults}" /> returned by the
///     <see cref="GetQueryResultsAsync" /> method is expected to conform to the following constraints:
///   </para>
///   <ul>
///     <li>
///       The result must have a non-null <see cref="ResponseEnvelope{QueryResults}.Data" />
///       property.
///     </li>
///     <li>
///       The <see cref="QueryResults.Result" /> property of the <see cref="QueryResults" /> must
///       contain only a single entry.
///     </li>
///     <li>
///       The <see cref="ResultData.Values" /> property of that <see cref="ResultData" /> must be
///       non-null and contain one or more entries.
///     </li>
///     <li>
///       All entries in the <see cref="ResultData.Values" /> list must have a
///       <see cref="Tuple{Decimal,String}.Item2" /> value that can be parsed as a
///       <seealso cref="Decimal" />.
///     </li>
///   </ul>
///   <para>
///     For the first three of those constraints, if the constraint is met, the
///     <see cref="QueryRangeAsync" /> method will log a warning and return null. For the last
///     constraint, if one of the values cannot be parsed as a <see cref="Decimal" />, a
///     <see cref="FormatException" /> will be raised.
///   </para>
/// </remarks>
public abstract class MetricQueryRunnerBase : IMetricQueryRunner {
  protected ILogger Logger { get; }

  protected MetricQueryRunnerBase(ILogger logger) {
    this.Logger = logger;
  }

  public async Task<IImmutableList<(DateTime Timestamp, Decimal Value)>?> QueryRangeAsync(
    HealthCheckIdentifier healthCheck,
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
      this.Logger.LogWarning(message: "Returned nothing for health check: {HealthCheck}", healthCheck);
      return null;
    } else if (qrResult.Data.Result.Count > 1) {
      // Bad config, multiple time series returned
      this.Logger.LogWarning(
        message: "Invalid configuration, multiple time series returned for health check: {HealthCheck}",
        healthCheck);
      return null;
    } else if ((qrResult.Data.Result.Count == 0) ||
      (qrResult.Data.Result[0].Values == null) ||
      (qrResult.Data.Result[0].Values!.Count == 0)) {
      // No samples
      this.Logger.LogWarning(message: "Returned no samples for health check: {HealthCheck}", healthCheck);
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

  /// <summary>
  ///   When overriden in a derived class, executes the specified <paramref name="query" /> and returns
  ///   whatever query results are returned.
  /// </summary>
  protected abstract Task<ResponseEnvelope<QueryResults>?> GetQueryResultsAsync(
    String query,
    DateTime start,
    DateTime end,
    CancellationToken cancellationToken);
}
