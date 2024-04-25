using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Maintenance;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.Logging;
using Prometheus;
using PrometheusQuerySdk;
using PrometheusQuerySdk.Models;

namespace Cms.BatCave.Sonar.Prometheus;

public class PrometheusService : IPrometheusService {

  private readonly ILogger<PrometheusService> _logger;
  private readonly IPrometheusRemoteProtocolClient _prometheusRemoteProtocolClient;
  private readonly IPrometheusClient _prometheusClient;

  public PrometheusService(
    ILogger<PrometheusService> logger,
    IPrometheusRemoteProtocolClient prometheusRemoteProtocolClient,
    IPrometheusClient prometheusClient) {

    this._logger = logger;
    this._prometheusRemoteProtocolClient = prometheusRemoteProtocolClient;
    this._prometheusClient = prometheusClient;
  }

  /// <inheritdoc/>
  public async Task<ServiceHealthData> WriteHealthCheckDataAsync(
    String environment,
    String tenant,
    String service,
    ServiceHealthData data,
    CancellationToken cancellationToken = default) {

    var freshHealthCheckSamples = await this.FilterStaleHealthCheckSamplesAsync(
      environment,
      tenant,
      service,
      data.HealthCheckSamples,
      cancellationToken);

    if (freshHealthCheckSamples.Count > 0) {
      List<Label> sharedTimeSeriesLabels = new() {
        new Label { Name = "__name__", Value = HealthDataHelper.ServiceHealthCheckDataMetricName },
        new Label { Name = HealthDataHelper.MetricLabelKeys.Environment, Value = environment },
        new Label { Name = HealthDataHelper.MetricLabelKeys.Tenant, Value = tenant },
        new Label { Name = HealthDataHelper.MetricLabelKeys.Service, Value = service }
      };

      var writeRequest = new WriteRequest {
        Metadata = {
          new MetricMetadata {
            Help = "The value returned by a service health check metric query.",
            Type = MetricMetadata.Types.MetricType.Gauge,
            MetricFamilyName = HealthDataHelper.ServiceHealthCheckDataMetricName
          }
        },
        Timeseries = {
          freshHealthCheckSamples.Select(kvp => new TimeSeries {
            Labels = {
              sharedTimeSeriesLabels,
              new Label { Name = HealthDataHelper.MetricLabelKeys.HealthCheck, Value = kvp.Key }
            },
            Samples = {
              kvp.Value.Select(sample => new Sample {
                Timestamp = (Int64)Math.Round(sample.Timestamp.MillisSinceUnixEpoch()),
                Value = sample.Value
              })
            }
          })
        }
      };

      await this._prometheusRemoteProtocolClient.WriteAsync(writeRequest, cancellationToken);
    }

    return new ServiceHealthData(freshHealthCheckSamples);
  }

  /// <summary>
  /// Returns a dictionary that maps health check name to a list of health check metric time series samples for that
  /// health check, where the samples are the entries from input that are less than an hour old and also newer than
  /// the latest recorded sample for the same health check. If none of the input samples for a given health check
  /// match the filter criteria, that health check key is omitted from the result.
  /// </summary>
  /// <param name="environment">The environment the samples belong to.</param>
  /// <param name="tenant">The tenant the samples belong to.</param>
  /// <param name="service">The service the samples belong to.</param>
  /// <param name="healthCheckSamples">The input health check samples.</param>
  /// <param name="cancellationToken">The cancellation token for the async operation.</param>
  /// <returns>
  /// A new immutable dictionary that maps health check name to health check metric time series samples that
  /// match the filter criteria.
  /// </returns>
  private async Task<IImmutableDictionary<String, IImmutableList<(DateTime Timestamp, Double Value)>>>
    FilterStaleHealthCheckSamplesAsync(
      String environment,
      String tenant,
      String service,
      IImmutableDictionary<String, IImmutableList<(DateTime Timestamp, Double Value)>> healthCheckSamples,
      CancellationToken cancellationToken) {

    var oneHour = TimeSpan.FromHours(1);
    var oneHourAgo = DateTime.UtcNow.Subtract(oneHour);
    var earliestSampleTimestamp = healthCheckSamples.Min(kvp => kvp.Value.Min(sample => sample.Timestamp));
    var earliestSampleTimeSpan = DateTime.UtcNow - earliestSampleTimestamp;
    // Pull no more than 1 hour's worth of data.
    var queryTimeSpan = TimeSpan.FromSeconds(Math.Min(earliestSampleTimeSpan.TotalSeconds, oneHour.TotalSeconds));

    var latestHealthCheckDataTimestamps = await this.QueryLatestHealthCheckDataTimestampsAsync(
      environment,
      tenant,
      service,
      queryTimeSpan,
      cancellationToken);

    Dictionary<String, IImmutableList<(DateTime Timestamp, Double Value)>> freshHealthCheckSamples = new();

    foreach (var (healthCheck, samples) in healthCheckSamples) {
      var freshSamples = samples.Where(sample => {
        if (sample.Timestamp < oneHourAgo) {
          this._logger.LogInformation(
            $"Dropping stale '{healthCheck}' sample ({sample.Timestamp}, {sample.Value})," +
            $" more than an hour old.");
          return false;
        }

        if (latestHealthCheckDataTimestamps.ContainsKey(healthCheck) &&
          (sample.Timestamp < latestHealthCheckDataTimestamps[healthCheck])) {
          this._logger.LogInformation(
            $"Dropping stale '{healthCheck}' sample ({sample.Timestamp}, {sample.Value})," +
            $" older than latest recorded sample.");
          return false;
        }

        return true;
      }).ToImmutableList();

      if (freshSamples.Count > 0) {
        freshHealthCheckSamples[healthCheck] = freshSamples;
      }
    }

    return freshHealthCheckSamples.ToImmutableDictionary();
  }

