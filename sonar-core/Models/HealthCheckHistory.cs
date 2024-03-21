using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public class HealthCheckHistory {

    public HealthCheckHistory(
      List<Dictionary<String, List<(DateTime, HealthStatus)>>> healthChecks) {

    this.HealthChecks = healthChecks;
  }

  [Required]
  public List<Dictionary<String, List<(DateTime, HealthStatus)>>> HealthChecks { get; init; }

}
