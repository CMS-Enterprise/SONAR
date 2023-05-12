using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record TenantHealth {

  public TenantHealth(
    String environmentName,
    String tenantName,
    DateTime? timestamp = null,
    HealthStatus? aggregateStatus = null,
    ServiceHierarchyHealth?[]? rootServices = null) {

    this.EnvironmentName = environmentName;
    this.TenantName = tenantName;
    this.Timestamp = timestamp;
    this.AggregateStatus = aggregateStatus;
    this.RootServices = rootServices;
  }

  [Required]
  public String EnvironmentName { get; init; }

  [Required]
  public String TenantName { get; init; }

  public DateTime? Timestamp { get; init; }

  public HealthStatus? AggregateStatus { get; init; }

  public ServiceHierarchyHealth?[]? RootServices { get; init; }
}
