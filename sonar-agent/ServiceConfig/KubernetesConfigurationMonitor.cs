using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Cms.BatCave.Sonar.Models;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Agent.ServiceConfig;

public sealed class KubernetesConfigurationMonitor : IDisposable {
  private readonly ILogger<KubernetesConfigurationMonitor> _logger;
  private readonly String _environment;
  private readonly ConfigurationHelper _configHelper;
  private readonly IKubernetes _kubeClient;
  private readonly Watcher<V1Namespace> _nsWatcher;
  private readonly Watcher<V1ConfigMap> _cmWatcher;

  // Mapping of K8S namespace names to tenant names for K8S namespaces that have sonar enabled
  private readonly ConcurrentDictionary<String, String> _knownNamespaceTenants;

  public event EventHandler<SonarTenantCreatedEventArgs>? TenantCreated;

  public KubernetesConfigurationMonitor(
    String environment,
    ConfigurationHelper configHelper,
    IKubernetes kubeClient,
    ILogger<KubernetesConfigurationMonitor> logger) {

    this._logger = logger;
    this._environment = environment;
    this._configHelper = configHelper;
    this._kubeClient = kubeClient;

    this._nsWatcher = this.CreateNamespaceWatcher();
    this._cmWatcher = this.CreateConfigMapWatcher();

    // Note: populated as new namespaces with label "sonar-monitoring=enabled" are added
    this._knownNamespaceTenants = new ConcurrentDictionary<String, String>();
  }

  private Watcher<V1Namespace> CreateNamespaceWatcher() {
    return this._kubeClient.CoreV1.ListNamespaceWithHttpMessagesAsync(
      labelSelector: KubernetesConfigSource.NamespaceLabelSelector, watch: true).Watch<V1Namespace, V1NamespaceList>(
      this.OnEventNamespace, this.OnErrorNamespace, this.OnClosedNamespace);
  }

  private Watcher<V1ConfigMap> CreateConfigMapWatcher() {
    return this._kubeClient.CoreV1.ListConfigMapForAllNamespacesWithHttpMessagesAsync(
      labelSelector: KubernetesConfigSource.ConfigMapLabelSelector, watch: true).Watch<V1ConfigMap, V1ConfigMapList>(
      this.OnEventConfigMap, this.OnErrorConfigMap, this.OnClosedConfigMap);
  }

  private void OnClosedNamespace() {
    this._logger.LogDebug("Namespace watcher - service connection closed");
  }

  private void OnErrorNamespace(Exception error) {
    this._logger.LogError("Namespace watcher - error: {ErrorMsg}", error);
  }

  private async void OnEventNamespace(WatchEventType eventType, V1Namespace resource) {
    String namespaceName = resource.Metadata.Name;
    String tenant = KubernetesConfigSource.GetTenantFromNamespaceConfig(resource);

    this._logger.LogDebug(
      "Namespace watcher - {NamespaceName} was {Event}", namespaceName, eventType);

    using var source = new CancellationTokenSource();
    var token = source.Token;

    if (eventType == WatchEventType.Added) {
      // occurs when a new namespace with "sonar-monitoring" == "enabled" added
      // NOTE: health check for tenant not scheduled until tenant has service configuration

    } else if (eventType == WatchEventType.Modified) {
      // occurs when...
      // A. namespace label "sonar-monitoring/tenant" is added/modified/changed
      // B. namespace was manually deleted (note: Modified event occurs before Deleted event)

      if (this._knownNamespaceTenants[namespaceName] != tenant) {
        var prevTenantName = this._knownNamespaceTenants[namespaceName];
        this._knownNamespaceTenants[namespaceName] = tenant;

        // fetch service configuration tied to namespace and assign it to new tenant
        var servicesHierarchy =
          this.GetTenantServicesHierarchy(namespaceName, tenant);

        // delete old tenant from SONAR API
        await this._configHelper.DeleteServices(this._environment, prevTenantName, token);

        // create new tenant in SONAR API with service configuration from previous tenant
        await this._configHelper.ConfigureServices(this._environment, servicesHierarchy, token);

        // schedule health check for new tenant
        this.TenantCreated?.Invoke(this, new SonarTenantCreatedEventArgs(tenant));
      }

    } else if (eventType == WatchEventType.Deleted) {
      // occurs when...:
      //  A. namespace label "sonar-monitoring" deleted
      //  B. namespace label "sonar-monitoring" was "enabled" previously but now is not
      //  C. namespace was manually deleted

      // remove namespace and tenant
      this._knownNamespaceTenants.TryRemove(namespaceName, out _);

      // remove configmaps associated with namespace get all configmaps for associated namespace
      await this._kubeClient.CoreV1.DeleteCollectionNamespacedConfigMapAsync(namespaceName, cancellationToken: token);

      // delete tenant from SONAR API
      await this._configHelper.DeleteServices(this._environment, tenant, token);
    }
  }

