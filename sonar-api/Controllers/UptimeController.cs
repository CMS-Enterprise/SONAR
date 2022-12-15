using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Prometheus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[Route("api/v2/uptime")]
public class UptimeController : ControllerBase {

  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly Uri _prometheusUrl;

  public UptimeController(
    ServiceDataHelper serviceDataHelper,
    IOptions<PrometheusConfiguration> prometheusConfig) {
    this._serviceDataHelper = serviceDataHelper;
    this._prometheusUrl =
      new Uri(
        $"{prometheusConfig.Value.Protocol}://{prometheusConfig.Value.Host}:{prometheusConfig.Value.Port}"
      );
  }

  [HttpGet("{environment}/tenants/{tenant}/services/{*servicePath}", Name = "GetTotalUptime")]
  [ProducesResponseType(typeof(List<UptimeModel>), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(500)]
  public async Task<IActionResult> GetTotalUptime(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String servicePath,
    [FromQuery] String? threshold,
    [FromQuery] DateTime? start,
    [FromQuery] DateTime? end,
    CancellationToken cancellationToken) {

    // Param validation
    DateTime endVal = default;
    DateTime startVal = default;
    // Default period of 7 days.
    TimeSpan period = TimeSpan.FromDays(7);
    DateTime now = DateTime.UtcNow;

    if (start == null) {
      // No start date provided, calculate start and end from default period of 7 days.
      endVal = now;
      startVal = now - period;
    } else if (end == null) {
      // Start date provided but no end date provided, set end date to now
      endVal = now;
    } else {
      // Both dates were provided.
      startVal = DateTime.SpecifyKind((DateTime)start, DateTimeKind.Utc);
      endVal = DateTime.SpecifyKind((DateTime)end, DateTimeKind.Utc);
    }

    // Calculate period.
    period = endVal.Subtract(startVal);

    if (DateTime.Compare(startVal, endVal) > 0) {
      throw new BadRequestException("Invalid Dates: Start date provided is after the end date.");
    }

    // If threshold is included in request, set value, otherwise default to Offline.
    HealthStatus thresholdVal;
    if (!Enum.TryParse(threshold, out thresholdVal)) {
      thresholdVal = HealthStatus.Offline;
    }

    // Validate service hierarchy.
    var (_, _, services) =
      await this._serviceDataHelper.FetchExistingConfiguration(environment, tenant, cancellationToken);
    var serviceChildIdsLookup = await this._serviceDataHelper.GetServiceChildIdsLookup(services, cancellationToken);

    // Validate specified service
    Service? existingService =
      await this._serviceDataHelper.GetSpecificService(environment, tenant, servicePath, serviceChildIdsLookup, cancellationToken);

    if (existingService == null) {
      throw new InvalidOperationException("Invalid service requested.");
    }

    // Traverse family tree, return in list form.
    var serviceFamily = await _serviceDataHelper.TraverseServiceFamilyTree(
      existingService,
      services,
      cancellationToken
    );

    Dictionary<Guid, List<Service>> serviceChildLookup;
    serviceChildLookup = serviceChildIdsLookup
      .Select(grp => new {
        parentId = grp.Key,
        children = grp.Select(childId => services[childId]) })
      .ToDictionary(val => val.parentId, val => val.children.ToList());

    // Create list of service names.
    var serviceNames = new List<String>();
    foreach (var service in serviceFamily) {
      serviceNames.Add(service.Name);
    }

    // Get Uptime Data from Prometheus.
    var upTimeData = await this.GetUptimeData(
      environment,
      tenant,
      serviceNames,
      startVal,
      endVal,
      period,
      thresholdVal,
      cancellationToken
    );

    // Build and return Uptime Model.
    var output =  BuildOutputTree(
      existingService,
      serviceChildLookup,
      upTimeData,
      thresholdVal);

    return this.Ok(output);
  }

  private async Task<Dictionary<String, (Dictionary<HealthStatus, Decimal>, Decimal currentUptime, Decimal totalUptime)>> GetUptimeData(
    String environment,
    String tenant,
    List<String> serviceNames,
    DateTime startVal,
    DateTime endVal,
    TimeSpan period,
    HealthStatus thresholdVal,
    CancellationToken cancellationToken) {

    // Prometheus Query setup.
    using var httpClient = new HttpClient();
    httpClient.BaseAddress = this._prometheusUrl;
    var promClient = new PrometheusClient(httpClient);

    var step = TimeSpan.FromSeconds(15);

    // Get Prometheus metrics based on multiple labels.
    var query = CreateMultiLabelQuery(serviceNames, environment, tenant);
    var response = await promClient.QueryRangeAsync(
      query,
      startVal,
      endVal,
      step,
      null,
      cancellationToken);

    // Error handling for response.
    if (response.Data == null) {
      throw new Exception("No data returned from Prometheus.");
    }

    // Iterate through results, save in unifiedData dictionary.
    var unifiedData = new Dictionary<String, Dictionary<HealthStatus, IEnumerable<(Decimal Timestamp, String Value)>>>();
    foreach (var result in response.Data.Result) {

      var serviceName = result.Labels["service"];
      var status = Enum.Parse<HealthStatus>(result.Labels[HealthController.ServiceHealthAggregateMetricName]);
      var values = result.Values;

      if (values == null) {
        throw new InvalidOperationException("Result set contained no values.");
      }

      // If result set doesn't contain key: add to dictionary
      //  Else: Result set contains key, perform merge of values
      if (!unifiedData.ContainsKey(serviceName)) {
        unifiedData.Add(serviceName, new Dictionary<HealthStatus, IEnumerable<(Decimal Timestamp, String Value)>>{{status, values}});
      } else {
        unifiedData[serviceName].Add(status, values);
      }
    }

    // Dictionary to store status intervals by service.
    var serviceHealthDataStructure = new Dictionary<String, (Dictionary<HealthStatus, Decimal>, Decimal currentUptime, Decimal totalTime)>();

    // Iterate over each value and store in series of tuples (timestamp, status).
    foreach (var kvp in unifiedData) {
      var enumerators = kvp.Value
        .Select(nestedKvp => (nestedKvp.Key, nestedKvp.Value.GetEnumerator()))
        .ToList();

      var healthStatusSeries = new List<(Decimal Timestamp, HealthStatus Status)>();

      while (enumerators.All(t => t.Item2.MoveNext())) {

        if (enumerators.Select(t => t.Item2.Current.Timestamp).ToHashSet().Count != 1) {
          throw new InvalidOperationException(
            "Error Determining Health Status: Prometheus returned time series with inconsistent timestamps.");
        }

        var currState = enumerators
          .Single(t => t.Item2.Current.Value == "1");

        healthStatusSeries.Add((currState.Item2.Current.Timestamp, currState.Key));
      }

      // Calculate durations for each status.
      var statusDictionary = new Dictionary<HealthStatus, Decimal>();
      Decimal? currTimestamp = null;
      Decimal? lastDowntime = null;
      Decimal totalTime = 0;
      foreach (var dataPoint in healthStatusSeries) {
        // If timestamps do not match, set implicit status
        var currKey = dataPoint.Status;
        var diff = currTimestamp != null ? (dataPoint.Timestamp - currTimestamp) : 0;

        // If timestamp is missing, set currKey to unknown.
        if (diff > step.Seconds) {
          currKey = HealthStatus.Unknown;
        }

        // If currKey is not in dictionary, create.
        //  Else, add diff to current value and save.
        if (!statusDictionary.ContainsKey(currKey)) {
          statusDictionary.Add(currKey, (Decimal)diff);
        } else {
          // Status exists in dictionary.
          statusDictionary[currKey] += (Decimal)diff;
        }

        // Check current Uptime
        if (currKey >= thresholdVal) {
          lastDowntime = dataPoint.Timestamp;
        }

        currTimestamp = dataPoint.Timestamp;
        totalTime += (Decimal)diff;
      }

      // Calculate current uptime
      var currentUptime = healthStatusSeries.Last().Timestamp - (lastDowntime ?? (Decimal)startVal.Subtract(DateTime.UnixEpoch).TotalSeconds);

      // Add dictionary to data structure.
      serviceHealthDataStructure.Add(
        kvp.Key,
        (statusDictionary, currentUptime, totalTime));
    }

    return serviceHealthDataStructure;
  }

  private static UptimeModel BuildOutputTree(
    Service rootService,
    Dictionary<Guid, List<Service>> serviceLookup,
    Dictionary<String, (Dictionary<HealthStatus, Decimal>, Decimal currentUptime, Decimal totalUptime)> uptimeData,
    HealthStatus threshold
  ) {

    // Get children for current service
    if (!serviceLookup.TryGetValue(rootService.Id, out var serviceChildren)) {
      serviceChildren = new List<Service>();
    }

    var uptimeChildren = new List<UptimeModel>();

    foreach (var t in serviceChildren) {
      var child = BuildOutputTree(t, serviceLookup, uptimeData, threshold);
      uptimeChildren.Add(child);
    }

    // Build root uptime model.
    var currUptimeData = uptimeData[rootService.Name];
    return CreateUptimeModel(currUptimeData, rootService, threshold, uptimeChildren);
  }

  private static UptimeModel CreateUptimeModel(
    (Dictionary<HealthStatus, Decimal>, Decimal currentUptime, Decimal totalTime) statusData,
    Service service,
    HealthStatus threshold,
    List<UptimeModel> children
  ) {
    Decimal totalUptimeSeconds = 0;
    Decimal unknownDurationSeconds = 0;

    foreach (var status in statusData.Item1) {
      if (status.Key == HealthStatus.Unknown) {
        unknownDurationSeconds += status.Value;
      } else if (status.Key < threshold) {
        totalUptimeSeconds += status.Value;
      }
    }

    double percentUptime = (double)totalUptimeSeconds / (double)statusData.totalTime;

    return new UptimeModel(
      service.Name,
      percentUptime,
      TimeSpan.FromSeconds((double)totalUptimeSeconds),
      TimeSpan.FromSeconds((double)statusData.currentUptime),
      TimeSpan.FromSeconds((double)unknownDurationSeconds),
      children.ToImmutableList()
    );
  }

  private static String CreateMultiLabelQuery(List<String> labels, String environment, String tenant) {

    String queryLabels = "";
    // For each label, append to string with "|" operator.
    for (int i = 0; i < labels.Count; i++) {
      if (i == 0) {
        queryLabels += labels[i];
      } else {
        queryLabels += $"|{labels[i]}";
      }
    }

    // Construct query string with format: query={service=~"service1|...serviceN"}.
    String query =
      "sonar_service_status{service=~" + $"\"{queryLabels}\"" + $",environment=\"{environment}\",tenant=\"{tenant}\"" + "}";
    return query;
  }
}

