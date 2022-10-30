using System;
using System.Collections.Generic;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ServiceHealth(
  DateTime Timestamp,
  HealthStatus AggregateStatus,
  IReadOnlyDictionary<String, HealthStatus> HealthChecks
);
