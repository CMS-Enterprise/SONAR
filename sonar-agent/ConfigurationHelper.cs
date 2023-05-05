using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Configuration;
using Cms.BatCave.Sonar.Agent.Options;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Agent;

public class ConfigurationHelper {
  private readonly RecordOptionsManager<ApiConfiguration> _apiConfig;
  private readonly ILogger<ConfigurationHelper> _logger;
  private readonly IConfigurationRoot _configRoot;
  private IKubernetes? _kubeClient;
  private const String TenantLabel = "sonar-monitoring/tenant";
  private const String ConfigOrderLabel = "sonar-config/order";
  public const String NamespaceLabelSelector = "sonar-monitoring=enabled";
  public const String ConfigMapLabelSelector = "sonar-config";
  private const String ServiceConfigurationFile = "service-config.json";

  public ConfigurationHelper(
    RecordOptionsManager<ApiConfiguration> apiConfig,
    ILogger<ConfigurationHelper> logger,
    IConfigurationRoot configRoot) {
    this._apiConfig = apiConfig;
    this._logger = logger;
    this._configRoot = configRoot;
    this._kubeClient = null;
  }

  private static readonly JsonSerializerOptions ConfigSerializerOptions = new() {
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
  };

  public static ServiceHierarchyConfiguration GetServiceHierarchyConfigurationFromJson(String jsonString) {
    try {
      var configuration =
        JsonSerializer.Deserialize<ServiceHierarchyConfiguration>(jsonString, ConfigSerializerOptions);

      if (configuration == null) {
        throw new InvalidConfigurationException("Invalid JSON service configuration: deserialized object is null.");
      }

      configuration.Validate();
      return configuration;
    } catch (Exception e) {
      throw new InvalidConfigurationException(message: $"Invalid JSON service configuration: {e.Message}", e);
    }
  }

  public static async Task<ServiceHierarchyConfiguration> GetServiceHierarchyConfigurationFromJsonAsync(
    Stream jsonStream,
    CancellationToken cancellationToken = default) {

    var jsonString = await new StreamReader(jsonStream).ReadToEndAsync(cancellationToken);
    return GetServiceHierarchyConfigurationFromJson(jsonString);
  }

  /// <summary>
  ///   Load Service Configuration from both local files and the Kubernetes API according to command line
  ///   options.
  /// </summary>
  /// <param name="opts">SONAR Agent command line options</param>
  /// <param name="agentConfig">SONAR Agent configuration</param>
  /// <param name="token">Cancellation token</param>
  /// <returns>
  ///   A dictionary of tenant names to <see cref="ServiceHierarchyConfiguration" /> for that tenant.
  /// </returns>
  /// <exception cref="ArgumentException">
  ///   Thrown when no tenant configuration is found.
  /// </exception>
  public async Task<Dictionary<String, ServiceHierarchyConfiguration>> LoadAndValidateJsonServiceConfig(
    SonarAgentOptions opts,
    AgentConfiguration agentConfig,
    CancellationToken token) {

    Dictionary<String, ServiceHierarchyConfiguration> configurationByTenant =
      new Dictionary<String, ServiceHierarchyConfiguration>();

    if (opts.KubernetesConfigurationOption) {
      // load configs from kubernetes api
      configurationByTenant = this.LoadKubernetesConfiguration(agentConfig.InClusterConfig);
    }

    if (opts.ServiceConfigFiles.Any()) {
      // load configs locally
      configurationByTenant.Add(
        agentConfig.DefaultTenant,
        await LoadLocalConfiguration(opts.ServiceConfigFiles.ToArray(), token)
      );
    }

    // if configuration set is empty, throw exception
    if (configurationByTenant.Count == 0) {
      throw new ArgumentException(
        "There are no tenants configured, either in local configuration files or in Kubernetes namespaces."
      );
    }

    // merge dictionaries in resultSet
    return configurationByTenant;
  }

