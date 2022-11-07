using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Prometheus;
using Microsoft.Extensions.Configuration;

namespace Cms.BatCave.Sonar.Agent;

internal static class Program {
  private static async Task Main(String[] args) {
    // API Configuration
    var builder = new ConfigurationBuilder()
      .SetBasePath(Directory.GetCurrentDirectory())
      .AddJsonFile("appsettings.json", false, true)
      .AddEnvironmentVariables()
      .AddCommandLine(args);
    IConfigurationRoot configuration = builder.Build();
    var apiConfig = configuration.GetSection("ApiConfig").BindCtor<ApiConfiguration>();

    // Create cancellation source, token, new task
    var source = new CancellationTokenSource();
    CancellationToken token = source.Token;

    // Event handler for SIGINT
    // Traps SIGINT to perform necessary cleanup
    Console.CancelKeyPress += delegate {
      Console.WriteLine("\nSIGINT received, begin cleanup...");
      source.Cancel();
    };

    try {
      var configFilePath = args[0];
      var servicesHierarchy = await Program.LoadAndValidateJsonServiceConfig(configFilePath, token);

      Console.WriteLine("Services:");
      foreach (var service in servicesHierarchy.Services) {
        Console.WriteLine($"- Name: {service.Name}");
        Console.WriteLine($"  DisplayName: {service.DisplayName}");
        Console.WriteLine($"  Description: {service.Description}");
        Console.WriteLine($"  Url: {service.Url}");
        if ((service.Children != null) && (service.Children.Count > 0)) {
          Console.WriteLine($"  Children:");
          foreach (var child in service.Children) {
            Console.WriteLine($"  - {child}");
          }
        }
        else {
          Console.WriteLine($"  Children: {service.Children}");
        }
      }

      Console.WriteLine("Root Services:");
      foreach (var rootService in servicesHierarchy.RootServices) {
        Console.WriteLine($"- {rootService}");
      }

      // Configure service hierarchy
      Console.WriteLine("Configuring services....");
      await Program.ConfigureServices(apiConfig, servicesHierarchy, token);

      // Hard coded 10 second interval
      var interval = TimeSpan.FromSeconds(10);
      Console.WriteLine("Initializing SONAR Agent...");

      // Run task that calls Health Check function
      var task = Task.Run(async delegate { await RunScheduledHealthCheck(interval, token, apiConfig); }, token);

      await task;
    } catch (IndexOutOfRangeException) {
      Console.Error.WriteLine("First command line argument must be service configuration file path.");
    } catch (OperationCanceledException e) {
      Console.Error.WriteLine(e.Message);
      Console.Error.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e.Message}");
      // Additional cleanup goes here
    } finally {
      source.Dispose();
    }
  }

  private static async Task<ServiceHierarchyConfiguration> LoadAndValidateJsonServiceConfig(
    String configFilePath,
    CancellationToken token) {

    await using var inputStream = new FileStream(configFilePath, FileMode.Open, FileAccess.Read);
    using JsonDocument document = await JsonDocument.ParseAsync(inputStream, cancellationToken: token);
    var configRoot = document.RootElement;

    try
    {
      var serviceHierarchy = JsonSerializer.Deserialize<ServiceHierarchyConfiguration>(configRoot);
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
          throw new OperationCanceledException($"{rootService} does not exist as a service in the configuration file.");
        }
      }

      Console.WriteLine("Service configuration is valid.");
      return serviceHierarchy;
    } catch (KeyNotFoundException) {
      throw new OperationCanceledException("Service configuration is invalid.");
    }
  }

  private static async Task ConfigureServices(ApiConfiguration apiConfig,
    ServiceHierarchyConfiguration servicesHierarchy,
    CancellationToken token) {

    // SONAR client
    using var http = new HttpClient();
    var client = new SonarClient(apiConfig.BaseUrl, http);
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

  private static async Task RunScheduledHealthCheck(
    TimeSpan interval, CancellationToken token, ApiConfiguration config) {
    Console.WriteLine($"Environment: {config.Environment}, Tenant: {config.Tenant}");
    // SONAR client
    var client = new SonarClient(baseUrl: "http://localhost:8081/", new HttpClient());
    await client.ReadyAsync(token);
    var i = 0;

    // Prometheus client
    using var httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri("http://localhost:9090/");
    var promClient = new PrometheusClient(httpClient);

    // If SIGINT received before interval starts, throw exception
    if (token.IsCancellationRequested) {
      Console.WriteLine("Health check task was cancelled before it got started.");
      throw new OperationCanceledException();
    }

    while (true) {
      if (token.IsCancellationRequested) {
        Console.WriteLine("cancelled");
        throw new OperationCanceledException();
      }

      // Get service hierarchy for given env and tenant
      var tenant = await client.GetTenantAsync(config.Environment, config.Tenant, token);
      Console.WriteLine($"Service Count: {tenant.Services.Count}");

      // hardcode healthcheck dictionary for now
      var healthChecks = new List<dynamic>();
      dynamic healthCheck1 = new ExpandoObject();
      dynamic healthCheck2 = new ExpandoObject();
      dynamic condition1 = new ExpandoObject();
      dynamic condition2 = new ExpandoObject();
      dynamic condition3 = new ExpandoObject();
      dynamic condition4 = new ExpandoObject();
      dynamic conditionList1 = new List<dynamic>();
      dynamic conditionList2 = new List<dynamic>();

      condition1.status = HealthStatus.Online;
      condition1.op = HealthOperator.GreaterThan;
      condition1.threshold = 3.12;

      condition2.status = HealthStatus.Online;
      condition2.op = HealthOperator.GreaterThan;
      condition2.threshold = 3.12;

      condition3.status = HealthStatus.Degraded;
      condition3.op = HealthOperator.GreaterThan;
      condition3.threshold = 3.11;

      condition4.status = HealthStatus.Offline;
      condition4.op = HealthOperator.GreaterThan;
      condition4.threshold = 3.10;

      conditionList1.Add(condition1);
      conditionList1.Add(condition2);
      conditionList2.Add(condition3);
      conditionList2.Add(condition4);

      healthCheck1.conditions = conditionList1;
      healthCheck1.name = "health-check-1";
      healthCheck2.conditions = conditionList2;
      healthCheck2.name = "health-check-2";

      healthChecks.Add(healthCheck1);
      healthChecks.Add(healthCheck2);

      foreach (var service in tenant.Services) {
        Console.WriteLine($"SONAR_AGENT: Evaluating health for Service: {service.Name}.");
        Int32? aggStatus = null;
        foreach (var healthCheck in healthChecks) {
          var currCheck = HealthStatus.Online;

          // Get Prometheus samples
          // var qResult = await promClient.QueryAsync("test", Convert.ToDateTime("2022-11-03T18:32:00Z"),
          //   TimeSpan.FromMilliseconds(1000), token);
          // var sample = qResult.Data.Result[0].Value;
          var qrResult = await promClient.QueryRangeAsync("test", Convert.ToDateTime("2022-11-03T18:32:00Z"),
            Convert.ToDateTime("2022-11-03T18:32:10Z"), TimeSpan.FromSeconds(1), null, token);

          var samples = qrResult.Data.Result[0].Values;

          foreach (var condition in healthCheck.conditions) {
            // Determine which comparison to execute
            // Evaluate all PromQL samples
            var evaluation = EvaluateSamples(condition.op, samples, condition.threshold);

            // If evaluation is true, set the current check to the condition's status
            // and output to Stdout
            if (evaluation) {
              currCheck = condition.status;
              // Console.WriteLine($"Sample {sample?.Value} was {(HealthOperator)condition.op} {condition.threshold}");
              Console.WriteLine(
                $"Service: {config.Environment}/{config.Tenant}/{service.Name}; Check: {healthCheck.name}; Status: {currCheck}");
              break;
            }
          }

          if (currCheck == HealthStatus.Online) {
            Console.WriteLine($"No conditions were met... service is online");
            Console.WriteLine(
              $"Service: {config.Environment}/{config.Tenant}/{service.Name}; Check: {healthCheck.name}; Status: {currCheck}");
          }

          if ((Int32)currCheck > (aggStatus ?? 0)) {
            aggStatus = (Int32)currCheck;
          }
        }

        Console.WriteLine(
          $"Service: {config.Environment}/{config.Tenant}/{service.Name}; AggStatus: {(aggStatus != null ? ((HealthStatus)aggStatus).ToString() : "Unknown")};"
        );
      }

      Console.WriteLine($"Iteration {i} of health check.");
      await Task.Delay(interval, token);
      i++;
    }
  }

  private static Boolean EvaluateSamples(
    HealthOperator op, IImmutableList<(Decimal Timestamp, String Value)> values, Double threshold) {
    // delegate functions for comparison
    Func<Double, Double, Boolean> equalTo = (x, y) => x == y;
    Func<Double, Double, Boolean> notEqual = (x, y) => x != y;
    Func<Double, Double, Boolean> greaterThan = (x, y) => x > y;
    Func<Double, Double, Boolean> greaterThanOrEqual = (x, y) => x >= y;
    Func<Double, Double, Boolean> lessThan = (x, y) => x < y;
    Func<Double, Double, Boolean> lessThanOrEqual = (x, y) => x <= y;

    Func<Double, Double, Boolean> comparison;
    switch (op) {
      case HealthOperator.Equal:
        comparison = equalTo;
        break;
      case HealthOperator.NotEqual:
        comparison = notEqual;
        break;
      case HealthOperator.GreaterThan:
        comparison = greaterThan;
        break;
      case HealthOperator.GreaterThanOrEqual:
        comparison = greaterThanOrEqual;
        break;
      case HealthOperator.LessThan:
        comparison = lessThan;
        break;
      case HealthOperator.LessThanOrEqual:
        comparison = lessThanOrEqual;
        break;
      default:
        throw new ArgumentException("Invalid comparison operator.");
    }

    // Iterate through list, if all meet condition, return true, else return false if ANY don't meet condition
    return !values.Any(val => !comparison(Convert.ToDouble(val.Value), threshold));
  }
}
