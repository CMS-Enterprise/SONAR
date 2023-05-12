using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ServiceHealth {

  public ServiceHealth(
    DateTime timestamp,
    HealthStatus aggregateStatus,
    IReadOnlyDictionary<String, HealthStatus> healthChecks) {

    this.Timestamp = timestamp;
    this.AggregateStatus = aggregateStatus;
    this.HealthChecks = healthChecks;
  }

  [Required]
  public DateTime Timestamp { get; init; }

  [Required]
  public HealthStatus AggregateStatus { get; init; }

  [Required]
  public IReadOnlyDictionary<String, HealthStatus> HealthChecks { get; init; }
}
