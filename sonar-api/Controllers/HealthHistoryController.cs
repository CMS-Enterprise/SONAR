using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using PrometheusQuerySdk;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Enum = System.Enum;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/health-history")]
public class HealthHistoryController : ControllerBase {
  private static readonly TimeSpan QueryRangeMaximumNumberDays = TimeSpan.FromDays(7);
  private readonly IPrometheusClient _prometheusClient;
  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly HealthDataHelper _healthDataHelper;
  private readonly String _sonarEnvironment;

  public HealthHistoryController(
    ServiceDataHelper serviceDataHelper,
    IPrometheusClient prometheusClient,
    HealthDataHelper healthDataHelper,
    IOptions<SonarHealthCheckConfiguration> sonarHealthConfig) {

    this._serviceDataHelper = serviceDataHelper;
    this._prometheusClient = prometheusClient;
    this._healthDataHelper = healthDataHelper;
    this._sonarEnvironment = sonarHealthConfig.Value.SonarEnvironment;
  }

  /// <summary>
  ///   Get the health history for all services within the specified Tenant.
  /// </summary>
  /// <param name="step">
  ///   The number of seconds that is incremented on each step.  Step cannot be greater
  ///   than 3600 (default 30)
  /// </param>
  /// <param name="tenant"></param>
  /// <param name="start">
  ///   The queries first evaluation time.  The start and end time cannot be greater
  ///   than 24 hours (default is current time)
  /// </param>
  /// <param name="end">
  ///   The queries evaluation time stops on or before this time.  The start and end time
  ///   cannot be greater than 24 hours (default is current time minus 1 hour)
  /// </param>
  /// <param name="environment"></param>
  /// <param name="cancellationToken"></param>
  [HttpGet("{environment}/tenants/{tenant}", Name = "GetServicesHealthHistory")]
  [ProducesResponseType(typeof(ServiceHierarchyHealthHistory[]), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(500)]
  public async Task<IActionResult> GetServicesHealthHistory(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [Optional] DateTime? start,
    [Optional] DateTime? end,
    [Optional] Int32? step,
    CancellationToken cancellationToken) {

    (DateTime startTime, DateTime endTime, Int32 stepIncrement) = ValidateParameters(start, end, step);

    var (_, _, services) =
      await this._serviceDataHelper.FetchExistingConfiguration(environment, tenant, cancellationToken);
    var serviceStatuses =
      await this.GetServicesStatusHistory(environment, tenant, startTime, endTime, stepIncrement, cancellationToken);
    var serviceChildIdsLookup = await this._serviceDataHelper.GetServiceChildIdsLookup(services, cancellationToken);
    var healthChecksByService = await this._healthDataHelper.GetHealthChecksByService(services, cancellationToken);

    return this.Ok(
      services.Values.Where(svc => svc.IsRootService)
        .Select(svc => HealthHistoryController.ToServiceHealthHistory(
          svc,
          services,
          serviceStatuses,
          serviceChildIdsLookup,
          healthChecksByService)
        )
        .ToArray()
    );
  }

  /// <summary>
  ///   Get the health history for a specific service, specified by its path in the service hierarchy.
  /// </summary>
  /// <remarks>
  ///   Get the health history for a specific service and its children.
  /// </remarks>
  /// <param name="step">
  ///   The number of seconds that is incremented on each step.  Step cannot be greater
  ///   than 3600 (default 30)
  /// </param>
  /// <param name="servicePath"></param>
  /// <param name="start">
  ///   The queries first evaluation time.  The start and end time cannot be greater
  ///   than 24 hours (default is current time)
  /// </param>
  /// <param name="end">
  ///   The queries evaluation time stops on or before this time.  The start and end time
  ///   cannot be greater than 24 hours (default is current time minus 1 hour)
  /// </param>
  /// <param name="environment"></param>
  /// <param name="tenant"></param>
  /// <param name="cancellationToken"></param>
  [HttpGet("{environment}/tenants/{tenant}/services/{*servicePath}", Name = "GetServiceHealthHistory")]
  [ProducesResponseType(typeof(ServiceHierarchyHealthHistory), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(500)]
  public async Task<IActionResult> GetServiceHealthHistory(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String servicePath,
    [Optional] DateTime? start,
    [Optional] DateTime? end,
    [Optional] Int32? step,
    CancellationToken cancellationToken) {

    var (startTime, endTime, stepIncrement) = ValidateParameters(start, end, step);

    // Checks if polling for Sonar Health
    if (String.Equals(environment, this._sonarEnvironment, StringComparison.OrdinalIgnoreCase) &&
      String.Equals(tenant, TenantDataHelper.SonarTenantName, StringComparison.OrdinalIgnoreCase)) {
      var sonarHealth = this.GetSonarHealthHierarchy(servicePath, cancellationToken);
      return this.Ok(sonarHealth);
    }

    var (_, _, services) =
      await this._serviceDataHelper.FetchExistingConfiguration(environment, tenant, cancellationToken);
    var serviceChildIdsLookup = await this._serviceDataHelper.GetServiceChildIdsLookup(services, cancellationToken);
    var existingService =
      await this._serviceDataHelper.GetSpecificService(
        environment, tenant, servicePath, serviceChildIdsLookup, cancellationToken);

    if (existingService == null) {
      throw new ResourceNotFoundException(nameof(Service), servicePath);
    }

    var serviceStatuses =
      await this.GetServicesStatusHistory(environment, tenant, startTime, endTime, stepIncrement, cancellationToken);
    var healthChecksByService = await this._healthDataHelper.GetHealthChecksByService(services, cancellationToken);

    return this.Ok(
      ToServiceHealthHistory(
        existingService,
        services,
        serviceStatuses,
        serviceChildIdsLookup,
        healthChecksByService)
    );
  }

  [HttpGet("{environment}/tenants/{tenant}/services/{service}/health-check-results",
    Name = "GetHistoricalHealthCheckResultsForService")]
  [ProducesResponseType(typeof(Dictionary<String, (DateTime Timestamp, HealthStatus Status)>), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(500)]
  public async Task<IActionResult> GetHistoricalHealthCheckResultsForService(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String service,
    [FromQuery] DateTime? timeQuery,
    CancellationToken cancellationToken) {

    var timestamp = timeQuery ?? DateTime.UtcNow;
    if (timestamp.Kind != DateTimeKind.Utc) {
      return this.BadRequest("Invalid timestamp");
    }

    var result = await this._healthDataHelper.GetHistoricalHealthCheckResultsForService(
      environment,
      tenant,
      service,
      timestamp,
      cancellationToken);

    return this.Ok(result);
  }

  private ServiceHierarchyHealthHistory GetSonarHealthHierarchy(
    String servicePath,
    CancellationToken cancellationToken) {

    var sonarConfiguration = this._serviceDataHelper.FetchSonarConfiguration();
    var sonarHealth = this._healthDataHelper.CheckSonarHealth(cancellationToken);

    var sonarConfig = sonarConfiguration.Services
      .Where(svc => svc.Name == servicePath)
      .Select(svc => (svc.Name, svc.DisplayName, svc.Description, svc.Url)).SingleOrDefault();

    var serviceHealth = sonarHealth.Result
      .Where(svc => svc.Name == servicePath)
      .Select(svc => (svc.Timestamp!.Value, svc.AggregateStatus!.Value)).SingleOrDefault();

    var serviceHealthInstance = new List<(DateTime Timestamp, HealthStatus AggregateStatus)> {
      (serviceHealth.Item1, serviceHealth.Item2)
    };

    var serviceHierarchyHealthHistory = new ServiceHierarchyHealthHistory(
      sonarConfig.Name,
      sonarConfig.DisplayName,
      sonarConfig.Description,
      sonarConfig.Url,
      serviceHealthInstance.ToImmutableList(),
      null
    );

    return serviceHierarchyHealthHistory;
  }

  private async Task<Dictionary<String, List<(DateTime Timestamp, HealthStatus Status)>>>
    GetServicesStatusHistory(
      String environment, String tenant, DateTime start, DateTime end, Int32 step,
      CancellationToken cancellationToken) {

    return await this._healthDataHelper.GetPrometheusQueryRangeValue(
      this._prometheusClient,
      $"max_over_time({HealthDataHelper.ServiceHealthAggregateMetricName}{{environment=\"{environment}\", tenant=\"{tenant}\"}}[{PrometheusClient.ToPrometheusDuration(TimeSpan.FromSeconds(step))}])",
      start, end, TimeSpan.FromSeconds(step),
      processResult: results => {
        // StateSet metrics are split into separate metric per-state
        // This code groups all the metrics for a given service and then determines which state is currently set.
        var metricByService =
          results.Result
            .Where(metric => metric.Values != null)
            .Select(metric => (metric.Labels, metric.Values!.ToList()))
            .ToLookup(
              keySelector: metric =>
                metric.Labels.TryGetValue(HealthDataHelper.MetricLabelKeys.Service, out var serviceName) ?
                  serviceName :
                  null, StringComparer.OrdinalIgnoreCase);

        var serviceStatusHistoryDictionary = new Dictionary<String, List<(DateTime Timestamp, HealthStatus Value)>>();
        // Iterate though the services grouping and create a time series list of each health status.
        foreach (var service in metricByService) {
          if (service.Key is null) {
            throw new InvalidOperationException("The time series for health status is missing the service label");
          }

          var statusList = new List<(DateTime, HealthStatus)>();

          // group metrics by timestamp to determine worst status at that particular time
          // resulting data structure will look like:
          //  timestamp: [ { labels, value }, { labels, value } ]
          var r = service.SelectMany(e => e.Item2.Select(timestampValTuple => new { e.Labels, timestampValTuple }))
            .GroupBy(
              val => val.timestampValTuple.Timestamp,
              val => (val.Labels, val.timestampValTuple.Value));

          foreach (var timestampGroup in r) {
            var statusTimestamp = DateTime.UnixEpoch.AddSeconds((Double)timestampGroup.Key);
            HealthStatus? worstStatus = null;

            // only iterate over values that have a reported status, determine the worst status in that subset
            foreach (var metric in timestampGroup
              .Where(e => e.Value == "1")) {

              // throw exception if metric does not have the sonar health status label
              if (!metric.Labels.TryGetValue(HealthDataHelper.ServiceHealthAggregateMetricName,
                out var healthStatusValue)) {
                throw new InvalidOperationException("The times series for a health status is missing a status label");
              }

              // if extracted health status is valid, determine the worst status
              if (Enum.TryParse<HealthStatus>(healthStatusValue, out var status)) {
                if (!worstStatus.HasValue ||
                  status.IsWorseThan((HealthStatus)worstStatus)) {
                  worstStatus = status;
                }
              }
            }
            statusList.Add((statusTimestamp, worstStatus ?? HealthStatus.Unknown));
          }

          serviceStatusHistoryDictionary.Add(service.Key, statusList);
        }

        return serviceStatusHistoryDictionary;
      },
      cancellationToken
    );
  }

  private static ServiceHierarchyHealthHistory ToServiceHealthHistory(
    Service service,
    IImmutableDictionary<Guid, Service> services,
    Dictionary<String, List<(DateTime Timestamp, HealthStatus Status)>> serviceStatuses,
    ILookup<Guid, Guid> serviceChildIdsLookup,
    ILookup<Guid, HealthCheck> healthChecksByService) {

    // The service will have its own status if it has health checks that have recorded status.
    var hasServiceStatus = serviceStatuses.TryGetValue(service.Name, out var serviceStatus);
    var hasHealthChecks = healthChecksByService[service.Id].Any();

    var children =
      serviceChildIdsLookup[service.Id].Select(sid => ToServiceHealthHistory(
        services[sid], services, serviceStatuses, serviceChildIdsLookup, healthChecksByService)).ToImmutableHashSet();

    var history = serviceStatus?.ToImmutableList();

    // If the parent service has health checks, but we don't have any status
    // information, then there is no point in aggregating the children, since
    // the effective status is unknown
    //
    // However, if the service has no health checks of its own then its
    // effective status is _only_ the aggregate status of its children
    if (hasServiceStatus || !hasHealthChecks) {
      foreach (var child in children) {
        if (child.AggregateStatus is not null) {
          history = (history != null ? AggregateStatus(history, child.AggregateStatus) : child.AggregateStatus)
            .ToImmutableList();
        }
      }
    }

    return new ServiceHierarchyHealthHistory(
      service.Name,
      service.DisplayName,
      service.Description,
      service.Url,
      history,
      children
    );

  }

  private static IEnumerable<(DateTime Timestamp, HealthStatus Status)>
    AggregateStatus(
      IImmutableList<(DateTime Timestamp, HealthStatus Status)> series1,
      IImmutableList<(DateTime Timestamp, HealthStatus Status)>? series2) {

    using var series2Enumerator = series2?.GetEnumerator();
    var series2End = false;

    if (series2Enumerator?.MoveNext() == true) {
      foreach (var stat1 in series1) {
        // Find the corresponding value in the second time series
        while (series2Enumerator.Current.Timestamp < stat1.Timestamp) {
          if (!series2Enumerator.MoveNext()) {
            series2End = true;
            break;
          }
        }

        if (series2End) {
          break;
        }

        if (stat1.Timestamp == series2Enumerator.Current.Timestamp) {
          // Find the "more severe" status
          var aggregateStatus =
            // Greater value is more severe, except that Unknown trumps everything else
            (stat1.Status == HealthStatus.Unknown) || (series2Enumerator.Current.Status == HealthStatus.Unknown) ?
              HealthStatus.Unknown :
              (stat1.Status > series2Enumerator.Current.Status ?
                stat1.Status :
                series2Enumerator.Current.Status);
          yield return (stat1.Timestamp, aggregateStatus);
        }
      }
    }
  }

  private static (DateTime, DateTime, Int32) ValidateParameters(
    DateTime? queryStart,
    DateTime? queryEnd,
    Int32? queryStep) {

    var end = queryEnd?.ToUniversalTime() ?? DateTime.UtcNow;
    var start = queryStart?.ToUniversalTime() ?? end.Subtract(TimeSpan.FromHours(1));
    var step = queryStep ?? 30;

    if (step > 3600) {
      throw new BadRequestException($"step {step} cannot be greater the 3600");
    }

    if (end <= start) {
      throw new BadRequestException("End date cannot be earlier or equal to the start date");
    }

    // End - Start cannot be greater than 7 days to be consistent with Metric history restriction.
    if ((end - start) >= QueryRangeMaximumNumberDays) {
      throw new BadRequestException(
        $"The number of days must be less than or equal to {QueryRangeMaximumNumberDays.Days}"
      );
    }

    return (start, end, step);
  }

}
