using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;
using PrometheusQuerySdk;
using PrometheusQuerySdk.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cms.BatCave.Sonar.Helpers;

public class HealthDataHelper {
  public const String ServiceHealthAggregateMetricName = "sonar_service_status";
  public const String ServiceHealthCheckMetricName = "sonar_service_health_check_status";
  public const String ServiceHealthCheckDataMetricName = "sonar_healthcheck_data";

  // When querying for the services current health, the maximum age of data
  // points from Prometheus to consider. If there are no data points newer than
  // this the services health status will be unknown.
  private static readonly TimeSpan MaximumServiceHealthAge = TimeSpan.FromHours(1);

  private readonly ServiceHealthCacheHelper _cacheHelper;
  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly PrometheusQueryHelper _prometheusQueryHelper;
  private readonly IPrometheusClient _prometheusClient;
  private readonly Uri _prometheusUrl;
  private readonly DataContext _dbContext;
  private readonly IOptions<DatabaseConfiguration> _dbConfig;
  private readonly ILogger<HealthDataHelper> _logger;
  private readonly TagsDataHelper _tagsDataHelper;
  private readonly IOptions<WebHostConfiguration> _webHostConfiguration;

  public HealthDataHelper(
    DataContext dbContext,
    ServiceHealthCacheHelper cacheHelper,
    ServiceDataHelper serviceDataHelper,
    PrometheusQueryHelper prometheusQueryHelper,
    IPrometheusClient prometheusClient,
    IOptions<PrometheusConfiguration> prometheusConfig,
    IOptions<DatabaseConfiguration> dbConfig,
    ILogger<HealthDataHelper> logger,
    TagsDataHelper tagsDataHelper,
    IOptions<WebHostConfiguration> webHostConfiguration) {


    this._cacheHelper = cacheHelper;
    this._serviceDataHelper = serviceDataHelper;
    this._prometheusQueryHelper = prometheusQueryHelper;
    this._prometheusClient = prometheusClient;
    this._prometheusUrl = new Uri(
      $"{prometheusConfig.Value.Protocol}://{prometheusConfig.Value.Host}:{prometheusConfig.Value.Port}/"
    );
    this._dbContext = dbContext;
    this._dbConfig = dbConfig;
    this._logger = logger;
    this._tagsDataHelper = tagsDataHelper;
    this._webHostConfiguration = webHostConfiguration;
  }

