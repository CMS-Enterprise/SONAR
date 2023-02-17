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
using Cms.BatCave.Sonar.Models;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using String = System.String;

namespace Cms.BatCave.Sonar.Agent;

public class ConfigurationHelper {
  private readonly RecordOptionsManager<ApiConfiguration> _apiConfig;
  private readonly ILogger<ConfigurationHelper> _logger;

  public ConfigurationHelper(RecordOptionsManager<ApiConfiguration> apiConfig,
    ILogger<ConfigurationHelper> logger) {
    this._apiConfig = apiConfig;
    this._logger = logger;
  }

  private static readonly JsonSerializerOptions ConfigSerializerOptions = new JsonSerializerOptions {
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
  };

  public async Task<Dictionary<String, ServiceHierarchyConfiguration>> LoadAndValidateJsonServiceConfig(
    SonarAgentOptions opts,
    AgentConfiguration agentConfig,
    CancellationToken token) {

    Dictionary<String, ServiceHierarchyConfiguration> resultSet =
      new Dictionary<String, ServiceHierarchyConfiguration>();

    if (opts.KubernetesConfigurationOption) {
      // load configs from kubernetes api
      resultSet = this.LoadKubernetesConfiguration(agentConfig.InClusterConfig);
    }

    if (opts.ServiceConfigFiles.Any()) {
      // load configs locally
      var args = opts.ServiceConfigFiles.ToArray();
      resultSet.Add(agentConfig.DefaultTenant, await LoadLocalConfiguration(args, token));
    }

    // if configuration set is empty, throw exception
    if (resultSet.Count == 0) {
      throw new ArgumentException();
    }

    // merge dictionaries in resultSet
    return resultSet;
  }

  private Dictionary<String, ServiceHierarchyConfiguration> LoadKubernetesConfiguration(Boolean inClusterConfig) {

    var services = new List<ServiceHierarchyConfiguration>();
    Dictionary<String, ServiceHierarchyConfiguration> result =
      new Dictionary<String, ServiceHierarchyConfiguration>();

    var tenantConfigDictionary = this.FetchKubeConfiguration(inClusterConfig);
    foreach (var kvp in tenantConfigDictionary) {
      foreach (var config in kvp.Value) {
        try {
          var serviceHierarchy =
            JsonSerializer.Deserialize<ServiceHierarchyConfiguration>(
              config.data,
              ConfigurationHelper.ConfigSerializerOptions);
          if (serviceHierarchy == null) {
            throw new InvalidOperationException(
              $"The specified service configuration file ({config}) could not be deserialized."
            );
          }

          var serviceNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
          // Ensure that each service in the list has a unique name
          // Check if a child service exists as a service
          foreach (var service in serviceHierarchy.Services) {
            if (serviceNames.Contains(service.Name)) {
              throw new InvalidOperationException(
                $"The specified service configuration file ({config}) is not valid. The service name '{service.Name}' is used multiple times."
              );
            } else {
              serviceNames.Add(service.Name);
            }

            if (service.Children != null) {
              foreach (var child in service.Children) {
                // Don't use serviceNames here because we don't want the order in which services are
                // declared to matter
                if (!serviceHierarchy.Services.Any(svc => String.Equals(svc.Name, child, StringComparison.OrdinalIgnoreCase))) {
                  throw new InvalidOperationException(
                    $"The specified service configuration file ({config}) is not valid. The child service '{child}' does not exist in the services array."
                  );
                }
              }
            }
          }

          // Check if root service exists as a service
          foreach (var rootService in serviceHierarchy.RootServices) {
            if (!serviceNames.Contains(rootService)) {
              throw new InvalidOperationException(
                $"The specified service configuration file ({config}) is not valid. The root service {rootService} does not exist in the services array."
              );
            }
          }

          // Add valid config to list
          services.Add(serviceHierarchy);
        } catch (KeyNotFoundException) {
          throw new InvalidOperationException("Service configuration is invalid.");
        }
      }
      result.Add(kvp.Key, services.Aggregate(MergeConfigurations));
    }

    return result;
  }


