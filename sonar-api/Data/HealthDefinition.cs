using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Cms.BatCave.Sonar.Data;

public class HealthDefinition {
  public TimeSpan Duration { get; init; }
  public String Expression { get; init; }
  public IImmutableList<HealthCondition> Conditions { get; init; }

  public HealthDefinition(
    TimeSpan duration,
    String expression,
    IImmutableList<HealthCondition> conditions) {

    this.Duration = duration;
    this.Expression = expression;
    this.Conditions = conditions;
  }
}
