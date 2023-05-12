using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Configuration;
using Cms.BatCave.Sonar.Agent.HealthChecks;
using Cms.BatCave.Sonar.Agent.HealthChecks.Metrics;
using Cms.BatCave.Sonar.Agent.Options;
using Cms.BatCave.Sonar.Agent.Logger;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Loki;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Prometheus;
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
        Path.Combine(opts.AppSettingsLocation,
          $"appsettings.{Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Development"}.json"),
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
      if (provider is FileConfigurationProvider fileProvider &&
        fileProvider.Source.FileProvider != null &&
        fileProvider.Source.Path != null) {
        logger.LogInformation("WATCHING {ConfigFileName}",
          fileProvider.Source.FileProvider.GetFileInfo(fileProvider.Source.Path).PhysicalPath);
        var configFileName = relativePathRegex.Replace(fileProvider.Source.Path, replacement: "");
        watchers.Add(
          new ConfigurationWatcher(provider).CreateConfigWatcher(Directory.GetCurrentDirectory(), configFileName));
      }
    }

    RecordOptionsManager<ApiConfiguration> apiConfig;
    RecordOptionsManager<PrometheusConfiguration> promConfig;
    RecordOptionsManager<LokiConfiguration> lokiConfig;
    RecordOptionsManager<AgentConfiguration> agentConfig;
    RecordOptionsManager<HealthCheckQueueProcessorConfiguration> httpHealthCheckConfiguration;
    try {
      apiConfig = Dependencies.CreateRecordOptions<ApiConfiguration>(
        configuration, "ApiConfig", loggerFactory);
      promConfig = Dependencies.CreateRecordOptions<PrometheusConfiguration>(
        configuration, "Prometheus", loggerFactory);
      lokiConfig = Dependencies.CreateRecordOptions<LokiConfiguration>(
        configuration, "Loki", loggerFactory);
      agentConfig = Dependencies.CreateRecordOptions<AgentConfiguration>(
        configuration, "AgentConfig", loggerFactory);
      httpHealthCheckConfiguration = Dependencies.CreateRecordOptions<HealthCheckQueueProcessorConfiguration>(
        configuration, "HttpHealthChecks", loggerFactory);
    } catch (RecordBindingException ex) {
      logger.LogError(ex, "Invalid sonar-agent configuration. {Detail}", ex.Message);
      return 1;
    }

    // Create cancellation source, token, new task
    using var source = new CancellationTokenSource();
    var token = source.Token;

    // Event handler for SIGINT
    // Traps SIGINT to perform necessary cleanup
    Console.CancelKeyPress += delegate {
      logger.Log(LogLevel.Information, "\nSIGINT received, begin cleanup...");
      source?.Cancel();
    };

    var configurationHelper = new ConfigurationHelper(
      apiConfig,
      loggerFactory.CreateLogger<ConfigurationHelper>(),
      configuration);
    Dictionary<String, ServiceHierarchyConfiguration> servicesHierarchy;
    try {
      // Load and merge configs
      servicesHierarchy = await configurationHelper.LoadAndValidateJsonServiceConfig(
        opts, agentConfig.Value, token);
    } catch (Exception ex) when (ex is InvalidConfigurationException) {
      logger.LogError(ex, "Invalid Service Configuration: {Message}", ex.Message);
      return 1;
    } catch (ArgumentException ex) {
      logger.LogError(ex, "No configuration option specified.");
      return 1;
    }

    // Configure service hierarchy
    logger.LogInformation("Configuring services....");
    await configurationHelper.ConfigureServices(servicesHierarchy, token);

    logger.LogInformation("Initializing SONAR Agent...");


    // Create HealthCheck Queue Processors

    // Prometheus client
    HttpClient CreatePrometheusHttpClient() {
      var promHttpClient = new HttpClient();
      promHttpClient.Timeout = TimeSpan.FromSeconds(agentConfig.Value.AgentInterval);
      promHttpClient.BaseAddress = new Uri(
        $"{promConfig.Value.Protocol}://{promConfig.Value.Host}:{promConfig.Value.Port}/");
      return promHttpClient;
    }

    var promClient = new PrometheusClient(CreatePrometheusHttpClient);

    // Loki Client
    HttpClient CreateLokiHttpClient() {
      var lokiHttpClient = new HttpClient();
      lokiHttpClient.Timeout = TimeSpan.FromSeconds(agentConfig.Value.AgentInterval);
      lokiHttpClient.BaseAddress = new Uri(
        $"{lokiConfig.Value.Protocol}://{lokiConfig.Value.Host}:{lokiConfig.Value.Port}/");
      return lokiHttpClient;
    }

    var lokiClient = new LokiClient(CreateLokiHttpClient);

    using var httpHealthCheckQueue = new HealthCheckQueueProcessor<HttpHealthCheckDefinition>(
      new HttpHealthCheckEvaluator(
        agentConfig,
        loggerFactory.CreateLogger<HttpHealthCheckEvaluator>()),
      httpHealthCheckConfiguration,
      loggerFactory.CreateLogger<HealthCheckQueueProcessor<HttpHealthCheckDefinition>>()
    );

    using var prometheusHealthCheckQueue = new HealthCheckQueueProcessor<MetricHealthCheckDefinition>(
      new MetricHealthCheckEvaluator(
        new CachingMetricQueryRunner(
          new ReportingMetricQueryRunner(
            new PrometheusMetricQueryRunner(
              promClient,
              loggerFactory.CreateLogger<PrometheusMetricQueryRunner>()),
            () => {
              var httpClient = new HttpClient();
              httpClient.Timeout = TimeSpan.FromSeconds(agentConfig.Value.AgentInterval);
              try {
                return (httpClient, new SonarClient(configuration, baseUrl: apiConfig.Value.BaseUrl, httpClient));
              } catch {
                httpClient.Dispose();
                throw;
              }
            },
            loggerFactory.CreateLogger<ReportingMetricQueryRunner>())),
        loggerFactory.CreateLogger<MetricHealthCheckEvaluator>()
      ),
      agentConfig,
      loggerFactory.CreateLogger<HealthCheckQueueProcessor<MetricHealthCheckDefinition>>()
    );

    using var lokiHealthCheckQueue = new HealthCheckQueueProcessor<MetricHealthCheckDefinition>(
      new MetricHealthCheckEvaluator(
        new CachingMetricQueryRunner(
          new ReportingMetricQueryRunner(
            new LokiMetricQueryRunner(
              lokiClient,
              loggerFactory.CreateLogger<LokiMetricQueryRunner>()),
            () => {
              var httpClient = new HttpClient();
              httpClient.Timeout = TimeSpan.FromSeconds(agentConfig.Value.AgentInterval);
              try {
                return (httpClient, new SonarClient(configuration, baseUrl: apiConfig.Value.BaseUrl, httpClient));
              } catch {
                httpClient.Dispose();
                throw;
              }
            },
            loggerFactory.CreateLogger<ReportingMetricQueryRunner>())),
        loggerFactory.CreateLogger<MetricHealthCheckEvaluator>()
      ),
      agentConfig,
      loggerFactory.CreateLogger<HealthCheckQueueProcessor<MetricHealthCheckDefinition>>()
    );

    var httpQueueProcessorTask = httpHealthCheckQueue.Run(token);
    var prometheusQueueProcessorTask = prometheusHealthCheckQueue.Run(token);
    var lokiQueueProcessorTask = lokiHealthCheckQueue.Run(token);

    var healthCheckHelper = new HealthCheckHelper(
      loggerFactory, apiConfig, agentConfig, httpHealthCheckQueue, prometheusHealthCheckQueue, lokiHealthCheckQueue);
    var tasks = new List<Task>();

    // Run task that calls Health Check function for every tenant
    if (opts.KubernetesConfigurationOption) {
      var k8sWatcher = new KubernetesConfigurationMonitor(
        loggerFactory.CreateLogger<KubernetesConfigurationMonitor>(),
        configurationHelper);

      k8sWatcher.TenantCreated += (sender, args) => {
        tasks.Add(healthCheckHelper.RunScheduledHealthCheck(configuration, args.Tenant, token));
      };
    } else {
      foreach (var kvp in servicesHierarchy.ToList()) {
        tasks.Add(healthCheckHelper.RunScheduledHealthCheck(configuration, kvp.Key, token));
      }
    }

    // Wait until user requests cancellation
    try {
      await Task.Delay(Timeout.Infinite, token);
    } catch (OperationCanceledException) {
      // User request cancellation
    }

    await Task.WhenAll(tasks);

    foreach (var watcher in watchers) {
      watcher.Dispose();
    }

    return 0;
  }
}
