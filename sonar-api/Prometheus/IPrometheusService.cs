using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Prometheus;

public interface IPrometheusService {
  /// <summary>
  /// Write the given raw <see cref="ServiceHealthData"/> time series samples for the given environment,
  /// tenant, and service to Prometheus using P8s' remote write API. Out-of-order samples and samples older
  /// than one hour (stale) are discarded (not written).
  /// </summary>
  /// <param name="environment">The environment the data belongs to.</param>
  /// <param name="tenant">The tenant the data belongs to.</param>
  /// <param name="service">The service the data belongs to.</param>
  /// <param name="data">The health check data to be written.</param>
  /// <param name="cancellationToken">The cancellation token for the async operation.</param>
  /// <returns>
  /// A new <see cref="ServiceHealthData"/> containing the samples that were actually written;
  /// can be empty if no samples were written because they all failed the filtering criteria.
  /// </returns>
  /// <exception cref="BadRequestException">If Prometheus returns a 4xx status.</exception>
  /// <exception cref="InternalServerErrorException">If there's any other problem calling Prometheus.</exception>
  Task<ServiceHealthData> WriteHealthCheckDataAsync(
    String environment,
    String tenant,
    String service,
    ServiceHealthData data,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Get the timestamps of the most recent raw health check metric data samples recorded to Prometheus that occurred
  /// within the given time range (where the range ends "right now") for all health checks of the given environment,
  /// tenant, and service.
  /// </summary>
  /// <param name="environment">The environment to get timestamps for.</param>
  /// <param name="tenant">The tenant to get timestamps for.</param>
  /// <param name="service">The service to get timestamps for.</param>
  /// <param name="range">The time range (ending at the current moment) to limit results to.</param>
  /// <param name="cancellationToken">The cancellation token for the async operation.</param>
  /// <returns>
  /// A dictionary mapping each health check name for the given environment, tenant, service to the timestamp of
  /// it's most recent raw data sample; can be empty if there are no samples for the environment, tenant, service
  /// in the time range. Only health checks with raw data present in the time range are included in the result.
  /// </returns>
  /// <exception cref="BadRequestException">If Prometheus returns a 4xx status.</exception>
  /// <exception cref="InternalServerErrorException">If there's any other problem calling Prometheus.</exception>
  Task<IImmutableDictionary<String, DateTime>> QueryLatestHealthCheckDataTimestampsAsync(
    String environment,
    String tenant,
    String service,
    TimeSpan range,
    CancellationToken cancellationToken = default);
}
