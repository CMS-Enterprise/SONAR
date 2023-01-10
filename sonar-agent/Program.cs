using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Options;
using Cms.BatCave.Sonar.Configuration;
using CommandLine;
using Microsoft.Extensions.Configuration;

namespace Cms.BatCave.Sonar.Agent;

internal static class Program {
  private static async Task Main(String[] args) {
    // Command Line Parsing
    var test= await HandleCommandLine(args,
      async opts => await RunSettings(opts));
  }
  private static Task<Int32> HandleCommandLine(
    String[] args,
    Func<InitSettings, Task<Int32>> runSettings) {

    var useDefaultVerb = ShouldUseDefaultVerb(args);

    var parser = new Parser(settings => {
      // Assume that unknown arguments will be handled by the dotnet Command-line configuration provider
      settings.IgnoreUnknownArguments = true;
      settings.HelpWriter = Console.Error;
    });
    var parserResult =
      parser.ParseArguments<InitSettings>(args);
    return parserResult
      .MapResult(
        runSettings,
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
    var preParseResult = preParser.ParseArguments<InitSettings>(args);
    var preParseErrors = preParseResult.Errors?.ToList();
    var useDefaultVerb =
      preParseErrors is { Count: 1 } &&
      (preParseErrors[0] is NoVerbSelectedError ||
       preParseErrors[0] is BadVerbSelectedError badVerbError && badVerbError.Token.StartsWith("--"));
    return useDefaultVerb;
  }

  private static async Task<Int32> RunSettings(InitSettings opts) {

    // API Configuration
    var builder = new ConfigurationBuilder()
      .SetBasePath(Directory.GetCurrentDirectory())
      .AddJsonFile(opts.AppSettingLocation+"/"+"appsettings.json", false, true)
      .AddJsonFile(opts.AppSettingLocation+"/"+$"appsettings.{Environment.GetEnvironmentVariable("ENVIRONMENT")}.json", true, true)
      .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();
        var apiConfig = configuration.GetSection("ApiConfig").BindCtor<ApiConfiguration>();
        var promConfig = configuration.GetSection("Prometheus").BindCtor<PrometheusConfiguration>();
        var lokiConfig = configuration.GetSection("Loki").BindCtor<LokiConfiguration>();

        // Create cancellation source, token, new task
        var source = new CancellationTokenSource();
        CancellationToken token = source.Token;

        // Event handler for SIGINT
        // Traps SIGINT to perform necessary cleanup
        Console.CancelKeyPress += delegate {
          Console.WriteLine("\nSIGINT received, begin cleanup...");
          source.Cancel();
        };

        try {
          // Load and merge configs
          var servicesHierarchy = await ConfigurationHelper.LoadAndValidateJsonServiceConfig(opts.ServiceConfigFiles.ToArray(), token);
          // Configure service hierarchy
          Console.WriteLine("Configuring services....");
          await ConfigurationHelper.ConfigureServices(configuration, apiConfig, servicesHierarchy, token);
          // Hard coded 10 second interval
          var interval = TimeSpan.FromSeconds(10);
          Console.WriteLine("Initializing SONAR Agent...");
          // Run task that calls Health Check function
          var task = Task.Run(
            async delegate {
              await HealthCheckHelper.RunScheduledHealthCheck(interval, configuration, apiConfig, promConfig,
                lokiConfig, token);
            }, token);
          await task;
        }
        catch (IndexOutOfRangeException) {
          Console.Error.WriteLine("First command line argument must be service configuration file path.");
        }
        catch (OperationCanceledException e) {
          Console.Error.WriteLine(e.Message);
          Console.Error.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e.Message}");
          // Additional cleanup goes here
        }
        finally {
          source.Dispose();
        }

    return 0;
  }
}

