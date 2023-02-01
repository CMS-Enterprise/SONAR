using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Options;
using Cms.BatCave.Sonar.Agent.Logger;
using Cms.BatCave.Sonar.Configuration;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Agent;

internal class Program {
  private static Task Main(String[] args) {
    // Command Line Parsing
    var parser = new Parser(settings => {
      // Assume that unknown arguments will be handled by the dotnet Command-line configuration
      // provider
      settings.IgnoreUnknownArguments = true;
      settings.HelpWriter = Console.Error;
    });
    var parserResult = parser.ParseArguments<SonarAgentOptions>(args);
    return parserResult
      .MapResult(
        Program.RunAgent,
        notParsedFunc: _ => Task.FromResult(1)
      );
  }

  private static async Task<Int32> RunAgent(SonarAgentOptions opts) {

    // API Configuration
    var builder = new ConfigurationBuilder()
      .SetBasePath(Directory.GetCurrentDirectory())
      .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
      .AddJsonFile(
        Path.Combine(opts.AppSettingsLocation, "appsettings.json"),
        optional: true,
        reloadOnChange: true)
      .AddJsonFile(
        Path.Combine(opts.AppSettingsLocation, $"appsettings.{Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Development"}.json"),
        optional: true,
        reloadOnChange: true)
      .AddEnvironmentVariables();

    IConfigurationRoot configuration = builder.Build();
    var apiConfig = configuration.GetSection("ApiConfig").BindCtor<ApiConfiguration>();
    var promConfig = configuration.GetSection("Prometheus").BindCtor<PrometheusConfiguration>();
    var lokiConfig = configuration.GetSection("Loki").BindCtor<LokiConfiguration>();
    var agentConfig = configuration.GetSection("AgentConfig").BindCtor<AgentConfiguration>();

    // Create cancellation source, token, new task
    var source = new CancellationTokenSource();
    CancellationToken token = source.Token;

    // Configure logging
    using var loggerFactory = LoggerFactory.Create(loggingBuilder => {
      loggingBuilder
        .AddConfiguration(configuration.GetSection("Logging"))
        .AddConsole(options => options.FormatterName = nameof(CustomFormatter))
        .AddConsoleFormatter<CustomFormatter, LoggingCustomOptions>();
    });

    var logger = loggerFactory.CreateLogger<Program>();

    // Event handler for SIGINT
    // Traps SIGINT to perform necessary cleanup
    Console.CancelKeyPress += delegate {
      logger.Log(LogLevel.Information, "\nSIGINT received, begin cleanup...");
      source.Cancel();
    };

    // Load and merge configs
    var servicesHierarchy = await ConfigurationHelper.LoadAndValidateJsonServiceConfig(
      opts.ServiceConfigFiles.ToArray(), source.Token);
    // Configure service hierarchy
    logger.LogInformation("Configuring services....");
    await ConfigurationHelper.ConfigureServices(configuration, apiConfig, servicesHierarchy, source.Token);
    var interval = TimeSpan.FromSeconds(agentConfig.AgentInterval);

    logger.LogInformation("Initializing SONAR Agent...");
    var healthCheckHelper = new HealthCheckHelper(loggerFactory.CreateLogger<HealthCheckHelper>());
    // Run task that calls Health Check function
    var task = Task.Run(
      () => healthCheckHelper.RunScheduledHealthCheck(
          interval, configuration, apiConfig, promConfig, lokiConfig, source.Token),
      source.Token
    );

    await task;

    return 0;
  }
}
