using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cms.BatCave.Sonar.Data;

[Table("ad_hoc_maintenance")]
public abstract class AdHocMaintenance : Maintenance {
  public Guid Id { get; init; }
  public Guid AppliedByUserId { get; init; }
  public DateTime StartTime { get; init; }
  public DateTime EndTime { get; init; }
  public Boolean IsRecording { get; init; }
  public DateTime? LastRecorded { get; init; }

  [NotMapped]
  public abstract String MaintenanceScope { get; }

  [NotMapped]
  public String MaintenanceType => "adhoc";

  protected AdHocMaintenance(
    Guid id,
    Guid appliedByUserId,
    DateTime startTime,
    DateTime endTime) {
    this.Id = id;
    this.AppliedByUserId = appliedByUserId;
    this.StartTime = startTime;
    this.EndTime = endTime;
  }
}

public class AdHocEnvironmentMaintenance : AdHocMaintenance {
  public Guid EnvironmentId { get; init; }

  public AdHocEnvironmentMaintenance(
    Guid id,
    Guid appliedByUserId,
    DateTime startTime,
    DateTime endTime,
    Guid environmentId
  ) : base(
    id,
    appliedByUserId,
    startTime,
    endTime
  ) {
    this.EnvironmentId = environmentId;
  }

  public sealed override String MaintenanceScope => "environment";

  public static AdHocEnvironmentMaintenance New(
    Guid appliedByUserId,
    DateTime startTime,
    DateTime endTime,
    Guid environmentId) =>
    new AdHocEnvironmentMaintenance(
      Guid.Empty,
      appliedByUserId,
      startTime,
      endTime,
      environmentId);
}

public class AdHocTenantMaintenance : AdHocMaintenance {
  public Guid TenantId { get; init; }

  public AdHocTenantMaintenance(
    Guid id,
    Guid appliedByUserId,
    DateTime startTime,
    DateTime endTime,
    Guid tenantId
  ) : base(
    id,
    appliedByUserId,
    startTime,
    endTime
  ) {
    this.TenantId = tenantId;
  }

  public sealed override String MaintenanceScope => "tenant";

  public static AdHocTenantMaintenance New(
    Guid appliedByUserId,
    DateTime startTime,
    DateTime endTime,
    Guid tenantId) =>
    new AdHocTenantMaintenance(
      Guid.Empty,
      appliedByUserId,
      startTime,
      endTime,
      tenantId);
}

public class AdHocServiceMaintenance : AdHocMaintenance {
  public Guid ServiceId { get; init; }

  public AdHocServiceMaintenance(
    Guid id,
    Guid appliedByUserId,
    DateTime startTime,
    DateTime endTime,
    Guid serviceId
  ) : base(
    id,
    appliedByUserId,
    startTime,
    endTime
  ) {
    this.ServiceId = serviceId;
  }

  public sealed override String MaintenanceScope => "service";

  public static AdHocServiceMaintenance New(
    Guid appliedByUserId,
    DateTime startTime,
    DateTime endTime,
    Guid serviceId) =>
    new AdHocServiceMaintenance(
      Guid.Empty,
      appliedByUserId,
      startTime,
      endTime,
      serviceId);
}
