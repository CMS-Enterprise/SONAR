using System;
using System.Collections.Immutable;

namespace Cms.BatCave.Sonar.Models;

public record UptimeModel(
  String Name,
  Double PercentUptime,
  TimeSpan TotalUptime,
  TimeSpan CurrentUptime,
  TimeSpan UnknownDuration,
  IImmutableList<UptimeModel> Children
  );
