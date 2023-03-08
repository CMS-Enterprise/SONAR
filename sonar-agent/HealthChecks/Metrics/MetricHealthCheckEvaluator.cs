using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Agent.HealthChecks.Metrics;

/// <summary>
///   An implementation of <see cref="IHealthCheckEvaluator{MetricHealthCheckDefinition}" /> for any
///   implementation of <see cref="IMetricQueryRunner" />.
/// </summary>
public class MetricHealthCheckEvaluator : IHealthCheckEvaluator<MetricHealthCheckDefinition> {

  private readonly IMetricQueryRunner _queryRunner;
  private readonly ILogger<MetricHealthCheckEvaluator> _logger;

  public MetricHealthCheckEvaluator(
    IMetricQueryRunner queryRunner,
    ILogger<MetricHealthCheckEvaluator> logger) {

    this._queryRunner = queryRunner;
    this._logger = logger;
  }

  public async Task<HealthStatus> EvaluateHealthCheckAsync(
    String name,
    MetricHealthCheckDefinition definition,
    CancellationToken cancellationToken = default) {

    // Compute start and end date based on cache
    var end = DateTime.UtcNow;

    // Get metric samples
    var qrResult = await this._queryRunner.QueryRangeAsync(
      name,
      definition.Expression,
      end.Subtract(definition.Duration),
      end,
      cancellationToken
    );

    // Failed to get valid query results.
    return qrResult == null ?
      HealthStatus.Unknown :
      this.ProcessMetricSamples(name, definition.Conditions, qrResult);
  }

  /// <summary>
  ///   Given a list of timestamp/value pairs (<paramref name="samples" />), evaluates a set of
  ///   <paramref name="conditions" /> against each sample to determine the resulting
  ///   <see cref="HealthStatus" />.
  /// </summary>
  /// <param name="name">
  ///   The name of the health check being evaluated. Used only for logging.
  /// </param>
  /// <param name="conditions">
  ///   The list of conditions being evaluated for the health check.
  /// </param>
  /// <param name="samples">
  ///   The time series the conditions are being evaluated against.
  /// </param>
  private HealthStatus ProcessMetricSamples(
    String name,
    IImmutableList<MetricHealthCondition> conditions,
    IImmutableList<(DateTime Timestamp, Decimal Value)> samples) {

    // Error handling
    var currCheck = HealthStatus.Online;
    if (samples.Count == 0) {
      // No samples
      this._logger.LogWarning(message: "Returned no samples for health check: {HealthCheck}", name);
      currCheck = HealthStatus.Unknown;
    } else {
      foreach (var condition in conditions) {
        // Determine which comparison to execute
        // Evaluate all PromQL samples
        var evaluation = EvaluateSamples(samples, condition.Operator, condition.Threshold);
        // If evaluation is true, set the current check to the condition's status
        // and output to Stdout
        if (evaluation) {
          currCheck = condition.Status;
          break;
        }
      }
    }

    return currCheck;
  }

  /// <summary>
  ///   Applies a condition to all of the samples in a time series.
  /// </summary>
  /// <remarks>
  ///   If the expression <c>time_series_value operator threshold</c> is <c>true</c> for all values in
  ///   the time series than this function returns true. For example, if the operator were
  ///   <see cref="HealthOperator.GreaterThan" />, the threshold was <c>5</c>, and the values were
  ///   <c>[7, 13, 11]</c> then the result would be <c>true</c>. However, if the values were
  ///   <c>[7, 3, 11]</c> the result would be <c>false</c> because <c>3</c> is not greater than the
  ///   threshold value.
  /// </remarks>
  /// <exception cref="ArgumentException">The specified <see cref="HealthOperator" /> value is not valid.</exception>
  private static Boolean EvaluateSamples(
    IImmutableList<(DateTime Timestamp, Decimal Value)> values,
    HealthOperator op,
    Decimal threshold) {

    // delegate functions for comparison
    Func<Decimal, Decimal, Boolean> comparison = op switch {
      HealthOperator.Equal => (x, y) => x == y,
      HealthOperator.NotEqual => (x, y) => x != y,
      HealthOperator.GreaterThan => (x, y) => x > y,
      HealthOperator.GreaterThanOrEqual => (x, y) => x >= y,
      HealthOperator.LessThan => (x, y) => x < y,
      HealthOperator.LessThanOrEqual => (x, y) => x <= y,
      _ => throw new ArgumentException("Invalid comparison operator.")
    };

    // Iterate through list, if all meet condition, return true, else return false if ANY don't meet condition
    return values.All(val => comparison(val.Value, threshold));
  }
}
