using System;
using System.Collections.Generic;
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
  private readonly HealthDataHelper _healthDataHelper;
  private readonly IPrometheusClient _prometheusClient;
  private readonly ILogger<VersionDataHelper> _logger;

  public VersionDataHelper(
    HealthDataHelper healthDataHelper,
    IPrometheusClient prometheusClient,
    ILogger<VersionDataHelper> logger) {
    this._healthDataHelper = healthDataHelper;
    this._prometheusClient = prometheusClient;
    this._logger = logger;
  }

  public async Task<List<ServiceVersionDetails>> GetVersionDetailsForService(
    String environment,
    String tenant,
    String service,
    CancellationToken cancellationToken) {

    List<ServiceVersionDetails> result;
    try {
      result = await this._healthDataHelper.GetLatestValuePrometheusQuery(
        this._prometheusClient,
        $"{ServiceVersionAggregateMetricName}{{environment=\"{environment}\", tenant=\"{tenant}\", service=\"{service}\"}}",
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
                if (versionGroup.Item2.Timestamp > latestTimestampVal && versionGroup.Labels.TryGetValue(ServiceVersionValueLabelName, out var versionVal)) {
                  latestTimestampVal = versionGroup.Item2.Timestamp;
                  version = versionVal;
                  timestamp = ConvertDecimalTimestampToDateTime(versionGroup.Item2.Timestamp);
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
        message: "Error querying Prometheus: {Message}. Using cached values",
        e.Message
      );
      result = new List<ServiceVersionDetails>();
    }

    return result;
  }

  private DateTime ConvertDecimalTimestampToDateTime(Decimal timestamp) {
    return DateTime.UnixEpoch.AddMilliseconds((Int64)(timestamp * 1000));
  }

}
