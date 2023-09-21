using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
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
  private Watcher<V1Namespace> _nsWatcher;
  private Watcher<V1ConfigMap> _cmWatcher;
  private readonly TimeSpan _retryDelay;
  private readonly ErrorReportsHelper _errorReportsHelper;

  private readonly ConcurrentDictionary<String, Watcher<V1Secret>> _secretWatchers = new();

  // Mapping of K8S namespace names to tenant names for K8S namespaces that have sonar enabled
  // Note: populated as new namespaces with label "sonar-monitoring=enabled" are added
  private readonly ConcurrentDictionary<String, String> _knownNamespaceTenants = new();

  // Mapping of processes that failed and are currently retrying
  private readonly ConcurrentDictionary<String, WatcherEventToken> _pendingProcesses;
  public event EventHandler<SonarTenantCreatedEventArgs>? TenantCreated;

  public KubernetesConfigurationMonitor(
    String environment,
    ConfigurationHelper configHelper,
    IKubernetes kubeClient,
    ILogger<KubernetesConfigurationMonitor> logger,
    ErrorReportsHelper errorReportsHelper) {

    this._logger = logger;
    this._errorReportsHelper = errorReportsHelper;
    this._environment = environment;
    this._configHelper = configHelper;
    this._kubeClient = kubeClient;

    this._nsWatcher = this.CreateNamespaceWatcher();
    this._cmWatcher = this.CreateConfigMapWatcher();
    this._retryDelay = TimeSpan.FromSeconds(30);
    this._pendingProcesses = new ConcurrentDictionary<String, WatcherEventToken>();
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
    this._logger.LogDebug("Namespace watcher - service connection closed, reconnecting...");
    this._nsWatcher.Dispose();
    this._nsWatcher = this.CreateNamespaceWatcher();
  }

  private void OnErrorNamespace(Exception error) {
    this._logger.LogError("Namespace watcher - error: {ErrorMsg}", error);
  }

  private record WatcherEventToken(
    WatchEventType EventType,
    CancellationTokenSource TokenSource);

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
          await this.DeleteServicesAsyncWrapper(existingTenant, WatchEventType.Deleted);

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
      await this.DeleteServicesAsyncWrapper(tenant, WatchEventType.Deleted);
    }
  }

  private void OnClosedConfigMap() {
    this._logger.LogDebug("ConfigMap watcher - service connection closed, reconnecting...");
    this._cmWatcher.Dispose();
    this._cmWatcher = this.CreateConfigMapWatcher();
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

    ServiceHierarchyConfiguration servicesHierarchy;

    try {
      servicesHierarchy = await this.GetTenantServicesHierarchy(
        resourceNamespace,
        tenant,
        KubernetesConfigSource.SecretsEnabled(namespaceConfig.Result.Metadata.Labels),
        token
      );
    } catch (InvalidConfigurationException e) {
      this._logger.LogError(
        e,
        message: "Tenant service configuration is invalid, ignoring {eventType} event: {tenant}.",
        eventType,
        tenant);
      return;
    }

    switch (eventType) {
      case WatchEventType.Added:
      case WatchEventType.Modified:
        this._logger.LogInformation(
          "ServiceHierarchyConfiguration changed for tenant {Tenant} - Known Services: ({Services})",
          tenant,
          String.Join(", ", servicesHierarchy.Services.Select(svc => svc.Name))
        );

        try {
          // create or update new Tenant service configuration
          await this.ConfigureServicesAsyncWrapper(
            tenant,
            eventType,
            new Dictionary<String, ServiceHierarchyConfiguration> {
              [tenant] = servicesHierarchy
            });
        } catch (ApiException ex) {
          // create error report
          await this._errorReportsHelper.CreateErrorReport(
            this._environment,
            new ErrorReportDetails(
              DateTime.UtcNow,
              tenant,
              null,
              null,
              AgentErrorLevel.Error,
              AgentErrorType.SaveConfiguration,
              ex.Message,
              null,
              null),
            token);
        }

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

          try {
            // update existing Tenant service configuration
            await this.ConfigureServicesAsyncWrapper(
              tenant,
              WatchEventType.Modified,
              new Dictionary<String, ServiceHierarchyConfiguration> {
                [tenant] = servicesHierarchy
              });
          } catch (ApiException ex) {
            // create error report
            await this._errorReportsHelper.CreateErrorReport(
              this._environment,
              new ErrorReportDetails(
                DateTime.UtcNow,
                tenant,
                null,
                null,
                AgentErrorLevel.Error,
                AgentErrorType.SaveConfiguration,
                ex.Message,
                null,
                null),
              token);
          }
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

  private async Task<ServiceHierarchyConfiguration> GetTenantServicesHierarchy(
    String namespaceName,
    String tenantName,
    Boolean includeSecrets,
    CancellationToken token) {

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

    try {
      // Ignore the tenant if it has no service configuration
      if (configLayers.Any()) {
        // Merge and validate the configuration
        var mergedConfig = configLayers.Aggregate(ServiceConfigMerger.MergeConfigurations);

        try {
          ServiceConfigValidator.ValidateServiceConfig(mergedConfig);
          return mergedConfig;

        } catch (InvalidConfigurationException e) {
          var data = (List<ValidationResult>)e.Data["errors"]!;
          var errorReport = new ErrorReportDetails(
            timestamp: DateTime.UtcNow,
            tenant: null,
            service: null,
            healthCheckName: null,
            level: AgentErrorLevel.Error,
            type: AgentErrorType.Validation,
            message: String.Join(" ", data.Select(e => e.ErrorMessage)),
            configuration: null,
            stackTrace: e.StackTrace
          );

          await this._errorReportsHelper.CreateErrorReport(this._environment, errorReport, token);
        }
      }
    } catch (InvalidConfigurationException e) {
      var errorReport = new ErrorReportDetails(
        timestamp: DateTime.UtcNow,
        tenant: null,
        service: null,
        healthCheckName: null,
        level: AgentErrorLevel.Error,
        type: AgentErrorType.Deserialization,
        message: e.Message,
        configuration: null,
        stackTrace: e.StackTrace
      );

      await this._errorReportsHelper.CreateErrorReport(this._environment, errorReport, token);
    }

    return ServiceHierarchyConfiguration.Empty;
  }

  private async Task DeleteServicesAsyncWrapper(
    String tenant,
    WatchEventType eventType) {

    await this.PerformRetryOperation(
      tenant,
      eventType,
      (token) => this._configHelper.DeleteServicesAsync(this._environment, tenant, token)
      );
  }

  private async Task ConfigureServicesAsyncWrapper(
    String tenant,
    WatchEventType eventType,
    Dictionary<String, ServiceHierarchyConfiguration> servicesHierarchy) {

    await this.PerformRetryOperation(
      tenant,
      eventType,
      (token) => this._configHelper.ConfigureServicesAsync(this._environment, servicesHierarchy, token)
      );
  }

  // This function takes in a function (SONAR-API async request) and will retry continuously until it succeeds.
  // The retry loop will only fail unless a subsequent event is raised for the same tenant.
  private async Task PerformRetryOperation(
    String tenant,
    WatchEventType eventType,
    Func<CancellationToken, Task> action) {
    if (this.AddUpdateProcessQueue(tenant, eventType, out var eventToken)) {
      try {
        while (true) {
          try {
            this._logger.LogInformation(
              "Attempting to save configuration for tenant: {Tenant}",
              tenant);
            await action(eventToken.TokenSource.Token);
            this._logger.LogInformation(
              "Service Configuration saved for tenant: {Tenant}",
              tenant);
            // Remove process from dictionary (if it exists)
            this.RemoveProcessFromQueue(tenant, eventToken);
            break;
          } catch (HttpRequestException ex) {
            // Request failed, add process to dictionary
            this._logger.LogError(ex,
              "HTTP Request Exception Code {Code}: {Message}",
              ex.StatusCode,
              ex.Message);
          }

          await Task.Delay(this._retryDelay, eventToken.TokenSource.Token);
        }
      } catch (TaskCanceledException e) {
        this._logger.LogError(e,
          "Process cancelled.");
        this.RemoveProcessFromQueue(tenant, this._pendingProcesses[tenant]);
      }
    }
  }

  // Evaluate new request against current pending requests (if any), add to dictionary if needed.
  private Boolean AddUpdateProcessQueue(
    String tenant,
    WatchEventType newRequestType,
    [NotNullWhen(true)]
    out WatcherEventToken? eventToken) {
    if (this._pendingProcesses.TryGetValue(tenant, out var currentProcess)) {
      // if both current and previous processes are of type Deleted, leave previous.
      if ((currentProcess.EventType == WatchEventType.Deleted) && (newRequestType == WatchEventType.Deleted)) {
        eventToken = null;
        return false;
      }
      // previous process exists, cancel
      currentProcess.TokenSource.Cancel();
      this._logger.LogInformation("Cancelled previous process...");
    }
    // add/update entry in dictionary
    this._pendingProcesses[tenant] = eventToken = new WatcherEventToken(newRequestType, new CancellationTokenSource());
    this._logger.LogInformation("Updated entry in process queue");
    return true;
  }

  // Remove pending process from concurrent dictionary
  private void RemoveProcessFromQueue(String tenant, WatcherEventToken value) {
    if (!this._pendingProcesses.TryRemove(new KeyValuePair<String, WatcherEventToken>(tenant, value))) {
      this._logger.LogWarning("Unable to remove entry from dictionary.");
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
