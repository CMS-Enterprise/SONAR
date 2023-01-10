using System;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record MetricHealthCondition(
  HealthOperator HealthOperator,
  Decimal Threshold,
  HealthStatus HealthStatus);
