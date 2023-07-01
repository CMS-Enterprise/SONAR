using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Helpers;

public class TenantDataHelper {
  public const String SonarTenantName = "sonar";

  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;
  private readonly HealthDataHelper _healthDataHelper;
  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly String _sonarEnvironment;

  public TenantDataHelper(
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable,
    HealthDataHelper healthDataHelper,
    ServiceDataHelper serviceDataHelper,
    IOptions<SonarHealthCheckConfiguration> sonarHealthConfig) {
    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
    this._healthDataHelper = healthDataHelper;
    this._serviceDataHelper = serviceDataHelper;
    this._sonarEnvironment = sonarHealthConfig.Value.SonarEnvironment;
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

    var tenants = await
      this._tenantsTable.Where(t => t.EnvironmentId == environment.Id)
        .ToListAsync(cancellationToken);

    var tenantList = new List<TenantHealth>();
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

      tenantList.Add(this.ToTenantHealth(tenant.Name, environment.Name, rootServiceHealth));
    }

    if (environment.Name == this._sonarEnvironment) {
      tenantList.Add(this.ToTenantHealth(
        SonarTenantName,
        environment.Name,
        (await this._healthDataHelper.CheckSonarHealth(cancellationToken)).ToArray()
      ));
    }

    return tenantList;
  }

  private TenantHealth ToTenantHealth(
    String tenantName,
    String environmentName,
    ServiceHierarchyHealth?[] rootServiceHealth
  ) {
    HealthStatus? aggregateStatus = HealthStatus.Unknown;
    DateTime? statusTimestamp = null;

    foreach (var rs in rootServiceHealth) {
      if (rs != null) {

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
      } else {
        // One of the child services has an "unknown" status, that means
        // this service will also have the "unknown" status.
        aggregateStatus = null;
        statusTimestamp = null;
        break;
      }
    }

    return new TenantHealth(environmentName, tenantName, statusTimestamp, aggregateStatus, rootServiceHealth);
  }
}
