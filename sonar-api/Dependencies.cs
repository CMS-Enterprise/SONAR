using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cms.BatCave.Sonar;

public class Dependencies {
  public virtual void RegisterDependencies(WebApplicationBuilder builder) {
    builder.Services.AddScoped<PrometheusRemoteWriteClient>();
    builder.Services.AddScoped<ServiceDataHelper>();
    builder.Services.AddScoped<ApiKeyDataHelper>();
    // Register all Configuration Option Classes for dependency injection
    new ConfigurationDependencyRegistration(builder.Configuration).RegisterDependencies(builder.Services);
    // Register DataContext and DbSet<> dependencies
    this.RegisterDataDependencies(builder);
  }

  protected virtual void RegisterDataDependencies(WebApplicationBuilder builder) {
    DataDependencyRegistration.RegisterDependencies<DataContext>(builder.Services);
  }
}
