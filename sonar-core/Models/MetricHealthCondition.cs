using System;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record MetricHealthCondition(
  HealthOperator Operator,
  Decimal Threshold,
  HealthStatus Status);