  public async Task<Dictionary<String, (DateTime Timestamp, HealthStatus Status)>> GetServiceStatuses(
    String environment,
    String tenant,
    CancellationToken cancellationToken) {

    Dictionary<String, (DateTime Timestamp, HealthStatus Status)> serviceStatuses;
    try {
      serviceStatuses = await this._prometheusQueryHelper.GetLatestValuePrometheusQuery(
        $"{HealthDataHelper.ServiceHealthAggregateMetricName}{{environment=\"{environment}\", tenant=\"{tenant}\"}}",
        MaximumServiceHealthAge,
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
        message: "Error querying Prometheus: {Message}. Using cached values",
        e.Message
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
        await this._prometheusQueryHelper.GetLatestValuePrometheusQuery(
          $"{HealthDataHelper.ServiceHealthCheckMetricName}{{environment=\"{environment}\", tenant=\"{tenant}\"}}",
          MaximumServiceHealthAge,
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
        message: "Error querying Prometheus: {Message}. Using cached values",
        e.Message
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
    Dictionary<(String Service, String HealthCheck), (DateTime Timestamp, HealthStatus Status)> healthCheckStatus,
    ILookup<Guid, ServiceTag> tagsByService,
    IImmutableDictionary<String, String?> inheritedTags,
    String environment,
    String tenant,
    ImmutableQueue<String> servicePathQueue) {
    // The service will have its own status if it has health checks that have recorded status.
    var hasServiceStatus = serviceStatuses.TryGetValue(service.Name, out var serviceStatus);
    var children =
      serviceChildIdsLookup[service.Id].Select(sid =>
          ToServiceHealth(
            services[sid],
            services,
            serviceStatuses,
            serviceChildIdsLookup,
            healthChecksByService,
            healthCheckStatus,
            tagsByService,
            this._tagsDataHelper.GetResolvedServiceTags(
              inheritedTags,
              tagsByService[service.Id].ToList()),
            environment,
            tenant,
            servicePathQueue.Enqueue(services[sid].Name)
          )
        )
        .ToImmutableHashSet();

    HealthStatus? aggregateStatus = hasServiceStatus ? serviceStatus.Status : null;
    DateTime? statusTimestamp = hasServiceStatus ? serviceStatus.Timestamp : null;
    HealthStatus? worstChildStatus = null;
    var healthChecks = healthChecksByService[service.Id].ToImmutableList();
    // Only aggregate the status of the children if the current service either
    // has a recorded status based on its health checks, or has no health
    // checks. (If there is missing information for the health check based
    // service then there is no point considering the status of the children).
    if (hasServiceStatus || !healthChecks.Any()) {
      // If this service has a status

      foreach (var child in children) {
        if (!child.AggregateStatus.HasValue) {
          worstChildStatus = null;
          statusTimestamp = null;
          break;
        }

        var currChildStatus = (HealthStatus)child.AggregateStatus;
        if (worstChildStatus == null ||
          currChildStatus.IsWorseThan((HealthStatus)worstChildStatus)) {
          worstChildStatus = child.AggregateStatus;
        }

        if (child.Timestamp.HasValue &&
          (!statusTimestamp.HasValue || (child.Timestamp.Value < statusTimestamp.Value))) {

          // The status timestamp should always be the *oldest* of the
          // recorded status data points.
          statusTimestamp = child.Timestamp.Value;
        }
      }
    }

    // service with status and child status
    if (aggregateStatus != null &&
      worstChildStatus != null) {
      aggregateStatus = ((HealthStatus)aggregateStatus).IsWorseThan((HealthStatus)worstChildStatus) ?
        aggregateStatus :
        worstChildStatus;
    } else if (aggregateStatus == null &&
      worstChildStatus != null) {
      // service with no status but has child with status
      // example: Parent with no health checks but children with health checks
      aggregateStatus = worstChildStatus;
    } else if (aggregateStatus != null &&
      worstChildStatus == null &&
      !children.IsEmpty) {
      // service has status, worst child status is null, set both to null as long as
      // service is not a leaf node (child with no children)
      aggregateStatus = null;
      statusTimestamp = null;
    }

    return new ServiceHierarchyHealth(
      service.Name,
      service.DisplayName,
      BuildDashboardLink(servicePathQueue, environment, tenant),
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
      children,
      this._tagsDataHelper.GetResolvedServiceTags(
        inheritedTags,
        tagsByService[service.Id].ToList())
    );
  }

  public String BuildDashboardLink(
    ImmutableQueue<String> servicePathQueue,
    String environment,
    String tenant) {
    var servicePath = String.Join("/", servicePathQueue);
    var dashboardBaseUrl =
      this._webHostConfiguration.Value.AllowedOrigins.FirstOrDefault() ??
      "http://localhost:8080";
    return $"{dashboardBaseUrl}/{environment}/tenants/{tenant}/services/{servicePath}";
  }

  public async Task<T>
  GetPrometheusQueryRangeValue<T>(
    IPrometheusClient prometheusClient,
    String promQuery, DateTime start, DateTime end, TimeSpan step,
    Func<QueryResults, T> processResult, CancellationToken cancellationToken) {

    try {
      var response = await prometheusClient.QueryRangeAsync(
        $"{promQuery}",
        start.ToUniversalTime(),
        end.ToUniversalTime(),
        step,
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
    } catch (Exception e) {

      throw new InternalServerErrorException(
        errorType: "PrometheusApiError",
        message: $"Error querying service health history status. {e.Message}"
      );
    }
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

  public async Task<List<ServiceHierarchyHealth>> CheckSonarHealth(
    CancellationToken cancellationToken) {
    var postgresCheck = await this.RunPostgresHealthCheck(cancellationToken);
    var prometheusCheck = await this.RunPrometheusSelfCheck(cancellationToken);
    var result = new List<ServiceHierarchyHealth>() { postgresCheck, prometheusCheck };
    return result;
  }

  private async Task<ServiceHierarchyHealth> RunPostgresHealthCheck(CancellationToken cancellationToken) {
    var aggStatus = HealthStatus.Online;
    var healthChecks =
      new Dictionary<String, (DateTime Timestamp, HealthStatus Status)?>();
    var connectionTestResult = HealthStatus.Online;
    var sonarDbTestResult = HealthStatus.Online;

    try {
      await _dbContext.Database.OpenConnectionAsync(cancellationToken: cancellationToken);
    } catch (InvalidOperationException e) {
      // Db connection issue
      this._logger.LogError(
        message: "Unexpected DB error: {Message}",
        e.Message
      );
      connectionTestResult = HealthStatus.Offline;
      sonarDbTestResult = HealthStatus.Unknown;
    } catch (PostgresException e) {
      // Sonar db issue
      this._logger.LogError(
        message: "Unexpected DB error: {Message}",
        e.Message
      );
      sonarDbTestResult = HealthStatus.Offline;
    } catch (SocketException e) {
      // Socket connection issue
      this._logger.LogError(
        message: "Unexpected socket exception: {Message}",
        e.Message
      );
    } catch (OperationCanceledException e) {
      // Operation cancelled
      this._logger.LogError(
        message: "Unexpected operation cancelled exception: {Message}",
        e.Message
      );
    }

    healthChecks.Add("connection-test", (DateTime.UtcNow, connectionTestResult));
    healthChecks.Add("sonar-database-test", (DateTime.UtcNow, sonarDbTestResult));

    // calculate aggStatus
    aggStatus = new[] { connectionTestResult, sonarDbTestResult }.Max();

    return new ServiceHierarchyHealth(
      "postgresql",
      "Postgresql",
      BuildDashboardLink(
        ImmutableQueue.Create<String>("postgresql"),
        "sonar-local",
        "sonar"),
  "The Postgresql instance that the SONAR API uses to persist service health information.",
      new Uri(
        $"postgresql://{_dbConfig.Value.Host}:{_dbConfig.Value.Port}/{_dbConfig.Value.Database}"),
      DateTime.UtcNow,
      aggStatus,
      healthChecks.ToImmutableDictionary(),
      null
    );
  }

  private async Task<ServiceHierarchyHealth> RunPrometheusSelfCheck(CancellationToken cancellationToken) {
    var httpClient = new HttpClient();
    httpClient.BaseAddress = this._prometheusUrl;
    var healthChecks =
      new Dictionary<String, (DateTime Timestamp, HealthStatus Status)?>();
    var readinessTest = HealthStatus.Online;
    var queryTest = HealthStatus.Online;

    try {
      await httpClient.GetAsync(
        "-/ready",
        cancellationToken);
    } catch (HttpRequestException e) {
      // Failed readiness probe
      this._logger.LogError(
        message: "Unexpected HTTP error: {Message}",
        e.Message
      );
      readinessTest = HealthStatus.Offline;
      queryTest = HealthStatus.Unknown;
    } catch (Exception e) {
      // Unknown exception
      this._logger.LogError(
        message: "Unexpected HTTP error: {Message}",
        e.Message
      );
      readinessTest = HealthStatus.Unknown;
      queryTest = HealthStatus.Unknown;
    }

    healthChecks.Add("readiness-probe", (DateTime.UtcNow, readinessTest));
    healthChecks.Add("test-query", (DateTime.UtcNow, queryTest));

    // calculate aggStatus
    var aggStatus = new[] { readinessTest, queryTest }.Max();

    return new ServiceHierarchyHealth(
      "prometheus",
      "Prometheus",
      BuildDashboardLink(
        ImmutableQueue.Create<String>("prometheus"),
        "sonar-local",
        "sonar"),
      "The Prometheus instance that the SONAR API uses to persist service health information.",
      this._prometheusUrl,
      DateTime.UtcNow,
      aggStatus,
      healthChecks.ToImmutableDictionary(),
      children: null
    );
  }

  public async Task<Dictionary<String, (DateTime Timestamp, HealthStatus Status)>> GetHistoricalHealthCheckResultsForService(
    String environment,
    String tenant,
    String service,
    DateTime timeQuery,
    CancellationToken token) {

    Dictionary<String, (DateTime Timestamp, HealthStatus Status)> result;
    try {

      result = await this._prometheusQueryHelper.GetInstantaneousValuePromQuery(
        $"{ServiceHealthCheckMetricName}{{environment=\"{environment}\", tenant=\"{tenant}\", service=\"{service}\"}}",
        timeQuery,
        processResult: results => {
          // StateSet metrics are split into separate metric per-state
          // This code groups all the metrics for a given state.
          var metricByHealthCheck =
            results.Result
              .Where(metric => metric.Value.HasValue)
              .Select(metric => (metric.Labels, metric.Value))
              .ToLookup(
                keySelector: metric => this.GetHealthCheckKey(metric.Labels),
                TupleComparer.From(StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase)
              );

          var healthMapping =
            new Dictionary<String, (DateTime Timestamp, HealthStatus Status)>();
          foreach (var group in metricByHealthCheck) {
            if ((group.Key.Item1 == null) || (group.Key.Item2 == null)) {
              // invalid metric that is missing labels, ignore
              continue;
            }

            (DateTime Timestamp, HealthStatus Status)? effectiveStatusTuple = null;
            foreach (var hcResult in group) {
              if ((hcResult.Value?.Value == "1") &&
                hcResult.Labels.TryGetValue(HealthDataHelper.ServiceHealthCheckMetricName, out var statusStr) &&
                Enum.TryParse<HealthStatus>(statusStr, out var status)) {

                if (effectiveStatusTuple == null) {
                  effectiveStatusTuple = (this.ConvertDecimalTimestampToDateTime(hcResult.Value.Value.Timestamp), status);
                }
              }
            }

            if (effectiveStatusTuple != null) {
              healthMapping.Add(group.Key.Item2, ((DateTime Timestamp, HealthStatus Status))effectiveStatusTuple);
            }
          }

          return healthMapping;
        },
        token
      );
    } catch (Exception e) {
      this._logger.LogError(
        message: "Error querying Prometheus: {Message}",
        e.Message
      );
      result = new Dictionary<String, (DateTime Timestamp, HealthStatus Status)>();
    }

    return result;
  }

  private DateTime ConvertDecimalTimestampToDateTime(Decimal timestamp) {
    return DateTime.UnixEpoch.AddMilliseconds((Int64)(timestamp * 1000));
  }

  public class MetricLabelKeys {
    public const String Environment = "environment";
    public const String Tenant = "tenant";
    public const String Service = "service";
    public const String HealthCheck = "check";
  }
}
