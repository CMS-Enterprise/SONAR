using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using TimeSeriesValue = System.Tuple<System.DateTime, System.Decimal>;

namespace Cms.BatCave.Sonar.Agent.HealthChecks.Metrics;

/// <summary>
///   An interface for implementing a querying of a time series database.
/// </summary>
/// <remarks>
///   The implementations of all methods on this interface should be thread safe.
/// </remarks>
public interface IMetricQueryRunner {
  /// <summary>
  ///   Executes a query expression in a metric data source and returns the resulting single metric
  ///   time-series as an <see cref="IImmutableList{TimeSeriesValue}" />
  /// </summary>
  /// <param name="healthCheckName">
  ///   The name of the health check this query is for. This argument is provided mainly for logging
  ///   purposes. It will typically be a combination of the service name and health
  ///   check name (i.e. "my-service/my-health-check"), but this is not a contractual guarantee.
  /// </param>
  /// <param name="expression">
  ///   The metric query expression.
  /// </param>
  /// <param name="start">
  ///   The start date for the time window to include in the results.
  /// </param>
  /// <param name="end">
  ///   The end date for the time window to include in the results.
  /// </param>
  /// <param name="cancellationToken">
  ///   A cancellation token that will signal a request to cancel query execution.
  /// </param>
  /// <returns>
  ///   An immutable list of tuples containing an ordered (from oldest to newest) series of DateTime
  ///   timestamps and Decimal values.
  /// </returns>
  Task<IImmutableList<(DateTime Timestamp, Decimal Value)>?> QueryRangeAsync(
    String healthCheckName,
    String expression,
    DateTime start,
    DateTime end,
    CancellationToken cancellationToken);
}
