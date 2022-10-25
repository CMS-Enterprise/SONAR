using System;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Data;

public class HealthCondition
{
  public HealthOperator HealthOperator { get; init; }
  public Double Threshold { get; init; }
  public HealthStatus HealthStatus { get; init; }

  public HealthCondition(
    HealthOperator healthOperator,
    Double threshold,
    HealthStatus healthStatus) {

    this.HealthOperator = healthOperator;
    this.Threshold = threshold;
    this.HealthStatus = healthStatus;
  }
}
