using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ActiveScheduledMaintenanceView {
  public ActiveScheduledMaintenanceView(
    Guid id,
    MaintenanceScope scope,
    String environment,
    String? tenant,
    String? service,
    String scheduleExpression,
    Int32 duration,
    String timeZone) {

    this.Id = id;
    this.Scope = scope;
    this.Environment = environment;
    this.Tenant = tenant;
    this.Service = service;
    this.ScheduleExpression = scheduleExpression;
    this.Duration = duration;
    this.TimeZone = timeZone;
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
  public String ScheduleExpression { get; }

  public Int32 Duration { get; }

  public String TimeZone { get; }


}

