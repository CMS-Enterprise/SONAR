using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ServiceHierarchyHealth {

  public ServiceHierarchyHealth(
    String name,
    String displayName,
    String dashboardLink,
    String? description = null,
    Uri? url = null,
    DateTime? timestamp = null,
    HealthStatus? aggregateStatus = null,
    IReadOnlyDictionary<String, (DateTime Timestamp, HealthStatus Status)?>? healthChecks = null,
    IImmutableSet<ServiceHierarchyHealth>? children = null,
    IImmutableDictionary<String, String?>? tags = null,
    Boolean isInMaintenance = false,
    String? inMaintenanceTypes = null
  ) {

    this.Name = name;
    this.DisplayName = displayName;
    this.DashboardLink = dashboardLink;
    this.Description = description;
    this.Url = url;
    this.Timestamp = timestamp;
    this.AggregateStatus = aggregateStatus;
    this.HealthChecks = healthChecks;
    this.Children = children;
    this.Tags = tags;
    this.IsInMaintenance = isInMaintenance;
    this.InMaintenanceTypes = inMaintenanceTypes;
  }

  [Required]
  public String Name { get; init; }

  [Required]
  public String DisplayName { get; init; }

  [Required]
  public String DashboardLink { get; init; }

  public String? Description { get; init; }

  public Uri? Url { get; init; }

  public DateTime? Timestamp { get; init; }

  public HealthStatus? AggregateStatus { get; init; }

  public IReadOnlyDictionary<String, (DateTime Timestamp, HealthStatus Status)?>? HealthChecks { get; init; }

  public IImmutableSet<ServiceHierarchyHealth>? Children { get; init; }
  public IImmutableDictionary<String, String?>? Tags { get; init; }

  /// <summary>
  /// Whether the service is currently in maintenance.
  /// </summary>
  public Boolean IsInMaintenance { get; init; }

  /// <summary>
  /// If <see cref="IsInMaintenance"/> is true, a comma-separated list of which maintenance types apply
  /// (ad-hoc, scheduled, or both; usually just one, but both are possible); otherwise null.
  /// </summary>
  public String? InMaintenanceTypes { get; init; }
}
