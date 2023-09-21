using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Configuration;
using Cms.BatCave.Sonar.Agent.HealthChecks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
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
  private readonly ErrorReportsHelper _errorReportsHelper;

  public HealthCheckHelper(
    ILoggerFactory loggerFactory,
    IOptions<ApiConfiguration> apiConfig,
    INotifyOptionsChanged<AgentConfiguration> agentConfig,
    HealthCheckQueueProcessor<HttpHealthCheckDefinition> httpHealthCheckQueue,
    HealthCheckQueueProcessor<MetricHealthCheckDefinition> prometheusHealthCheckQueue,
    HealthCheckQueueProcessor<MetricHealthCheckDefinition> lokiHealthCheckQueue,
    ErrorReportsHelper errorReportsHelper) {
    this._loggerFactory = loggerFactory;
    this._logger = loggerFactory.CreateLogger<HealthCheckHelper>();
    this._apiConfig = apiConfig;
    this._agentConfig = agentConfig;
    this._httpHealthCheckQueue = httpHealthCheckQueue;
    this._prometheusHealthCheckQueue = prometheusHealthCheckQueue;
    this._lokiHealthCheckQueue = lokiHealthCheckQueue;
    this._errorReportsHelper = errorReportsHelper;
  }

  public async Task RunScheduledHealthCheck(
    String tenant,
    CancellationToken token) {

    // TODO: Enable automatic update of SONAR client settings upon configuration file change(s)

    // SONAR client
    using var sonarHttpClient = new HttpClient();
    sonarHttpClient.Timeout = TimeSpan.FromSeconds(this._agentConfig.Value.AgentInterval);
    var client = new SonarClient(this._apiConfig, sonarHttpClient);

    // stateful DS for data smoothing
    var dataSmoothingState = new Dictionary<
      (String Service, String HealthCheck),
      (HealthStatus CachedStatus, HealthStatus OutlierStatus, Int32? Frequency)>();

    while (!token.IsCancellationRequested) {
      // Configs
      var env = this._apiConfig.Value.Environment;
      var interval = TimeSpan.FromSeconds(this._agentConfig.Value.AgentInterval);
      var startTime = DateTime.UtcNow;

      // Get service hierarchy for given env and tenant
      ServiceHierarchyConfiguration? tenantResult = null;
      try {
        tenantResult = await client.GetTenantAsync(env, tenant, token);
      } catch (ApiException ex) {
        if (ex.StatusCode == 404) {
          //tenant no longer exists, breaking out of loop and exiting
          this._logger.LogInformation(
            message: "SONAR API reports Tenant {Tenant} does not exist, health check worker exiting",
            tenant
          );
          break;
        }

        var errMessage = $"SONAR API reports Tenant {tenant} an API error has occurred {ex.StatusCode} {ex.Message}";
        this._logger.LogError(errMessage);
        await this._errorReportsHelper.CreateErrorReport(
          env,
          new ErrorReportDetails(
            DateTime.UtcNow,
            tenant,
            null,
            null,
            AgentErrorLevel.Error,
            AgentErrorType.Execution,
            errMessage,
            null,
            null
          ),
          token);
      } catch (HttpRequestException ex) {
        var errMessage =
          $"A network error occurred attempting to get tenant information {tenant} from SONAR API: {ex.Message}";
        this._logger.LogError(errMessage);
        await this._errorReportsHelper.CreateErrorReport(
          env,
          new ErrorReportDetails(
            DateTime.UtcNow,
            tenant,
            null,
            null,
            AgentErrorLevel.Error,
            AgentErrorType.Execution,
            errMessage,
            null,
            null
          ),
          token);
      } catch (TaskCanceledException ex) {
        var errMessage =
          $"HTTP request timed out attempting get tenant information {tenant} from SONAR API: {ex.Message}";
        this._logger.LogError(errMessage);
        await this._errorReportsHelper.CreateErrorReport(
          env,
          new ErrorReportDetails(
            DateTime.UtcNow,
            tenant,
            null,
            null,
            AgentErrorLevel.Error,
            AgentErrorType.Execution,
            errMessage,
            null,
            null
          ),
          token);
      }

      var pendingHealthChecks = new List<(
        String Service,
        String HealthCheck,
        Task<HealthStatus> Status,
        Int16? smoothingTolerance)>();

      // Iterate over each service
      if (tenantResult != null) {
        this._logger.LogDebug("Service Count: {ServiceCount}", tenantResult.Services.Count);
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
                await this._errorReportsHelper.CreateErrorReport(env,
                  new ErrorReportDetails(
                    DateTime.UtcNow,
                    tenant,
                    service.Name,
                    healthCheck.Type.ToString(),
                    AgentErrorLevel.Error,
                    AgentErrorType.Execution,
                    $"Healthcheck Type {healthCheck.Type} is not supported.",
                    null,
                    null
                  ),
                  token);
                throw new NotSupportedException("Healthcheck Type is not supported.");
            }

            pendingHealthChecks.Add((service.Name, healthCheck.Name, futureStatus, healthCheck.SmoothingTolerance));
          }
        }
      }

      // Note: currently we wait for all health checks to complete and then send status for each
      // service one at a time to the SONAR API. This could be improved upon by waiting for each
      // service's health checks to complete in parallel and reporting service status as results
      // become available.
      var healthCheckResults = new List<(String Service, String HealthCheck, HealthStatus Status, Int16? smoothingTolerance)>();
      foreach (var (service, check, futureStatus, smoothingTolerance) in pendingHealthChecks) {
        healthCheckResults.Add(
          (service, check, await futureStatus, smoothingTolerance)
        );
      }

      // Keep track of current service health checks for pruning
      var currentServiceHealthChecks = new List<(String Service, String HealthCheck)>();
      foreach (var group in healthCheckResults.GroupBy(r => r.Service)) {
        // Initialize aggStatus to null
        HealthStatus? aggStatus = null;
        var checkResults = new Dictionary<String, HealthStatus>();
        foreach (var (_, check, status, smoothingTolerance) in group) {
          // If currCheck is Unknown or currCheck is worse than aggStatus (as long as aggStatus is not Unknown)
          // set aggStatus to currCheck
          var currentStatus = status;
          // If configuration specifies that data smoothing is enabled, get smoothed status
          if (smoothingTolerance != null && smoothingTolerance > 0) {
            currentStatus = PerformDataSmoothing(
              dataSmoothingState,
              group.Key,
              check,
              status,
              (Int16)smoothingTolerance);
          }

          if ((currentStatus == HealthStatus.Unknown) ||
            ((aggStatus != HealthStatus.Unknown) && (currentStatus > (aggStatus ?? 0)))) {
            aggStatus = currentStatus;
          }

          // Add service/check to dictionary
          currentServiceHealthChecks.Add((group.Key, check));

          // Set checkResults
          checkResults.Add(check, currentStatus);
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

      // Smoothing state cleanup, remove stale service/healthcheck entries
      dataSmoothingState.Keys.Except(currentServiceHealthChecks).ToList().ForEach(k => {
        dataSmoothingState.Remove(k);
      });

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
      var errMessage = $"Failed to send status data to SONAR API, Code: {e.StatusCode}, Message: {e.Message}";
      this._logger.LogError(errMessage);
      await this._errorReportsHelper.CreateErrorReport(
        env,
        new ErrorReportDetails(
          DateTime.UtcNow,
          tenant,
          service,
          null,
          AgentErrorLevel.Error,
          AgentErrorType.Execution,
          errMessage,
          null,
          null
        ),
        token);
    } catch (HttpRequestException ex) {
      var errMessage =
        $"A network error occurred attempting to record status data for {env} {tenant} in SONAR API: {ex.Message}";
      this._logger.LogError(errMessage);
      await this._errorReportsHelper.CreateErrorReport(
        env,
        new ErrorReportDetails(
          DateTime.UtcNow,
          tenant,
          service,
          null,
          AgentErrorLevel.Error,
          AgentErrorType.Execution,
          errMessage,
          null,
          null
        ),
        token);
    } catch (TaskCanceledException ex) {
      //First check to make sure this is not a local cancellation
      String errMessage;
      if (token.IsCancellationRequested) {
        errMessage =
          $"Local client has cancelled the request to record status data for {env} {tenant} in SONAR API: {ex.Message}";
      } else {
        errMessage =
          $"HTTP request timed out attempting to record status data for {env} {tenant} in SONAR API: {ex.Message}";
      }
      this._logger.LogError(errMessage);
      // create error report
      await this._errorReportsHelper.CreateErrorReport(
        env,
        new ErrorReportDetails(
          DateTime.UtcNow,
          tenant,
          service,
          null,
          AgentErrorLevel.Error,
          AgentErrorType.Execution,
          errMessage,
          null,
          null
        ),
        token);
    }
  }

  private HealthStatus PerformDataSmoothing(
    Dictionary<
      (String Service, String HealthCheck),
      (HealthStatus CachedStatus, HealthStatus OutlierStatus, Int32? Frequency)
    > smoothingRecord,
    String service,
    String check,
    HealthStatus currStatus,
    Int16 tolerance) {

    var key = (service, check);

    // no entry exists for Service/HealthCheck tuple, create new entry
    if (!smoothingRecord.TryGetValue(key, out var entry)) {
      smoothingRecord.Add(key, (currStatus, currStatus, null));
      return currStatus;
    }

    // current status is same as previous, return current status
    if (entry.CachedStatus == currStatus) {
      return currStatus;
    }

    // current status is different than cached status, determine if smoothing is needed
    if (entry.Frequency != null && (entry.Frequency + 1) > tolerance) {
      // threshold exceeded, remove and return current status
      smoothingRecord.Remove(key);
      return currStatus;
    }

    // threshold not met, update entry and perform smoothing, return current status
    smoothingRecord[key] = (
      entry.CachedStatus,
      currStatus,
      entry.Frequency == null ?
        1 :
        entry.Frequency + 1);
    return entry.CachedStatus;
  }
}
