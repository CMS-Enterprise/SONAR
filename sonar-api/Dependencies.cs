using System;
using System.Net.Http;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Controllers;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Prometheus;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar;

public class Dependencies {
  public virtual void RegisterDependencies(WebApplicationBuilder builder) {
    builder.Services.AddScoped<PrometheusRemoteWriteClient>();
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
    builder.Services.AddScoped<HealthDataHelper>();
    builder.Services.AddScoped<CacheHelper>();
    builder.Services.AddScoped<IApiKeyRepository, DbApiKeyRepository>(serviceProvider =>
      new DbApiKeyRepository(
        serviceProvider.GetRequiredService<DataContext>(),
        serviceProvider.GetRequiredService<DbSet<ApiKey>>(),
        serviceProvider.GetRequiredService<DbSet<Environment>>(),
        serviceProvider.GetRequiredService<DbSet<Tenant>>()
      ));

    builder.Services.AddHttpClient<IPrometheusRemoteProtocolClient, PrometheusRemoteProtocolClient>((provider, client) => {
      var config = provider.GetRequiredService<IOptions<PrometheusConfiguration>>().Value;
      client.BaseAddress = new Uri($"{config.Protocol}://{config.Host}:{config.Port}");
    });
    builder.Services.AddScoped<IPrometheusService, PrometheusService>();
    // Register all Configuration Option Classes for dependency injection
    new ConfigurationDependencyRegistration(builder.Configuration).RegisterDependencies(builder.Services);
    // Register DataContext and DbSet<> dependencies
    this.RegisterDataDependencies(builder);
  }

  protected virtual void RegisterDataDependencies(WebApplicationBuilder builder) {
    DataDependencyRegistration.RegisterDependencies<DataContext>(builder.Services);
  }
}
