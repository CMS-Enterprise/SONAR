using System;
using System.Collections.Generic;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Agent;

public class HealthCheckHelper {
  private readonly ILoggerFactory _loggerFactory;
  private readonly ILogger<HealthCheckHelper> _logger;
  private readonly IOptions<ApiConfiguration> _apiConfig;
  private readonly INotifyOptionsChanged<AgentConfiguration> _agentConfig;
  private readonly HealthCheckQueueProcessor<HttpHealthCheckDefinition> _httpHealthCheckQueue;
  private readonly HealthCheckQueueProcessor<MetricHealthCheckDefinition> _prometheusHealthCheckQueue;
  private readonly HealthCheckQueueProcessor<MetricHealthCheckDefinition> _lokiHealthCheckQueue;

  public HealthCheckHelper(
    ILoggerFactory loggerFactory,
    IOptions<ApiConfiguration> apiConfig,
    INotifyOptionsChanged<AgentConfiguration> agentConfig,
    HealthCheckQueueProcessor<HttpHealthCheckDefinition> httpHealthCheckQueue,
    HealthCheckQueueProcessor<MetricHealthCheckDefinition> prometheusHealthCheckQueue,
    HealthCheckQueueProcessor<MetricHealthCheckDefinition> lokiHealthCheckQueue) {
    this._loggerFactory = loggerFactory;
    this._logger = loggerFactory.CreateLogger<HealthCheckHelper>();
    this._apiConfig = apiConfig;
    this._agentConfig = agentConfig;
    this._httpHealthCheckQueue = httpHealthCheckQueue;
    this._prometheusHealthCheckQueue = prometheusHealthCheckQueue;
    this._lokiHealthCheckQueue = lokiHealthCheckQueue;
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

    while (!token.IsCancellationRequested) {
      // Configs
      var env = this._apiConfig.Value.Environment;
      var interval = TimeSpan.FromSeconds(this._agentConfig.Value.AgentInterval);
      var startTime = DateTime.UtcNow;

      // Get service hierarchy for given env and tenant
      ServiceHierarchyConfiguration? tenantResult = null;
      try {
        tenantResult = await client.GetTenantAsync(env, tenant, token);
      } catch (ApiException e) {
        if (tenantResult == null) {
          Console.WriteLine($"Tenant {tenant} does not exist. Health check run is exiting.");
          break;
        }
      }

      this._logger.LogDebug("Service Count: {ServiceCount}", tenantResult.Services.Count);

      var pendingHealthChecks = new List<(String Service, String HealthCheck, Task<HealthStatus> Status)>();
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
          Task<HealthStatus> futureStatus;
          var healthCheckId = new HealthCheckIdentifier(env, tenant, service.Name, healthCheck.Name);

          switch (healthCheck.Type) {
            case HealthCheckType.PrometheusMetric:
              futureStatus = this._prometheusHealthCheckQueue.QueueHealthCheck(
                tenant,
                healthCheckId,
                (MetricHealthCheckDefinition)healthCheck.Definition
              );
              break;
            case HealthCheckType.LokiMetric:
              futureStatus = this._lokiHealthCheckQueue.QueueHealthCheck(
                tenant,
                healthCheckId,
                (MetricHealthCheckDefinition)healthCheck.Definition
              );
              break;
            case HealthCheckType.HttpRequest:
              futureStatus = this._httpHealthCheckQueue.QueueHealthCheck(
                tenant,
                healthCheckId,
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