  /// <inheritdoc/>
  public async Task<IImmutableDictionary<String, DateTime>> QueryLatestHealthCheckDataTimestampsAsync(
    String environment,
    String tenant,
    String service,
    TimeSpan range,
    CancellationToken cancellationToken = default) {

    // Example query:
    // sonar_healthcheck_data{environment='foo',tenant='baz',service='my-root'}[1h]

    var query = String.Format(
      format: "{0}{{environment='{1}',tenant='{2}',service='{3}'}}[{4}]",
      HealthDataHelper.ServiceHealthCheckDataMetricName,
      environment,
      tenant,
      service,
      PrometheusClient.ToPrometheusDuration(range));

    this._logger.LogDebug("Querying latest health check data timestamps: {query}", query);

    var queryResponse =
      await this._prometheusClient.QueryAsync(query, DateTime.UtcNow, cancellationToken: cancellationToken);

    var latestServiceHealthMetricsTimestamps = queryResponse.Data?.Result.ToDictionary(
      keySelector: result => result.Labels[HealthDataHelper.MetricLabelKeys.HealthCheck],
      elementSelector: result => {
        var millisSinceUnixEpoch = Math.Round((result.Values?.Max(sample => sample.Timestamp) ?? 0) * 1000);
        var unixDateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds((Int64)millisSinceUnixEpoch);
        return unixDateTimeOffset.UtcDateTime;
      }).ToImmutableDictionary();

    return latestServiceHealthMetricsTimestamps ?? ImmutableDictionary<String, DateTime>.Empty;
  }

  /// <inheritdoc/>
  public async Task<IImmutableList<(DateTime, Double)>> QuerySpecificHealthCheckDataAsync(
    String environment,
    String tenant,
    String service,
    String healthCheck,
    DateTime start,
    DateTime end,
    CancellationToken cancellationToken) {

    // Example query:
    // sonar_healthcheck_data{environment='foo',tenant='baz',service='my-root',check='my-root-health-check'}[start:end]

    var query = String.Format(
      format: "{0}{{environment='{1}',tenant='{2}',service='{3}',check='{4}'}}",
      HealthDataHelper.ServiceHealthCheckDataMetricName,
      environment,
      tenant,
      service,
      healthCheck);

    this._logger.LogDebug(
      "Querying health check data timestamps from {start} to {end} (UTC): {query}",
      start, end, query);

    var queryResponse = await this._prometheusClient.QueryRangeAsync(
      query,
      start,
      end,
      TimeSpan.FromSeconds(30),
      timeout: null,
      cancellationToken);

    var timestampMetrics = new List<(DateTime, Double)>();
    var serviceHealthMetrics = queryResponse.Data?.Result.ToDictionary(
      keySelector: result => result.Labels[HealthDataHelper.MetricLabelKeys.HealthCheck],
      elementSelector: result => {
        if (result.Values?.Count > 0) {
          foreach (var sample in result.Values) {
            var millisSinceUnixEpoch = Math.Round(sample.Timestamp * 1000);
            var unixDateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds((Int64)millisSinceUnixEpoch);
            var metricValue = sample.Value;
            timestampMetrics.Add((unixDateTimeOffset.DateTime, Convert.ToDouble(metricValue)));
          }
        }

        return timestampMetrics;
      });
    if ((serviceHealthMetrics == null) || serviceHealthMetrics.ToImmutableDictionary().IsEmpty) {
      return new List<(DateTime, Double)>().ToImmutableList();
    }

    return serviceHealthMetrics[healthCheck].ToImmutableList();
  }

