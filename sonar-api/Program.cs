using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Options;
using CommandLine;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar;

public class Program {
  public static async Task<Int32> Main(String[] args) {
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.

    builder.Services.AddLogging(logging => {
      logging
        .AddConsole(consoleOptions => {
          consoleOptions.LogToStandardErrorThreshold = LogLevel.Error;
        });
    });
    builder.Services.AddDbContext<DataContext>();
    builder.Services.AddControllers();

    // Enable OpenAPI documentation
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Register all Configuration Option Classes for dependency injection
    new ConfigurationDependencyRegistration(builder.Configuration).RegisterDependencies(builder.Services);


    // Disable un-awaited task warning because we actually await
    var (errors, result) =
      await Parser.Default.ParseArguments<ServeOptions, InitOptions>(args)
        .MapResult<ServeOptions, InitOptions, Task<(IEnumerable<Error>?, Int32?)>>(
          async opts => {
            builder.WebHost.UseUrls("http://localhost:8081");
            await using var app = builder.Build();
            return (null, await RunServe(app, opts));
          },
          async opts => {
            await using var app = builder.Build();
            return (null, await RunInit(app, opts));
          },
          err => Task.FromResult<(IEnumerable<Error>?, Int32?)>((err, null)));

    return errors?.Any() == true ? 1 : result.GetValueOrDefault(0);
  }

  private static async Task<Int32> RunServe(WebApplication app, ServeOptions options) {
    // Configure the HTTP request pipeline.
    app.UseSwagger(swaggerOptions => {
      swaggerOptions.RouteTemplate = "api/doc/{documentName}/open-api.{json|yaml}";
    });
    if (app.Environment.IsDevelopment()) {
      app.UseSwaggerUI(swaggerUiOptions => {
        swaggerUiOptions.SwaggerEndpoint("/api/doc/v1/open-api.json", "v1");
        swaggerUiOptions.RoutePrefix = "api/doc-ui";
      });
    }

    app.UseAuthorization();
    // Route requests based on Controller attribute annotations
    app.MapControllers();

    await app.RunAsync();

    return 0;
  }

  private static async Task<Int32> RunInit(IHost app, InitOptions opts) {
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (opts.Force) {
      var deleted = await db.Database.EnsureDeletedAsync();
      if (deleted) {
        logger.LogInformation("Existing database deleted");
      }
    }

    try {
      if (!await db.Database.EnsureCreatedAsync()) {
        logger.LogInformation("Database already exists, creation skipped");
      }
    } catch (Exception ex) {
      logger.LogError(ex, "An unexpected error occurred creating the database");
    }

    return 0;
  }
}
