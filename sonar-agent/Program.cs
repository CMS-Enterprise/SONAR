using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Loki;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Prometheus;
using Cms.BatCave.Sonar.Query;
using Microsoft.Extensions.Configuration;

namespace Cms.BatCave.Sonar.Agent;

internal static class Program {
  private static readonly JsonSerializerOptions ConfigSerializerOptions = new JsonSerializerOptions {
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
  };

  private static async Task Main(String[] args) {
    // API Configuration
    var builder = new ConfigurationBuilder()
      .SetBasePath(Directory.GetCurrentDirectory())
      .AddJsonFile("appsettings.json", false, true)
      .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ENVIRONMENT")}.json", true, true)
      .AddEnvironmentVariables()
      .AddCommandLine(args);
    IConfigurationRoot configuration = builder.Build();
    var apiConfig = configuration.GetSection("ApiConfig").BindCtor<ApiConfiguration>();
    var promConfig = configuration.GetSection("Prometheus").BindCtor<PrometheusConfiguration>();
    var lokiConfig = configuration.GetSection("Loki").BindCtor<LokiConfiguration>();
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
      // Load and merge configs
      var servicesHierarchy = await Program.LoadAndValidateJsonServiceConfig(args, token);
      // Configure service hierarchy
      Console.WriteLine("Configuring services....");
      await Program.ConfigureServices(apiConfig, servicesHierarchy, token);
      // Hard coded 10 second interval
      var interval = TimeSpan.FromSeconds(10);
      Console.WriteLine("Initializing SONAR Agent...");
      // Run task that calls Health Check function
      var task = Task.Run(async delegate {
        await RunScheduledHealthCheck(interval, apiConfig, promConfig, lokiConfig, token);
      }, token);
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
    String[] args,
    CancellationToken token) {

    List<ServiceHierarchyConfiguration> validConfigs = new List<ServiceHierarchyConfiguration>();
    foreach (var config in args) {
      await using var inputStream = new FileStream(config, FileMode.Open, FileAccess.Read);
      using JsonDocument document = await JsonDocument.ParseAsync(inputStream, cancellationToken: token);
      var configRoot = document.RootElement;

      try {
        var serviceHierarchy =
          JsonSerializer.Deserialize<ServiceHierarchyConfiguration>(configRoot, Program.ConfigSerializerOptions);
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


  private static async Task ConfigureServices(
    ApiConfiguration apiConfig,
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
    TimeSpan interval, ApiConfiguration config, PrometheusConfiguration pConfig, LokiConfiguration lConfig, CancellationToken token) {
    // Configs
    var env = config.Environment;
    var tenant = config.Tenant;
    // SONAR client
    var client = new SonarClient(baseUrl: config.BaseUrl, new HttpClient());
    await client.ReadyAsync(token);
    var i = 0;

    // Prometheus client
    using var promHttpClient = new HttpClient();
    promHttpClient.BaseAddress = new Uri($"{pConfig.Protocol}://{pConfig.Host}:{pConfig.Port}/");
    var promClient = new PrometheusClient(promHttpClient);
    // Loki Client
    using var lokiHttpClient = new HttpClient();
    lokiHttpClient.BaseAddress = new Uri($"{lConfig.Protocol}://{lConfig.Host}:{lConfig.Port}/");
    var lokiClient = new LokiClient(lokiHttpClient);
    // HTTP Metric client
    using var httpMetricClient = new HttpClient();
    httpMetricClient.Timeout = TimeSpan.FromSeconds(5);
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
      var tenantResult = await client.GetTenantAsync(config.Environment, config.Tenant, token);
      Console.WriteLine($"Service Count: {tenantResult.Services.Count}");
      // Iterate over each service
      foreach (var service in tenantResult.Services) {
        // Initialize aggStatus to null
        HealthStatus? aggStatus = null;
        // Get service's health checks here
        var healthChecks = service.HealthChecks;
        var checkResults = new Dictionary<String, HealthStatus>();
        // If no checks are returned, log error and continue
        if (healthChecks == null) {
          Console.WriteLine("No Health Checks associated with this service.");
          continue;
        }

        foreach (var healthCheck in healthChecks) {
          HealthStatus currCheck;

          switch (healthCheck.Type) {
            case HealthCheckType.PrometheusMetric:
              var definition = (PrometheusHealthCheckDefinition)healthCheck.Definition;
              currCheck = await Program.RunHealthCheck(promClient, service, healthCheck, definition, token);
              break;
            case HealthCheckType.LokiMetric:
              var lokiDefinition = (LokiHealthCheckDefinition)healthCheck.Definition;
              currCheck = await Program.RunLokiHealthCheck(lokiClient, service, healthCheck, lokiDefinition, token);
              break;
            case HealthCheckType.HttpRequest:
              var httpDefinition = (HttpHealthCheckDefinition)healthCheck.Definition;
              currCheck = await Program.RunHttpHealthCheck(
                httpMetricClient,
                service,
                healthCheck,
                httpDefinition,
                token);
              break;
            default:
              throw new NotSupportedException("Healthcheck Type is not supported.");
          }
          // If currCheck is Unknown or currCheck is worse than aggStatus (as long as aggStatus is not Unknown)
          // set aggStatus to currCheck
          if ((currCheck == HealthStatus.Unknown) ||
              ((aggStatus != HealthStatus.Unknown) && (currCheck > (aggStatus ?? 0)))) {
            aggStatus = currCheck;
          }

          // Set checkResults
          checkResults.Add(healthCheck.Name, currCheck);
        }

        // Send result data here
        if (aggStatus != null) {
          await SendHealthData(env, tenant, service.Name, checkResults, client, aggStatus ?? HealthStatus.Unknown,
            token);
        }
      }

      Console.WriteLine($"Iteration {i} of health check.");
      await Task.Delay(interval, token);
      i++;
    }
  }

  private static async Task<HealthStatus> RunHealthCheck(
    IPrometheusClient promClient,
    ServiceConfiguration service,
    HealthCheckModel healthCheck,
    PrometheusHealthCheckDefinition definition,
    CancellationToken token) {

    // Set start and end for date range, Get Prometheus samples
    var end = DateTime.UtcNow;
    var start = end.Subtract(definition.Duration);
    var qrResult = await promClient.QueryRangeAsync(
      definition.Expression, start, end, TimeSpan.FromSeconds(1), null, token
    );

    return ProcessQueryResults(service, healthCheck, definition.Conditions, qrResult);
  }

  private static async Task<HealthStatus> RunLokiHealthCheck(
    ILokiClient lokiClient,
    ServiceConfiguration service,
    HealthCheckModel healthCheck,
    LokiHealthCheckDefinition definition,
    CancellationToken token) {

    // Set start and end for date range, Get Prometheus samples
    var end = DateTime.UtcNow;
    var start = end.Subtract(definition.Duration);
    var qrResult = await lokiClient.QueryRangeAsync(
      definition.Expression, start, end, direction: Direction.Forward, cancellationToken: token
    );

    return ProcessQueryResults(service, healthCheck, definition.Conditions, qrResult);
  }

  private static HealthStatus ProcessQueryResults(
    ServiceConfiguration service,
    HealthCheckModel healthCheck,
    IImmutableList<MetricHealthCondition> conditions,
    ResponseEnvelope<QueryResults> qrResult) {

    // Error handling
    var currCheck = HealthStatus.Online;
    if (qrResult.Data == null) {
      // No data, bad request
      Console.Error.WriteLine($"Returned nothing for health check: {healthCheck.Name}");
      currCheck = HealthStatus.Unknown;
    } else if (qrResult.Data.Result.Count > 1) {
      // Bad config, multiple time series returned
      Console.Error.WriteLine(
        $"Invalid configuration, multiple time series returned for health check: {healthCheck.Name}");
      currCheck = HealthStatus.Unknown;
    } else if ((qrResult.Data.Result.Count == 0) ||
               (qrResult.Data.Result[0].Values == null) ||
               (qrResult.Data.Result[0].Values!.Count == 0)) {
      // No samples
      Console.Error.WriteLine($"Returned no samples for health check: {healthCheck.Name}");
      currCheck = HealthStatus.Unknown;
    } else {
      // Successfully obtained samples from PromQL, evaluate against all conditions for given check
      var samples = qrResult.Data.Result[0].Values;
      foreach (var condition in conditions) {
        // Determine which comparison to execute
        // Evaluate all PromQL samples
        var evaluation = Program.EvaluateSamples(condition.HealthOperator, samples!, condition.Threshold);
        // If evaluation is true, set the current check to the condition's status
        // and output to Stdout
        if (evaluation) {
          currCheck = condition.HealthStatus;
          break;
        }
      }
    }

    return currCheck;
  }

    private static async Task<HealthStatus> RunHttpHealthCheck(
    HttpClient client,
    ServiceConfiguration service,
    HealthCheckModel healthCheck,
    HttpHealthCheckDefinition definition,
    CancellationToken token) {

    var currCheck = HealthStatus.Online;

    // Initialize variables needed for Http request and request duration calculation.
    TimeSpan duration;
    HttpResponseMessage response;

    try {
      // Send request to url specified in definition, calculate duration of request
      DateTime now = DateTime.Now;
      response = await client.GetAsync(definition.Url, token);
      duration = DateTime.Now - now;
    } catch (HttpRequestException e) {

      // Request failed, set currCheck to offline and return.
      return HealthStatus.Offline;
    } catch (InvalidOperationException e) {

      // Error with requestURI, log and return unknown status.
      Console.Error.WriteLine($"Invalid request URI: ${definition.Url}");
      return HealthStatus.Unknown;
    } catch (UriFormatException e) {

      // Invalid request URI format, log and return unknown status.
      Console.Error.WriteLine($"Invalid request URI format: {definition.Url}");
      return HealthStatus.Unknown;
    }

    // Passed error handling, get status code from response.
    var statusCode = (ushort)response.StatusCode;

    // Evaluate response based on conditions
    //  If there is a ResponseTimeCondition, evaluate.
    //  If there is a StatusCodeCondition, evaluate.
    foreach (var condition in definition.Conditions) {
      // Evaluate conditions based on http condition type.
      if (condition.Type == HttpHealthCheckConditionType.HttpResponseTime) {
        var responseCondition = (ResponseTimeCondition)condition;
        if (duration > responseCondition.ResponseTime) {
          Console.WriteLine("Request duration exceeded threshold.");
          currCheck = responseCondition.Status;
        }
      } else if (condition.Type == HttpHealthCheckConditionType.HttpStatusCode) {
        var statusCondition = (StatusCodeCondition)condition;
        if (statusCondition.StatusCodes.Contains(statusCode)) {
          Console.WriteLine($"Request status code {statusCode} met condition.");
          currCheck = statusCondition.Status;
        }
      }
    }

    return currCheck;
  }

  private static async Task SendHealthData(
    String env, String tenant, String service,
    Dictionary<String, HealthStatus> results, SonarClient client, HealthStatus aggStatus, CancellationToken token) {
    var ts = DateTime.UtcNow;
    var healthChecks = new ReadOnlyDictionary<String, HealthStatus>(results);
    ServiceHealth body = new ServiceHealth(ts, aggStatus, healthChecks);

    Console.WriteLine(
      $"Env: {env}, Tenant: {tenant}, Service: {service}, Time: {body.Timestamp}, AggStatus: {body.AggregateStatus}");
    body.HealthChecks.ToList().ForEach(x => Console.WriteLine($"{x.Key}: {x.Value}"));
    try {
      await client.RecordStatusAsync(env, tenant, service, body, token);
    } catch (ApiException e) {
      Console.Error.WriteLine($"HTTP Request Error, Code: {e.StatusCode}, Message: {e.Message}");
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
