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
using Cms.BatCave.Sonar.Agent.Telemetry;
using Cms.BatCave.Sonar.Agent.VersionChecks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Factories;
using Cms.BatCave.Sonar.Loki;
using Cms.BatCave.Sonar.Models;
using PrometheusQuerySdk;
using CommandLine;
using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using KubernetesConfigurationMonitor = Cms.BatCave.Sonar.Agent.ServiceConfig.KubernetesConfigurationMonitor;

namespace Cms.BatCave.Sonar.Agent;

internal class Program {
  private static Task<Int32> Main(String[] args) {
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
    RecordOptionsManager<MetricServerConfiguration> metricsConfig;
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
      metricsConfig = Dependencies.CreateRecordOptions<MetricServerConfiguration>(
        configuration, "MetricServer", loggerFactory);
    } catch (RecordBindingException ex) {
      logger.LogError(ex, "Invalid sonar-agent configuration. {_Message}", ex.Message);
      return 1;
    }

    using var meterProvider = Sdk.CreateMeterProviderBuilder()
      .AddMeter("Sonar.HealthStatus")
      .AddMeter("System.Runtime")
      .AddRuntimeInstrumentation()
      .AddPrometheusHttpListener(options => {
        options.UriPrefixes = new[] {
          $"http://*:{metricsConfig.Value.Port}/"
        };
      })
      .Build();

    using var listener = new RuntimeCounterEventListener();

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

    var configFiles = opts.ServiceConfigFiles.ToArray();
    var configSources = configFiles.Length > 0 ?
      new[] {
        new LocalFileServiceConfigSource(agentConfig.Value.DefaultTenant, configFiles)
      } :
      Enumerable.Empty<IServiceConfigSource>();

    var kubeClientFactory = new KubeClientFactory(loggerFactory.CreateLogger<KubeClientFactory>());

    if (opts.KubernetesConfigurationOption) {
      var kubeClient = kubeClientFactory.CreateKubeClient(agentConfig.Value.InClusterConfig);
      disposables.Add(kubeClient);
      configSources =
        configSources.Append(
          new KubernetesConfigSource(kubeClient, loggerFactory.CreateLogger<KubernetesConfigSource>())
        );
    }

    (IDisposable, ISonarClient) SonarClientFactory() {
      var http = new HttpClient();
      return (http, new SonarClient(apiConfig, http));
    }

    var errorReportsHelper = new ErrorReportsHelper(
      SonarClientFactory,
      loggerFactory.CreateLogger<ErrorReportsHelper>());

    var configurationHelper = new ConfigurationHelper(
      new AggregateServiceConfigSource(configSources),
      SonarClientFactory,
      loggerFactory.CreateLogger<ConfigurationHelper>(),
      errorReportsHelper
    );

    AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
      UnhandledExceptionErrorReportHandler(e.ExceptionObject, logger, errorReportsHelper,
        apiConfig.Value.Environment,
        token).Wait();

    IDictionary<String, ServiceHierarchyConfiguration> servicesHierarchy;
    try {
      // Load and merge configs
      servicesHierarchy = await configurationHelper.LoadAndValidateJsonServiceConfigAsync(
        apiConfig.Value.Environment,
        token);
    } catch (Exception ex) when (ex is InvalidConfigurationException or ArgumentException) {
      logger.LogError(ex, "Invalid Service Configuration: {_Message}", ex.Message);
      return 1;
    }

    // Configure service hierarchy
    logger.LogInformation("Configuring services....");

    const Int32 threshold = 10;
    var isSuccess = false;
    var retryDelay = TimeSpan.FromSeconds(30);
    for (var attempts = 0; attempts <= threshold; attempts++) {
      logger.LogInformation("Saving configuration, attempt {Attempts}", attempts);
      try {
        // if IsNonProd has a value (false or true) make sure it matches the sonar api environment setting.
        if (apiConfig.Value.IsNonProd != null) {
          // Call sonar API and get the environment's isNonProd value. Compare the value to the configuration value.
          // If they do not match use the configuration value to update Sonar api.
          var envHealth = await GetOrCreateEnvironment(apiConfig, agentConfig.Value.AgentInterval, logger, token);
          if (apiConfig.Value.IsNonProd != envHealth.IsNonProd) {
            //Update isNonProd in Sonar API
            EnvironmentModel model = new EnvironmentModel(apiConfig.Value.Environment, (Boolean)apiConfig.Value.IsNonProd);
            try {
              await UpdateEnvironment(apiConfig, agentConfig.Value.AgentInterval, model, logger, token);
            } catch (ApiException apiEx) when (apiEx is ApiException { StatusCode: 403 }) {
              logger.LogWarning("Unable to update environment {Environment}: {_Message} ", apiConfig.Value.Environment, apiEx.Message);
              await errorReportsHelper.CreateErrorReport(
                apiConfig.Value.Environment,
                new ErrorReportDetails(
                  DateTime.UtcNow,
                  null,
                  null,
                  null,
                  AgentErrorLevel.Warning,
                  AgentErrorType.SaveConfiguration,
                  $"Unable to update environment {apiConfig.Value.Environment}: {apiEx.Message} ",
                  null,
                  null),
                token);
            }
          }
        }

        await configurationHelper.ConfigureServicesAsync(apiConfig.Value.Environment, servicesHierarchy, token);
        isSuccess = true;
        break;
      } catch (HttpRequestException ex) {
        logger.LogError(ex,
          "HTTP Request Exception Code {StatusCode}: {_Message}",
          ex.StatusCode,
          ex.Message);
        // create error report
        await errorReportsHelper.CreateErrorReport(
          apiConfig.Value.Environment,
          new ErrorReportDetails(
            DateTime.UtcNow,
            null,
            null,
            null,
            AgentErrorLevel.Error,
            AgentErrorType.SaveConfiguration,
            ex.Message,
            null,
            null),
          token);
      } catch (ApiException ex) {
        logger.LogError(ex,
          "SONAR API Returned an Error {StatusCode}: {_Message}",
          ex.StatusCode,
          ex.Message);
        // create error report
        await errorReportsHelper.CreateErrorReport(
          apiConfig.Value.Environment,
          new ErrorReportDetails(
            DateTime.UtcNow,
            null,
            null,
            null,
            AgentErrorLevel.Error,
            AgentErrorType.SaveConfiguration,
            ex.Message,
            null,
            null),
          token);
      }

      await Task.Delay(retryDelay, token);
    }

