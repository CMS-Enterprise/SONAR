using System;
using System.Collections.Immutable;

namespace Cms.BatCave.Sonar.Models;

public record HealthDefinition(
  TimeSpan Duration,
  String Expression,
  IImmutableList<HealthCondition> Conditions
);

