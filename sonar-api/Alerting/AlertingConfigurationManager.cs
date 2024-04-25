using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Factories;
using Cms.BatCave.Sonar.Helpers;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Alerting;

/// <summary>
///   Helper class for creating and updating Alertmanager and Prometheus Alerting Configuration in
///   Kubernetes.
/// </summary>
public class AlertingConfigurationManager {
  private const String AlertmanagerTemplatesMountPath = "/sonar-config/templates";

  private static readonly JsonSerializerOptions AlertmanagerJsonSerializerOptions = new() {
    // technically Alertmanager expects snake_case, but CamelCase is the closest
    // option, so multi-word properties need explicit JsonPropertyName attributes.
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  private readonly KubeClientFactory _kubeClientFactory;
  private readonly IOptions<KubernetesApiAccessConfiguration> _kubernetesApiAccessConfig;
  private readonly AlertingReceiverConfigurationGenerator _receiverConfigurationGenerator;
  private readonly AlertingRulesConfigurationGenerator _ruleConfigurationGenerator;
  private readonly AlertingGlobalConfigurationGenerator _globalConfigurationGenerator;
  private readonly AlertingDataHelper _alertingDataHelper;
  private readonly AlertingConfigurationHelper _alertingConfigurationHelper;
  private readonly ILogger<AlertingConfigurationManager> _logger;

  public AlertingConfigurationManager(
    KubeClientFactory kubeClientFactory,
    IOptions<KubernetesApiAccessConfiguration> kubernetesApiAccessConfig,
    AlertingGlobalConfigurationGenerator globalConfigurationGenerator,
    AlertingReceiverConfigurationGenerator receiverConfigurationGenerator,
    AlertingRulesConfigurationGenerator ruleConfigurationGenerator,
    AlertingDataHelper alertingDataHelper,
    AlertingConfigurationHelper alertingConfigurationHelper,
    ILogger<AlertingConfigurationManager> logger) {

    this._kubeClientFactory = kubeClientFactory;
    this._kubernetesApiAccessConfig = kubernetesApiAccessConfig;
    this._globalConfigurationGenerator = globalConfigurationGenerator;
    this._receiverConfigurationGenerator = receiverConfigurationGenerator;
    this._ruleConfigurationGenerator = ruleConfigurationGenerator;
    this._alertingDataHelper = alertingDataHelper;
    this._alertingConfigurationHelper = alertingConfigurationHelper;
    this._logger = logger;
  }

  /// <summary>
  ///   Creates or updates a ConfigMap used to configure Alertmanager according to SONAR tenant
  ///   configurations.
  /// </summary>
  public async Task CreateOrUpdateAlertmanagerConfigMapAsync(
    CancellationToken cancellationToken) {

    var kubeClient = this._kubeClientFactory.CreateKubeClient(this._kubernetesApiAccessConfig.Value.IsInCluster);
    var targetNamespace = this._kubernetesApiAccessConfig.Value.TargetNamespace;

    // Perform the read before reading configuration data to avoid the race condition where we are
    // overwriting a new version of the ConfigMap with older configuration
    var latestAlertingConfigVersion = await this._alertingDataHelper
      .FetchLatestAlertingConfigVersionNumberAsync(cancellationToken);
    var latestAlertingConfigVersionString = latestAlertingConfigVersion.ToString();
    var annotationsSection = new Dictionary<String, String> {
      [AlertingConfigurationHelper.ConfigMapVersionAnnotationKey] = latestAlertingConfigVersionString
    };

    var existingAlertmanagerConfigMap = await kubeClient.GetConfigMapAsync(
      targetNamespace,
      AlertingConfigurationHelper.AlertmanagerConfigMapName,
      cancellationToken);

    var existingPrometheusAlertingRulesConfigMap = await kubeClient.GetConfigMapAsync(
      targetNamespace,
      AlertingConfigurationHelper.PrometheusAlertingRulesConfigMapName,
      cancellationToken
    );

    var (globalConfigurationData, alertmanagerSecretData) =
      this._globalConfigurationGenerator.GenerateAlertmanagerConfigData();
    var receivers =
      await this._receiverConfigurationGenerator.GenerateAlertmanagerReceiverConfiguration(cancellationToken);
    var (rules, rootRoute, inhibitRules) =
      await this._ruleConfigurationGenerator.GenerateAlertingRulesConfiguration(cancellationToken);

    var alertmanagerConfigMapData = new Dictionary<String, String> {
      ["alertmanager-config.yaml"] = JsonSerializer.Serialize(
        new Dictionary<String, Object> {
          ["global"] = globalConfigurationData,
          ["receivers"] = receivers,
          ["route"] = rootRoute,
          // The "templates" key of Alertmanager config is a list of files from which custom notification template
          // definitions are read. The last component of the file path may use a wildcard matcher. In our case, we
          // give the wildcard path where we mount the custom SONAR notification templates in the Alertmanager pod.
          ["templates"] = new List<String> { $"{AlertmanagerTemplatesMountPath}/*.tmpl" },
          ["inhibit_rules"] = inhibitRules
        },
        AlertmanagerJsonSerializerOptions
      )
    };

    await CreateOrUpdateConfigMap(
      kubeClient,
      AlertingConfigurationHelper.AlertmanagerConfigMapName,
      existingAlertmanagerConfigMap,
      alertmanagerConfigMapData
    );

    var prometheusAlertingRulesConfigMapData = new Dictionary<String, String> {
      ["alerting-rules.yaml"] = JsonSerializer.Serialize(rules, AlertmanagerJsonSerializerOptions)
    };

    await CreateOrUpdateConfigMap(
      kubeClient,
      AlertingConfigurationHelper.PrometheusAlertingRulesConfigMapName,
      existingPrometheusAlertingRulesConfigMap,
      prometheusAlertingRulesConfigMapData
    );

    var alertmanagerSecretName = this._alertingConfigurationHelper.GetAlertmanagerSecretName();

    // We don't really have to worry about a stale read here since these settings are more or less static
    var existingAlertmanagerSecret = await kubeClient.GetSecretAsync(
      targetNamespace,
      alertmanagerSecretName,
      cancellationToken);

    if (existingAlertmanagerSecret == null) {
      await kubeClient.CreateSecretAsync(
        targetNamespace,
        alertmanagerSecretName,
        annotationsSection,
        alertmanagerSecretData,
        cancellationToken);
    } else {
      await kubeClient.UpdateSecretAsync(
        targetNamespace,
        alertmanagerSecretName,
        existingAlertmanagerSecret,
        annotationsSection,
        alertmanagerSecretData,
        cancellationToken);
    }

    kubeClient.Dispose();

    async Task CreateOrUpdateConfigMap(
      IKubernetes client,
      String configMapName,
      V1ConfigMap? v1ConfigMap,
      IDictionary<String, String> dataSection) {

      if (v1ConfigMap == null) {
        await client.CreateConfigMapAsync(
          targetNamespace,
          configMapName,
          annotationsSection,
          dataSection,
          cancellationToken);
      } else {
        var existingConfigMapConfigVersion =
          Convert.ToInt32(v1ConfigMap.Metadata.Annotations?[AlertingConfigurationHelper.ConfigMapVersionAnnotationKey]);

        // Compare latest alerting configuration version with version annotated in existing ConfigMap
        if (existingConfigMapConfigVersion > latestAlertingConfigVersion) {
          this._logger.LogInformation(
            message: "Could not update the alerting ConfigMap {ConfigMapName} with the " +
            "configuration version ({AttemptedVersion}), since the version " +
            "in the ConfigMap is a later one (versionNumber: {ExistingVersion}).",
            configMapName,
            latestAlertingConfigVersionString,
            existingConfigMapConfigVersion);
        } else if (existingConfigMapConfigVersion < latestAlertingConfigVersion) {
          await client.UpdateConfigMapAsync(
            targetNamespace,
            configMapName,
            v1ConfigMap,
            annotationsSection,
            dataSection,
            cancellationToken);
          this._logger.LogInformation(
            message: "Updated {configMapName} to version {latestAlertingConfigVersion}",
            configMapName,
            latestAlertingConfigVersion);
        }
      }
    }
  }
}
