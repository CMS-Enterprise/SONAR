using System;
using k8s;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Factories;

public class KubeClientFactory {

  private readonly ILogger<KubeClientFactory> _logger;

  public KubeClientFactory(ILogger<KubeClientFactory> logger) {
    this._logger = logger;
  }

  /// <summary>
  ///  Creates Kubernetes instance based on whether or not configuration is in the cluster.
  /// </summary>
  public Kubernetes CreateKubeClient(Boolean inClusterConfig) {
    var config = inClusterConfig ?
      KubernetesClientConfiguration.InClusterConfig() :
      KubernetesClientConfiguration.BuildDefaultConfig();

    var kubernetes = new k8s.Kubernetes(config);

    this._logger.LogDebug(
      "Connecting to Kubernetes Host: {Host}, Namespace: {Namespace}, BaseUri: {BaseUri}",
      config.Host, config.Namespace, kubernetes.BaseUri);

    return kubernetes;
  }
}
