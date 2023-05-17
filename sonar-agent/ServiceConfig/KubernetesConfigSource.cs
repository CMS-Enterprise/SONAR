using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Models;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Agent.ServiceConfig;

/// <summary>
///   Loads service configuration from Kubernetes.
/// </summary>
/// <remarks>
///   Any Kubernetes Namespace having a label "sonar-monitoring" with the value "enabled" will be
///   scanned for ConfigMaps with the label "sonar-config" having the value "true". These ConfigMaps
///   are expected to have a single data key "service-config.json" which will be deserialized as
///   <see cref="ServiceHierarchyConfiguration" />. When there are multiple such ConfigMaps they will
///   be merged in the order specified in the "sonar-config/order" label.
/// </remarks>
public class KubernetesConfigSource : IServiceConfigSource {
  private const String TenantLabel = "sonar-monitoring/tenant";
  private const String ConfigOrderLabel = "sonar-config/order";
  private const String ServiceConfigurationFile = "service-config.json";

  /// <summary>
  ///   This label is set to "true" for namespaces where loading secrets is enabled.
  /// </summary>
  public const String SecretConfigLabel = "sonar-config-secrets";
  public const String NamespaceLabelSelector = "sonar-monitoring=enabled";
  public const String ConfigLabel = "sonar-config";
  public const String ConfigLabelSelector = "sonar-config=true";

  private readonly ILogger<KubernetesConfigSource> _logger;
  private readonly IKubernetes _kubeClient;

  private readonly ConcurrentDictionary<String, V1Namespace> _tenantNamespaceCache =
    new(StringComparer.OrdinalIgnoreCase);

  public KubernetesConfigSource(IKubernetes kubeClient, ILogger<KubernetesConfigSource> logger) {
    this._kubeClient = kubeClient;
    this._logger = logger;
  }

