using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;

public record MetricHealthCheckDefinition : HealthCheckDefinition {

  public MetricHealthCheckDefinition(
    TimeSpan duration,
    String expression,
    IImmutableList<MetricHealthCondition> conditions) {

    this.Duration = duration;
    this.Expression = expression;
    this.Conditions = conditions;
  }

  [Required]
  public TimeSpan Duration { get; init; }

  [Required]
  public String Expression { get; init; }

  [Required]
  public IImmutableList<MetricHealthCondition> Conditions { get; init; }
}
