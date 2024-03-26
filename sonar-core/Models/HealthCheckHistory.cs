using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record HealthCheckHistory {
  public HealthCheckHistory(Dictionary<String, List<(DateTime, HealthStatus)>> healthChecks) {
    this.HealthChecks = healthChecks;
  }

  [Required]
  public Dictionary<String, List<(DateTime, HealthStatus)>> HealthChecks { get; init; }
}
