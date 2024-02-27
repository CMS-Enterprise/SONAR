using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Factories;
using k8s.Models;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Helpers;

public class AlertingConfigurationHelper {
  private const String AlertmanagerSecretName = "sonar-alertmanager-secrets";
  public const String AlertmanagerConfigMapName = "sonar-alertmanager-config";
  public const String PrometheusAlertingRulesConfigMapName = "sonar-alerting-rules";
  public const String ConfigMapVersionAnnotationKey = "versionNumber";

  private readonly IOptions<KubernetesApiAccessConfiguration> _kubernetesApiAccessConfig;
  private readonly KubeClientFactory _kubeClientFactory;

  public AlertingConfigurationHelper(
    KubeClientFactory kubeClientFactory,
    IOptions<KubernetesApiAccessConfiguration> kubernetesApiAccessConfig) {

    this._kubeClientFactory = kubeClientFactory;
    this._kubernetesApiAccessConfig = kubernetesApiAccessConfig;
  }

  public String GetAlertmanagerSecretName() {
    return AlertmanagerSecretName;
  }

  /// <summary>
  /// Fetch the ConfigMap generated for the Alertmanager configuration.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token for the async operation.</param>
  /// <returns>The ConfigMap generated for the Alertmanager configuration.</returns>
  public async Task<V1ConfigMap?> FetchAlertingConfigMap(CancellationToken cancellationToken) {
    var kubeClient = this._kubeClientFactory
      .CreateKubeClient(this._kubernetesApiAccessConfig.Value.IsInCluster);

    return await kubeClient.GetConfigMapAsync(
      this._kubernetesApiAccessConfig.Value.TargetNamespace,
      AlertmanagerConfigMapName,
      cancellationToken);
  }


  /// <summary>
  /// Fetch the ConfigMap generated for the Prometheus Alerting Rules configuration.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token for the async operation.</param>
  /// <returns>The ConfigMap generated for the Prometheus Alerting Rules configuration.</returns>
  public async Task<V1ConfigMap?> FetchPrometheusAlertingRulesConfigMap(CancellationToken cancellationToken) {
    var kubeClient = this._kubeClientFactory
      .CreateKubeClient(this._kubernetesApiAccessConfig.Value.IsInCluster);

    return await kubeClient.GetConfigMapAsync(
      this._kubernetesApiAccessConfig.Value.TargetNamespace,
      PrometheusAlertingRulesConfigMapName,
      cancellationToken);
  }

  /// <summary>
  /// Fetch the Secret generated for Alerting.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token for the async operation.</param>
  /// <returns>The Secret generated for Alerting.</returns>
  public async Task<V1Secret?> FetchAlertmanagerSecret(CancellationToken cancellationToken) {
    var kubeClient = this._kubeClientFactory
      .CreateKubeClient(this._kubernetesApiAccessConfig.Value.IsInCluster);

    return await kubeClient.GetSecretAsync(
      this._kubernetesApiAccessConfig.Value.TargetNamespace,
      AlertmanagerSecretName,
      cancellationToken);
  }

  /// <summary>
  /// Converts the string value of a Kubernetes resource's versionNumber (if it exists) into an integer.
  /// </summary>
  /// <param name="annotationsSection">A dictionary of strings representing the Annotations section of a Kubernetes resource</param>
  /// <returns>The integer value for a Kubernetes resource's versionNumber</returns>
  public Int32 GetAlertingKubernetesResourceVersionInt(
    IDictionary<String, String>? annotationsSection) {
    return Convert.ToInt32(
      annotationsSection?[ConfigMapVersionAnnotationKey] ?? "-1");
  }
}
