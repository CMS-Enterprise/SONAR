using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Cms.BatCave.Sonar.Agent.HealthChecks.Metrics;

public interface IMetricQueryRunner {
  Task<IImmutableList<(DateTime Timestamp, Decimal Value)>?> QueryRangeAsync(
    String name,
    String expression,
    DateTime start,
    DateTime end,
    CancellationToken cancellationToken);
}
