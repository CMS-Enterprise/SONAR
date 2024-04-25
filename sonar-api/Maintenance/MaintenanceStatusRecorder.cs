using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Helpers.Maintenance;
using Cms.BatCave.Sonar.Prometheus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Maintenance;

public abstract class MaintenanceStatusRecorder<TMaintenance> : IMaintenanceStatusRecorder
  where TMaintenance : class, Data.Maintenance {

  private readonly ILogger<MaintenanceStatusRecorder<TMaintenance>> _logger;
  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;
  private readonly DbSet<Service> _servicesTable;
  private readonly DataContext _dataContext;
  private readonly IPrometheusService _prometheusService;

  protected readonly MaintenanceDataHelper<TMaintenance> MaintenanceDataHelper;

  protected MaintenanceStatusRecorder(
    ILogger<MaintenanceStatusRecorder<TMaintenance>> logger,
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable,
    DbSet<Service> servicesTable,
    DataContext dataContext,
    IPrometheusService prometheusService,
    MaintenanceDataHelper<TMaintenance> maintenanceDataHelper) {

    this._logger = logger;
    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
    this._servicesTable = servicesTable;
    this._dataContext = dataContext;
    this._prometheusService = prometheusService;
    this.MaintenanceDataHelper = maintenanceDataHelper;
  }

  /// <summary>
  /// Record maintenance status for all services currently in maintenance for the current
  /// <typeparamref name="TMaintenance"/> type.
  /// </summary>
  public async Task RecordAsync(DateTime when, TimeSpan recordingPeriod, CancellationToken ct) {
    IImmutableList<TMaintenance> maintenanceConfigs = ImmutableList<TMaintenance>.Empty;

    try {
      maintenanceConfigs = await this.MaintenanceDataHelper.LockForRecordingAsync(when, recordingPeriod, ct);
      var activeMaintenanceConfigs = maintenanceConfigs.Where(this.MaintenanceIsActive).ToImmutableList();

      if (activeMaintenanceConfigs.Any()) {
        var serviceMaintenances = new List<ServiceMaintenance>();
        foreach (var maintenance in activeMaintenanceConfigs) {
          serviceMaintenances.AddRange(await this.GetServicesInMaintenanceAsync(maintenance, ct));
        }

        this._logger.LogInformation(
          message: "Recording {numStatuses} service statuses for {numActive} active {maintenanceType} configurations.",
          serviceMaintenances.Count,
          activeMaintenanceConfigs.Count,
          typeof(TMaintenance).Name);

        if (this._logger.IsEnabled(LogLevel.Trace)) {
          var sb = new StringBuilder();
          sb.Append($"Services currently in {typeof(TMaintenance).Name}:");
          foreach (var serviceMaintenance in serviceMaintenances) {
            sb.AppendLine().Append(serviceMaintenance);
          }
          this._logger.LogTrace(sb.ToString());
        }

        await this._prometheusService.WriteServiceMaintenanceStatusAsync(serviceMaintenances.ToImmutableList(), ct);
      }
    } finally {
      if (maintenanceConfigs.Any()) {
        await this.ReleaseAsync(maintenanceConfigs, ct);
      } else {
        this._logger.LogDebug(
          message: "Did not lock any {maintenanceType} configurations for recording.",
          typeof(TMaintenance).Name);
      }
    }
  }

  /// <summary>
  /// Whether the given maintenance configuration is active. This is on a per-maintenance-type basis: scheduled
  /// maintenances are active when the current time is inside of a time window defined by their schedule expression
  /// and duration; ad-hoc maintenances are always active as long as the record exists in the database.
  /// </summary>
  protected abstract Boolean MaintenanceIsActive(TMaintenance maintenance);

  /// <summary>
  /// Get the service maintenance projection of all services in scope of the given maintenance configuration.
  /// Basically, selects all services joined to their respective tenant and environment, and filters that set
  /// based on the rows that match the associated entity id of the given maintenance configuration. So, if the given
  /// maintenance configuration is for an environment, all rows where the environment id matches the id in the
  /// maintenance config are returned. If for a tenant, then all rows where the tenant id matches, and so on.
  /// </summary>
  private async Task<IImmutableList<ServiceMaintenance>> GetServicesInMaintenanceAsync(
    TMaintenance maintenance,
    CancellationToken ct) {

    var serviceMaintenancesBaseQuery =
      this._environmentsTable
        .Join(this._tenantsTable, e => e.Id, t => t.EnvironmentId, (e, t) => new { e, t })
        .Join(this._servicesTable, row => row.t.Id, s => s.TenantId, (row, s) => new { row.e, row.t, s })
        .Select(row => new ServiceMaintenance {
          EnvironmentId = row.e.Id,
          EnvironmentName = row.e.Name,
          TenantId = row.t.Id,
          TenantName = row.t.Name,
          ServiceId = row.s.Id,
          ServiceName = row.s.Name,
          MaintenanceScope = maintenance.MaintenanceScope,
          MaintenanceType = maintenance.MaintenanceType
        });

    var serviceMaintenances =
      await serviceMaintenancesBaseQuery
        .Where(this.MatchesAssocEntity(maintenance))
        .ToListAsync(ct);

    if (maintenance.MaintenanceScope == "service") {
      // If the maintenance is service-scoped, the query above will only return the single row that matches the service
      // pointed at by the maintenance record. It's possible that the service has child services, and they should also
      // be considered as in-maintenance. So we need to perform an additional query to get the ServiceMaintenances of
      // any child services, which can be identified by a recursive query against the service relationship table.
      var childServiceIds = await this._dataContext.Database
        .SqlQueryRaw<Guid>(
          sql: @"
            WITH RECURSIVE children AS (
                SELECT service_id
                FROM service_relationship
                WHERE parent_service_id = {0}
                UNION ALL
                SELECT child.service_id
                FROM service_relationship AS child
                INNER JOIN children ON children.service_id = child.parent_service_id
            ) SELECT * FROM children;",
          parameters: serviceMaintenances.Single().ServiceId)
        .ToListAsync(ct);

      serviceMaintenances.AddRange(
        await serviceMaintenancesBaseQuery
          .Where(sm => childServiceIds.Contains(sm.ServiceId))
          .ToListAsync(ct));
    }

    return serviceMaintenances.ToImmutableList();
  }

  /// <summary>
  /// Return a predicate expression that evaluates to true if a service is in scope of the given
  /// maintenance configuration. Depends on the maintenance scope, for environments the predicate is based on
  /// environment ids, for tenants, tenant ids and so on.
  /// </summary>
  protected abstract Expression<Func<ServiceMaintenance, Boolean>> MatchesAssocEntity(TMaintenance maintenance);

  /// <summary>
  /// Release the lock on the given maintenance configurations and perform any necessary cleanup after maintenance
  /// status recording is completed. When ad-hoc maintenances have expired (the current time is after their end time),
  /// their rows are deleted from the database during this cleanup.
  /// </summary>
  protected virtual async Task ReleaseAsync(IImmutableList<TMaintenance> maintenances, CancellationToken ct) {
    await this.MaintenanceDataHelper.ReleaseRecordingLockAsync(maintenances, ct);
  }
}
