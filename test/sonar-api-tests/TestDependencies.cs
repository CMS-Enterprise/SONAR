using System;
using System.Linq;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Tests;

public class TestDependencies : Dependencies {
  private readonly String _databaseName;
  private readonly ILoggerProvider _loggerProvider;

  public TestDependencies(String databaseName, ILoggerProvider loggerProvider) {
    this._databaseName = databaseName;
    this._loggerProvider = loggerProvider;
  }
  public override void RegisterDependencies(WebApplicationBuilder builder) {
    builder.Logging.AddProvider(this._loggerProvider);

    base.RegisterDependencies(builder);
    builder.Services.Remove(builder.Services.Single(sd => sd.ServiceType == typeof(IOptions<DatabaseConfiguration>)));
    builder.Services.AddScoped<IOptions<DatabaseConfiguration>>(provider => {
      var baseConfig = builder.Configuration.GetSection("Database").BindCtor<DatabaseConfiguration>();
      return new OptionsWrapper<DatabaseConfiguration>(
        new DatabaseConfiguration(
          baseConfig.Host,
          baseConfig.Port,
          baseConfig.Username,
          baseConfig.Password,
          this._databaseName,
          baseConfig.DbLogging
        )
      );
    });
  }
}
