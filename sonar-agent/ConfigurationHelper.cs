using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
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
using Cms.BatCave.Sonar.Models.Validation;
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
        throw new InvalidConfigurationException("Invalid JSON service configuration: Deserialized object is null.");
      }

      var validator = new RecursivePropertyValidator();
      var validationResults = new List<ValidationResult>();
      var isValid = validator.TryValidateObjectProperties(configuration, validationResults);

      if (!isValid) {
        throw new InvalidConfigurationException(
          message: "Invalid JSON service configuration: One or more validation errors occurred.",
          new Dictionary<String, Object?> { ["errors"] = validationResults });
      }

      return configuration;
    } catch (Exception e) when (e is not InvalidConfigurationException) {
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
      configurationByTenant = await this.LoadKubernetesConfigurationAsync(agentConfig.InClusterConfig);
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
  private async Task<Dictionary<String, ServiceHierarchyConfiguration>> LoadKubernetesConfigurationAsync(Boolean inClusterConfig) {
    // TODO(BATAPI-207): currently if an tenant has invalid configuration that prevents configuration being
    // loaded from all tenants. An error in one tenant's configuration should not adversely affect
    // other tenants.

    Dictionary<String, ServiceHierarchyConfiguration> result =
      new Dictionary<String, ServiceHierarchyConfiguration>();

    var tenantConfigDictionary = await this.FetchKubeConfigurationAsync(inClusterConfig);
    foreach (var kvp in tenantConfigDictionary) {
      if (kvp.Value.Count != 0) {
        var services = this.GetServicesHierarchy(kvp.Value);
        if (services != null) {
          result.Add(kvp.Key, services);
        }
      }
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

    // SONAR client
    // TODO(BATAPI-208): make this a dependency injected via the ConfigurationHelper constructor
    using var http = new HttpClient();
    var client = new SonarClient(this._configRoot, this._apiConfig.Value.BaseUrl, http);

    foreach (var tenantServices in tenantServiceDictionary) {
      var tenant = tenantServices.Key;
      var servicesHierarchy = tenantServices.Value;
      try {
        // Set up service configuration for specified environment and tenant
        await client.CreateTenantAsync(
            this._apiConfig.Value.Environment, tenant, servicesHierarchy, token);
      } catch (ApiException requestException) {
        if (requestException.StatusCode == 409) {
          // Updating service configuration for existing environment and tenant
          await client.UpdateTenantAsync(
              this._apiConfig.Value.Environment, tenant, servicesHierarchy, token);
        }
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
  private async Task<Dictionary<String, List<(Int16 order, String data)>>> FetchKubeConfigurationAsync(Boolean inClusterConfig) {

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

      // For this tenant get its service configuration from config maps and secrets
      var tenant = this.GetTenantFromNamespaceConfig(item);
      var configMaps = this._kubeClient.CoreV1.ListNamespacedConfigMap(item.Metadata.Name);
      var configMapConfiguration = this.GetServiceConfigurationList(configMaps);
      var secretConfiguration = await this.GetSecretConfigurationsAsync(item.Metadata.Name);

      // Merge service configuration coming form both secrets and config map and re-order
      var sortedConfigs = configMapConfiguration.Concat(secretConfiguration)
        .OrderBy(serviceConfig => serviceConfig.Item1).ToList();

      if (results.ContainsKey(tenant)) {
        throw new InvalidConfigurationException(
          $"Multiple namespaces are configured with the same SONAR tenant name: {tenant}",
          data: new Dictionary<String, Object?> {
            ["tenant"] = tenant
          }
        );
      }

      results.Add(tenant, sortedConfigs);
    }
    return results;
  }

  private async Task<List<(Int16, String)>> GetSecretConfigurationsAsync(String nameSpace) {
    var unsortedConfigs = new List<(Int16 order, String data)>();

    if (this._kubeClient == null) {
      throw new InvalidOperationException(
        $"The method {nameof(GetSecretConfigurationsAsync)} cannot be called before the Kubernetes API client is initialized"
      );
    }

    //Get secrets for this namespace and collect the sonar configuration.  Only one Secret should have sonar configuration
    var secrets = await this._kubeClient.CoreV1.ListNamespacedSecretWithHttpMessagesAsync(nameSpace);
    //Iterate over the secrets for this namespaces.
    if (secrets != null) {
      foreach (var v1Secret in secrets.Body.Items) {
        //Get the labels to make sure Sonar is enabled for monitoring.  If no, go to the next secret
        if (v1Secret == null) {
          continue;
        }

        //Check that the secret hase the label sonar-config.  If no, go to the next secret
        var labels = this.GetConfigurationLabels(v1Secret.Metadata);
        if (!this.IsSonarConfigMap(labels)) {
          continue;
        }

        //We have a secret in this namespace with sonar enabled and sonar-config set.
        var order = this.GetConfigurationOrder(v1Secret.Metadata);
        var data = this.GetServiceConfigurationFromSecret(v1Secret);
        unsortedConfigs.Add((order, data));
      }
    }

    return unsortedConfigs;
  }

  private String GetServiceConfigurationFromSecret(V1Secret secret) {
    return secret.Data.ContainsKey(ServiceConfigurationFile) ?
      Encoding.UTF8.GetString(secret.Data[ServiceConfigurationFile]) :
      String.Empty;
  }

  public String GetTenantFromNamespaceConfig(V1Namespace namespaceConfig) {
    return namespaceConfig.Metadata.Labels.ContainsKey(TenantLabel) ?
      namespaceConfig.Metadata.Labels[TenantLabel] :
      namespaceConfig.Metadata.Name;
  }

  private IDictionary<String, String> GetConfigurationLabels(V1ObjectMeta meta) {
    var labels = meta.EnsureLabels();
    this._logger.LogDebug("Found Secret: {ConfigMap}, Labels: {Labels}",
        meta.Name,
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
    V1ObjectMeta meta
    ) {

    // determine order from label, set to max value if order not specified
    return meta.Labels.ContainsKey(ConfigOrderLabel) ?
      Int16.Parse(meta.Labels[ConfigOrderLabel]) :
      Int16.MaxValue;
  }

  public List<(Int16 order, String data)> GetServiceConfigurationList(V1ConfigMapList configMaps) {
    var unsortedConfigs =
      new List<(Int16 order, String data)>();

    foreach (var map in configMaps.Items) {
      if (map == null) {
        continue;
      }
      var labels = this.GetConfigurationLabels(map.Metadata);
      if (!this.IsSonarConfigMap(labels)) {
        continue;
      }

      var order = this.GetConfigurationOrder(map.Metadata);

      unsortedConfigs.Add((order, map.Data.ContainsKey(ServiceConfigurationFile) ?
        map.Data[ServiceConfigurationFile] : String.Empty));
    }

    return unsortedConfigs;
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
    if (this._kubeClient == null) {
      throw new InvalidOperationException();
    }
    return this._kubeClient;
  }
}