    if (!isSuccess) {
      var maxConfigSavingErrMessage = "Maximum number of attempts reached for configuration saving";
      logger.LogError(maxConfigSavingErrMessage);
      await errorReportsHelper.CreateErrorReport(apiConfig.Value.Environment,
        new ErrorReportDetails(
          DateTime.UtcNow,
          null,
          null,
          null,
          AgentErrorLevel.Fatal,
          AgentErrorType.SaveConfiguration,
          maxConfigSavingErrMessage,
          null,
          null),
        token);
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
        loggerFactory.CreateLogger<HttpHealthCheckEvaluator>(),
        SonarClientFactory),
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
                return (httpClient, new SonarClient(apiConfig, httpClient));
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
                return (httpClient, new SonarClient(apiConfig, httpClient));
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

    var healthCheckHelper = new HealthCheckHelper(
      loggerFactory,
      apiConfig,
      agentConfig,
      httpHealthCheckQueue,
      prometheusHealthCheckQueue,
      lokiHealthCheckQueue,
      errorReportsHelper);

    var tasks = new List<(Task Task, String Description, String? Tenant)>(new (Task, String, String?)[] {
      (httpHealthCheckQueue.Run(token), "Http Health Check Queue Processor", null),
      (prometheusHealthCheckQueue.Run(token), "Prometheus Health Check Queue Processor", null),
      (lokiHealthCheckQueue.Run(token), "Loki Health Check Queue Processor", null),
    });

    // Create Version Check Queue Processors

    using var versionCheckQueueProcessor = new VersionCheckQueueProcessor(agentConfig);

    // Always start the HTTP Version Check processing task.
    using var versionRequesterHttpClient = new HttpClient();
    versionRequesterHttpClient.Timeout = TimeSpan.FromSeconds(agentConfig.Value.AgentInterval);
    var httpVersionRequester = new HttpResponseBodyVersionRequester(versionRequesterHttpClient);
    tasks.Add((versionCheckQueueProcessor.StartAsync(httpVersionRequester, token), "Http Version Check Processor", null));

    using var sonarHttpClient = new HttpClient();
    sonarHttpClient.Timeout = TimeSpan.FromSeconds(agentConfig.Value.AgentInterval);
    var versionCheckHelper = new VersionCheckHelper(
      loggerFactory.CreateLogger<VersionCheckHelper>(),
      agentConfig,
      apiConfig,
      versionCheckQueueProcessor,
      new SonarClient(apiConfig, sonarHttpClient));

    // Run task that calls Health Check and Version Check function for every tenant
    if (opts.KubernetesConfigurationOption) {
      var kubeClient = kubeClientFactory.CreateKubeClient(agentConfig.Value.InClusterConfig);
      var k8sWatcher = new KubernetesConfigurationMonitor(
        apiConfig.Value.Environment,
        configurationHelper,
        kubeClient,
        loggerFactory.CreateLogger<KubernetesConfigurationMonitor>(),
        errorReportsHelper);

      // Start the Kustomization Version Check processing task
      var kustomizationVersionRequester = new FluxKustomizationVersionRequester(kubeClient);
      tasks.Add((
        versionCheckQueueProcessor.StartAsync(kustomizationVersionRequester, token),
        "FluxKustomization Version Check Processor",
        null
      ));

      // Start the HelmRelease Version Check processing task
      var helmReleaseVersionRequester = new FluxHelmReleaseVersionRequester(
        kubeClient,
        loggerFactory.CreateLogger<FluxHelmReleaseVersionRequester>());
      tasks.Add((
        versionCheckQueueProcessor.StartAsync(helmReleaseVersionRequester, token),
        "FluxHelmRelease Version Check Processor",
        null
      ));

      //Start the Kubernetes resource version check processing task
      var kubernetesVersionRequester = new KubernetesImageVersionRequester(kubeClient);
      tasks.Add((
        versionCheckQueueProcessor.StartAsync(kubernetesVersionRequester, token),
        "Kubernetes resource version check processor",
        null
        ));

      disposables.Add(k8sWatcher);
      disposables.Add(kubeClient);

      k8sWatcher.TenantCreated += (sender, args) => {
        tasks.Add((
          healthCheckHelper.RunScheduledHealthCheck(args.Tenant, source, token),
          "Health Check Executor",
          args.Tenant
        ));
        tasks.Add((
          versionCheckHelper.RunScheduledVersionChecks(args.Tenant, source, token),
          "Version Check Executor",
          args.Tenant
        ));
      };
    }