  /// <summary>
  ///   Load service configuration from Kubernetes.
  /// </summary>
  /// <remarks>
  ///   Any Kubernetes Namespace having a label "sonar-monitoring" with the value "enabled" will be
  ///   scanned for ConfigMaps with the label "sonar-config" having the value "true". These ConfigMaps
  ///   are expected to have a single data key "service-config.json" which will be deserialized as
  ///   <see cref="ServiceHierarchyConfiguration" />. When there are multiple such ConfigMaps they will
  ///   be merged in the order specified in the "sonar-config/order" label.
  /// </remarks>
  /// <param name="inClusterConfig"></param>
  /// <returns>
  ///   A dictionary mapping the name of each Tenant with configuration in Kubernetes to it's
  ///   <see cref="ServiceHierarchyConfiguration" />.
  /// </returns>
  /// <exception cref="InvalidOperationException">
  ///   One of the service configurations found in Kubernetes was not valid.
  /// </exception>
  private Dictionary<String, ServiceHierarchyConfiguration> LoadKubernetesConfiguration(Boolean inClusterConfig) {
    // TODO(BATAPI-207): currently if an tenant has invalid configuration that prevents configuration being
    // loaded from all tenants. An error in one tenant's configuration should not adversely affect
    // other tenants.

    Dictionary<String, ServiceHierarchyConfiguration> result =
      new Dictionary<String, ServiceHierarchyConfiguration>();

    var tenantConfigDictionary = this.FetchKubeConfiguration(inClusterConfig);
    foreach (var kvp in tenantConfigDictionary) {
      var services = this.GetServicesHierarchy(kvp.Value);
      result.Add(kvp.Key, services);
    }

    return result;
  }

  /// <summary>
  ///   Loads <see cref="ServiceHierarchyConfiguration" /> from a list of local json files by
  ///   deserializing and merging the files together in the order they are specified.
  /// </summary>
  private static async Task<ServiceHierarchyConfiguration> LoadLocalConfiguration(
    String[] filePaths,
    CancellationToken token
    ) {

    var services = new List<ServiceHierarchyConfiguration>();

    foreach (var config in filePaths) {
      await using var inputStream = new FileStream(config, FileMode.Open, FileAccess.Read);
      services.Add(await GetServiceHierarchyConfigurationFromJsonAsync(inputStream, token));
    }

    return services.Aggregate(MergeConfigurations);
  }

  private static ServiceHierarchyConfiguration MergeConfigurations(
    ServiceHierarchyConfiguration prev,
    ServiceHierarchyConfiguration next) {
    // Compare services
    var serviceResults = prev.Services;
    foreach (var currService in next.Services) {
      // If current service was not in previous service list, add to current service list
      var existingService = prev.Services.SingleOrDefault(x =>
        String.Equals(x.Name, currService.Name, StringComparison.OrdinalIgnoreCase));
      if (existingService == null) {
        serviceResults = serviceResults.Add(currService);
      } else {
        // current service exists in previous list, replace with newer version
        serviceResults = serviceResults.Select(x => {
          if (String.Equals(x.Name, currService.Name, StringComparison.OrdinalIgnoreCase)) {
            return currService;
          } else {
            return x;
          }
        }).ToImmutableList();
      }
    }

    // Merge Root Services
    return new ServiceHierarchyConfiguration(
      serviceResults,
      prev.RootServices.Concat(next.RootServices).ToImmutableHashSet()
    );
  }

  /// <summary>
  ///   Save the specified service configuration to the SONAR API. This configuration may be for a new or
  ///   existing tenant.
  /// </summary>
  public async Task ConfigureServices(
    Dictionary<String, ServiceHierarchyConfiguration> tenantServiceDictionary,
    CancellationToken token) {

    // Create sonar client and send all tenant configurations
    // TODO(BATAPI-208): make this a dependency injected via the ConfigurationHelper constructor
    using var http = new HttpClient();
    var client = new SonarClient(this._configRoot, this._apiConfig.Value.BaseUrl, http);

    foreach (var tenantServices in tenantServiceDictionary) {
      var tenant = tenantServices.Key;
      var servicesHierarchy = tenantServices.Value;
      try {
        // Set up service configuration for specified environment and tenant
        await client.CreateTenantAsync(this._apiConfig.Value.Environment, tenant, servicesHierarchy, token);
      } catch (ApiException requestException) {
        if (requestException.StatusCode == 409) {
          // Updating service configuration for existing environment and tenant
          await client.UpdateTenantAsync(this._apiConfig.Value.Environment, tenant, servicesHierarchy, token);
        }
      } catch (HttpRequestException ex) {
        this._logger.LogError($"An network error occurred attempting to setup configuration in Sonar Central for Tenant {tenant} : {ex.Message}");
      } catch (TaskCanceledException ex) {
        this._logger.LogError($"HTTP request timed out attempting setup configuration in Sonar Central for Tenant {tenant} : {ex.Message}");
      } catch (Exception ex) {
        this._logger.LogError($"Failed to setup configuration in Sonar Central for Tenant {tenant} : {ex.Message}");
      }
    }
  }

  public async Task DeleteServices(
    String tenant,
    CancellationToken token) {

    // SONAR client
    using var http = new HttpClient();
    var client = new SonarClient(this._configRoot, this._apiConfig.Value.BaseUrl, http);

    try {
      await client.DeleteTenantAsync(this._apiConfig.Value.Environment, tenant, token);
    } catch (ApiException e) {
      this._logger.LogError(
        e,
        "Failed to delete tenant service configuration in SONAR API, Code: {StatusCode}, Message: {Message}",
        e.StatusCode,
        e.Message
      );
    }
  }

