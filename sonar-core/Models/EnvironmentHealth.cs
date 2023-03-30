using System;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record EnvironmentHealth(
  String EnvironmentName,
  DateTime? Timestamp,
  HealthStatus? AggregateStatus);
