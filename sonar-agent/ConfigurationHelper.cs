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
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.Configuration;

namespace Cms.BatCave.Sonar.Agent;

public static class ConfigurationHelper {
  private static readonly JsonSerializerOptions ConfigSerializerOptions = new JsonSerializerOptions {
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
  };

  public static async Task<ServiceHierarchyConfiguration> LoadAndValidateJsonServiceConfig(
    String[] args,
    CancellationToken token) {

    var validConfigs = new List<ServiceHierarchyConfiguration>();
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
        validConfigs.Add(serviceHierarchy);
      } catch (KeyNotFoundException) {
        throw new InvalidOperationException("Service configuration is invalid.");
      }
    }

    // merge valid configs into single ServiceHierarchyConfiguration
    return validConfigs.Aggregate(MergeConfigurations);
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

  public static async Task ConfigureServices(
    IConfigurationRoot configRoot,
    ApiConfiguration apiConfig,
    ServiceHierarchyConfiguration servicesHierarchy,
    CancellationToken token) {

    // SONAR client
    using var http = new HttpClient();
    var client = new SonarClient(configRoot, apiConfig.BaseUrl, http);
    await client.ReadyAsync(token);

    try {
      // Set up service configuration for specified environment and tenant
      await client.CreateTenantAsync(apiConfig.Environment, apiConfig.Tenant, servicesHierarchy, token);
    } catch (ApiException requestException) {
      if (requestException.StatusCode == 409) {
        // Update service configuration for existing environment and tenant
        await client.UpdateTenantAsync(apiConfig.Environment, apiConfig.Tenant, servicesHierarchy, token);
      }
    }
  }
}
