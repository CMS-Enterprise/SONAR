using System;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Factories;
using Cms.BatCave.Sonar.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Alerting;

public class AlertingConfigSyncService : BackgroundService {
  private readonly ILogger<AlertingConfigSyncService> _logger;
  private readonly IServiceProvider _serviceProvider;
  private readonly TimeSpan _syncInterval;

  public AlertingConfigSyncService(
    ILogger<AlertingConfigSyncService> logger,
    IServiceProvider serviceProvider
    ) {

    this._logger = logger;
    this._serviceProvider = serviceProvider;
    this._syncInterval = TimeSpan.FromSeconds(
      serviceProvider.GetRequiredService<IOptions<GlobalAlertingConfiguration>>().Value.ConfigSyncIntervalSeconds);
  }

  protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
    var timer = new PeriodicTimer(this._syncInterval);

    using (var scope = this._serviceProvider.CreateScope()) {
      var kubeApiAccessConfig = scope.ServiceProvider.GetRequiredService<IOptions<KubernetesApiAccessConfiguration>>();
      if (!kubeApiAccessConfig.Value.IsEnabled) {
        this._logger.LogInformation("Kubernetes API access is disabled, alerting config sync service will not run.");
        return;
      }
    }

    this._logger.LogInformation("Starting the alerting config sync service.");

    do {
      try {
        await this.SyncConfigAsync(cancellationToken);
      } catch (Exception e) {
        this._logger.LogError(e, message: "Unexpected exception while performing alerting config sync.");
      }
    } while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken));
  }

  private async Task SyncConfigAsync(CancellationToken cancellationToken) {
    this._logger.LogDebug(message: "Performing alerting config sync at {timestamp}", DateTime.UtcNow);

    using var scope = this._serviceProvider.CreateScope();
    var alertingDataHelper = scope.ServiceProvider.GetRequiredService<AlertingDataHelper>();
    var alertingConfigHelper = scope.ServiceProvider.GetRequiredService<AlertingConfigurationHelper>();
    var kubeClientFactory = scope.ServiceProvider.GetRequiredService<KubeClientFactory>();
    var kubeApiAccessConfig = scope.ServiceProvider.GetRequiredService<IOptions<KubernetesApiAccessConfiguration>>();
    using var kubeClient = kubeClientFactory.CreateKubeClient(kubeApiAccessConfig.Value.IsInCluster);

    var latestAlertingConfigVersionTask = alertingDataHelper
      .FetchLatestAlertingConfigVersionNumberAsync(cancellationToken);
    var alertmanagerConfigMapTask = kubeClient.GetConfigMapAsync(
      kubeApiAccessConfig.Value.TargetNamespace,
      AlertingConfigurationHelper.AlertmanagerConfigMapName,
      cancellationToken);
    var prometheusConfigMapTask = kubeClient.GetConfigMapAsync(
      kubeApiAccessConfig.Value.TargetNamespace,
      AlertingConfigurationHelper.PrometheusAlertingRulesConfigMapName,
      cancellationToken);

    var latestAlertingConfigVersion = await latestAlertingConfigVersionTask;
    var alertmanagerConfigMap = await alertmanagerConfigMapTask;
    var prometheusConfigMap = await prometheusConfigMapTask;

    var currentAlertmanagerConfigVersion = alertingConfigHelper
      .GetAlertingKubernetesResourceVersionInt(alertmanagerConfigMap?.Metadata.Annotations);
    var currentPrometheusRulesVersion = alertingConfigHelper
      .GetAlertingKubernetesResourceVersionInt(prometheusConfigMap?.Metadata.Annotations);

    var configMapsAreUpToDate =
      (currentAlertmanagerConfigVersion == latestAlertingConfigVersion) &&
      (currentPrometheusRulesVersion == latestAlertingConfigVersion);

    if (!configMapsAreUpToDate) {
      this._logger.LogInformation(
        message: "Latest alerting config version: {latestAlertingConfigVersion}, " +
          "current Alertmanager config version: {currentAlertmanagerConfigVersion}, " +
          "current Prometheus rules version: {currentPrometheusRulesVersion}; " +
          "Alertmanager and Prometheus config maps will be updated.",
        latestAlertingConfigVersion,
        currentAlertmanagerConfigVersion,
        currentPrometheusRulesVersion);

      var alertingConfigurationManager = scope.ServiceProvider.GetRequiredService<AlertingConfigurationManager>();
      await alertingConfigurationManager.CreateOrUpdateAlertmanagerConfigMapAsync(cancellationToken);
    }

    this._logger.LogDebug(
      message: "Alertmanager and Prometheus config are up to date at version: {latestAlertingConfigVersion}.",
      latestAlertingConfigVersion);
  }
}