  private static async Task<ServiceHierarchyConfiguration> LoadLocalConfiguration(
    String[] args,
    CancellationToken token
    ) {

    var services = new List<ServiceHierarchyConfiguration>();

    foreach (var config in args) {
      await using var inputStream = new FileStream(config, FileMode.Open, FileAccess.Read);
      try {
        var serviceHierarchy =
          await JsonSerializer.DeserializeAsync<ServiceHierarchyConfiguration>(
            inputStream,
            ConfigurationHelper.ConfigSerializerOptions,
            token
          );
        if (serviceHierarchy == null) {
          throw new InvalidOperationException(
            $"The specified service configuration file ({config}) could not be deserialized."
          );
        }

        var serviceNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        // Ensure that each service in the list has a unique name
        // Check if a child service exists as a service
        foreach (var service in serviceHierarchy.Services) {
          if (serviceNames.Contains(service.Name)) {
            throw new InvalidOperationException(
              $"The specified service configuration file ({config}) is not valid. The service name '{service.Name}' is used multiple times."
            );
          } else {
            serviceNames.Add(service.Name);
          }

          if (service.Children != null) {
            foreach (var child in service.Children) {
              // Don't use serviceNames here because we don't want the order in which services are
              // declared to matter
              if (!serviceHierarchy.Services.Any(svc => String.Equals(svc.Name, child, StringComparison.OrdinalIgnoreCase))) {
                throw new InvalidOperationException(
                  $"The specified service configuration file ({config}) is not valid. The child service '{child}' does not exist in the services array."
                );
              }
            }
          }
        }

        // Check if root service exists as a service
        foreach (var rootService in serviceHierarchy.RootServices) {
          if (!serviceNames.Contains(rootService)) {
            throw new InvalidOperationException(
              $"The specified service configuration file ({config}) is not valid. The root service {rootService} does not exist in the services array."
            );
          }
        }

        // Add valid config to list
        services.Add(serviceHierarchy);
      } catch (KeyNotFoundException) {
        throw new InvalidOperationException("Service configuration is invalid.");
      }
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

  public async Task ConfigureServices(
    IConfigurationRoot configRoot,
    Dictionary<String, ServiceHierarchyConfiguration> tenantServiceDictionary,
    CancellationToken token) {

    // SONAR client
    using var http = new HttpClient();
    var client = new SonarClient(configRoot, this._apiConfig.Value.BaseUrl, http);
    await client.ReadyAsync(token);

    foreach (var tenantServices in tenantServiceDictionary) {
      var tenant = tenantServices.Key;
      var servicesHierarchy = tenantServices.Value;
      try {
        // Set up service configuration for specified environment and tenant
        await client.CreateTenantAsync(
            this._apiConfig.Value.Environment, tenant, servicesHierarchy, token);
      } catch (ApiException requestException) {
        if (requestException.StatusCode == 409) {
          // Update service configuration for existing environment and tenant
          await client.UpdateTenantAsync(
              this._apiConfig.Value.Environment, tenant, servicesHierarchy, token);
        }
      }
    }

  }

  public Dictionary<String, List<(Int16 order, String data)>> FetchKubeConfiguration(Boolean inClusterConfig) {

    var config = inClusterConfig ? KubernetesClientConfiguration.InClusterConfig() :
      KubernetesClientConfiguration.BuildDefaultConfig();
    IKubernetes client = new Kubernetes(config);

    Dictionary<String, List<(Int16 order, String data)>> results =
      new Dictionary<String, List<(Int16 order, String data)>>();
    var unsortedConfigs =
      new List<(Int16 order, String data)>();

    this._logger.LogDebug("Host: {Host}, Namespace: {Namespace}, BaseUri: {BaseUri}", config.Host, config.Namespace, client.BaseUri);
    // get list of namespaces
    var list = client.CoreV1.ListNamespace(labelSelector: "sonar-monitoring=enabled");

    this._logger.LogDebug("Found Namespaces: {Namespaces}", String.Join(
      ",",
      list.Items.Select(i => i.Metadata.Name)));

    foreach (var item in list.Items) {

      // if namespace is null, skip
      if (item == null) {
        continue;
      }

      // determine tenant
      var tenant = item.Metadata.Labels.ContainsKey("sonar-monitoring/tenant") ?
        item.Metadata.Labels["sonar-monitoring/tenant"] :
        item.Metadata.Name;

      var configMaps = client.CoreV1.ListNamespacedConfigMap(item.Metadata.Name);

      foreach (var map in configMaps.Items) {
        if (map == null) {
          continue;
        }

        // get labels for current configMap
        var labels = map.Metadata.EnsureLabels();
        this._logger.LogDebug("Found ConfigMap: {ConfigMap}, Labels: {Labels}",
          map.Metadata.Name,
          String.Join(",", labels.Select(l => l.Key)));

        if (labels == null) {
          Console.WriteLine("skipping...");
          continue;
        }

        // skip configMap if it doesn't contain sonar-config label
        if (!labels.ContainsKey("sonar-config") ||
          (labels["sonar-config"] != "true")) {
          continue;
        }

        // determine order from label, set to max value if order not specified
        var order = labels.ContainsKey("sonar-config/order") ?
          Int16.Parse(map.Metadata.Labels["sonar-config/order"]) :
          Int16.MaxValue;

        unsortedConfigs.Add((order, map.Data["service-config.json"]));
      }

      results.Add(tenant, unsortedConfigs.OrderBy(
        c => c.order).ToList());
    }

    return results;
  }
}
