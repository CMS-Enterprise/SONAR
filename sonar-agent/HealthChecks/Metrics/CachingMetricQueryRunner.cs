using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cms.BatCave.Sonar.Agent.HealthChecks.Metrics;

public class CachingMetricQueryRunner : IMetricQueryRunner {
  private readonly ConcurrentDictionary<String, IImmutableList<(DateTime Timestamp, Decimal Value)>> _cache = new();
  private readonly IMetricQueryRunner _innerQueryRunner;

  public CachingMetricQueryRunner(IMetricQueryRunner innerQueryRunner) {
    this._innerQueryRunner = innerQueryRunner;
  }

  public async Task<IImmutableList<(DateTime Timestamp, Decimal Value)>?> QueryRangeAsync(
    String healthCheckName,
    String expression,
    DateTime start,
    DateTime end,
    CancellationToken cancellationToken) {

    var cacheBasedStart = this.GetStartDate(healthCheckName, start);

    var samples = await this._innerQueryRunner.QueryRangeAsync(
      healthCheckName,
      expression,
      cacheBasedStart,
      end,
      cancellationToken
    );

    return samples != null ? this.UpdateCache(healthCheckName, samples, start) : null;
  }

  private DateTime GetStartDate(
    String name,
    DateTime start) {

    // If no cached values, use the original start date
    // Else, cached values exist, calculate start date from last cached value.
    if (!this._cache.TryGetValue(name, out var cachedData) || (cachedData.Count == 0)) {
      return start;
    } else {
      var lastCachedTimestamp = cachedData.Last().Timestamp;
      return lastCachedTimestamp < start ? start : lastCachedTimestamp;
    }
  }

  private IImmutableList<(DateTime Timestamp, Decimal Value)> UpdateCache(
    String name,
    IImmutableList<(DateTime Timestamp, Decimal Value)> newResults,
    DateTime start) {

    // If cache does not contain key, insert entire response envelope into dictionary.
    //  Else, cache contains service, truncate and concat.
    return this._cache.AddOrUpdate(
      name,
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
