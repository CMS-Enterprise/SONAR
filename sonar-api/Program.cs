using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Authentication;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Data.Services;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Json;
using Cms.BatCave.Sonar.Logger;
using Cms.BatCave.Sonar.Middlewares;
using Cms.BatCave.Sonar.OpenApi;
using Cms.BatCave.Sonar.Options;
using CommandLine;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Okta.AspNetCore;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cms.BatCave.Sonar;

public class Program {
  public static async Task<Int32> Main(String[] args) {
    var builder = Program.CreateWebApplicationBuilder(args, new Dependencies());

    return await HandleCommandLine(args,
      runServe: async opts => {
        // Configure additional services that are only available when running the serve command.
        // Note: anything registered or configured here is not available when running unit tests.

        // Enable OpenAPI documentation
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options => {
          options.SwaggerDoc(
            name: "v2",
            new OpenApiInfo {
              Title = "SONAR API v2",
              Version = "2"
            }
          );
          options.SwaggerDoc(
            name: "v1",
            new OpenApiInfo {
              Title = "Legacy Health Check API",
              Version = "1"
            }
          );
          options.DocInclusionPredicate((docName, description) => {
            if (description.TryGetMethodInfo(out var method) && (method.DeclaringType != null)) {
              var versionAttribute = method.DeclaringType.GetCustomAttribute<ApiVersionAttribute>();
              if (versionAttribute != null) {
                return versionAttribute.Versions.Any(v => $"v{v.MajorVersion?.ToString() ?? v.ToString()}" == docName);
              }
            }

            return false;
          });
          options.IncludeXmlComments(Path.Combine(
            AppContext.BaseDirectory,
            $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"
          ));

          options.OperationFilter<DefaultContentTypeOperationFilter>();
          options.SchemaFilter<TupleSchemaFilter>();
          options.DocumentFilter<FillInVersionParameterFilter>();
        });
        // apply common opts
        ApplyCommonOptions(builder, opts);

        var config = builder.Configuration.GetSection("WebHost").BindCtor<WebHostConfiguration>();
        var urls = new List<String>();
        if (config.BindOptions.HasFlag(BindOption.Ipv4)) {
          urls.Add("http://0.0.0.0:8081");
        }
        if (config.BindOptions.HasFlag(BindOption.Ipv6)) {
          urls.Add("http://[::]:8081");
        }

        builder.WebHost.UseUrls(urls.ToArray());

        await using var app = BuildApplication(builder);
        return await RunServe(app, opts);
      },
      runMigrateDb: async opts => {
        await using var app = builder.Build();
        return await RunMigrateDb(app, opts);
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
          consoleOptions.FormatterName = nameof(CustomFormatter);
          consoleOptions.LogToStandardErrorThreshold = LogLevel.Error;
        }).AddConsoleFormatter<CustomFormatter, LoggingCustomOptions>();
    });

    dependencies.RegisterDependencies(builder);

    return builder;
  }

  public static WebApplication BuildApplication(WebApplicationBuilder builder) {

    // Add Okta Authentication
    var oktaConfig = builder.Configuration.GetSection("Okta").BindCtor<OktaConfiguration>();
    builder.Services
      .AddAuthentication(options => {
        options.DefaultScheme = MultiSchemeAuthenticationHandler.SchemeName;
      })
      .AddOktaWebApi(ToOktaWebApiOptions(oktaConfig))
      .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName,
        options => {
          options.ClaimsIssuer = "sonar-api";
        }
      )
      .AddScheme<AuthenticationSchemeOptions, MultiSchemeAuthenticationHandler>(
        MultiSchemeAuthenticationHandler.SchemeName,
        options => {
        }
      );

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IAuthorizationHandler, EnvironmentTenantScopeAuthorizationHandler>();
    builder.Services.AddScoped<IAuthorizationHandler, IgnoreEnvironmentTenantScopeAuthorizationHandler>();


    var authenticatedPolicy =
      new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddRequirements(new EnvironmentTenantScopeRequirement())
        .Build();

    var adminPolicy =
      new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddRequirements(new EnvironmentTenantScopeRequirement(PermissionType.Admin))
        .Build();

    var allowScopedPolicy =
      new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddRequirements(new IgnoreEnvironmentTenantScopeRequirement())
        .Build();

    builder.Services
      .AddAuthorization(options => {
        options.AddPolicy("Admin", adminPolicy);
        // Grant access to principals that have a different scope than that of the request URL
        options.AddPolicy("AllowAnyScope", allowScopedPolicy);
        options.DefaultPolicy = authenticatedPolicy;
      });

    // Web API Specific Dependencies
    var mvcBuilder = builder.Services.AddControllers(options => {
      options.ReturnHttpNotAcceptable = true;
      options.Filters.Add(new AuthorizeFilter(authenticatedPolicy));
    });

    mvcBuilder.AddJsonOptions(options => {
      // Serialize c# enums as strings
      options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
      options.JsonSerializerOptions.Converters.Add(new ArrayTupleConverterFactory());
    });

    mvcBuilder.AddApplicationPart(typeof(Program).Assembly);

    mvcBuilder.Services
      .AddApiVersioning(options => {
        // options.RouteConstraintName = "apiVersion";
        options.UnsupportedApiVersionStatusCode = 404;
      });

    var app = builder.Build();

    //Open Telemetry, default to /metrics
    app.UseOpenTelemetryPrometheusScrapingEndpoint();

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

    app.UseMiddleware<RequestTracingMiddleware>();
    app.UseMiddleware<ProblemDetailExceptionMiddleware>();
    app.UseAuthentication();

    app.UseMiddleware<UserPermissionClaimsMiddleware>();
    app.UseAuthorization();

    // Route requests based on Controller attribute annotations
    app.MapControllers();

    return app;
  }

  private static OktaWebApiOptions ToOktaWebApiOptions(OktaConfiguration oktaConfig) {
    var oktaOptions = new OktaWebApiOptions {
      OktaDomain = oktaConfig.OktaDomain,
    };

    // If AuthorizationServerId and Audience are not specified, use the Okta defaults
    if (!String.IsNullOrEmpty(oktaConfig.AuthorizationServerId)) {
      oktaOptions.AuthorizationServerId = oktaConfig.AuthorizationServerId;
    }

    if (!String.IsNullOrEmpty(oktaConfig.Audience)) {
      oktaOptions.Audience = oktaConfig.Audience;
    }

    return oktaOptions;
  }

  private static Task<Int32> HandleCommandLine(
    String[] args,
    Func<ServeOptions, Task<Int32>> runServe,
    Func<MigrateDbOptions, Task<Int32>> runMigrateDb) {

    var useDefaultVerb = ShouldUseDefaultVerb(args);

    var parser = new Parser(settings => {
      // Assume that unknown arguments will be handled by the dotnet Command-line configuration provider
      settings.IgnoreUnknownArguments = true;
      settings.HelpWriter = Console.Error;
    });
    var parserResult =
      parser.ParseArguments<ServeOptions, MigrateDbOptions>(
        useDefaultVerb ? args.Prepend(ServeOptions.VerbName) : args);
    return parserResult
      .MapResult(
        runServe,
        runMigrateDb,
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
    var preParseResult = preParser.ParseArguments<ServeOptions, MigrateDbOptions>(args);
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
    app.UseSwaggerUI(swaggerUiOptions => {
      swaggerUiOptions.SwaggerEndpoint(url: "/api/doc/v2/open-api.json", name: "Version 2");
      swaggerUiOptions.SwaggerEndpoint(url: "/api/doc/v1/open-api.json", name: "Version 1 (Legacy)");
      swaggerUiOptions.RoutePrefix = "api/doc-ui";
    });

    await app.RunAsync();

    return 0;
  }

  private static void ApplyCommonOptions(WebApplicationBuilder builder, CommonOptions options) {
    if (!String.IsNullOrEmpty(options.AppSettingsLocation)) {
      builder.Configuration.AddJsonFile(Path.Combine(options.AppSettingsLocation, "appsettings.json"), optional: true);
    }
  }

  private static async Task<Int32> RunMigrateDb(WebApplication app, MigrateDbOptions opts) {
    await using var scope = app.Services.CreateAsyncScope();
    var dbMigrationService = scope.ServiceProvider.GetRequiredService<IDbMigrationService>();

    if (opts.ReCreate) {
      if (app.Environment.IsDevelopment()) {
        await dbMigrationService.ReCreateDbAsync();
      } else {
        throw new InvalidOperationException("Database re-creation is only allowed in development environments!");
      }
    }

    await dbMigrationService.ProvisionMigrationsHistoryTable();

    if (opts.TargetMigration != null) {
      await dbMigrationService.MigrateDbAsync(opts.TargetMigration);
    } else {
      await dbMigrationService.MigrateDbAsync();
    }

    return 0;
  }
}
