using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ServiceHierarchyInfo {

  public ServiceHierarchyInfo(
    String name,
    String displayName,
    String dashboardLink,
    String? description = null,
    Uri? url = null,
    DateTime? timestamp = null,
    HealthStatus? aggregateStatus = null,
    IReadOnlyDictionary<VersionCheckType, String>? versions = null,
    IReadOnlyDictionary<String, (DateTime Timestamp, HealthStatus Status)?>? healthChecks = null,
    IImmutableSet<ServiceHierarchyInfo>? children = null,
    IImmutableDictionary<String, String?>? tags = null) {

    this.Name = name;
    this.DisplayName = displayName;
    this.DashboardLink = dashboardLink;
    this.Description = description;
    this.Url = url;
    this.Timestamp = timestamp;
    this.AggregateStatus = aggregateStatus;
    this.Versions = versions;
    this.HealthChecks = healthChecks;
    this.Children = children;
    this.Tags = tags;
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
  public IReadOnlyDictionary<VersionCheckType, String>? Versions { get; }

  public IReadOnlyDictionary<String, (DateTime Timestamp, HealthStatus Status)?>? HealthChecks { get; init; }

  public IImmutableSet<ServiceHierarchyInfo>? Children { get; init; }
  public IImmutableDictionary<String, String?>? Tags { get; init; }
}
