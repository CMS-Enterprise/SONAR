using System;
using System.Collections.Generic;

namespace Cms.BatCave.Sonar.Data;

public class HealthDefinition
{
  public TimeSpan Duration { get; init; }
  public String Expression { get; init; }
  public List<HealthCondition> Conditions { get; init; }

  public HealthDefinition(
    TimeSpan duration,
    String expression,
    List<HealthCondition> conditions)
  {

    this.Duration = duration;
    this.Expression = expression;
    this.Conditions = conditions;
  }
}
