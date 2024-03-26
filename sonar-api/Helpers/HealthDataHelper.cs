using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Alertmanager;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Prometheus;
using k8s.Models;
using PrometheusQuerySdk;
using PrometheusQuerySdk.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Prometheus;

namespace Cms.BatCave.Sonar.Helpers;

public class HealthDataHelper {
  public const String ServiceHealthAggregateMetricName = "sonar_service_status";
  public const String ServiceHealthCheckMetricName = "sonar_service_health_check_status";
  public const String ServiceHealthCheckDataMetricName = "sonar_healthcheck_data";
  public const String PrometheusSelfCheckName = "sonar_prometheus_self_check";
  public const String PrometheusWriteTestMetricName = "sonar_remote_write_test";

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
  private readonly AlertingDataHelper _alertingDataHelper;
  private readonly AlertingConfigurationHelper _alertingConfigurationHelper;
  private readonly IOptions<GlobalAlertingConfiguration> _globalAlertingConfig;
  private readonly IOptions<KubernetesApiAccessConfiguration> _kubernetesApiAccessConfig;
  private readonly IPrometheusRemoteProtocolClient _prometheusRemoteProtocolClient;
  private readonly IAlertmanagerService _alertmanagerService;
  private readonly IPrometheusService _prometheusService;
  private readonly SonarHealthCheckConfiguration _sonarHealthConfig;

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
    IOptions<WebHostConfiguration> webHostConfiguration,
    AlertingDataHelper alertingDataHelper,
    AlertingConfigurationHelper alertingConfigurationHelper,
    IOptions<GlobalAlertingConfiguration> globalAlertingConfig,
    IOptions<KubernetesApiAccessConfiguration> kubernetesApiAccessConfig,
    IPrometheusRemoteProtocolClient prometheusRemoteProtocolClient,
    IAlertmanagerService alertmanagerService,
    IPrometheusService prometheusService,
    IOptions<SonarHealthCheckConfiguration> sonarHealthConfig) {

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
    this._alertingDataHelper = alertingDataHelper;
    this._alertingConfigurationHelper = alertingConfigurationHelper;
    this._globalAlertingConfig = globalAlertingConfig;
    this._kubernetesApiAccessConfig = kubernetesApiAccessConfig;
    this._prometheusRemoteProtocolClient = prometheusRemoteProtocolClient;
    this._alertmanagerService = alertmanagerService;
    this._prometheusService = prometheusService;
    this._sonarHealthConfig = sonarHealthConfig.Value;
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

    var sonarHealthCheckTasks = new List<Task<ServiceHierarchyHealth>> {
      this.RunPostgresHealthCheck(cancellationToken),
      this.RunPrometheusSelfCheck(cancellationToken),
      this.RunAlertDeliveryHealthChecks(cancellationToken)
    };

    if (this._kubernetesApiAccessConfig.Value.IsEnabled) {
      sonarHealthCheckTasks.Add(this.RunAlertingConfigHealthCheck(cancellationToken));
    }

    return (await Task.WhenAll(sonarHealthCheckTasks)).ToList();
  }

  private async Task<ServiceHierarchyHealth> RunAlertDeliveryHealthChecks(CancellationToken cancellationToken) {
    var now = DateTime.UtcNow;

    var alwaysFiringAlertStatusTask = this._alertmanagerService.GetAlwaysFiringAlertStatusAsync(cancellationToken);
    var alertmanagerScrapeStatusTask = this._prometheusService.GetAlertmanagerScrapeStatusAsync(cancellationToken);
    var emailNotificationsStatusTask = this._prometheusService.GetAlertmanagerNotificationsStatusAsync(
      integration: "email",
      cancellationToken: cancellationToken);

    var healthChecks = new Dictionary<String, (DateTime, HealthStatus)?> {
      ["always-firing-alert"] = (now, await alwaysFiringAlertStatusTask),
      ["alertmanager-scraping"] = (now, await alertmanagerScrapeStatusTask),
      ["email-notifications"] = (now, await emailNotificationsStatusTask)
    };

    return new ServiceHierarchyHealth(
      name: "alert-delivery",
      displayName: "AlertDelivery",
      dashboardLink: this.BuildDashboardLink(
        servicePathQueue: ImmutableQueue.Create("alert-delivery"),
        environment: this._sonarHealthConfig.SonarEnvironment,
        tenant: TenantDataHelper.SonarTenantName),
      description: "The Prometheus and Alertmanager facilities used for SONAR service alert notification delivery.",
      timestamp: now,
      aggregateStatus: GetAggregateHealthStatus(healthChecks),
      healthChecks: healthChecks
    );
  }

  public static HealthStatus GetAggregateHealthStatus(IDictionary<String, (DateTime, HealthStatus)?> healthChecks) {
    return healthChecks.Aggregate(HealthStatus.Online, (aggregateStatus, healthCheck) => {
      var (_, maybeTimestampStatusTuple) = healthCheck;
      var (_, status) = maybeTimestampStatusTuple ?? (DateTime.UtcNow, HealthStatus.Unknown);
      return status.IsWorseThan(aggregateStatus) ? status : aggregateStatus;
    });
  }

  private async Task<ServiceHierarchyHealth> RunPostgresHealthCheck(CancellationToken cancellationToken) {
    var healthChecks = new Dictionary<String, (DateTime Timestamp, HealthStatus Status)?>();
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

    return new ServiceHierarchyHealth(
      name: "postgresql",
      displayName: "Postgresql",
      dashboardLink: this.BuildDashboardLink(
        servicePathQueue: ImmutableQueue.Create("postgresql"),
        environment: this._sonarHealthConfig.SonarEnvironment,
        tenant: TenantDataHelper.SonarTenantName),
      description: "The Postgresql instance that the SONAR API uses to persist service health information.",
      url: new Uri($"postgresql://{_dbConfig.Value.Host}:{_dbConfig.Value.Port}/{_dbConfig.Value.Database}"),
      timestamp: DateTime.UtcNow,
      aggregateStatus: GetAggregateHealthStatus(healthChecks),
      healthChecks: healthChecks
    );
  }

  private async Task<ServiceHierarchyHealth> RunPrometheusSelfCheck(CancellationToken cancellationToken) {
    var httpClient = new HttpClient();
    httpClient.BaseAddress = this._prometheusUrl;
    var healthChecks =
      new Dictionary<String, (DateTime Timestamp, HealthStatus Status)?>();
    var readinessTest = HealthStatus.Online;
    var writeTest = HealthStatus.Online;

    // perform readiness test
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
    } catch (Exception e) {
      // Unknown exception
      this._logger.LogError(
        message: "Unexpected HTTP error: {Message}",
        e.Message
      );
      readinessTest = HealthStatus.Unknown;
    }

    // perform write test
    Random rnd = new Random();
    var val = rnd.Next(0, Int32.MaxValue);
    var instance = new Guid();

    var writeData =
      new WriteRequest {
        Metadata = {
          new MetricMetadata {
            Help = "SONAR Prometheus Self Check.",
            Type = MetricMetadata.Types.MetricType.Stateset,
            MetricFamilyName = HealthDataHelper.PrometheusSelfCheckName
          }
        }
      };

    writeData.Timeseries.Add(CreatePrometheusSelfCheckMetric(
      PrometheusWriteTestMetricName,
      instance,
      DateTime.UtcNow,
      val));

    try {
      var prometheusTask = this._prometheusRemoteProtocolClient
        .WriteAsync(writeData, cancellationToken);
    } catch (Exception e) {
      // Unknown exception
      this._logger.LogError(
        message: "Unexpected HTTP error: {Message}",
        e.Message
      );
      writeTest = HealthStatus.Unknown;
    }

    // query test
    var response = await this._prometheusClient.QueryAsync(
      $"{PrometheusWriteTestMetricName}{{instance=\"{instance}\"}}",
      DateTime.UtcNow,
      cancellationToken: cancellationToken
    );

    var queryTest = PerformPrometheusQueryTest(response, instance.ToString(), val);

    healthChecks.Add("readiness-probe", (DateTime.UtcNow, readinessTest));
    healthChecks.Add("write-test", (DateTime.UtcNow, writeTest));
    healthChecks.Add("test-query", (DateTime.UtcNow, queryTest));

    return new ServiceHierarchyHealth(
      name: "prometheus",
      displayName: "Prometheus",
      dashboardLink: this.BuildDashboardLink(
        servicePathQueue: ImmutableQueue.Create("prometheus"),
        environment: this._sonarHealthConfig.SonarEnvironment,
        tenant: TenantDataHelper.SonarTenantName),
      description: "The Prometheus instance that the SONAR API uses to persist service health information.",
      url: this._prometheusUrl,
      timestamp: DateTime.UtcNow,
      aggregateStatus: GetAggregateHealthStatus(healthChecks),
      healthChecks: healthChecks
    );
  }

  private static HealthStatus PerformPrometheusQueryTest(
    ResponseEnvelope<QueryResults>? response,
    String instanceValue,
    Int32 testValue) {

    // check if response is not present/unsuccessful status code
    if (response == null ||
      response.Status != ResponseStatus.Success ||
      response.Data == null) {

      return HealthStatus.Unknown;
    }

    var data = response.Data.Result.FirstOrDefault();
    if (data == null) {
      return HealthStatus.Unknown;
    }

    // test instance label
    if (!data.Labels.TryGetValue("instance", out var val) ||
      val != instanceValue) {
      return HealthStatus.Unknown;
    }

    // test value
    var value = data.Value;
    if (value != null && testValue != Int32.Parse(value.Value.Value)) {
      return HealthStatus.Unknown;
    }

    return HealthStatus.Online;
  }

  private static TimeSeries CreatePrometheusSelfCheckMetric(
    String metricName,
    Guid instance,
    DateTime timestamp,
    Int32 value) {

    var timestampMs = (Int64)timestamp.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMilliseconds;

    var ts = new TimeSeries {
      Labels = {
        new Label { Name = "__name__", Value = metricName },
        new Label { Name = "instance", Value = instance.ToString() },
      },
      Samples = {
        new Sample {
          Timestamp = timestampMs,
          Value = value
        }
      }
    };

    return ts;
  }

  public async Task<Dictionary<String, (DateTime Timestamp, HealthStatus Status)>> GetHealthCheckResultForService(
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

  public async Task<List<(DateTime Timestamp, HealthStatus Value)>> GetHealthCheckResultsForService(
  String environment,
  String tenant,
  String service,
  String healthCheck,
  DateTime start,
  DateTime end,
  Int32 step,
  CancellationToken cancellationToken) {

    // Example query:
    // sonar_healthcheck_data{environment='foo',tenant='baz',service='test-metric-app',check='example'}[100s]
    var query = String.Format(
      format: "max_over_time({0}{{environment='{1}',tenant='{2}',service='{3}',check='{4}'}}[{5}])",
      HealthDataHelper.ServiceHealthCheckMetricName,
      environment,
      tenant,
      service,
      healthCheck,
      PrometheusClient.ToPrometheusDuration(TimeSpan.FromSeconds(step)));

    return await this._prometheusQueryHelper.GetPrometheusQueryRangeValue(
      "health check history",
      query,
      start,
      end,
      TimeSpan.FromSeconds(step),
      processResult: results => {
        // StateSet metrics are split into separate metric per-state
        // This code groups all the metrics for a given service and then determines which state is currently set.
        var metricByService = //TODO rename Healthcheck
          results.Result
            .Where(series => series.Values != null && series.Values.Any())
            .Select(series => (series.Labels, series.Values!.ToList()))
            .ToLookup(
              keySelector: metric =>
                metric.Labels.TryGetValue(MetricLabelKeys.HealthCheck, out var healthCheckName) ?
                  healthCheckName :
                  null, StringComparer.OrdinalIgnoreCase);

        var statusList = new List<(DateTime, HealthStatus)>();
        // Iterate though the services grouping and create a time series list of each health status.
        foreach (var svc in metricByService) {
          if (svc.Key is null) {
            throw new InvalidOperationException("The time series for health status is missing the service label");
          }

          // group metrics by timestamp to determine worst status at that particular time
          // resulting data structure will look like:
          //  timestamp: [ { labels, value }, { labels, value } ]
          var r = svc.SelectMany(e => e.Item2.Select(timestampValTuple => new { e.Labels, timestampValTuple }))
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
              if (!metric.Labels.TryGetValue(ServiceHealthCheckMetricName,
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
        }
        return statusList;
      },
      cancellationToken);
  }

  private DateTime ConvertDecimalTimestampToDateTime(Decimal timestamp) {
    return DateTime.UnixEpoch.AddMilliseconds((Int64)(timestamp * 1000));
  }

  public async Task<ServiceHierarchyHealth> RunAlertingConfigHealthCheck(
    CancellationToken cancellationToken) {
    var currentTimestamp = DateTime.UtcNow;

    // Fetch latest AlertingConfigurationVersion from the database
    var latestAlertingConfigVersion = await this._alertingDataHelper
      .FetchLatestAlertingConfigVersionAsync(cancellationToken);

    // Fetch all ConfigMaps and Secrets generated for alerting
    var existingAlertmanagerConfigMap = await this._alertingConfigurationHelper
      .FetchAlertingConfigMap(cancellationToken);
    var existingPrometheusAlertingRulesConfigMap = await this._alertingConfigurationHelper
      .FetchPrometheusAlertingRulesConfigMap(cancellationToken);
    var existingAlertmanagerSecret = await this._alertingConfigurationHelper
      .FetchAlertmanagerSecret(cancellationToken);

    var healthChecks = this.GetAlertingConfigSyncStatusAsync(
      currentTimestamp,
      latestAlertingConfigVersion,
      existingAlertmanagerConfigMap,
      existingPrometheusAlertingRulesConfigMap,
      existingAlertmanagerSecret);

    return new ServiceHierarchyHealth(
      name: "alertingconfig",
      displayName: "AlertingConfig",
      dashboardLink: this.BuildDashboardLink(
        servicePathQueue: ImmutableQueue.Create("alertingconfig"),
        environment: this._sonarHealthConfig.SonarEnvironment,
        tenant: TenantDataHelper.SonarTenantName),
      description: "The Kubernetes resources that SONAR API uses to persist Alertmanager configuration " +
      "for recipients and Prometheus rules.",
      timestamp: DateTime.UtcNow,
      aggregateStatus: GetAggregateHealthStatus(healthChecks),
      healthChecks: healthChecks);
  }

  public Dictionary<String, (DateTime Timestamp, HealthStatus Status)?> GetAlertingConfigSyncStatusAsync(
    DateTime currentTimestamp,
    AlertingConfigurationVersion latestAlertingConfigVersion,
    V1ConfigMap? alertmanagerConfigMap,
    V1ConfigMap? prometheusAlertingRulesConfigMap,
    V1Secret? alertmanagerSecret) {

    // Get health status of each alerting Kubernetes resource
    var alertmanagerConfigResult = (alertmanagerConfigMap == null) ?
      HealthStatus.Offline :
      this.GetAlertingConfigHealthCheckStatus(
        currentTimestamp,
        latestAlertingConfigVersion,
        alertmanagerConfigMap.Metadata.Annotations);

    var prometheusRulesResult = (prometheusAlertingRulesConfigMap == null) ?
      HealthStatus.Offline :
      this.GetAlertingConfigHealthCheckStatus(
        currentTimestamp,
        latestAlertingConfigVersion,
        prometheusAlertingRulesConfigMap.Metadata.Annotations);

    var alertmanagerSecretResult = (alertmanagerSecret == null) ?
      HealthStatus.Offline :
      this.GetAlertingConfigHealthCheckStatus(
        currentTimestamp,
        latestAlertingConfigVersion,
        alertmanagerSecret.Metadata.Annotations);

    var healthChecks = new Dictionary<String, (DateTime Timestamp, HealthStatus Status)?>();
    healthChecks.Add("alertmanager-config", (DateTime.UtcNow, alertmanagerConfigResult));
    healthChecks.Add("prometheus-rules", (DateTime.UtcNow, prometheusRulesResult));
    healthChecks.Add("alertmanager-secret", (DateTime.UtcNow, alertmanagerSecretResult));

    return healthChecks;
  }

  private HealthStatus GetAlertingConfigHealthCheckStatus(
    DateTime currentTimestamp,
    AlertingConfigurationVersion latestAlertingConfigVersion,
    IDictionary<String, String>? annotationsSection) {

    var lastPossibleAlertingConfigSyncTimestamp = currentTimestamp
      .Subtract(new TimeSpan(0, 0, this._globalAlertingConfig.Value.ConfigSyncIntervalSeconds));
    var latestAlertingConfigVersionTimestamp = latestAlertingConfigVersion.Timestamp;

    // Compare the versionNumber annotation on each Kubernetes resource with the version read from the database
    var latestAlertingConfigVersionNumber = latestAlertingConfigVersion.VersionNumber;
    var alertingKubernetesResourceVersion = this._alertingConfigurationHelper
      .GetAlertingKubernetesResourceVersionInt(annotationsSection);

    if (alertingKubernetesResourceVersion < latestAlertingConfigVersionNumber) {
      // Check which timestamp is more recent
      if (latestAlertingConfigVersionTimestamp > lastPossibleAlertingConfigSyncTimestamp) {
        return HealthStatus.AtRisk;
      } else if (latestAlertingConfigVersionTimestamp < lastPossibleAlertingConfigSyncTimestamp) {
        return HealthStatus.Degraded;
      }
    }

    return HealthStatus.Online;
  }

  public class MetricLabelKeys {
    public const String Environment = "environment";
    public const String Tenant = "tenant";
    public const String Service = "service";
    public const String HealthCheck = "check";
  }
}
