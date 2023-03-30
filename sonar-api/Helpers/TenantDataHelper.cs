using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Helpers;

public class TenantDataHelper {
  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;
  private readonly HealthDataHelper _healthDataHelper;
  private readonly ServiceDataHelper _serviceDataHelper;

  public TenantDataHelper(
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable,
    HealthDataHelper healthDataHelper,
    ServiceDataHelper serviceDataHelper) {
    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
    this._healthDataHelper = healthDataHelper;
    this._serviceDataHelper = serviceDataHelper;
  }

  public async Task<Tenant> FetchExistingTenantAsync(
    String environmentName,
    String tenantName,
    CancellationToken cancellationToken) {

    // Check if the environment and tenant exist
    var result =
      await this._environmentsTable
        .Where(e => e.Name == environmentName)
        .LeftJoin(
          this._tenantsTable.Where(t => t.Name == tenantName),
          leftKeySelector: e => e.Id,
          rightKeySelector: t => t.EnvironmentId,
          resultSelector: (env, t) => new { Environment = env, Tenant = t })
        .SingleOrDefaultAsync(cancellationToken);

    if (result == null) {
      throw new ResourceNotFoundException(nameof(Environment), environmentName);
    } else if (result.Tenant == null) {
      throw new ResourceNotFoundException(nameof(Tenant), tenantName);
    }

    return result.Tenant;
  }

  public async Task<IList<TenantHealth>> GetTenantsHealth(
    Environment environment,
    CancellationToken cancellationToken) {
    var tenantList = new List<TenantHealth>();
    var res = await this._tenantsTable.ToListAsync(cancellationToken);
    var tenants = res.Where(t => t.EnvironmentId == environment.Id);

    foreach (var tenant in tenants) {
      var (_, _, services) =
        await this._serviceDataHelper.FetchExistingConfiguration(environment.Name, tenant.Name, cancellationToken);

      var serviceStatuses = await this._healthDataHelper.GetServiceStatuses(
        environment.Name, tenant.Name, cancellationToken);

      var healthCheckStatus = await this._healthDataHelper.GetHealthCheckStatus(
        environment.Name, tenant.Name, cancellationToken);

      var serviceChildIdsLookup = await this._serviceDataHelper.GetServiceChildIdsLookup(services, cancellationToken);
      var healthChecksByService = await this._healthDataHelper.GetHealthChecksByService(services, cancellationToken);

      //All root services for tenant
      var rootServiceHealth = services.Values.Where(svc => svc.IsRootService)
        .Select(svc => this._healthDataHelper.ToServiceHealth(
          svc, services, serviceStatuses, serviceChildIdsLookup, healthChecksByService, healthCheckStatus)
        ).ToArray();

      tenantList.Add(this.ToTenantHealth(tenant, environment, rootServiceHealth));
    }

    return tenantList;
  }

  private TenantHealth ToTenantHealth(
    Tenant tenant,
    Environment environment,
    ServiceHierarchyHealth?[] rootServiceHealth
  ) {
    HealthStatus? aggregateStatus = HealthStatus.Unknown;
    DateTime? statusTimestamp = null;

    foreach (var rs in rootServiceHealth) {
      if (rs.AggregateStatus.HasValue) {
        if ((aggregateStatus == null) ||
          (aggregateStatus < rs.AggregateStatus) ||
          (rs.AggregateStatus == HealthStatus.Unknown)) {
          aggregateStatus = rs.AggregateStatus;
        }

        // The child service should always have a timestamp here, but double check anyway
        if (rs.Timestamp.HasValue &&
          (!statusTimestamp.HasValue || (rs.Timestamp.Value < statusTimestamp.Value))) {
          // The status timestamp should always be the *oldest* of the
          // recorded status data points.
          statusTimestamp = rs.Timestamp.Value;
        }
      } else {
        // One of the child services has an "unknown" status, that means
        // this service will also have the "unknown" status.
        aggregateStatus = null;
        statusTimestamp = null;
        break;
      }
    }

    return new TenantHealth(environment.Name, tenant.Name, statusTimestamp, aggregateStatus);
  }
}
