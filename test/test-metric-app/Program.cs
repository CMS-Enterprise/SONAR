using System;
using System.Threading.Tasks;
using Prometheus;

namespace Cms.BatCave.Sonar.TestApp;

internal static class Program {
  private const Int32 MetricLimit = 120;
  private const Double SawWaveIncrement = 10;
  private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(10);

  public static async Task Main(String[] args) {
    using var server = new MetricServer(port: 2020);
    server.Start();

    var sawWaveMetric = Metrics.CreateGauge(
      name: "example_saw_wave",
      help: "A test metric that linearly increases until it reaches some threshold"
    );

    Console.WriteLine("Test Metrics App Running (press Ctrl+C to stop)...");
    while (true) {
      // Update example_saw_wave
      if (sawWaveMetric.Value < Program.MetricLimit) {
        sawWaveMetric.Inc(Program.SawWaveIncrement);
      } else {
        sawWaveMetric.Set(0);
      }

      await Task.Delay(Program.UpdateInterval);
    }
  }
}
