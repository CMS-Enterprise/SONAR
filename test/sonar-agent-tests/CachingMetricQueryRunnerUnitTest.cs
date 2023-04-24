using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.HealthChecks;
using Cms.BatCave.Sonar.Agent.HealthChecks.Metrics;
using Cms.BatCave.Sonar.Extensions;
using Xunit;
using TimeSeries = System.Collections.Immutable.IImmutableList<(System.DateTime Timestamp, System.Decimal Value)>;

namespace Cms.BatCave.Sonar.Agent.Tests;

public class CachingMetricQueryRunnerUnitTest {
  // No cache, dates are un-changed, results returned as is
  [Fact]
  public async Task NewQuery_DatesUnchanged() {
    var hc = $"{Guid.NewGuid()}";
    var hcId = new HealthCheckIdentifier("env", "ten", "svc", hc);
    var expr = $"{Guid.NewGuid()}";
    var end = DateTime.UtcNow;
    var start = end.Subtract(TimeSpan.FromMinutes(10));
    var data = MockMetricQueryRunner.GenerateData(start, end).ToImmutableList();

    var mockRunner = MockMetricQueryRunner.CreateFixedReturnMock(data);

    var cache = new CachingMetricQueryRunner(mockRunner.Object);

    var result = await cache.QueryRangeAsync(hcId, expr, start, end, CancellationToken.None);

    // Verify the inner IMetricQueryRunner is called with the original start and end date
    mockRunner.Verify(qr => qr.QueryRangeAsync(hcId, expr, start, end, CancellationToken.None));

    // The data should be returned as is
    Assert.NotNull(result);
    Assert.Equal(data, result);
  }

  // Cached data, overlapping time windows.
  [Fact]
  public async Task TwoQueries_OverlappingTimeWindows_CacheDataUsed() {
    var hc = $"{Guid.NewGuid()}";
    var hcId = new HealthCheckIdentifier("env", "ten", "svc", hc);
    var expr = $"{Guid.NewGuid()}";
    var initialEnd = DateTime.UtcNow;
    var initialStart = initialEnd.Subtract(TimeSpan.FromMinutes(10));

    var mockRunner = MockMetricQueryRunner.CreateFunctionalMock();

    var cache = new CachingMetricQueryRunner(mockRunner.Object);

    var initialData = await cache.QueryRangeAsync(hcId, expr, initialStart, initialEnd, CancellationToken.None);
    // Disregard the initial invocations
    mockRunner.Invocations.Clear();

    var subsequentStart = initialStart.AddSeconds(30);
    var subsequentEnd = initialEnd.AddSeconds(30);

    var subsequentData = await cache.QueryRangeAsync(hcId, expr, subsequentStart, subsequentEnd, CancellationToken.None);

    // Verify the inner IMetricQueryRunner is called with a modified start date
    mockRunner.Verify(qr => qr.QueryRangeAsync(hcId, expr, initialEnd, subsequentEnd, CancellationToken.None));

    Assert.NotNull(initialData);
    Assert.NotNull(subsequentData);
    var overlap = initialData.SkipWhile(v => v.Timestamp < subsequentStart).ToList();
    Assert.True(subsequentData.StartsWith(overlap));
    Assert.True(subsequentData.Count > overlap.Count);
  }

  // Cached data, non-overlapping time windows
  // Cached data, overlapping time windows.
  [Fact]
  public async Task TwoQueries_NonOverlappingTimeWindows_CacheDataNotUsed() {
    var hc = $"{Guid.NewGuid()}";
    var hcId = new HealthCheckIdentifier("env", "ten", "svc", hc);
    var expr = $"{Guid.NewGuid()}";
    var initialEnd = DateTime.UtcNow;
    var initialStart = initialEnd.Subtract(TimeSpan.FromMinutes(10));

    var mockRunner = MockMetricQueryRunner.CreateFunctionalMock();

    var cache = new CachingMetricQueryRunner(mockRunner.Object);

    var initialData = await cache.QueryRangeAsync(hcId, expr, initialStart, initialEnd, CancellationToken.None);
    // Disregard the initial invocations
    mockRunner.Invocations.Clear();

    var subsequentStart = initialEnd.AddSeconds(30);
    var subsequentEnd = subsequentStart.AddMinutes(10);

    var subsequentData = await cache.QueryRangeAsync(hcId, expr, subsequentStart, subsequentEnd, CancellationToken.None);

    // Verify the inner IMetricQueryRunner is called with a modified start date
    mockRunner.Verify(qr => qr.QueryRangeAsync(hcId, expr, subsequentStart, subsequentEnd, CancellationToken.None));

    Assert.NotNull(initialData);
    Assert.NotNull(subsequentData);
    Assert.True(subsequentData.First().Timestamp > initialData.Last().Timestamp);
  }

}
