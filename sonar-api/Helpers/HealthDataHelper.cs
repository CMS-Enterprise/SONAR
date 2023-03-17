using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Prometheus;
using Cms.BatCave.Sonar.Query;
using Cms.BatCave.Sonar.System;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Helpers;

public class HealthDataHelper {
  public const String ServiceHealthAggregateMetricName = "sonar_service_status";
  public const String ServiceHealthCheckMetricName = "sonar_service_health_check_status";

  // When querying for the services current health, the maximum age of data
  // points from Prometheus to consider. If there are no data points newer than
  // this the services health status will be unknown.
  private static readonly TimeSpan MaximumServiceHealthAge = TimeSpan.FromHours(1);

  private readonly CacheHelper _cacheHelper;
  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly IPrometheusClient _prometheusClient;
  private readonly ILogger<HealthDataHelper> _logger;

  public HealthDataHelper(
    CacheHelper cacheHelper,
    ServiceDataHelper serviceDataHelper,
    IPrometheusClient prometheusClient,
    ILogger<HealthDataHelper> logger) {
    this._cacheHelper = cacheHelper;
    this._serviceDataHelper = serviceDataHelper;
    this._prometheusClient = prometheusClient;
    this._logger = logger;
  }

    public async Task<Dictionary<String, (DateTime Timestamp, HealthStatus Status)>> GetServiceStatuses(
    String environment,
    String tenant,
    CancellationToken cancellationToken) {
    Dictionary<String, (DateTime Timestamp, HealthStatus Status)> serviceStatuses;
    try {
      serviceStatuses = await this.GetLatestValuePrometheusQuery(
        this._prometheusClient,
        $"{HealthDataHelper.ServiceHealthAggregateMetricName}{{environment=\"{environment}\", tenant=\"{tenant}\"}}",
        processResult: results => {
          // StateSet metrics are split into separate metric per-state
          // This code groups all the metrics for a given service and then determines which state is currently set.
          var metricByService =
            results.Result
              .Where(metric => metric.Values != null)
              .Select(metric => (metric.Labels, metric.Values!.OrderByDescending(v => v.Timestamp).FirstOrDefault()))
              .ToLookup(
                keySelector: metric =>
                  metric.Labels.TryGetValue(MetricLabelKeys.Service, out var serviceName) ? serviceName : null,
                StringComparer.OrdinalIgnoreCase
              );

          var healthMapping =
            new Dictionary<String, (DateTime Timestamp, HealthStatus Status)>(StringComparer.OrdinalIgnoreCase);
          foreach (var group in metricByService) {
            if (group.Key == null) {
              // This group contains metrics that are missing the service name label. These should not exist.
              continue;
            }

            var healthStatus =
              this.GetEffectiveHealthStatus(HealthDataHelper.ServiceHealthAggregateMetricName, group);

            if (healthStatus.HasValue) {
              healthMapping[group.Key] = healthStatus.Value;
            }
          }

          return healthMapping;
        },
        cancellationToken
      );
    } catch (Exception e) {
      this._logger.LogError(
        message: $"Error querying Prometheus: {e.Message}. Using cached values."
      );
      serviceStatuses = await this._cacheHelper.FetchServiceCache(environment, tenant, cancellationToken);
    }

    return serviceStatuses;
  }

    private (String? Service, String? HealthCheck) GetHealthCheckKey(IImmutableDictionary<String, String> labels) {
      labels.TryGetValue(MetricLabelKeys.Service, out var serviceName);
      labels.TryGetValue(MetricLabelKeys.HealthCheck, out var checkName);

      return (serviceName, checkName);
    }

