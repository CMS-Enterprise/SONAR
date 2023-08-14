using System;
using System.Threading;
using System.Threading.Tasks;
using PrometheusQuerySdk.Models;

namespace Cms.BatCave.Sonar.Loki;

public interface ILokiClient {
  Task<ResponseEnvelope<QueryResults>> QueryAsync(
    String query,
    Int32 limit,
    DateTime timestamp,
    Direction direction,
    CancellationToken cancellationToken = default);

  Task<ResponseEnvelope<QueryResults>> QueryRangeAsync(
    String query,
    DateTime start,
    DateTime end,
    Int32? limit = default,
    TimeSpan? step = default,
    Direction? direction = default,
    CancellationToken cancellationToken = default);
}