  /// <inheritdoc />
  public async Task<HealthStatus> GetAlertmanagerScrapeStatusAsync(CancellationToken cancellationToken) {
    var status = HealthStatus.Online;

    try {
      var queryResponse = await this._prometheusClient.QueryAsync(
        query: "timestamp(alertmanager_build_info)",
        timestamp: DateTime.UtcNow,
        cancellationToken: cancellationToken);

      if (queryResponse.Status is ResponseStatus.Error) {
        this._logger.LogError(
          message: "Querying last Alertmanager metrics scrape time returned error: {queryError}",
          queryResponse.Error);
        status = HealthStatus.Unknown;
      } else if ((queryResponse.Data?.Result).IsNullOrEmpty()) {
        this._logger.LogError("Querying last Alertmanager metrics scrape time returned empty result.");
        status = HealthStatus.Offline;
      } else {
        var (_, lastSampleUnixTimeSecondsString) = queryResponse.Data!.Result[0].Value!.Value;
        var lastSampleUnixTimeSeconds = Convert.ToDecimal(lastSampleUnixTimeSecondsString);
        var lastSampleAge = DateTime.UtcNow.Subtract(lastSampleUnixTimeSeconds.UnixTimeSecondsToUtcDateTime());
        // We _should_ see this metric updated once for every scape interval, but allow a little slop for
        // things like network delays.
        var maxAgeThreshold = IPrometheusService.AlertmanagerScrapeInterval + TimeSpan.FromMinutes(1);

        if (lastSampleAge > maxAgeThreshold) {
          this._logger.LogError(
            message: "The last Alertmanager metrics scrape is older than {threshold}",
            maxAgeThreshold);
          status = HealthStatus.Degraded;
        }
      }
    } catch (Exception e) {
      this._logger.LogError(
        exception: e,
        message: "Unexpected error querying last Alertmanager metrics scrape time.");
      status = HealthStatus.Unknown;
    }

    return status;
  }

  /// <inheritdoc />
  public async Task<HealthStatus> GetAlertmanagerNotificationsStatusAsync(
    String integration,
    CancellationToken cancellationToken) {

    const String metricName = "alertmanager_notification_requests_failed_total";
    var status = HealthStatus.Online;

    try {
      // Query time range 4x the scrape interval plus a little slop to ensure we look at data from the last 3 scrapes.
      var queryTimeRange = (IPrometheusService.AlertmanagerScrapeInterval * 4) + TimeSpan.FromMinutes(1);
      var query = String.Format(
        format: "sum(increase({0}{{integration='{1}'}}[{2}]))",
        metricName,
        integration,
        PrometheusClient.ToPrometheusDuration(queryTimeRange));

      var queryResponse = await this._prometheusClient.QueryAsync(
        query: query,
        timestamp: DateTime.UtcNow,
        cancellationToken: cancellationToken);

      if (queryResponse.Status is ResponseStatus.Error) {
        this._logger.LogError(
          message: "Querying {metricName} returned error: {queryError}",
          metricName,
          queryResponse.Error);
        status = HealthStatus.Unknown;
      } else if (!(queryResponse.Data?.Result).IsNullOrEmpty()) {
        var (_, notificationsFailedIncreaseString) = queryResponse.Data!.Result[0].Value!.Value;
        var notificationsFailedIncrease = Convert.ToDecimal(notificationsFailedIncreaseString);

        if (notificationsFailedIncrease > 0) {
          this._logger.LogError(
            message: "Alertmanager {integration} notification failures have increased in the last {queryTimeRange}.",
            integration,
            queryTimeRange);
          status = HealthStatus.Degraded;
        }
      }
    } catch (Exception e) {
      this._logger.LogError(
        exception: e,
        message: "Unexpected error querying {metricName}.",
        metricName);
      status = HealthStatus.Unknown;
    }

    return status;
  }

