using System;
using System.Linq;
using System.Linq.Expressions;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Helpers.Maintenance;
using Cms.BatCave.Sonar.Prometheus;
using Cronos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Maintenance;

public class ScheduledMaintenanceStatusRecorder<TMaintenance> : MaintenanceStatusRecorder<TMaintenance>
  where TMaintenance : ScheduledMaintenance {

  public ScheduledMaintenanceStatusRecorder(
    ILogger<ScheduledMaintenanceStatusRecorder<TMaintenance>> logger,
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable,
    DbSet<Service> servicesTable,
    DataContext dataContext,
    IPrometheusService prometheus,
    MaintenanceDataHelper<TMaintenance> maintenanceDataHelper)
    : base(logger, environmentsTable, tenantsTable, servicesTable, dataContext, prometheus, maintenanceDataHelper) { }

  protected override Boolean MaintenanceIsActive(TMaintenance maintenance) {
    return CronExpression
      .Parse(maintenance.ScheduleExpression)
      .GetOccurrences(
        DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(maintenance.DurationMinutes)),
        DateTime.UtcNow,
        TimeZoneInfo.FindSystemTimeZoneById(maintenance.ScheduleTimeZone))
      .Any();
  }

  protected override Expression<Func<ServiceMaintenance, Boolean>> MatchesAssocEntity(TMaintenance maintenance) {
    return maintenance switch {
      ScheduledEnvironmentMaintenance m => sm => sm.EnvironmentId == m.EnvironmentId,
      ScheduledTenantMaintenance m => sm => sm.TenantId == m.TenantId,
      ScheduledServiceMaintenance m => sm => sm.ServiceId == m.ServiceId,
      _ => throw new NotImplementedException($"{typeof(TMaintenance)} is unhandled.")
    };
  }
}
