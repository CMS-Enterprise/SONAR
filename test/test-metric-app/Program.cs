using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Cms.BatCave.Sonar.TestApp;

internal class Program {
  private const Int32 MetricLimit = 120;
  private const Double SawWaveIncrement = 10;
  private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(10);

  private const Int32 NumOfHealthStatus = 5;

  public static async Task Main(String[] args) {
    var configBuilder = new ConfigurationBuilder()
      .SetBasePath(Directory.GetCurrentDirectory())
      .AddJsonFile("appsettings.json", false, true)
      .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ENVIRONMENT")}.json", true, true)
      .AddEnvironmentVariables()
      .AddCommandLine(args);
    var configuration = configBuilder.Build();

    // Enable metrics server to expose example metrics to Prometheus
    using var server = new MetricServer(port: configuration.GetValue("MetricsPort", 2020));
    server.Start();

    var sawWaveMetric = Metrics.CreateGauge(
      name: "example_saw_wave",
      help: "A test metric that linearly increases until it reaches some threshold"
    );

    // Loki
    int count = 0, numOfErrors = 0;
    using ILoggerFactory loggerFactory = LoggerFactory.Create(loggingBuilder => {
      loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
      loggingBuilder.AddConsole();
      loggingBuilder.AddLoki();
    });

    var logger = loggerFactory.CreateLogger<Program>();

    Console.WriteLine("Test Metrics App Running (press Ctrl+C to stop)...");

    while (true) {
      // Update example_saw_wave
      if (sawWaveMetric.Value < Program.MetricLimit) {
        sawWaveMetric.Inc(Program.SawWaveIncrement);
      } else {
        sawWaveMetric.Set(0);
      }

      //Print log information to Loki
      // Intervals of 30s, print out a different number of errors up to num of healthStatus
      if (count == 3) {
        for (int i = 0; i <= numOfErrors; i++) {
          if (i == 0) {
            logger.LogInformation("Informational Log");
          } else {
            logger.LogError("Error # {ErrorCount}", i);
          }
          count = 0;
        }

        if (numOfErrors == NumOfHealthStatus - 1) {
          numOfErrors = 0;
        } else {
          numOfErrors++;
        }
      }
      count++;

      await Task.Delay(Program.UpdateInterval);
    }
  }
}
