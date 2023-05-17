using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

  private readonly ConcurrentDictionary<String, Watcher<V1Secret>> _secretWatchers = new();

  // Mapping of K8S namespace names to tenant names for K8S namespaces that have sonar enabled
  // Note: populated as new namespaces with label "sonar-monitoring=enabled" are added
  private readonly ConcurrentDictionary<String, String> _knownNamespaceTenants = new();

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
  }

  private Watcher<V1Namespace> CreateNamespaceWatcher() {
    return this._kubeClient.CoreV1
      .ListNamespaceWithHttpMessagesAsync(
        labelSelector: KubernetesConfigSource.NamespaceLabelSelector,
        watch: true)
      .Watch<V1Namespace, V1NamespaceList>(
        this.OnEventNamespace, this.OnErrorNamespace, this.OnClosedNamespace);
  }

  private Watcher<V1ConfigMap> CreateConfigMapWatcher() {
    return this._kubeClient.CoreV1
      .ListConfigMapForAllNamespacesWithHttpMessagesAsync(
        labelSelector: KubernetesConfigSource.ConfigLabelSelector,
        watch: true)
      .Watch<V1ConfigMap, V1ConfigMapList>(
        this.OnEventConfigMap, this.OnErrorConfigMap, this.OnClosedConfigMap);
  }

  private Watcher<V1Secret> CreateSecretWatcher(String @namespace) {
    return this._kubeClient.CoreV1
      .ListNamespacedSecretWithHttpMessagesAsync(
        @namespace,
        labelSelector: KubernetesConfigSource.ConfigLabelSelector,
        watch: true)
      .Watch<V1Secret, V1SecretList>(
        this.OnEventSecret, this.OnErrorSecret, this.OnClosedSecret);
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
      // occurs when a new namespace with "sonar-monitoring" == "enabled" added or the value of the
      // "sonar-monitoring" label is changed to "enabled"
      // NOTE: health check for tenant not scheduled until tenant has service configuration

      // If SONAR config secrets are enabled, begin watching secrets
      if (KubernetesConfigSource.SecretsEnabled(resource.Metadata.Labels)) {
        this._logger.LogDebug(
          "Begin Watching Secrets for Namespace {NamespaceName}", namespaceName);
        this._secretWatchers[resource.Metadata.Name] =
          this.CreateSecretWatcher(resource.Metadata.Name);
      }
    } else if (eventType == WatchEventType.Modified) {
      // occurs when...
      // A. namespace label "sonar-monitoring/tenant" is added/modified/changed
      // B. namespace was manually deleted (note: Modified event occurs before Deleted event)
      // C. secret config was either enabled or disabled for the namespace

      WatchEventType? potentialConfigChange = null;
      var raiseTenantCreateEvent = false;
      if (this._knownNamespaceTenants.TryGetValue(namespaceName, out var existingTenant)) {
        if (existingTenant != tenant) {
          // delete old tenant from SONAR API
          await this._configHelper.DeleteServicesAsync(this._environment, existingTenant, token);

          // create new tenant in SONAR API with service configuration from previous tenant
          potentialConfigChange = WatchEventType.Added;
          raiseTenantCreateEvent = true;
        }
      }

      var secretsEnabled =
        KubernetesConfigSource.SecretsEnabled(resource.Metadata.Labels);

      if (secretsEnabled) {
        if (!this._secretWatchers.ContainsKey(namespaceName)) {
          var newSecretWatcher = this.CreateSecretWatcher(namespaceName);
          if (!this._secretWatchers.TryAdd(namespaceName, newSecretWatcher)) {
            // unlikely race condition
            newSecretWatcher.Dispose();
          }

          // By starting the watcher we should automatically pick up changes
        }
      } else {
        if (this._secretWatchers.TryRemove(namespaceName, out var watcher)) {
          // secret config was previously enabled.
          watcher.Dispose();
          // assume that this means some Secret is no longer applicable and treat this like a "deleted" event
          potentialConfigChange = WatchEventType.Deleted;
        }
      }

      if (potentialConfigChange.HasValue) {
        await this.HandleConfigResourceEventAsync(
          potentialConfigChange.Value,
          namespaceName,
          token
        );
      }

      if (raiseTenantCreateEvent) {
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

      if (this._secretWatchers.TryRemove(namespaceName, out var watcher)) {
        // Stop watching the secrets for this namespace
        watcher.Dispose();
      }

      // delete tenant from SONAR API
      await this._configHelper.DeleteServicesAsync(this._environment, tenant, token);
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

    await this.HandleConfigResourceEventAsync(
      eventType,
      resource.Metadata.NamespaceProperty,
      CancellationToken.None
    );
  }

  private async Task HandleConfigResourceEventAsync(
    WatchEventType eventType,
    String resourceNamespace,
    CancellationToken token) {

    var namespaceConfig =
      this._kubeClient.CoreV1.ReadNamespaceAsync(resourceNamespace, cancellationToken: token);
    var tenant = KubernetesConfigSource.GetTenantFromNamespaceConfig(namespaceConfig.Result);

    var servicesHierarchy = this.GetTenantServicesHierarchy(
      resourceNamespace,
      tenant,
      KubernetesConfigSource.SecretsEnabled(namespaceConfig.Result.Metadata.Labels)
    );

    switch (eventType) {
      case WatchEventType.Added:
      case WatchEventType.Modified:
        this._logger.LogInformation(
          "ServiceHierarchyConfiguration changed for tenant {Tenant} - Known Services: ({Services})",
          tenant,
          String.Join(", ", servicesHierarchy.Services.Select(svc => svc.Name))
        );

        // create or update new Tenant service configuration
        await this._configHelper.ConfigureServicesAsync(
          this._environment,
          new Dictionary<String, ServiceHierarchyConfiguration> {
            [tenant] = servicesHierarchy
          },
          token
        );

        if (this._knownNamespaceTenants.TryAdd(resourceNamespace, tenant)) {
          // We weren't previously monitoring this tenant, begin the monitoring thread
          // schedule health check for new tenant
          this.TenantCreated?.Invoke(this, new SonarTenantCreatedEventArgs(tenant));
        }

        break;
      case WatchEventType.Deleted:
        // if associated namespace was NOT deleted
        if (this._knownNamespaceTenants.ContainsKey(resourceNamespace)) {
          this._logger.LogInformation(
            "ServiceHierarchyConfiguration changed for tenant {Tenant} - Known Services: ({Services})",
            tenant,
            String.Join(", ", servicesHierarchy.Services.Select(svc => svc.Name))
          );
          // update existing Tenant service configuration
          await this._configHelper.ConfigureServicesAsync(
            this._environment,
            new Dictionary<String, ServiceHierarchyConfiguration> {
              [tenant] = servicesHierarchy
            },
            token
          );
        }

        break;
    }
  }

  private async void OnEventSecret(WatchEventType eventType, V1Secret resource) {
    this._logger.LogDebug(
      "Secret watcher - {Secret} was {Event}", resource.Metadata.Name, eventType);

    await this.HandleConfigResourceEventAsync(
      eventType,
      resource.Metadata.NamespaceProperty,
      CancellationToken.None
    );
  }

  private void OnErrorSecret(Exception error) {
    this._logger.LogError(error, "Secret watcher - error: {ErrorMsg}", error.Message);
  }

  private void OnClosedSecret() {
    this._logger.LogDebug("Secret watcher - service connection closed");
  }

  private ServiceHierarchyConfiguration GetTenantServicesHierarchy(
    String namespaceName,
    String tenantName,
    Boolean includeSecrets) {

    // get all configmaps and secrets for associated namespace
    var configMaps = this._kubeClient.CoreV1.ListNamespacedConfigMap(namespaceName).Items;
    var secrets =
      includeSecrets ?
        this._kubeClient.CoreV1.ListNamespacedSecret(namespaceName).Items :
        Enumerable.Empty<V1Secret>();

    // select and order those configmaps and secrets that contain SONAR service configuration
    var configLayers =
      KubernetesConfigSource
        .GetServiceConfigurationLayers(tenantName, configMaps, secrets, this._logger)
        .ToImmutableList();

    // Ignore the tenant if it has no service configuration
    if (configLayers.Any()) {
      // Merge and validate the configuration
      var mergedConfig = configLayers.Aggregate(ServiceConfigMerger.MergeConfigurations);

      ServiceConfigValidator.ValidateServiceConfig(mergedConfig);

      return mergedConfig;
    } else {
      return ServiceHierarchyConfiguration.Empty;
    }
  }

  public void Dispose() {
    this._cmWatcher.Dispose();
    this._nsWatcher.Dispose();
    foreach (var watcher in this._secretWatchers.Values) {
      watcher.Dispose();
    }
  }
}
