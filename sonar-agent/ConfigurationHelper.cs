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

    List<ServiceHierarchyConfiguration> validConfigs = new List<ServiceHierarchyConfiguration>();
    foreach (var config in args) {
      await using var inputStream = new FileStream(config, FileMode.Open, FileAccess.Read);
      using JsonDocument document = await JsonDocument.ParseAsync(inputStream, cancellationToken: token);
      var configRoot = document.RootElement;

      try {
        var serviceHierarchy =
          JsonSerializer.Deserialize<ServiceHierarchyConfiguration>(configRoot, ConfigSerializerOptions);
        var listServiceConfig = serviceHierarchy.Services;
        if (listServiceConfig == null) {
          throw new OperationCanceledException("There is no configuration for services.");
        }

        var listRootServices = serviceHierarchy.RootServices;
        if (listRootServices == null) {
          throw new OperationCanceledException("There is no configuration for root services.");
        }

        var servicesList = new List<String>();

        // Check if a child service exists as a service
        foreach (var service in listServiceConfig) {
          servicesList.Add(service.Name);

          if (service.Children != null) {
            foreach (var child in service.Children) {
              if (!servicesList.Contains(child)) {
                throw new OperationCanceledException($"{child} does not exist as a service in the configuration file.");
              }
            }
          }
        }

        // Check if root service exists as a service
        foreach (var rootService in listRootServices) {
          if (!servicesList.Contains(rootService)) {
            throw new OperationCanceledException(
              $"{rootService} does not exist as a service in the configuration file.");
          }
        }

        Console.WriteLine("Service configuration is valid.");
        // Add valid config to list
        validConfigs.Add(serviceHierarchy);
      } catch (KeyNotFoundException) {
        throw new OperationCanceledException("Service configuration is invalid.");
      }
    }

    // merge valid configs into single ServiceHierarchyConfiguration
    ServiceHierarchyConfiguration result = validConfigs.Aggregate(MergeConfigurations);
    // Print merged services and root services to console
    Console.WriteLine("Services:");
    foreach (var service in result.Services) {
      Console.WriteLine($"- Name: {service.Name}");
      Console.WriteLine($"  DisplayName: {service.DisplayName}");
      Console.WriteLine($"  Description: {service.Description}");
      Console.WriteLine($"  Url: {service.Url}");
      if ((service.Children != null) && (service.Children.Count > 0)) {
        Console.WriteLine($"  Children:");
        foreach (var child in service.Children) {
          Console.WriteLine($"  - {child}");
        }
      } else {
        Console.WriteLine($"  Children: {service.Children}");
      }
    }

    Console.WriteLine("Root Services:");
    foreach (var rootService in result.RootServices) {
      Console.WriteLine($"- {rootService}");
    }

    return result;
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
        Console.WriteLine($"service {existingService.Name} already exists, replacing with newer version.");
        serviceResults = serviceResults.Select(x => {
          if (String.Equals(x.Name, currService.Name, StringComparison.OrdinalIgnoreCase)) {
            return currService;
          } else {
            return x;
          }
        }).ToImmutableList();
      }
    }

    // Replace Root Services
    return new ServiceHierarchyConfiguration(serviceResults, next.RootServices);
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
