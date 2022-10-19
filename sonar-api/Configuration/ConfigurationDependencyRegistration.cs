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
  }
}
