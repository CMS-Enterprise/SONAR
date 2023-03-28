using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Json;
using Cms.BatCave.Sonar.Middlewares;
using Cms.BatCave.Sonar.OpenApi;
using Cms.BatCave.Sonar.Options;
using CommandLine;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace Cms.BatCave.Sonar;

public class Program {
  public static async Task<Int32> Main(String[] args) {
    var builder = Program.CreateWebApplicationBuilder(args, new Dependencies());

    return await HandleCommandLine(args,
      // serve command
      async opts => {
        // Configure additional services that are only available when running the serve command.
        // Note: anything registered or configured here is not available when running unit tests.

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
          options.SchemaFilter<TupleSchemaFilter>();
        });

        builder.WebHost.UseUrls("http://0.0.0.0:8081");

        await using var app = BuildApplication(builder);
        return await RunServe(app, opts);
      },
      // init command
      async opts => {
        await using var app = builder.Build();
        return await RunInit(app, opts);
      }
    );
  }

  public static WebApplicationBuilder CreateWebApplicationBuilder(
    String[] args,
    Dependencies dependencies) {

    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddLogging(logging => {
      logging
        .AddConsole(consoleOptions => {
          consoleOptions.LogToStandardErrorThreshold = LogLevel.Error;
        });
    });

    dependencies.RegisterDependencies(builder);

    return builder;
  }

  public static WebApplication BuildApplication(WebApplicationBuilder builder) {
    // Web API Specific Dependencies
    var mvcBuilder = builder.Services.AddControllers(options => {
      options.ReturnHttpNotAcceptable = true;
    });

    mvcBuilder.AddJsonOptions(options => {
      // Serialize c# enums as strings
      options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
      options.JsonSerializerOptions.Converters.Add(new ArrayTupleConverterFactory());
    });

    mvcBuilder.AddApplicationPart(typeof(Program).Assembly);

    var app = builder.Build();

    // Get CORS configuration
    var webHostConfig =
      app.Configuration.GetSection("WebHost").BindCtor<WebHostConfiguration>();
    if (webHostConfig.AllowedOrigins != null) {
      app.UseCors(policyBuilder => {
        policyBuilder.WithOrigins(webHostConfig.AllowedOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod();
      });
    }

    app.UseMiddleware<ProblemDetailExceptionMiddleware>();
    app.UseMiddleware<ApiKeyMiddleware>();
    app.UseAuthorization();

    // Route requests based on Controller attribute annotations
    app.MapControllers();

    return app;
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
    using (var initScope = app.Services.CreateScope()) {
      var logger = initScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
      var config = initScope.ServiceProvider.GetRequiredService<IConfiguration>();
      if (!config.GetSection("ApiKey").Exists()) {
        logger.LogWarning("Default ApiKey not set in configuration");
      } else {
        // Validate API key
        Program.ValidateApiKey(logger, config.GetValue<String>("ApiKey"));
      }
    }

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

  private const Int32 ApiKeyByteLength = 32;
  private static Boolean ValidateApiKey(
    ILogger logger,
    String configApiKey) {
    Boolean apiKeyIsValid = false;

    // Check if configured API key is Base64 and of correct length
    try {
      var decodedBytes = Convert.FromBase64String(configApiKey);

      if (decodedBytes.Length != ApiKeyByteLength) {
        logger.LogError("Default ApiKey Validation: Invalid length for API key");
      }

      apiKeyIsValid = true;
    } catch {
      logger.LogError("Default ApiKey Validation: API key is not Base64 encoded");
    }

    return apiKeyIsValid;
  }
}
