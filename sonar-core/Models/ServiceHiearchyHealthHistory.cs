using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ServiceHierarchyHealthHistory {

  public ServiceHierarchyHealthHistory(
    String name,
    String displayName,
    String? description = null,
    Uri? url = null,
    IImmutableList<(DateTime Timestamp, HealthStatus AggregateStatus)>? aggregateStatus = null,
    IImmutableSet<ServiceHierarchyHealthHistory>? children = null) {

    this.Name = name;
    this.DisplayName = displayName;
    this.Description = description;
    this.Url = url;
    this.AggregateStatus = aggregateStatus;
    this.Children = children;
  }

  [Required]
  public String Name { get; init; }

  [Required]
  public String DisplayName { get; init; }

  public String? Description { get; init; }

  public Uri? Url { get; init; }

  public IImmutableList<(DateTime Timestamp, HealthStatus AggregateStatus)>? AggregateStatus { get; init; }

  public IImmutableSet<ServiceHierarchyHealthHistory>? Children { get; init; }
}
