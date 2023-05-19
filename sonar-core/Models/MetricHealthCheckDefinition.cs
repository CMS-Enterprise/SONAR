using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Cms.BatCave.Sonar.Models;

public sealed record MetricHealthCheckDefinition : HealthCheckDefinition {

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

  public Boolean Equals(MetricHealthCheckDefinition? other) {
    return other is not null &&
      Object.Equals(this.Duration, other.Duration) &&
      String.Equals(this.Expression, other.Expression) &&
      this.Conditions.Zip(other.Conditions, Object.Equals).All(x => x);
  }

  public override Int32 GetHashCode() {
    return HashCode.Combine(
      this.Duration,
      this.Expression,
      // JsonSerializer does not respect null constraints
      this.Conditions != null ?
        (Object)HashCodes.From(this.Conditions) :
        null);
  }
}
