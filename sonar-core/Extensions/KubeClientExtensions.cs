using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace Cms.BatCave.Sonar.Extensions;

public static class KubeClientExtensions {

  /// <summary>
  /// Processes list of ConfigMaps for a specified Namespace name and returns a ConfigMap
  /// with the specified ConfigMap name or null if not found.
  /// </summary>
  public static async Task<V1ConfigMap?> GetConfigMapAsync(
    this IKubernetes kubeClient,
    String namespaceName,
    String configMapName,
    CancellationToken cancellationToken) {

    // Get ConfigMaps in specified namespace
    var configMaps = await kubeClient.CoreV1.ListNamespacedConfigMapAsync(
      namespaceName,
      fieldSelector: $"metadata.name={configMapName}",
      cancellationToken: cancellationToken
    );

    return configMaps.Items.SingleOrDefault();
  }

  /// <summary>
  /// Creates a ConfigMap with a given name and configuration in a specified namespace.
  /// </summary>
  public static async Task CreateConfigMapAsync(
    this IKubernetes kubeClient,
    String namespaceName,
    String configMapName,
    IDictionary<String, String> annotationsSection,
    IDictionary<String, String> dataSection,
    CancellationToken cancellationToken) {

    var metadata = new V1ObjectMeta(
      name: configMapName,
      namespaceProperty: namespaceName,
      annotations: annotationsSection);

    var configMap = new V1ConfigMap(
      apiVersion: "v1",
      kind: "ConfigMap",
      metadata: metadata,
      data: dataSection);

    await kubeClient.CoreV1.CreateNamespacedConfigMapAsync(
      body: configMap,
      namespaceParameter: namespaceName,
      cancellationToken: cancellationToken);
  }

  /// <summary>
  /// Updates the configuration in an existing ConfigMap with a given name
  /// in a specified existing namespace.
  /// </summary>
  public static async Task UpdateConfigMapAsync(
    this IKubernetes kubeClient,
    String existingNamespaceName,
    String existingConfigMapName,
    V1ConfigMap existingConfigMap,
    IDictionary<String, String> annotationsSectionUpdate,
    IDictionary<String, String> dataSectionUpdate,
    CancellationToken cancellationToken) {

    existingConfigMap.Data = dataSectionUpdate;
    existingConfigMap.Metadata.Annotations = annotationsSectionUpdate;

    await kubeClient.CoreV1.ReplaceNamespacedConfigMapAsync(
      body: existingConfigMap,
      name: existingConfigMapName,
      namespaceParameter: existingNamespaceName,
      cancellationToken: cancellationToken);
  }

  public static async Task<V1Secret?> GetSecretAsync(
    this IKubernetes kubeClient,
    String namespaceName,
    String secretName,
    CancellationToken cancellationToken) {

    // Get Secrets in specified namespace
    var secrets = await kubeClient.CoreV1.ListNamespacedSecretAsync(
      namespaceName,
      fieldSelector: $"metadata.name={secretName}",
      cancellationToken: cancellationToken
    );

    return secrets.Items.FirstOrDefault();
  }

  /// <summary>
  /// Creates a Secret with a given name in a specified namespace.
  /// </summary>
  public static async Task CreateSecretAsync(
    this IKubernetes kubeClient,
    String namespaceName,
    String secretName,
    IDictionary<String, String> annotationsSection,
    IDictionary<String, String> secretDataSection,
    CancellationToken cancellationToken) {

    var metadata = new V1ObjectMeta(
      name: secretName,
      namespaceProperty: namespaceName,
      annotations: annotationsSection);

    var secret = new V1Secret(
      apiVersion: "v1",
      kind: "Secret",
      metadata: metadata,
      stringData: secretDataSection);

    await kubeClient.CoreV1.CreateNamespacedSecretAsync(
      body: secret,
      namespaceParameter: namespaceName,
      cancellationToken: cancellationToken);
  }

  /// <summary>
  /// Updates the configuration in an existing Secret with a given name
  /// in a specified existing namespace.
  /// </summary>
  public static async Task UpdateSecretAsync(
    this IKubernetes kubeClient,
    String existingNamespaceName,
    String existingSecretName,
    V1Secret existingSecret,
    IDictionary<String, String> annotationsSectionUpdate,
    IDictionary<String, String> dataSectionUpdate,
    CancellationToken cancellationToken) {

    existingSecret.Data = null;
    existingSecret.Metadata.Annotations = annotationsSectionUpdate;
    existingSecret.StringData = dataSectionUpdate;

    await kubeClient.CoreV1.ReplaceNamespacedSecretAsync(
      body: existingSecret,
      name: existingSecretName,
      namespaceParameter: existingNamespaceName,
      cancellationToken: cancellationToken);
  }
}
