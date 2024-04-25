using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ActiveAdHocMaintenanceView {
  public ActiveAdHocMaintenanceView(
    Guid id,
    MaintenanceScope scope,
    String environment,
    String? tenant,
    String? service,
    String appliedByUserName,
    DateTime startTime,
    DateTime endTime) {

    this.Id = id;
    this.Scope = scope;
    this.Environment = environment;
    this.Tenant = tenant;
    this.Service = service;
    this.AppliedByUserName = appliedByUserName;
    this.StartTime = startTime;
    this.EndTime = endTime;
  }

  [Required]
  public Guid Id { get; }

  [Required]
  public MaintenanceScope Scope { get; }

  [Required]
  public String Environment { get; }

  public String? Tenant { get; }

  public String? Service { get; }

  [Required]
  public String AppliedByUserName { get; }

  [Required]
  public DateTime StartTime { get; }

  [Required]
  public DateTime EndTime { get; }
}

