using System;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record HealthCondition(
  HealthOperator HealthOperator,
  Double Threshold,
  HealthStatus HealthStatus
  );

