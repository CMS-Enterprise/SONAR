using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cms.BatCave.Sonar.Agent;

internal static class Program {
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
      var servicesHierarchy = await ConfigurationHelper.LoadAndValidateJsonServiceConfig(args, token);
      // Configure service hierarchy
      Console.WriteLine("Configuring services....");
      await ConfigurationHelper.ConfigureServices(apiConfig, servicesHierarchy, token);
      // Hard coded 10 second interval
      var interval = TimeSpan.FromSeconds(10);
      Console.WriteLine("Initializing SONAR Agent...");
      // Run task that calls Health Check function
      var task = Task.Run(async delegate {
        await HealthCheckHelper.RunScheduledHealthCheck(interval, apiConfig, promConfig, lokiConfig, token);
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
}
