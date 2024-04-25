using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Helpers.Maintenance;
using Cms.BatCave.Sonar.Prometheus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Maintenance;

public class AdHocMaintenanceStatusRecorder<TMaintenance> : MaintenanceStatusRecorder<TMaintenance>
  where TMaintenance : AdHocMaintenance {

  public AdHocMaintenanceStatusRecorder(
    ILogger<AdHocMaintenanceStatusRecorder<TMaintenance>> logger,
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable,
    DbSet<Service> servicesTable,
    DataContext dataContext,
    IPrometheusService prometheus,
    MaintenanceDataHelper<TMaintenance> maintenanceDataHelper)
    : base(logger, environmentsTable, tenantsTable, servicesTable, dataContext, prometheus, maintenanceDataHelper) { }

  protected override Boolean MaintenanceIsActive(TMaintenance maintenance) => true;

  protected override Expression<Func<ServiceMaintenance, Boolean>> MatchesAssocEntity(TMaintenance maintenance) {
    return maintenance switch {
      AdHocEnvironmentMaintenance m => sm => sm.EnvironmentId == m.EnvironmentId,
      AdHocTenantMaintenance m => sm => sm.TenantId == m.TenantId,
      AdHocServiceMaintenance m => sm => sm.ServiceId == m.ServiceId,
      _ => throw new NotImplementedException($"{typeof(TMaintenance)} is unhandled.")
    };
  }

  protected override async Task ReleaseAsync(IImmutableList<TMaintenance> maintenances, CancellationToken ct) {
    await base.ReleaseAsync(maintenances, ct);

    var expiredMaintenances = maintenances.Where(m => m.EndTime < DateTime.UtcNow).ToArray();
    await this.MaintenanceDataHelper.RemoveRangeAsync(expiredMaintenances, ct);
  }

}
