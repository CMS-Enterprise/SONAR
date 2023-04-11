using System;
using System.Collections.Immutable;
using System.Text;
using Cms.BatCave.Sonar.Extensions;

namespace Cms.BatCave.Sonar.Models;

public record ServiceHealthData(
  // This is a mapping from health check name to a list of health check metric time series samples.
  IImmutableDictionary<String, IImmutableList<(DateTime Timestamp, Double Value)>> HealthCheckSamples
) {
  public override String ToString() {
    StringBuilder sb = new();
    sb.Append($"{nameof(HealthCheckSamples)} {{ ");
    foreach (var (healthCheck, samples) in this.HealthCheckSamples) {
      sb.Append($"\n  [\"{healthCheck}\"] = {{");
      foreach (var (timestamp, value) in samples ?? ImmutableArray<(DateTime Timestamp, Double Value)>.Empty) {
        sb.Append($"\n    ({timestamp.MillisSinceUnixEpoch()}, {value}),");
      }
      sb.Remove(sb.Length - 1, length: 1);
      sb.Append("\n  },");
    }
    sb.Remove(sb.Length - 1, length: 1);
    sb.Append("\n}");
    return sb.ToString();
  }
}
