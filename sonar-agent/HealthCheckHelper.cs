using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Configuration;
using Cms.BatCave.Sonar.Agent.HealthChecks;
using Cms.BatCave.Sonar.Agent.HealthChecks.Metrics;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Loki;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Prometheus;
using Cms.BatCave.Sonar.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Agent;

public class HealthCheckHelper {
  private static readonly
    Dictionary<(String serviceName, String healthCheck), IImmutableList<(Decimal Timestamp, String Value)>>
    Cache = new();

  private static readonly HttpHealthCheckCondition DefaultStatusCodeCondition = new StatusCodeCondition(
    new UInt16[] { 200, 204 },
    HealthStatus.Online
  );

  private readonly ILoggerFactory _loggerFactory;
  private readonly ILogger<HealthCheckHelper> _logger;
  private readonly IOptions<ApiConfiguration> _apiConfig;
  private readonly IOptions<PrometheusConfiguration> _promConfig;
  private readonly IOptions<LokiConfiguration> _lokiConfig;
  private readonly INotifyOptionsChanged<AgentConfiguration> _agentConfig;

  public HealthCheckHelper(
    ILoggerFactory loggerFactory,
    IOptions<ApiConfiguration> apiConfig,
    IOptions<PrometheusConfiguration> promConfig,
    IOptions<LokiConfiguration> lokiConfig,
    INotifyOptionsChanged<AgentConfiguration> agentConfig) {
    this._loggerFactory = loggerFactory;
    this._logger = loggerFactory.CreateLogger<HealthCheckHelper>();
    this._apiConfig = apiConfig;
    this._promConfig = promConfig;
    this._lokiConfig = lokiConfig;
    this._agentConfig = agentConfig;
  }

