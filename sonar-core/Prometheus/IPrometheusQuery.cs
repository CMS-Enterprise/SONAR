using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cms.BatCave.Sonar.Prometheus;

public interface IPrometheusClient {
  Task<ResponseEnvelope<QueryResults>> QueryAsync(
    String query,
    DateTime timestamp,
    TimeSpan timeout,
    CancellationToken cancellationToken = default);

  Task<ResponseEnvelope<QueryResults>> QueryPostAsync(
    QueryPostRequest request,
    CancellationToken cancellationToken = default);

  Task<ResponseEnvelope<QueryResults>> QueryRangeAsync(
    String query,
    DateTime start,
    DateTime end,
    TimeSpan step,
    TimeSpan timeout,
    CancellationToken cancellationToken = default);

  Task<ResponseEnvelope<QueryResults>> QueryRangePostAsync(
    QueryRangePostRequest request,
    CancellationToken cancellationToken = default);
}
