using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record EnvironmentHealth {

  public EnvironmentHealth(
    String environmentName,
    DateTime? timestamp = null,
    HealthStatus? aggregateStatus = null,
    Boolean isNonProd = false,
    Boolean isInMaintenance = false,
    String? inMaintenanceTypes = null) {

    this.EnvironmentName = environmentName;
    this.Timestamp = timestamp;
    this.AggregateStatus = aggregateStatus;
    this.IsNonProd = isNonProd;
    this.IsInMaintenance = isInMaintenance;
    this.InMaintenanceTypes = inMaintenanceTypes;
  }

  [Required]
  public String EnvironmentName { get; init; }

  public DateTime? Timestamp { get; init; }

  public HealthStatus? AggregateStatus { get; init; }

  public Boolean IsNonProd { get; init; }

  /// <summary>
  /// Whether the entire environment is currently in maintenance.
  /// </summary>
  public Boolean IsInMaintenance { get; init; }

  /// <summary>
  /// If <see cref="IsInMaintenance"/> is true, a comma-separated list of which maintenance types apply
  /// (ad-hoc, scheduled, or both; usually just one, but both are possible); otherwise null.
  /// </summary>
  public String? InMaintenanceTypes { get; init; }
}
