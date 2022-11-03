using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Json;
using Cms.BatCave.Sonar.OpenApi;
using Cms.BatCave.Sonar.Options;
using CommandLine;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

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

    builder.Services.AddScoped<PrometheusRemoteWriteClient>();

    var mvcBuilder = builder.Services.AddControllers(options => {
      options.ReturnHttpNotAcceptable = true;
      options.Filters.Add<ProblemDetailExceptionFilterAttribute>();
    });

    mvcBuilder.AddJsonOptions(options => {
      // Serialize c# enums as strings
      options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
      options.JsonSerializerOptions.Converters.Add(new ArrayTupleConverterFactory());
    });

    // Register all Configuration Option Classes for dependency injection
    new ConfigurationDependencyRegistration(builder.Configuration).RegisterDependencies(builder.Services);
    // Register DataContext and DbSet<> dependencies
    DataDependencyRegistration.RegisterDependencies(builder.Services);

    return await HandleCommandLine(args,
      async opts => {
        // Web API Specific Dependencies

        // Enable OpenAPI documentation
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options => {
          options.SwaggerDoc(
            name: "v2",
            new OpenApiInfo {
              Title = "SONAR API v2",
              Version = "v2"
            }
          );
          options.IncludeXmlComments(Path.Combine(
            AppContext.BaseDirectory,
            $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"
          ));

          options.OperationFilter<DefaultContentTypeOperationFilter>();
        });

        builder.WebHost.UseUrls("http://localhost:8081");

        await using var app = builder.Build();
        return await RunServe(app, opts);
      },
      async opts => {
        await using var app = builder.Build();
        return await RunInit(app, opts);
      }
    );
  }

  private static Task<Int32> HandleCommandLine(
    String[] args,
    Func<ServeOptions, Task<Int32>> runServe,
    Func<InitOptions, Task<Int32>> runInit) {

    var useDefaultVerb = ShouldUseDefaultVerb(args);

    var parser = new Parser(settings => {
      // Assume that unknown arguments will be handled by the dotnet Command-line configuration provider
      settings.IgnoreUnknownArguments = true;
      settings.HelpWriter = Console.Error;
    });
    var parserResult =
      parser.ParseArguments<ServeOptions, InitOptions>(useDefaultVerb ? args.Prepend(ServeOptions.VerbName) : args);
    return parserResult
      .MapResult(
        runServe,
        runInit,
        _ => Task.FromResult<Int32>(1)
      );
  }

  private static Boolean ShouldUseDefaultVerb(String[] args) {
    // Unfortunately setting the Verb.IsDefault to true causes the verb to be selected even when
    // an unknown verb is specified, which could lead to unexpected behavior given typos. This code
    // detects if no verb is specified so that we can inject the default.
    var preParser = new Parser(settings => {
      settings.IgnoreUnknownArguments = true;
      settings.AutoHelp = false;
    });
    var preParseResult = preParser.ParseArguments<ServeOptions, InitOptions>(args);
    var preParseErrors = preParseResult.Errors?.ToList();
    var useDefaultVerb =
      preParseErrors is { Count: 1 } &&
      (preParseErrors[0] is NoVerbSelectedError ||
        preParseErrors[0] is BadVerbSelectedError badVerbError && badVerbError.Token.StartsWith("--"));
    return useDefaultVerb;
  }

  private static async Task<Int32> RunServe(WebApplication app, ServeOptions options) {
    // Configure the HTTP request pipeline.
    app.UseSwagger(swaggerOptions => {
      swaggerOptions.RouteTemplate = "api/doc/{documentName}/open-api.{json|yaml}";
    });
    if (app.Environment.IsDevelopment()) {
      app.UseSwaggerUI(swaggerUiOptions => {
        swaggerUiOptions.SwaggerEndpoint("/api/doc/v2/open-api.json", "v2");
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
