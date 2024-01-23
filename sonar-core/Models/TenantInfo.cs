using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record TenantInfo {

  public TenantInfo(
    String environmentName,
    String tenantName,
    Boolean isNonProd,
    DateTime? timestamp = null,
    HealthStatus? aggregateStatus = null,
    ServiceHierarchyInfo[]? rootServices = null) {

    this.EnvironmentName = environmentName;
    this.TenantName = tenantName;
    this.IsNonProd = isNonProd;
    this.Timestamp = timestamp;
    this.AggregateStatus = aggregateStatus;
    this.RootServices = rootServices;
  }

  [Required]
  public String EnvironmentName { get; init; }

  [Required]
  public String TenantName { get; init; }

  [Required]
  public Boolean IsNonProd { get; init; }

  public DateTime? Timestamp { get; init; }

  public HealthStatus? AggregateStatus { get; init; }

  public ServiceHierarchyInfo[]? RootServices { get; init; }
}
