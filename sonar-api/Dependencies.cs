using System;
using System.Net.Http;
using Cms.BatCave.Sonar.Alerting;
using Cms.BatCave.Sonar.Alertmanager;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Data.Services;
using Cms.BatCave.Sonar.Factories;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Helpers.Maintenance;
using Cms.BatCave.Sonar.Maintenance;
using Cms.BatCave.Sonar.Prometheus;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using PrometheusQuerySdk;

namespace Cms.BatCave.Sonar;

public class Dependencies {
  public virtual void RegisterDependencies(WebApplicationBuilder builder) {
    builder.Services.AddScoped<IPrometheusClient>(provider => {
      var config = provider.GetRequiredService<IOptions<PrometheusConfiguration>>();
      return new PrometheusClient(() => {
        var httpClient = new HttpClient();
        httpClient.BaseAddress =
          new Uri($"{config.Value.Protocol}://{config.Value.Host}:{config.Value.Port}/");
        return httpClient;
      });
    });
    builder.Services.AddScoped<ServiceDataHelper>();
    builder.Services.AddScoped<EnvironmentDataHelper>();
    builder.Services.AddScoped<TenantDataHelper>();
    builder.Services.AddScoped<ApiKeyDataHelper>();
    builder.Services.AddScoped<PrometheusQueryHelper>();
    builder.Services.AddScoped<HealthDataHelper>();
    builder.Services.AddScoped<VersionDataHelper>();
    builder.Services.AddScoped<ServiceHealthCacheHelper>();
    builder.Services.AddScoped<ServiceVersionCacheHelper>();
    builder.Services.AddScoped<IApiKeyRepository, DbApiKeyRepository>();
    builder.Services.AddScoped<IPermissionsRepository, DbPermissionRepository>();
    builder.Services.AddScoped<KeyHashHelper>();
    builder.Services.AddScoped<ErrorReportsDataHelper>();
    builder.Services.AddScoped<ValidationHelper>();
    builder.Services.AddScoped<TagsDataHelper>();
    builder.Services.AddScoped<AlertingDataHelper>();
    builder.Services.AddScoped<KubeClientFactory>();
    builder.Services.AddScoped<AlertingGlobalConfigurationGenerator>();
    builder.Services.AddScoped<AlertingReceiverConfigurationGenerator>();
    builder.Services.AddScoped<AlertingRulesConfigurationGenerator>();
    builder.Services.AddScoped<AlertingConfigurationManager>();
    builder.Services.AddScoped<AlertingConfigurationHelper>();
    builder.Services.AddMaintenanceDataHelpers();
    builder.Services.AddScoped<UserDataHelper>();

    builder.Services.AddHttpClient<IPrometheusRemoteProtocolClient, PrometheusRemoteProtocolClient>((provider, client) => {
      var config = provider.GetRequiredService<IOptions<PrometheusConfiguration>>().Value;
      client.BaseAddress = new Uri($"{config.Protocol}://{config.Host}:{config.Port}");
    });
    builder.Services.AddScoped<IPrometheusService, PrometheusService>();
    builder.Services.AddHttpClient<IAlertmanagerClient, AlertmanagerClient>((provider, client) => {
      var config = provider.GetRequiredService<IOptions<AlertmanagerConfiguration>>().Value;
      client.BaseAddress = new Uri($"{config.Protocol}://{config.Host}:{config.Port}");
      client.Timeout = TimeSpan.FromSeconds(config.RequestTimeoutSeconds);
    });
    builder.Services.AddScoped<IAlertmanagerService, AlertmanagerService>();
    builder.Services.AddScoped<IDbMigrationService, DbMigrationService>();

    // Register all Configuration Option Classes for dependency injection
    new ConfigurationDependencyRegistration(builder.Configuration).RegisterDependencies(builder.Services);
    // Register DataContext and DbSet<> dependencies
    this.RegisterDataDependencies(builder);

    builder.Services.AddScoped<AlertingRulesConfigurationGenerator>();

    builder.Services.AddOpenTelemetry()
      .WithMetrics(metricBuilder => {
        metricBuilder.AddMeter("Sonar.EntityFramework.DbCommands");
        metricBuilder.AddPrometheusExporter();
      });

    builder.Services.AddHostedService<AlertingConfigSyncService>();
    builder.Services.AddMaintenanceStatusRecordingService();
  }

  protected virtual void RegisterDataDependencies(WebApplicationBuilder builder) {
    DataDependencyRegistration.RegisterDependencies<DataContext>(builder.Services);
  }
}
