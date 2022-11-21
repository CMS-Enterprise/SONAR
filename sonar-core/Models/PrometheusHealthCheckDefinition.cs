using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;

public record PrometheusHealthCheckDefinition(
  [property:Required]
  TimeSpan Duration,
  [property:Required]
  String Expression,
  [property:Required]
  IImmutableList<MetricHealthCondition> Conditions) : HealthCheckDefinition();
