using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TimeSeries = System.Collections.Immutable.IImmutableList<(System.DateTime Timestamp, System.Decimal Value)>;

namespace Cms.BatCave.Sonar.Agent.HealthChecks.Metrics;

/// <summary>
///   An implementation of <see cref="IMetricQueryRunner" /> that adds caching capability.
/// </summary>
/// <remarks>
///   <para>
///     It is assumed that for a given health check, subsequent queries will always be in the future
///     relative to cached data, and only data for the most recently requested window of time will be
///     cached. This cache is designed around the notion that the query will be repeatedly executed
///     over a relatively large window of time (say 10 minutes), but that the start and end times for
///     that window would gradually progress forward (by say 30 second increments). So each time the
///     query is run, only the most recent 30 seconds of data really needs to be retrieved.
///   </para>
/// </remarks>
public class CachingMetricQueryRunner : IMetricQueryRunner {
  private readonly ConcurrentDictionary<(String Name, String Expression), TimeSeries> _cache = new();
  private readonly IMetricQueryRunner _innerQueryRunner;

  public CachingMetricQueryRunner(IMetricQueryRunner innerQueryRunner) {
    this._innerQueryRunner = innerQueryRunner;
  }

  public async Task<IImmutableList<(DateTime Timestamp, Decimal Value)>?> QueryRangeAsync(
    HealthCheckIdentifier healthCheck,
    String expression,
    DateTime start,
    DateTime end,
    CancellationToken cancellationToken) {

    var cacheBasedStart = this.GetStartDate(healthCheck, expression, start);

    var samples = await this._innerQueryRunner.QueryRangeAsync(
      healthCheck,
      expression,
      cacheBasedStart,
      end,
      cancellationToken
    );

    return samples != null ? this.UpdateCache(healthCheck.ToString(), expression, samples, start) : null;
  }

  private DateTime GetStartDate(
    HealthCheckIdentifier healthCheck,
    String expression,
    DateTime start) {

    // If no cached values, use the original start date
    // Else, cached values exist, calculate start date from last cached value.
    if (!this._cache.TryGetValue((healthCheck.ToString(), expression), out var cachedData) || (cachedData.Count == 0)) {
      return start;
    } else {
      var lastCachedTimestamp = cachedData.Last().Timestamp;
      return lastCachedTimestamp < start ? start : lastCachedTimestamp;
    }
  }

  private IImmutableList<(DateTime Timestamp, Decimal Value)> UpdateCache(
    String name,
    String expression,
    IImmutableList<(DateTime Timestamp, Decimal Value)> newResults,
    DateTime start) {

    // If cache does not contain key, insert entire response envelope into dictionary.
    //  Else, cache contains service, truncate and concat.

    return this._cache.AddOrUpdate(
      (name, expression),
      addValueFactory: (_) => newResults,
      updateValueFactory: (_, cachedValues) => {
        var newDataStart = newResults.First().Timestamp;

        // Skip old cached samples that came before the current time window
        var updatedCache = cachedValues.SkipWhile(val => val.Timestamp < start)
          // If there is overlapping data from the cache and the new results, drop the duplicate samples from the cache
          .TakeWhile(d => d.Timestamp < newDataStart)
          // Concatenate the cached data with the new results
          .Concat(newResults)
          .ToImmutableList();

        return updatedCache;
      }
    );
  }
}