  public async Task RunScheduledHealthCheck(
    IConfigurationRoot configRoot,
    String tenant,
    CancellationToken token) {

    // TODO: Enable automatic update of SONAR client settings upon configuration file change(s)

    // SONAR client
    using var sonarHttpClient = new HttpClient();
    sonarHttpClient.Timeout = TimeSpan.FromSeconds(this._agentConfig.Value.AgentInterval);
    var client = new SonarClient(configRoot, baseUrl: this._apiConfig.Value.BaseUrl, sonarHttpClient);
    await client.ReadyAsync(token);

    // Prometheus client
    HttpClient CreatePrometheusHttpClient() {
      var promHttpClient = new HttpClient();
      promHttpClient.Timeout = TimeSpan.FromSeconds(this._agentConfig.Value.AgentInterval);
      promHttpClient.BaseAddress = new Uri(
        $"{this._promConfig.Value.Protocol}://{this._promConfig.Value.Host}:{this._promConfig.Value.Port}/");
      return promHttpClient;
    }

    var promClient = new PrometheusClient(CreatePrometheusHttpClient);

    // Loki Client
    HttpClient CreateLokiHttpClient() {
      var lokiHttpClient = new HttpClient();
      lokiHttpClient.Timeout = TimeSpan.FromSeconds(this._agentConfig.Value.AgentInterval);
      lokiHttpClient.BaseAddress = new Uri(
        $"{this._lokiConfig.Value.Protocol}://{this._lokiConfig.Value.Host}:{this._lokiConfig.Value.Port}/");
      return lokiHttpClient;
    }

    var lokiClient = new LokiClient(CreateLokiHttpClient);

    using var httpHealthCheckQueue = new HealthCheckQueueProcessor<HttpHealthCheckDefinition>(
      new HttpHealthCheckEvaluator(
        this._agentConfig,
        this._loggerFactory.CreateLogger<HttpHealthCheckEvaluator>()),
      this._agentConfig
    );

    using var prometheusHealthCheckQueue = new HealthCheckQueueProcessor<MetricHealthCheckDefinition>(
      new MetricHealthCheckEvaluator(
        new CachingMetricQueryRunner(
          new PrometheusMetricQueryRunner(
            promClient,
            this._loggerFactory.CreateLogger<PrometheusMetricQueryRunner>())),
        this._loggerFactory.CreateLogger<MetricHealthCheckEvaluator>()
      ),
      this._agentConfig
    );

    using var lokiHealthCheckQueue = new HealthCheckQueueProcessor<MetricHealthCheckDefinition>(
      new MetricHealthCheckEvaluator(
        new CachingMetricQueryRunner(
          new LokiMetricQueryRunner(
            lokiClient,
            this._loggerFactory.CreateLogger<LokiMetricQueryRunner>())),
        this._loggerFactory.CreateLogger<MetricHealthCheckEvaluator>()
      ),
      this._agentConfig
    );

    using var httpQueueProcessor = httpHealthCheckQueue.Run(token);
    using var prometheusQueueProcessor = prometheusHealthCheckQueue.Run(token);
    using var lokQueueProcessor = lokiHealthCheckQueue.Run(token);

    while (!token.IsCancellationRequested) {
      // Configs
      var env = this._apiConfig.Value.Environment;
      var interval = TimeSpan.FromSeconds(this._agentConfig.Value.AgentInterval);
      var startTime = DateTime.UtcNow;

      // Get service hierarchy for given env and tenant
      var tenantResult = await client.GetTenantAsync(env, tenant, token);
      this._logger.LogDebug("Service Count: {ServiceCount}", tenantResult.Services.Count);

      var pendingHealthChecks = new List<(String Service, String HealthCheck, Future<HealthStatus> Status)>();
      // Iterate over each service
      foreach (var service in tenantResult.Services) {
        // Get service's health checks here
        var healthChecks = service.HealthChecks;
        // If no checks are returned, continue
        if ((healthChecks == null) || (healthChecks.Count == 0)) {
          if ((service.Children == null) || (service.Children.Count == 0)) {
            // This service serves no purpose in configuration
            this._logger.LogWarning(
              "No Health Checks or child services associated with {ServiceName}",
              service.Name
            );
          } else {
            this._logger.LogDebug("No Health Checks associated with service {ServiceName}", service.Name);
          }

          continue;
        }

        foreach (var healthCheck in healthChecks) {
          Future<HealthStatus> futureStatus;
          switch (healthCheck.Type) {
            case HealthCheckType.PrometheusMetric:
              futureStatus = prometheusHealthCheckQueue.QueueHealthCheck(
                tenant,
                $"{service.Name}/{healthCheck.Name}",
                (MetricHealthCheckDefinition)healthCheck.Definition
              );
              break;
            case HealthCheckType.LokiMetric:
              futureStatus = lokiHealthCheckQueue.QueueHealthCheck(
                tenant,
                $"{service.Name}/{healthCheck.Name}",
                (MetricHealthCheckDefinition)healthCheck.Definition
              );
              break;
            case HealthCheckType.HttpRequest:
              futureStatus = httpHealthCheckQueue.QueueHealthCheck(
                tenant,
                $"{service.Name}/{healthCheck.Name}",
                (HttpHealthCheckDefinition)healthCheck.Definition
              );
              break;
            default:
              throw new NotSupportedException("Healthcheck Type is not supported.");
          }

          pendingHealthChecks.Add((service.Name, healthCheck.Name, futureStatus));
        }
      }

      // Note: currently we wait for all health checks to complete and then send status for each
      // service one at a time to the SONAR API. This could be improved upon by waiting for each
      // service's health checks to complete in parallel and reporting service status as results
      // become available.
      var healthCheckResults = new List<(String Service, String HealthCheck, HealthStatus Status)>();
      foreach (var (service, check, futureStatus) in pendingHealthChecks) {
        healthCheckResults.Add(
          (service, check, await futureStatus)
        );
      }

      foreach (var group in healthCheckResults.GroupBy(r => r.Service)) {
        // Initialize aggStatus to null
        HealthStatus? aggStatus = null;
        var checkResults = new Dictionary<String, HealthStatus>();
        foreach (var (_, check, status) in group) {
          // If currCheck is Unknown or currCheck is worse than aggStatus (as long as aggStatus is not Unknown)
          // set aggStatus to currCheck
          if ((status == HealthStatus.Unknown) ||
            ((aggStatus != HealthStatus.Unknown) && (status > (aggStatus ?? 0)))) {
            aggStatus = status;
          }

          // Set checkResults
          checkResults.Add(check, status);
        }

        // Send result data to SONAR API
        if (aggStatus != null) {
          await this.SendHealthData(
            env,
            tenant,
            group.Key,
            checkResults,
            client,
            aggStatus.Value,
            token
          );
        }
      }

      var elapsed = DateTime.UtcNow.Subtract(startTime);
      if (elapsed > interval) {
        this._logger.LogWarning(
          "Performing health checks took longer than the allotted interval (Interval: {Interval}, Elapsed Time: {Elapsed})",
          interval,
          elapsed
        );
      } else {
        await Task.Delay(interval.Subtract(elapsed), token);
      }
    }
  }

  private async Task SendHealthData(
    String env, String tenant, String service,
    Dictionary<String, HealthStatus> results,
    SonarClient client,
    HealthStatus aggStatus,
    CancellationToken token) {

    var ts = DateTime.UtcNow;
    var healthChecks = new ReadOnlyDictionary<String, HealthStatus>(results);
    var body = new ServiceHealth(ts, aggStatus, healthChecks);

    this._logger.LogInformation(
      "Env: {Environment}, Tenant: {Tenant}, Service: {Service}, Time: {Timestamp}, AggStatus: {AggregateStatus}",
      env,
      tenant,
      service,
      body.Timestamp,
      body.AggregateStatus
    );

    try {
      await client.RecordStatusAsync(env, tenant, service, body, token);
    } catch (ApiException e) {
      this._logger.LogError(
        e,
        "Failed to send status data to SONAR API, Code: {StatusCode}, Message: {Message}",
        e.StatusCode,
        e.Message
      );
    }
  }
}