  public async Task<Dictionary<(String Service, String HealthCheck), (DateTime Timestamp, HealthStatus Status)>>
    GetHealthCheckStatus(
      String environment,
      String tenant,
      CancellationToken cancellationToken) {
    Dictionary<(String Service, String HealthCheck), (DateTime Timestamp, HealthStatus Status)> healthCheckStatus;
    try {
      healthCheckStatus =
        await this.GetLatestValuePrometheusQuery(
          this._prometheusClient,
          $"{HealthDataHelper.ServiceHealthCheckMetricName}{{environment=\"{environment}\", tenant=\"{tenant}\"}}",
          processResult: results => {
            // StateSet metrics are split into separate metric per-state
            // This code groups all the metrics for a given state.
            var metricByHealthCheck =
              results.Result
                .Where(metric => metric.Values != null)
                .Select(metric => (metric.Labels, metric.Values!.OrderByDescending(v => v.Timestamp).FirstOrDefault()))
                .ToLookup(
                  keySelector: metric => this.GetHealthCheckKey(metric.Labels),
                  TupleComparer.From(StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase)
                );

            var healthMapping =
              new Dictionary<(String Service, String HealthCheck), (DateTime Timestamp, HealthStatus Status)>(
                TupleComparer.From<String, String>(StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase)
              );
            foreach (var group in metricByHealthCheck) {
              if ((group.Key.Item1 == null) || (group.Key.Item2 == null)) {
                // This group contains metrics that are missing either the service name label or the
                // metric name label. These should not exist.
                continue;
              }

              var healthStatus =
                this.GetEffectiveHealthStatus(HealthDataHelper.ServiceHealthCheckMetricName, group);

              if (healthStatus.HasValue) {
                healthMapping[(group.Key.Item1, group.Key.Item2)] = healthStatus.Value;
              }
            }

            return healthMapping;
          },
          cancellationToken
        );
    } catch (Exception e) {
      this._logger.LogError(
        message: $"Error querying Prometheus: {e.Message}. Using cached values."
      );
      healthCheckStatus = await this._cacheHelper.FetchHealthCheckCache(environment, tenant, cancellationToken);
    }

    return healthCheckStatus;
  }

  public async Task<ILookup<Guid, HealthCheck>> GetHealthChecksByService(
    ImmutableDictionary<Guid, Service> services,
    CancellationToken cancellationToken) {
    var serviceHealthChecks =
      await this._serviceDataHelper.FetchExistingHealthChecks(services.Keys, cancellationToken);

    var healthChecksByService = serviceHealthChecks.ToLookup(hc => hc.ServiceId);

    return healthChecksByService;
  }

  // Recursively creates a ServiceHierarchyHealth for the specified service.
  //
  // For each service there are potentially two components that determine its
  // aggregate health status:
  // 1) The health recorded in Prometheus which will be based on the health
  //    checks defined for that service.
  // 2) The aggregate health status of each of its child services.
  //
  // The aggregate health status of a service is always the "worst" of the
  // health check and child service statuses. If *any* health information is
  // missing, then the aggregate health status will be null.
  public ServiceHierarchyHealth ToServiceHealth(
    Service service,
    ImmutableDictionary<Guid, Service> services,
    Dictionary<String, (DateTime Timestamp, HealthStatus Status)> serviceStatuses,
    ILookup<Guid, Guid> serviceChildIdsLookup,
    ILookup<Guid, HealthCheck> healthChecksByService,
    Dictionary<(String Service, String HealthCheck), (DateTime Timestamp, HealthStatus Status)> healthCheckStatus) {
    // The service will have its own status if it has health checks that have recorded status.
    var hasServiceStatus = serviceStatuses.TryGetValue(service.Name, out var serviceStatus);

    var children =
      serviceChildIdsLookup[service.Id].Select(sid => ToServiceHealth(
            services[sid], services, serviceStatuses, serviceChildIdsLookup, healthChecksByService, healthCheckStatus
          )
        )
        .ToImmutableHashSet();

    HealthStatus? aggregateStatus = hasServiceStatus ? serviceStatus.Status : null;
    DateTime? statusTimestamp = hasServiceStatus ? serviceStatus.Timestamp : null;

    var healthChecks = healthChecksByService[service.Id].ToImmutableList();
    // Only aggregate the status of the children if the current service either
    // has a recorded status based on its health checks, or has no health
    // checks. (If there is missing information for the health check based
    // service then there is no point considering the status of the children).
    if (hasServiceStatus || !healthChecks.Any()) {
      // If this service has a status
      foreach (var child in children) {
        if (child.AggregateStatus.HasValue) {
          if ((aggregateStatus == null) ||
            (aggregateStatus.Value < child.AggregateStatus.Value) ||
            (child.AggregateStatus == HealthStatus.Unknown)) {

            aggregateStatus = child.AggregateStatus;
          }

          // The child service should always have a timestamp here, but double check anyway
          if (child.Timestamp.HasValue &&
            (!statusTimestamp.HasValue || (child.Timestamp.Value < statusTimestamp.Value))) {

            // The status timestamp should always be the *oldest* of the
            // recorded status data points.
            statusTimestamp = child.Timestamp.Value;
          }
        } else {
          // One of the child services has an "unknown" status, that means
          // this service will also have the "unknown" status.
          aggregateStatus = null;
          statusTimestamp = null;
          break;
        }
      }
    }

    return new ServiceHierarchyHealth(
      service.Name,
      service.DisplayName,
      service.Description,
      service.Url,
      statusTimestamp,
      aggregateStatus,
      healthChecks.ToImmutableDictionary(
        hc => hc.Name,
        hc => healthCheckStatus.TryGetValue((service.Name, hc.Name), out var checkStatus) ?
          checkStatus :
          ((DateTime, HealthStatus)?)null
      ),
      children
    );
  }

