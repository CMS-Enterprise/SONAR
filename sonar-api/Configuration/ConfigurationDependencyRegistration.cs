using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cms.BatCave.Sonar.Configuration;

public class ConfigurationDependencyRegistration {
  private readonly ConfigurationManager _configuration;

  public ConfigurationDependencyRegistration(ConfigurationManager configuration) {
    this._configuration = configuration;
  }

  public void RegisterDependencies(IServiceCollection services) {
    services.ConfigureRecord<DatabaseConfiguration>(this._configuration.GetSection("Database"));
    services.ConfigureRecord<PrometheusConfiguration>(this._configuration.GetSection("Prometheus"));
    services.ConfigureRecord<SonarHealthCheckConfiguration>(this._configuration.GetSection("SonarHealthCheck"));
    services.ConfigureRecord<WebHostConfiguration>(this._configuration.GetSection("WebHost"));
    services.ConfigureRecord<LegacyEndpointConfiguration>(this._configuration.GetSection("LegacyEndpoint"));
    services.ConfigureRecord<SecurityConfiguration>(this._configuration.GetSection("Security"));
    services.ConfigureRecord<OktaConfiguration>(this._configuration.GetSection("Okta"));
  }
}
