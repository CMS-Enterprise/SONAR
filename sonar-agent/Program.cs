using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Configuration;
using Cms.BatCave.Sonar.Agent.HealthChecks;
using Cms.BatCave.Sonar.Agent.HealthChecks.Metrics;
using Cms.BatCave.Sonar.Agent.Options;
using Cms.BatCave.Sonar.Logger;
using Cms.BatCave.Sonar.Agent.ServiceConfig;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Loki;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Prometheus;
using CommandLine;
using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using KubernetesConfigurationMonitor = Cms.BatCave.Sonar.Agent.ServiceConfig.KubernetesConfigurationMonitor;

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
    var disposables = new List<IDisposable>();
    foreach (var provider in configuration.Providers) {
      if (provider is FileConfigurationProvider fileProvider &&
        fileProvider.Source.FileProvider != null &&
        fileProvider.Source.Path != null) {
        logger.LogInformation("WATCHING {ConfigFileName}",
          fileProvider.Source.FileProvider.GetFileInfo(fileProvider.Source.Path).PhysicalPath);
        var configFileName = relativePathRegex.Replace(fileProvider.Source.Path, replacement: "");
        disposables.Add(
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
      // ReSharper disable once AccessToDisposedClosure
      // (this is fine, once Program exits this isn't going to get triggered).
      source?.Cancel();
    };

    Kubernetes CreateKubeClient() {
      var config = agentConfig.Value.InClusterConfig ?
        KubernetesClientConfiguration.InClusterConfig() :
        KubernetesClientConfiguration.BuildDefaultConfig();

      var kubernetes = new Kubernetes(config);
      logger.LogDebug(
        "Connecting to Kubernetes Host: {Host}, Namespace: {Namespace}, BaseUri: {BaseUri}",
        config.Host, config.Namespace, kubernetes.BaseUri);
      return kubernetes;
    }

    var configFiles = opts.ServiceConfigFiles.ToArray();
    var configSources = configFiles.Length > 0 ?
      new[] {
        new LocalFileServiceConfigSource(agentConfig.Value.DefaultTenant, configFiles)
      } :
      Enumerable.Empty<IServiceConfigSource>();

    if (opts.KubernetesConfigurationOption) {
      var kubeClient = CreateKubeClient();
      disposables.Add(kubeClient);
      configSources =
        configSources.Append(
          new KubernetesConfigSource(kubeClient, loggerFactory.CreateLogger<KubernetesConfigSource>())
        );
    }

    (IDisposable, ISonarClient) SonarClientFactory() {
      var http = new HttpClient();
      return (http, new SonarClient(configuration, apiConfig.Value.BaseUrl, http));
    }

    var configurationHelper = new ConfigurationHelper(
      new AggregateServiceConfigSource(configSources),
      SonarClientFactory,
      loggerFactory.CreateLogger<ConfigurationHelper>()
    );

    IDictionary<String, ServiceHierarchyConfiguration> servicesHierarchy;
    try {
      // Load and merge configs
      servicesHierarchy = await configurationHelper.LoadAndValidateJsonServiceConfigAsync(token);
    } catch (Exception ex) when (ex is InvalidConfigurationException or ArgumentException) {
      logger.LogError(ex, "Invalid Service Configuration: {Message}", ex.Message);
      return 1;
    }

    // Configure service hierarchy
    logger.LogInformation("Configuring services....");

    Int32 threshold = 10;
    Boolean isSuccess = false;
    TimeSpan retryDelay = TimeSpan.FromSeconds(30);
    for (Int32 attempts = 0; attempts <= threshold; attempts++) {
      logger.LogInformation($"Saving configuration, attempt {attempts}.");
      try {
        await configurationHelper.ConfigureServicesAsync(apiConfig.Value.Environment, servicesHierarchy, token);
        isSuccess = true;
        break;
      } catch (HttpRequestException ex) {
        logger.LogError(ex,
          "HTTP Request Exception Code {Code}: {Message}",
          ex.StatusCode,
          ex.Message);
      }

      await Task.Delay(retryDelay, token);
    }

    if (!isSuccess) {
      logger.LogError("Maximum number of attempts reached for configuration saving.");
      return 1;
    }

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
      var kubeClient = CreateKubeClient();
      var k8sWatcher = new KubernetesConfigurationMonitor(
        apiConfig.Value.Environment,
        configurationHelper,
        kubeClient,
        loggerFactory.CreateLogger<KubernetesConfigurationMonitor>());

      disposables.Add(kubeClient);
      disposables.Add(k8sWatcher);

      k8sWatcher.TenantCreated += (sender, args) => {
        tasks.Add(healthCheckHelper.RunScheduledHealthCheck(configuration, args.Tenant, token));
      };

      // The namespace watcher will automatically start threads for existing tenants configured via
      // Kubernetes, but we have to manually start the default tenant if it exists.
      if (servicesHierarchy.TryGetValue(agentConfig.Value.DefaultTenant, out var services)) {
        // We don't monitor local files for changes, so if there aren't any services configured,
        // there is no need to start a thread.
        if (services.Services.Count > 0) {
          tasks.Add(
            healthCheckHelper.RunScheduledHealthCheck(
              configuration,
              agentConfig.Value.DefaultTenant,
              token
            ));
        }
      }
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

    foreach (var watcher in disposables) {
      watcher.Dispose();
    }

    return 0;
  }
}