  /// <summary>
  ///   Fetch configuration from the Kubernetes API.
  /// </summary>
  /// <remarks>
  ///   Any Kubernetes Namespace having a label "sonar-monitoring" with the value "enabled" will be
  ///   scanned for ConfigMaps with the label "sonar-config" having the value "true". These ConfigMaps
  ///   are expected to have a single data key "service-config.json". When there are multiple such
  ///   ConfigMaps they will be returned in the order specified in the "sonar-config/order" label (from
  ///   least to greatest).
  /// </remarks>
  private Dictionary<String, List<(Int16 order, String data)>> FetchKubeConfiguration(Boolean inClusterConfig) {

    var config = inClusterConfig ? KubernetesClientConfiguration.InClusterConfig() :
      KubernetesClientConfiguration.BuildDefaultConfig();
    this._kubeClient = new Kubernetes(config);

    Dictionary<String, List<(Int16 order, String data)>> results =
      new Dictionary<String, List<(Int16 order, String data)>>();

    this._logger.LogDebug("Host: {Host}, Namespace: {Namespace}, BaseUri: {BaseUri}",
      config.Host, config.Namespace, this._kubeClient.BaseUri);

    // get list of namespaces
    var list = this._kubeClient.CoreV1.ListNamespace(labelSelector: NamespaceLabelSelector);

    this._logger.LogDebug("Found Namespaces: {Namespaces}", String.Join(
      ",",
      list.Items.Select(i => i.Metadata.Name)));

    foreach (var item in list.Items) {

      // if namespace is null, skip
      if (item == null) {
        continue;
      }

      // determine tenant and get its service configuration
      var tenant = this.GetTenantFromNamespaceConfig(item);
      var configMaps = this._kubeClient.CoreV1.ListNamespacedConfigMap(item.Metadata.Name);
      var sortedConfigs = this.GetServiceConfigurationList(configMaps);

      results.Add(tenant, sortedConfigs);
    }

    return results;
  }

  public String GetTenantFromNamespaceConfig(V1Namespace namespaceConfig) {
    return namespaceConfig.Metadata.Labels.ContainsKey(TenantLabel) ?
      namespaceConfig.Metadata.Labels[TenantLabel] :
      namespaceConfig.Metadata.Name;
  }

  private IDictionary<String, String> GetConfigMapLabels(V1ConfigMap map) {
    var labels = map.Metadata.EnsureLabels();
    this._logger.LogDebug("Found ConfigMap: {ConfigMap}, Labels: {Labels}",
      map.Metadata.Name,
      String.Join(",", labels.Select(l => l.Key)));
    return labels;
  }

  private Boolean IsSonarConfigMap(IDictionary<String, String> configMapLabels) {
    if ((configMapLabels == null) ||
      (!configMapLabels.ContainsKey(ConfigMapLabelSelector) ||
        (configMapLabels[ConfigMapLabelSelector] != "true"))) {
      return false;
    }

    return true;
  }

  private Int16 GetConfigurationOrder(
    V1ConfigMap map,
    IDictionary<String, String> labels) {

    // determine order from label, set to max value if order not specified
    return labels.ContainsKey(ConfigOrderLabel) ?
      Int16.Parse(map.Metadata.Labels[ConfigOrderLabel]) :
      Int16.MaxValue;
  }

  public List<(Int16 order, String data)>? GetServiceConfigurationList(V1ConfigMapList configMaps) {
    var unsortedConfigs =
      new List<(Int16 order, String data)>();

    foreach (var map in configMaps.Items) {
      if (map == null) {
        continue;
      }
      var labels = this.GetConfigMapLabels(map);
      if (!this.IsSonarConfigMap(labels)) {
        continue;
      }

      var order = this.GetConfigurationOrder(map, labels);

      unsortedConfigs.Add((order, map.Data.ContainsKey(ServiceConfigurationFile) ?
        map.Data[ServiceConfigurationFile] : String.Empty));
    }

    return unsortedConfigs.OrderBy(c => c.order).ToList();
  }

  public ServiceHierarchyConfiguration? GetServicesHierarchy(
    List<(Int16 order, String data)> sortedConfigs) {

    var services = new List<ServiceHierarchyConfiguration>();

    // Get valid service configuration
    foreach (var config in sortedConfigs) {
      services.Add(GetServiceHierarchyConfigurationFromJson(config.data));
    }

    return services.Aggregate(MergeConfigurations);
  }

  public IKubernetes GetKubernetesClient() {
    return this._kubeClient;
  }
}
