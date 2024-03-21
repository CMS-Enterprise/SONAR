using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Prometheus;

public interface IPrometheusService {

  // How often Prometheus is configured to scape Alertmanager metrics.
  public static TimeSpan AlertmanagerScrapeInterval => TimeSpan.FromMinutes(3);
  // How often Prometheus is configured to evaluate rules.
  public static TimeSpan RuleEvaluationInterval => TimeSpan.FromMinutes(1);

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

  /// <summary>
  /// Get single metric time series for a specific health check under the given environment, tenant, and service
  /// recorded to Prometheus between a specified start and end DateTime; otherwise get health check metric data
  /// samples from within the last 10 minutes UTC.
  /// </summary>
  /// <param name="environment">The environment to get timestamps for.</param>
  /// <param name="tenant">The tenant to get timestamps for.</param>
  /// <param name="service">The service to get timestamps for.</param>
  /// <param name="healthCheck">The health check to get timestamps for.</param>
  /// <param name="start">The starting DateTime of the results.</param>
  /// <param name="end">The desired ending DateTime of the results.</param>
  /// <param name="cancellationToken">The cancellation token for the async operation.</param>
  /// <returns>
  /// Timestamps and metric values of the health check between the specified starting and ending date time OR from
  /// within the last 10 minutes UTC; can be empty if there are no samples for the health check under the
  /// environment, tenant, service in the time range.
  /// </returns>
  /// <exception cref="BadRequestException">If Prometheus returns a 4xx status.</exception>
  /// <exception cref="InternalServerErrorException">If there's any other problem calling Prometheus.</exception>
  Task<IImmutableList<(DateTime, Double)>> QuerySpecificHealthCheckDataAsync(
    String environment,
    String tenant,
    String service,
    String healthCheck,
    DateTime start,
    DateTime end,
    CancellationToken cancellationToken);

  /// <summary>
  /// Get the current status of Alertmanager metrics scraping. This is a health check to ensure that Alertmanager
  /// metrics are up-to-date. We query Prometheus for the <c>alertmanager_build_info</c> metric and validate that it
  /// has been sampled recently.
  /// </summary>
  /// <returns>
  /// <see cref="HealthStatus.Online"/> if the build info metric is present and was updated recently.
  /// <see cref="HealthStatus.Degraded"/> if the build info metric is present, but hasn't been updated recently.
  /// <see cref="HealthStatus.Offline"/> if data is missing for the build info metric.
  /// <see cref="HealthStatus.Unknown"/> if there was any unexpected error querying the metric data.
  /// </returns>
  Task<HealthStatus> GetAlertmanagerScrapeStatusAsync(CancellationToken cancellationToken);

  /// <summary>
  /// Get the current status of Alertmanager notification delivery for a given integration. This is a health check
  /// for whether there have been any recent notification request failures reported by Alertmanager for the given
  /// integration according to the <c>alertmanager_notification_requests_failed_total</c> metric.
  /// </summary>
  /// <param name="integration">The notification integration type to check metrics for, e.g. "email" or "slack".</param>
  /// <param name="cancellationToken">The cancellation token for the async operation.</param>
  /// <returns>
  /// <see cref="HealthStatus.Online"/> if no notification request failures for the given integration have been reported
  /// recently.
  /// <see cref="HealthStatus.Degraded"/> if any notification request failures for the given integration have been
  /// reported recently.
  /// <see cref="HealthStatus.Unknown"/> if there was any unexpected error querying the metric data.
  /// </returns>
  Task<HealthStatus> GetAlertmanagerNotificationsStatusAsync(String integration, CancellationToken cancellationToken);
}