  private void OnClosedConfigMap() {
    this._logger.LogDebug("ConfigMap watcher - service connection closed");
  }

  private void OnErrorConfigMap(Exception error) {
    this._logger.LogError("ConfigMap watcher - error: {ErrorMsg}", error);
  }

  private async void OnEventConfigMap(WatchEventType eventType, V1ConfigMap resource) {
    this._logger.LogDebug(
      "ConfigMap watcher - {ConfigMapName} was {Event}", resource.Metadata.Name, eventType);

    using var source = new CancellationTokenSource();
    var token = source.Token;

    // determine tenant
    var configMapNamespace = resource.Metadata.NamespaceProperty;
    var namespaceConfig =
      this._kubeClient.CoreV1.ReadNamespaceWithHttpMessagesAsync(configMapNamespace, cancellationToken: token);
    var tenant = KubernetesConfigSource.GetTenantFromNamespaceConfig(namespaceConfig.Result.Body);

    var servicesHierarchy = this.GetTenantServicesHierarchy(configMapNamespace, tenant);

    switch (eventType) {
      case WatchEventType.Added:
        // create new Tenant service configuration
        await this._configHelper.ConfigureServices(this._environment, servicesHierarchy, token);
        // associate namespace with tenant
        this._knownNamespaceTenants[configMapNamespace] = tenant;
        // schedule health check for new tenant
        this.TenantCreated?.Invoke(this, new SonarTenantCreatedEventArgs(tenant));
        break;
      case WatchEventType.Modified:
        // update existing Tenant service configuration
        await this._configHelper.ConfigureServices(this._environment, servicesHierarchy, token);
        break;
      case WatchEventType.Deleted: {
          // if associated namespace was NOT deleted
          if (this._knownNamespaceTenants.ContainsKey(configMapNamespace)) {
            // update existing Tenant service configuration
            await this._configHelper.ConfigureServices(this._environment, servicesHierarchy, token);
          }

          break;
        }
    }
  }

  private Dictionary<String, ServiceHierarchyConfiguration> GetTenantServicesHierarchy(
    String namespaceName,
    String tenantName) {

    var servicesHierarchy = new Dictionary<String, ServiceHierarchyConfiguration>();

    // get all configmaps for associated namespace
    var configMaps = this._kubeClient.CoreV1.ListNamespacedConfigMap(namespaceName);

    if ((configMaps != null) && (configMaps.Items.Count > 0)) {
      // check if configmap contains service configuration
      var configLayers =
        KubernetesConfigSource.GetServiceConfigurationLayers(configMaps).ToImmutableList();

      if (configLayers.Any()) {
        var mergedConfig = configLayers.Aggregate(ServiceConfigMerger.MergeConfigurations);

        ServiceConfigValidator.ValidateServiceConfig(mergedConfig);

        servicesHierarchy.Add(tenantName, mergedConfig);
      }
    }

    return servicesHierarchy;
  }

  public void Dispose() {
    this._cmWatcher.Dispose();
    this._nsWatcher.Dispose();
  }
}
