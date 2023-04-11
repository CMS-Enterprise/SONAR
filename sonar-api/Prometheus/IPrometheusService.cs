using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Prometheus;

public interface IPrometheusService {
  Task WriteHealthCheckDataAsync(
    String environment,
    String tenant,
    String service,
    ServiceHealthData data,
    CancellationToken cancellationToken = default);

  Task<IImmutableDictionary<String, DateTime>> QueryLatestHealthCheckDataTimestampsAsync(
    String environment,
    String tenant,
    String service,
    TimeSpan range,
    CancellationToken cancellationToken = default);
}
