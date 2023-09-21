using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Configuration;
using Cms.BatCave.Sonar.Agent.VersionChecks;
using Cms.BatCave.Sonar.Agent.VersionChecks.Models;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Agent;

public class VersionCheckHelper {

  private readonly ILogger<VersionCheckHelper> _logger;
  private readonly IOptions<AgentConfiguration> _agentConfig;
  private readonly IOptions<ApiConfiguration> _apiConfig;
  private readonly VersionCheckQueueProcessor _queueProcessor;
  private readonly ISonarClient _sonarClient;

  public VersionCheckHelper(
    ILogger<VersionCheckHelper> logger,
    IOptions<AgentConfiguration> agentConfig,
    IOptions<ApiConfiguration> apiConfig,
    VersionCheckQueueProcessor queueProcessor,
    ISonarClient sonarClient) {

    this._logger = logger;
    this._agentConfig = agentConfig;
    this._apiConfig = apiConfig;
    this._queueProcessor = queueProcessor;
    this._sonarClient = sonarClient;
  }

  public async Task RunScheduledVersionChecks(
    String tenant,
    CancellationTokenSource cancellationSource,
    CancellationToken cancellationToken) {

    try {
      var environment = this._apiConfig.Value.Environment;

      while (!cancellationToken.IsCancellationRequested) {
        var interval = TimeSpan.FromSeconds(this._agentConfig.Value.AgentInterval);
        var startTime = DateTime.UtcNow;

        ServiceHierarchyConfiguration? tenantConfig = null;

        try {
          tenantConfig = await this._sonarClient.GetTenantAsync(environment, tenant, cancellationToken);
        } catch (ApiException e) when (e is { StatusCode: 404 }) {
          this._logger.LogInformation(
            message: "SONAR API reports Tenant {Tenant} does not exist, version check worker exiting",
            tenant);
          break;
        } catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken) {
          // Don't log if cancellation was requested
          throw;
        } catch (Exception e) {
          this._logger.LogError(
            exception: e,
            message: "Failed to retrieve service hierarchy configuration for Tenant: {tenant}",
            tenant);
        }

        if (tenantConfig != null) {
          var completedChecks = await this.RequestVersions(tenant, tenantConfig);
          await this.RecordVersions(environment, tenant, completedChecks);
        }

        var elapsed = DateTime.UtcNow - startTime;
        if (elapsed < interval) {
          try {
            await Task.Delay(interval - elapsed, cancellationToken);
          } catch (TaskCanceledException) {
            // ignore
          }
        } else {
          this._logger.LogWarning(
            message:
            "Performing version checks took longer than the allotted interval (Interval: {Interval}, Elapsed Time: {Elapsed})",
            interval,
            elapsed);
        }
      }
    } catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken) {
      // Cancellation has already been requested
      throw;
    } catch (Exception) {
      // Signal the main thread that we need to shut down due to an unhandled exception
      cancellationSource.Cancel();
      throw;
    }
  }

  private async Task<IEnumerable<CompletedServiceVersionCheck>> RequestVersions(
    String tenant,
    ServiceHierarchyConfiguration tenantConfig) {

    this._logger.LogDebug(message: "Requesting service versions for Tenant: {tenant}, Time: {now}", tenant,
      DateTime.UtcNow);

    var checks = tenantConfig.Services.SelectMany(service =>
        service.VersionChecks?.Select(model =>
          new PendingServiceVersionCheck {
            Service = service.Name,
            Model = model,
            Task = this._queueProcessor.QueueVersionCheck(tenant, model)
          }) ?? ImmutableList<PendingServiceVersionCheck>.Empty)
      .ToImmutableList();

    foreach (var check in checks) {
      try {
        await check.Task;

        this._logger.LogInformation(
          message: "Tenant: {tenant}, Service: {check.Service}, Version: {version}, Time: {now}",
          tenant,
          check.Service,
          check.Task.Result,
          DateTime.UtcNow);
      } catch (AggregateException e) {
        foreach (var eInnerException in e.InnerExceptions) {
          LogVersionRequestFailedError(check, eInnerException);
        }
      } catch (Exception e) {
        LogVersionRequestFailedError(check, e);
      }
    }

    void LogVersionRequestFailedError(ServiceVersionCheck check, Exception e) {
      this._logger.LogError(
        exception: e,
        message: "Version request failed for Tenant: {tenant}, Service: {service}, VersionCheckType: {type}",
        tenant,
        check.Service,
        check.Model.VersionCheckType);
    }

    return checks.Where(check => check.Task.IsCompletedSuccessfully)
      .Select(check => new CompletedServiceVersionCheck {
        Service = check.Service,
        Model = check.Model,
        Response = check.Task.Result
      });
  }

  private async Task RecordVersions(
    String environment,
    String tenant,
    IEnumerable<CompletedServiceVersionCheck> completedChecks) {

    foreach (var serviceGrouping in completedChecks.GroupBy(check => check.Service)) {
      var service = serviceGrouping.Key;
      var serviceVersion = new ServiceVersion(
        timestamp: DateTime.UtcNow,
        versionChecks: serviceGrouping.ToImmutableDictionary(
          keySelector: check => check.Model.VersionCheckType,
          elementSelector: check => check.Response.Version));

      try {
        await this._sonarClient.RecordServiceVersionAsync(environment, tenant, service, serviceVersion);
      } catch (Exception e) {
        this._logger.LogError(
          exception: e,
          message: "Recording service version failed for Tenant: {tenant}, Service: {service}",
          tenant,
          service);
      }
    }
  }

  private abstract record ServiceVersionCheck {
    public String Service { get; init; } = default!;
    public VersionCheckModel Model { get; init; } = default!;
  }

  private record PendingServiceVersionCheck : ServiceVersionCheck {
    public Task<VersionResponse> Task { get; init; } = default!;
  }

  private record CompletedServiceVersionCheck : ServiceVersionCheck {
    public VersionResponse Response { get; init; } = default!;
  }

}
