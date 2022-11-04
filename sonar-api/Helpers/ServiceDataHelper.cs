using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Extensions;
using Microsoft.EntityFrameworkCore;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Helpers;

public class ServiceDataHelper {
  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;
  private readonly DbSet<Service> _servicesTable;
  private readonly DbSet<ServiceRelationship> _relationshipsTable;
  private readonly DbSet<HealthCheck> _healthChecksTable;

  public ServiceDataHelper(
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable,
    DbSet<Service> servicesTable,
    DbSet<ServiceRelationship> relationshipsTable,
    DbSet<HealthCheck> healthChecksTable) {

    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
    this._servicesTable = servicesTable;
    this._relationshipsTable = relationshipsTable;
    this._healthChecksTable = healthChecksTable;
  }

  public async Task<(Environment, Tenant, ImmutableDictionary<Guid, Service>)> FetchExistingConfiguration(
    String environmentName,
    String tenantName,
    CancellationToken cancellationToken) {

    var results =
      await this._environmentsTable
        .Where(e => e.Name == environmentName)
        .LeftJoin(
          this._tenantsTable.Where(t => t.Name == tenantName),
          leftKeySelector: e => e.Id,
          rightKeySelector: t => t.EnvironmentId,
          resultSelector: (env, t) => new { Environment = env, Tenant = t })
        .LeftJoin(
          this._servicesTable,
          leftKeySelector: row => row.Tenant != null ? row.Tenant.Id : (Guid?)null,
          rightKeySelector: svc => svc.TenantId,
          resultSelector: (row, svc) => new {
            row.Environment,
            row.Tenant,
            Service = svc
          })
        .ToListAsync(cancellationToken);

    var environment = results.FirstOrDefault()?.Environment;
    var tenant = results.FirstOrDefault()?.Tenant;
    if (environment == null) {
      throw new ResourceNotFoundException(nameof(Environment), environmentName);
    } else if (tenant == null) {
      throw new ResourceNotFoundException(nameof(Tenant), tenantName);
    }

    var serviceMap =
      results
        .Select(r => r.Service)
        .NotNull()
        .ToImmutableDictionary(svc => svc.Id);

    return (environment, tenant, serviceMap);
  }

  public async Task<Service> FetchExistingService(
    String environmentName,
    String tenantName,
    String serviceName,
    CancellationToken cancellationToken) {

    var results =
      await this._environmentsTable
        .Where(e => e.Name == environmentName)
        .LeftJoin(
          this._tenantsTable.Where(t => t.Name == tenantName),
          leftKeySelector: e => e.Id,
          rightKeySelector: t => t.EnvironmentId,
          resultSelector: (env, t) => new { Environment = env, Tenant = t })
        .LeftJoin(
          this._servicesTable.Where(svc => svc.Name == serviceName),
          leftKeySelector: row => row.Tenant != null ? row.Tenant.Id : (Guid?)null,
          rightKeySelector: svc => svc.TenantId,
          resultSelector: (row, svc) => new {
            row.Environment,
            row.Tenant,
            Service = svc
          })
        .ToListAsync(cancellationToken);

    var result = results.SingleOrDefault();
    if (result == null) {
      throw new ResourceNotFoundException(nameof(Environment), environmentName);
    } else if (result.Tenant == null) {
      throw new ResourceNotFoundException(nameof(Tenant), tenantName);
    } else if (result.Service == null) {
      throw new ResourceNotFoundException(nameof(Service), serviceName);
    }

    return result.Service;
  }

  public async Task<IList<ServiceRelationship>> FetchExistingRelationships(
    IEnumerable<Guid> serviceIds,
    CancellationToken cancellationToken) {

    return
      await this._relationshipsTable
        .Where(r => serviceIds.Contains(r.ParentServiceId))
        .ToListAsync(cancellationToken);
  }

  public async Task<IList<HealthCheck>> FetchExistingHealthChecks(
    IEnumerable<Guid> serviceIds,
    CancellationToken cancellationToken) {

    return
      await this._healthChecksTable
        .Where(hc => serviceIds.Contains(hc.ServiceId))
        .ToListAsync(cancellationToken);
  }
}
