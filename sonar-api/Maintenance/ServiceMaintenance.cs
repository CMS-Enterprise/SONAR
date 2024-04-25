using System;
using Cms.BatCave.Sonar.Extensions;
using Prometheus;

namespace Cms.BatCave.Sonar.Maintenance;

/// <summary>
/// DTO class for passing around service maintenance data.
/// </summary>
public sealed record ServiceMaintenance {
  public Guid EnvironmentId = Guid.Empty;
  public String EnvironmentName = String.Empty;
  public Guid TenantId = Guid.Empty;
  public String TenantName = String.Empty;
  public Guid ServiceId = Guid.Empty;
  public String ServiceName = String.Empty;
  public String MaintenanceScope = String.Empty;
  public String MaintenanceType = String.Empty;

  public override String ToString() {
    return $"ServiceMaintenance{{" +
      $"{nameof(this.EnvironmentId)}: {this.EnvironmentId}, " +
      $"{nameof(this.EnvironmentName)}: {this.EnvironmentName}, " +
      $"{nameof(this.TenantId)}: {this.TenantId}, " +
      $"{nameof(this.TenantName)}: {this.TenantName}, " +
      $"{nameof(this.ServiceId)}: {this.ServiceId}, " +
      $"{nameof(this.ServiceName)}: {this.ServiceName}, " +
      $"{nameof(this.MaintenanceScope)}: {this.MaintenanceScope}, " +
      $"{nameof(this.MaintenanceType)}: {this.MaintenanceType}}}";
  }
}
