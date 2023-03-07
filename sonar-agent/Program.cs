using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Options;
using Cms.BatCave.Sonar.Agent.Logger;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Models;
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

    // Configure logging
    using var loggerFactory = LoggerFactory.Create(loggingBuilder => {
      loggingBuilder
        .AddConfiguration(configuration.GetSection("Logging"))
        .AddConsole(options => options.FormatterName = nameof(CustomFormatter))
        .AddConsoleFormatter<CustomFormatter, LoggingCustomOptions>();
    });

    var logger = loggerFactory.CreateLogger<Program>();

    // Create watcher for each configuration file
    var relativePathRegex = new Regex(@"^\./");
    var watchers = new List<IDisposable>();
    foreach (var provider in configuration.Providers) {
      if (provider is FileConfigurationProvider fileProvider) {
        logger.LogInformation("WATCHING {ConfigFileName}",
          fileProvider.Source.FileProvider.GetFileInfo(fileProvider.Source.Path).PhysicalPath);
        var configFileName = relativePathRegex.Replace(fileProvider.Source.Path, replacement: "");
        watchers.Add(
          new ConfigurationWatcher(provider).CreateConfigWatcher(Directory.GetCurrentDirectory(), configFileName));
      }
    }

    var dependencies = new Dependencies();
    RecordOptionsManager<ApiConfiguration> apiConfig;
    RecordOptionsManager<PrometheusConfiguration> promConfig;
    RecordOptionsManager<LokiConfiguration> lokiConfig;
    RecordOptionsManager<AgentConfiguration> agentConfig;
    try {
      apiConfig = dependencies.CreateRecordOptions<ApiConfiguration>(
        configuration, "ApiConfig", loggerFactory);
      promConfig = dependencies.CreateRecordOptions<PrometheusConfiguration>(
        configuration, "Prometheus", loggerFactory);
      lokiConfig = dependencies.CreateRecordOptions<LokiConfiguration>(
        configuration, "Loki", loggerFactory);
      agentConfig = dependencies.CreateRecordOptions<AgentConfiguration>(
        configuration, "AgentConfig", loggerFactory);
    } catch (RecordBindingException ex) {
      logger.LogError(ex, "Invalid sonar-agent configuration. {Detail}", ex.Message);
      return 1;
    }

    // Create cancellation source, token, new task
    using var source = new CancellationTokenSource();
    CancellationToken token = source.Token;

    // Event handler for SIGINT
    // Traps SIGINT to perform necessary cleanup
    Console.CancelKeyPress += delegate {
      logger.Log(LogLevel.Information, "\nSIGINT received, begin cleanup...");
      source.Cancel();
    };

    var configurationHelper = new ConfigurationHelper(apiConfig,
      loggerFactory.CreateLogger<ConfigurationHelper>());
    Dictionary<String, ServiceHierarchyConfiguration> servicesHierarchy;
    try {
      // Load and merge configs
      servicesHierarchy = await configurationHelper.LoadAndValidateJsonServiceConfig(
        opts, agentConfig.Value, source.Token);
    } catch (Exception ex) when (ex is InvalidOperationException or JsonException) {
      logger.LogError(ex, "Invalid Service Configuration: {Message}", ex.Message);
      return 1;
    } catch (ArgumentException ex) {
      logger.LogError(ex, "No configuration option specified.");
      return 1;
    }

    // Configure service hierarchy
    logger.LogInformation("Configuring services....");

    await configurationHelper.ConfigureServices(configuration, servicesHierarchy, source.Token);

    logger.LogInformation("Initializing SONAR Agent...");
    var healthCheckHelper = new HealthCheckHelper(
      loggerFactory.CreateLogger<HealthCheckHelper>(), apiConfig, promConfig, lokiConfig, agentConfig);
    // Run task that calls Health Check function
    var task = Task.Run(
      () => healthCheckHelper.RunScheduledHealthCheck(configuration, source.Token),
      source.Token
    );

    await task;

    foreach (var watcher in watchers) {
      watcher.Dispose();
    }

    return 0;
  }
}
