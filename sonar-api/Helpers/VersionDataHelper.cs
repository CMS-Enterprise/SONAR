using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.Logging;
using PrometheusQuerySdk;

namespace Cms.BatCave.Sonar.Helpers;

public class VersionDataHelper {
  public const String ServiceVersionAggregateMetricName = "sonar_service_version";
  public const String ServiceVersionCheckMetricName = "sonar_service_version_check_value";
  public const String ServiceVersionTypeLabelName = "version_type";
  public const String ServiceVersionValueLabelName = "version_value";

  // When querying for the service's current version, the maximum age of data
  // points from Prometheus to consider. If there are no data points newer than
  // this the services version will not be returned.
  private static readonly TimeSpan MaximumServiceVersionAge = TimeSpan.FromHours(1);

  private readonly HealthDataHelper _healthDataHelper;
  private readonly PrometheusQueryHelper _prometheusQueryHelper;
  private readonly IPrometheusClient _prometheusClient;
  private readonly ILogger<VersionDataHelper> _logger;
  private readonly ServiceVersionCacheHelper _versionCacheHelper;

  public VersionDataHelper(
    HealthDataHelper healthDataHelper,
    PrometheusQueryHelper prometheusQueryHelper,
    IPrometheusClient prometheusClient,
    ILogger<VersionDataHelper> logger,
    ServiceVersionCacheHelper versionCacheHelper) {
    this._healthDataHelper = healthDataHelper;
    this._prometheusQueryHelper = prometheusQueryHelper;
    this._prometheusClient = prometheusClient;
    this._logger = logger;
    this._versionCacheHelper = versionCacheHelper;
  }

  public async Task<List<ServiceVersionDetails>> GetVersionDetailsForService(
    String environment,
    String tenant,
    String service,
    CancellationToken cancellationToken) {

    List<ServiceVersionDetails> result;
    try {
      result = await this._prometheusQueryHelper.GetLatestValuePrometheusQuery(
        $"{ServiceVersionAggregateMetricName}{{environment=\"{environment}\", tenant=\"{tenant}\", service=\"{service}\"}}",
        MaximumServiceVersionAge,
        processResult: results => {
          var serviceVersionDetails = new List<ServiceVersionDetails>();
          var versionValuesByVersionType = results.Result
            .Where(series => (series.Values != null) && series.Values.Any())
            .Select(series => (
              series.Labels,
              series.Values!
                .OrderByDescending(v => v.Timestamp)
                .FirstOrDefault()))
            .ToLookup(
              keySelector: series =>
                series.Labels.TryGetValue(ServiceVersionTypeLabelName, out var versionType) ? versionType : null,
              StringComparer.OrdinalIgnoreCase);

          foreach (var group in versionValuesByVersionType) {
            if (group.Key == null) {
              continue;
            }

            if (Enum.TryParse<VersionCheckType>(group.Key, out var versionType)) {
              Decimal latestTimestampVal = 0;
              String version = String.Empty;
              DateTime timestamp = DateTime.MinValue;

              foreach (var versionGroup in group) {
                if ((versionGroup.Item2.Timestamp > latestTimestampVal) &&
                  versionGroup.Labels.TryGetValue(ServiceVersionValueLabelName, out var versionVal)) {

                  latestTimestampVal = versionGroup.Item2.Timestamp;
                  version = versionVal;
                  timestamp = this.ConvertDecimalTimestampToDateTime(versionGroup.Item2.Timestamp);
                }
              }

              if (latestTimestampVal > 0) {
                serviceVersionDetails.Add(
                  new ServiceVersionDetails(versionType, version, timestamp));
              }
            }
          }
          return serviceVersionDetails;
        },
        cancellationToken);
    } catch (Exception e) {
      this._logger.LogError(
        message: "Error querying Prometheus: {Message}. Using cached service version values",
        e.Message
      );
      result = await this._versionCacheHelper.FetchServiceVersionCache(
        environment,
        tenant,
        service,
        cancellationToken);
    }

    return result;
  }

  public async Task<IImmutableDictionary<String, IImmutableList<ServiceVersionDetails>>> GetServiceVersionLookup(
    String environment,
    String tenant,
    CancellationToken cancellationToken) {

    IImmutableDictionary<String, IImmutableList<ServiceVersionDetails>> result;
    try {
      result = await this._prometheusQueryHelper.GetLatestValuePrometheusQuery(
        $"{ServiceVersionAggregateMetricName}{{environment=\"{environment}\", tenant=\"{tenant}\"}}",
        MaximumServiceVersionAge,
        processResult: results => {
          var versionDetailsLookup =
            new Dictionary<String, IImmutableList<ServiceVersionDetails>>(StringComparer.OrdinalIgnoreCase);

          var versionValuesByVersionType = results.Result
            .Where(series => (series.Values != null) && series.Values.Any())
            .Select(series => (
              series.Labels,
              series.Values!
                .OrderByDescending(v => v.Timestamp)
                .FirstOrDefault()))
            .ToLookup(
              keySelector: series =>
                (series.Labels.TryGetValue("service", out var serviceName) ? (String?)serviceName : null,
                  series.Labels.TryGetValue(ServiceVersionTypeLabelName, out var versionType) ? (String?)versionType : null),
              new TupleComparer<String?, String?>(StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase));

          foreach (var group in versionValuesByVersionType) {
            if ((group.Key.Item1 == null) || (group.Key.Item2 == null)) {
              // The time series is missing either the service name or the version type label
              continue;
            }

            var serviceName = group.Key.Item1;
            if (!versionDetailsLookup.TryGetValue(serviceName, out var versionList)) {
              versionList = ImmutableList<ServiceVersionDetails>.Empty;
            }

            if (Enum.TryParse<VersionCheckType>(group.Key.Item2, out var versionType)) {
              var latestTimestampVal = 0m;
              var version = String.Empty;
              var timestamp = DateTime.MinValue;

              foreach (var versionGroup in group) {
                if ((versionGroup.Item2.Timestamp > latestTimestampVal) &&
                  versionGroup.Labels.TryGetValue(ServiceVersionValueLabelName, out var versionVal)) {

                  latestTimestampVal = versionGroup.Item2.Timestamp;
                  version = versionVal;
                  timestamp = this.ConvertDecimalTimestampToDateTime(versionGroup.Item2.Timestamp);
                }
              }

              if (latestTimestampVal > 0) {
                versionDetailsLookup[serviceName] =
                  versionList.Add(new ServiceVersionDetails(versionType, version, timestamp));
              }
            }
          }

          return versionDetailsLookup.ToImmutableDictionary();
        },
        cancellationToken);
    } catch (Exception e) {
      this._logger.LogError(
        message: "Error querying Prometheus: {Message}. Using cached values",
        e.Message
      );

      // TODO: get data from db cache
      result = ImmutableDictionary<String, IImmutableList<ServiceVersionDetails>>.Empty;
    }

    return result;
  }

  private DateTime ConvertDecimalTimestampToDateTime(Decimal timestamp) {
    return DateTime.UnixEpoch.AddMilliseconds((Int64)(timestamp * 1000));
  }

}
