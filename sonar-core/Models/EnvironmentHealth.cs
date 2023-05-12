using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record EnvironmentHealth {

  public EnvironmentHealth(
    String environmentName,
    DateTime? timestamp = null,
    HealthStatus? aggregateStatus = null) {

    this.EnvironmentName = environmentName;
    this.Timestamp = timestamp;
    this.AggregateStatus = aggregateStatus;
  }

  [Required]
  public String EnvironmentName { get; init; }

  public DateTime? Timestamp { get; init; }

  public HealthStatus? AggregateStatus { get; init; }
}
