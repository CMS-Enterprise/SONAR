using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cms.BatCave.Sonar.Data;

[Table("scheduled_maintenance")]
public abstract class ScheduledMaintenance : Maintenance {
  public Guid Id { get; init; }
  public String ScheduleExpression { get; init; }

  public String ScheduleTimeZone { get; init; }
  public Int32 DurationMinutes { get; init; }
  public Boolean IsRecording { get; init; }
  public DateTime? LastRecorded { get; init; }

  [NotMapped]
  public abstract String MaintenanceScope { get; }

  [NotMapped]
  public String MaintenanceType => "scheduled";

  protected ScheduledMaintenance(
    Guid id,
    String scheduleExpression,
    String scheduleTimeZone,
    Int32 durationMinutes
  ) {
    this.Id = id;
    this.ScheduleExpression = scheduleExpression;
    this.ScheduleTimeZone = scheduleTimeZone;
    this.DurationMinutes = durationMinutes;
  }
}

public class ScheduledEnvironmentMaintenance : ScheduledMaintenance {
  public Guid EnvironmentId { get; init; }

  public ScheduledEnvironmentMaintenance(
    Guid id,
    String scheduleExpression,
    String scheduleTimeZone,
    Int32 durationMinutes,
    Guid environmentId
  ) : base(
    id,
    scheduleExpression,
    scheduleTimeZone,
    durationMinutes
  ) {
    this.EnvironmentId = environmentId;
  }

  public sealed override String MaintenanceScope => "environment";

  public static ScheduledEnvironmentMaintenance New(
    String scheduleExpression,
    String scheduleTimeZone,
    Int32 durationMinutes,
    Guid environmentId) =>
    new ScheduledEnvironmentMaintenance(
      Guid.Empty,
      scheduleExpression,
      scheduleTimeZone,
      durationMinutes,
      environmentId);
}

public class ScheduledTenantMaintenance : ScheduledMaintenance {
  public Guid TenantId { get; init; }

  public ScheduledTenantMaintenance(
    Guid id,
    String scheduleExpression,
    String scheduleTimeZone,
    Int32 durationMinutes,
    Guid tenantId
  ) : base(
    id,
    scheduleExpression,
    scheduleTimeZone,
    durationMinutes) {
    this.TenantId = tenantId;
  }

  public sealed override String MaintenanceScope => "tenant";

  public static ScheduledTenantMaintenance New(
    String scheduleExpression,
    String scheduleTimeZone,
    Int32 durationMinutes,
    Guid tenantId) =>
    new ScheduledTenantMaintenance(
      Guid.Empty,
      scheduleExpression,
      scheduleTimeZone,
      durationMinutes,
      tenantId);
}

public class ScheduledServiceMaintenance : ScheduledMaintenance {
  public Guid ServiceId { get; init; }

  public ScheduledServiceMaintenance(
    Guid id,
    String scheduleExpression,
    String scheduleTimeZone,
    Int32 durationMinutes,
    Guid serviceId
  ) : base(
    id,
    scheduleExpression,
    scheduleTimeZone,
    durationMinutes) {
    this.ServiceId = serviceId;
  }

  public sealed override String MaintenanceScope => "service";

  public static ScheduledServiceMaintenance New(
    String scheduleExpression,
    String scheduleTimeZone,
    Int32 durationMinutes,
    Guid serviceId) =>
    new ScheduledServiceMaintenance(
      Guid.Empty,
      scheduleExpression,
      scheduleTimeZone,
      durationMinutes,
      serviceId);
}
