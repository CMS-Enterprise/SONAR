using System;
using System.Collections.Immutable;

namespace Cms.BatCave.Sonar.Models;

public record ServiceHealthMetrics(
  // This is a mapping from Health check name to a list of health check metric time series samples.
  IImmutableDictionary<String, IImmutableList<(DateTime Timestamp, Double Value)>> HealthCheckSamples
);