    // The namespace watcher will automatically start threads for existing tenants configured via
    // Kubernetes, but we have to manually start the default tenant if it exists.
    if (servicesHierarchy.TryGetValue(agentConfig.Value.DefaultTenant, out var services)) {
      // We don't monitor local files for changes, so if there aren't any services configured,
      // there is no need to start a thread.
      if (services.Services.Count > 0) {
        tasks.Add((
          healthCheckHelper.RunScheduledHealthCheck(agentConfig.Value.DefaultTenant, source, token),
          "Health Check Executor",
          agentConfig.Value.DefaultTenant
        ));
        tasks.Add((
          versionCheckHelper.RunScheduledVersionChecks(agentConfig.Value.DefaultTenant, source, token),
          "Version Check Executor",
          agentConfig.Value.DefaultTenant
        ));
      }
    }

    // Wait until user, or one of the processor threads requests cancellation
    try {
      await Task.Delay(Timeout.Infinite, token);
    } catch (OperationCanceledException) {
      // User request cancellation
    }

    logger.LogDebug("SONAR Agent process cancelled");

    var error = false;
    // This should wait for completion and raise exceptions that occurred on worker threads
    foreach (var (task, desc, tenant) in tasks) {
      try {
        if (tenant != null) {
          logger.LogDebug("Awaiting Task: {_Description} (Tenant: {Tenant}, Status: {Status})", desc, tenant, task.Status);
        } else {
          logger.LogDebug("Awaiting Task: {_Description} (Status: {Status})", desc, task.Status);
        }
        await task;
      } catch (OperationCanceledException) {
        // Ignore user requested cancellation errors
      } catch (Exception ex) {
        if (tenant != null) {
          logger.LogError(ex, "Task '{_Description}' raised an unhandled exception (Tenant: {Tenant})", desc, tenant);
        } else {
          logger.LogError(ex, "Task '{_Description}' raised an unhandled exception", desc);
        }
        error = true;
      }
    }

    foreach (var watcher in disposables) {
      watcher.Dispose();
    }

    return error ? 1 : 0;
  }

  static async Task UnhandledExceptionErrorReportHandler(
    Object exceptionObj,
    ILogger<Program> logger,
    ErrorReportsHelper errorReportsHelper,
    String env,
    CancellationToken token) {
    var e = (Exception)exceptionObj;

    // create error report
    await errorReportsHelper.CreateErrorReport(env,
      new ErrorReportDetails(
        DateTime.UtcNow,
        null,
        null,
        null,
        AgentErrorLevel.Fatal,
        AgentErrorType.Unknown,
        $"Unhandled exception occured with following message: {e.Message}",
        null,
        null),
      token);
    logger.LogError(e, "Unhandled exception occured with following message: {_Message}", e.Message);
  }

  static async Task<EnvironmentHealth> GetOrCreateEnvironment(RecordOptionsManager<ApiConfiguration> apiConfig, double waitInterval, ILogger logger, CancellationToken token) {

    EnvironmentHealth environmentHealth;
    using var httpClient = new HttpClient() {
      Timeout = TimeSpan.FromSeconds(waitInterval)
    };
    var sonarClient = new SonarClient(apiConfig, httpClient);
    try {
      environmentHealth = await sonarClient.GetEnvironmentAsync(apiConfig.Value.Environment, token);
    } catch (ApiException apiEx) {
      if (apiEx.StatusCode == 404) {
        logger.LogInformation("{Environment} not found attempting to re-create it: ", apiConfig.Value.Environment);
        EnvironmentModel m = new EnvironmentModel(apiConfig.Value.Environment, false);
        var envModel = await sonarClient.CreateEnvironmentAsync(m, token);
        environmentHealth = new EnvironmentHealth(environmentName: envModel.Name, isNonProd: envModel.IsNonProd, aggregateStatus: null);
      } else {
        throw;
      }
    }
    return environmentHealth;
  }

  static async Task UpdateEnvironment(RecordOptionsManager<ApiConfiguration> apiConfig, Double waitInterval, EnvironmentModel model, ILogger logger, CancellationToken token) {

    using var httpClient = new HttpClient() {
      Timeout = TimeSpan.FromSeconds(waitInterval)
    };
    var sonarClient = new SonarClient(apiConfig, httpClient);
    await sonarClient.UpdateEnvironmentAsync(model.Name, model, token);
  }

}