  /// <inheritdoc />
  public async Task WriteServiceMaintenanceStatusAsync(
    IImmutableList<ServiceMaintenance> records,
    CancellationToken cancellationToken) {

    var writeRequest = new WriteRequest {
      Metadata = { MaintenanceStatusMetricMetadata.MetricMetadata },
      Timeseries = {
        records.Select(sm => new TimeSeries {
          Labels = {
            new Label { Name = "__name__", Value = MaintenanceStatusMetricMetadata.MetricFamilyName },
            new Label { Name = MaintenanceStatusMetricMetadata.EnvironmentLabel, Value = sm.EnvironmentName },
            new Label { Name = MaintenanceStatusMetricMetadata.TenantLabel, Value = sm.TenantName },
            new Label { Name = MaintenanceStatusMetricMetadata.ServiceLabel, Value = sm.ServiceName },
            new Label { Name = MaintenanceStatusMetricMetadata.MaintenanceScopeLabel, Value = sm.MaintenanceScope },
            new Label { Name = MaintenanceStatusMetricMetadata.MaintenanceTypeLabel, Value = sm.MaintenanceType }
          },
          Samples = {
            new Sample { Timestamp = (Int64)Math.Round(DateTime.UtcNow.MillisSinceUnixEpoch()), Value = 1 }
          }
        })
      }
    };

    await this._prometheusRemoteProtocolClient.WriteAsync(writeRequest, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<(Boolean IsInMaintenance, String? MaintenanceTypes)> GetScopedCurrentMaintenanceStatus(
    String environment,
    String? tenant,
    String? service,
    MaintenanceScope scope,
    CancellationToken cancellationToken) {

    // Build scope query string.
    // For example, if Service-scoped maintenance is requested, we would want to
    // find all Service-scoped, Tenant-scoped, and Environment-scoped maintenances.
    var scopeQueryString = Enum.GetValues<MaintenanceScope>()
      .Where(s => s <= scope)
      .Select(s => s.ToString().ToLower())
      .Aggregate((current, next) =>
        $"{current.ToString().ToLower()}|{next.ToString().ToLower()}");

    String labelMatchers = $"{MaintenanceStatusMetricMetadata.MaintenanceScopeLabel}=~\"{scopeQueryString}\", " +
      $"{MaintenanceStatusMetricMetadata.EnvironmentLabel}=\"{environment}\"";

    if (!String.IsNullOrEmpty(tenant)) {
      labelMatchers += $", {MaintenanceStatusMetricMetadata.TenantLabel}=\"{tenant}\"";
    }

    if (!String.IsNullOrEmpty(service)) {
      labelMatchers += $", {MaintenanceStatusMetricMetadata.ServiceLabel}=\"{service}\"";
    }

    String promQuery = $"{MaintenanceStatusMetricMetadata.MetricFamilyName}" + $"{{{labelMatchers}}}";
    (Boolean, String?) result = (false, null);
    try {
      var queryResponse = await this._prometheusClient.QueryAsync(promQuery, DateTime.UtcNow, cancellationToken: cancellationToken);

      if (queryResponse.Status is ResponseStatus.Error) {
        this._logger.LogError(
          message: "Querying {metricName} returned error: {queryError}",
          MaintenanceStatusMetricMetadata.MetricFamilyName,
          queryResponse.Error);
      } else {
        // Query succeeded
        // Find distinct results based on Maintenance Type and aggregate into comma separated string.
        if (queryResponse.Data?.Result.Count > 0) {
          result = (true, queryResponse.Data.Result.Select(series =>
              series.Labels.TryGetValue(MaintenanceStatusMetricMetadata.MaintenanceTypeLabel, out var maintenanceType) ?
                maintenanceType :
                null
            )
            .Where(s => !String.IsNullOrEmpty(s))
            .Distinct()
            .Aggregate((current, next) => $"{current},{next}"));
        }
      }
    } catch (Exception e) {
      this._logger.LogError(
        exception: e,
        message: "Unexpected error querying {metricName}.",
        MaintenanceStatusMetricMetadata.MetricFamilyName);
    }

    return result;
  }
}