  public async IAsyncEnumerable<String> GetTenantsAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken) {

    var namespaces = await this.GetTenantNamespacesAsync(cancellationToken);

    foreach (var entry in namespaces) {
      yield return entry.TenantName;
    }
  }

  public async IAsyncEnumerable<ServiceHierarchyConfiguration> GetConfigurationLayersAsync(
    String tenant,
    [EnumeratorCancellation] CancellationToken cancellationToken) {

    if (!this._tenantNamespaceCache.TryGetValue(tenant, out var k8sNamespace)) {
      // We haven't heard of this tenant, see if it exists in Kubernetes
      var namespaces =
        await this._kubeClient.CoreV1.ListNamespaceAsync(
          labelSelector: NamespaceLabelSelector,
          cancellationToken: cancellationToken);

      var match =
        namespaces.Items
          .SingleOrDefault(ns => String.Equals(tenant, GetTenantFromNamespaceConfig(ns)));

      if (match != null) {
        k8sNamespace = match;
      } else {
        // No such tenant exists in Kubernetes, return an empty enumerable
        yield break;
      }
    }

    var namespaceLabels = k8sNamespace.Metadata.Labels;

    var configMaps =
      await this._kubeClient.CoreV1.ListNamespacedConfigMapAsync(
        k8sNamespace.Metadata.Name,
        labelSelector: ConfigLabelSelector,
        cancellationToken: cancellationToken
      );

    var secrets = SecretsEnabled(namespaceLabels) ?
      (await this._kubeClient.CoreV1.ListNamespacedSecretAsync(
        k8sNamespace.Metadata.Name,
        labelSelector: ConfigLabelSelector,
        cancellationToken: cancellationToken)).Items :
      Enumerable.Empty<V1Secret>();

    foreach (var config in GetServiceConfigurationLayers(tenant, configMaps.Items, secrets, this._logger)) {
      yield return config;
    }
  }

  public static Boolean SecretsEnabled(IDictionary<String, String> namespaceLabels) {
    return namespaceLabels.TryGetValue(SecretConfigLabel, out var value) &&
      String.Equals("enabled", value, StringComparison.OrdinalIgnoreCase);
  }

  private async Task<IEnumerable<(String NamespaceName, String TenantName)>> GetTenantNamespacesAsync(
    CancellationToken cancellationToken) {

    var namespaceList =
      await this._kubeClient.CoreV1.ListNamespaceAsync(
        labelSelector: NamespaceLabelSelector,
        cancellationToken: cancellationToken);

    this._logger.LogDebug(
      "Found Namespaces: {Namespaces}",
      String.Join(",", namespaceList.Items.Select(i => i.Metadata.Name)));

    return this.GetTenantNamespaces(namespaceList);
  }

  private IEnumerable<(String NamespaceName, String TenantName)> GetTenantNamespaces(
    V1NamespaceList namespaces) {

    foreach (var ns in namespaces.Items) {
      if (ns != null) {
        // Determine the tenant name based on its labels
        var tenantName = GetTenantFromNamespaceConfig(ns);
        // Cache the namespace object for this tenant
        this._tenantNamespaceCache[tenantName] = ns;
        yield return (ns.Metadata.Name, tenantName);
      }
    }
  }

  public static String GetTenantFromNamespaceConfig(V1Namespace namespaceConfig) {
    return namespaceConfig.Metadata.Labels.ContainsKey(TenantLabel) ?
      namespaceConfig.Metadata.Labels[TenantLabel] :
      namespaceConfig.Metadata.Name;
  }

  /// <summary>
  ///   Processes lists of ConfigMaps and Secrets for the specified tenant, selecting those that are
  ///   labeled as SONAR configuration, and ordering them according to the corresponding labels.
  /// </summary>
  public static IEnumerable<ServiceHierarchyConfiguration> GetServiceConfigurationLayers(
    String tenant,
    IEnumerable<V1ConfigMap> configMaps,
    IEnumerable<V1Secret> secrets,
    ILogger logger) {

    var unsortedConfigs = new List<(Int16 order, String data)>();

    foreach (var map in configMaps) {
      var labels = map.Metadata.EnsureLabels();
      if (!IsSonarConfigMap(labels)) {
        continue;
      }

      if (!map.Data.TryGetValue(ServiceConfigurationFile, out var data)) {
        logger.LogWarning(
          "The ConfigMap {ConfigMap} for tenant {Tenant} is labeled as SONAR configuration, but it does not contain a {ServiceConfigurationKey} key",
          map.Metadata.Name,
          tenant,
          ServiceConfigurationFile
        );
        continue;
      }

      unsortedConfigs.Add((GetConfigurationOrder(labels), data));
    }

    foreach (var secret in secrets) {
      var labels = secret.Metadata.EnsureLabels();
      if (!IsSonarConfigMap(labels)) {
        continue;
      }

      if (!secret.Data.TryGetValue(ServiceConfigurationFile, out var data)) {
        logger.LogWarning(
          "The Secret {Secret} for tenant {Tenant} is labeled as SONAR configuration, but it does not contain a {ServiceConfigurationKey} key",
          secret.Metadata.Name,
          tenant,
          ServiceConfigurationFile
        );
        continue;
      }

      unsortedConfigs.Add((GetConfigurationOrder(labels), Encoding.UTF8.GetString(data)));
    }

    return unsortedConfigs.OrderBy(c => c.order)
      .Select(c => JsonServiceConfigDeserializer.Deserialize(c.data));
  }

  private static Boolean IsSonarConfigMap(IDictionary<String, String> configMapLabels) {
    return configMapLabels.ContainsKey(ConfigLabel) &&
      (configMapLabels[ConfigLabel] == "true");
  }

  private static Int16 GetConfigurationOrder(
    IDictionary<String, String> labels) {

    // determine order from label, set to max value if order not specified
    return labels.ContainsKey(ConfigOrderLabel) ?
      Int16.Parse(labels[ConfigOrderLabel]) :
      Int16.MaxValue;
  }
}