  private async Task<T> GetLatestValuePrometheusQuery<T>(
    IPrometheusClient prometheusClient,
    String promQuery,
    Func<QueryResults, T> processResult,
    CancellationToken cancellationToken) {

    var response = await prometheusClient.QueryAsync(
      // metric{tag="value"}[time_window]
      $"{promQuery}[{PrometheusClient.ToPrometheusDuration(HealthDataHelper.MaximumServiceHealthAge)}]",
      DateTime.UtcNow,
      cancellationToken: cancellationToken
    );

    if (response.Status != ResponseStatus.Success) {
      this._logger.LogError(
        message: "Unexpected error querying service health status from Prometheus ({ErrorType}): {ErrorMessage}",
        response.ErrorType,
        response.Error
      );
      throw new InternalServerErrorException(
        errorType: "PrometheusApiError",
        message: "Error querying service health status."
      );
    }

    if (response.Data == null) {
      this._logger.LogError(
        message: "Prometheus unexpectedly returned null data for query {Query}",
        promQuery
      );
      throw new InternalServerErrorException(
        errorType: "PrometheusApiError",
        message: "Error querying service health status."
      );
    }

    return processResult(response.Data);
  }

  private (DateTime timestamp, HealthStatus status)? GetEffectiveHealthStatus(
    String metricName,
    IEnumerable<(IImmutableDictionary<String, String> Labels, (Decimal Timestamp, String Flag) Value)> group) {

    (Decimal Timestamp, HealthStatus Status)? current = null;
    foreach (var entry in group) {
      // Filter to only the metrics that have the flag set, and ignore any metrics then do not have
      // a valid HealthStatus label.
      if ((entry.Value.Flag == "1") &&
        entry.Labels.TryGetValue(metricName, out var statusStr) &&
        Enum.TryParse<HealthStatus>(statusStr, out var status)) {

        if (current == null) {
          current = (entry.Value.Timestamp, status);
        } else if (entry.Value.Timestamp > current.Value.Timestamp) {
          // If there are multiple statuses with the flag set, use the newest one.
          current = (entry.Value.Timestamp, status);
        } else if ((entry.Value.Timestamp == current.Value.Timestamp) && (status > current.Value.Status)) {
          // If there are multiple statuses with the flag set and the same timestamp, use the most severe
          current = (entry.Value.Timestamp, status);
        }
      }
    }

    return current.HasValue ?
      (DateTime.UnixEpoch.AddMilliseconds((Int64)(current.Value.Timestamp * 1000)), current.Value.Status) :
      null;
  }

  public class MetricLabelKeys {
    public const String Environment = "environment";
    public const String Tenant = "tenant";
    public const String Service = "service";
    public const String HealthCheck = "check";
  }
}
