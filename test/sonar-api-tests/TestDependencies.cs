using System;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Tests;

public class TestDependencies : Dependencies {
  private readonly String _databaseName;

  public TestDependencies(String databaseName) {
    this._databaseName = databaseName;
  }
  public override void RegisterDependencies(WebApplicationBuilder builder) {
    builder.Services.AddScoped<IOptions<TestDatabaseConfiguration>>(provider => {
      var baseConfig = provider.GetRequiredService<IOptions<DatabaseConfiguration>>();
      return new OptionsWrapper<TestDatabaseConfiguration>(
        new TestDatabaseConfiguration(
          baseConfig.Value.Host,
          baseConfig.Value.Port,
          baseConfig.Value.Username,
          baseConfig.Value.Password,
          this._databaseName,
          baseConfig.Value.DbLogging
        )
      );
    });
    base.RegisterDependencies(builder);
  }

  protected override void RegisterDataDependencies(WebApplicationBuilder builder) {
    // Use TestDataContext to customize Database settings per-test-class
    DataDependencyRegistration.RegisterDependencies<TestDataContext>(builder.Services);
  }
}
