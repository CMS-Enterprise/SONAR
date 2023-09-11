using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Controllers;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using String = System.String;

namespace Cms.BatCave.Sonar.Helpers;

public class ServiceHealthCacheHelper {
  private readonly DataContext _dbContext;
  private readonly DbSet<ServiceHealthCache> _serviceHealthCacheTable;
  private readonly DbSet<HealthCheckCache> _healthCheckCacheTable;
  private readonly ILogger<ServiceHealthCacheHelper> _logger;

  public ServiceHealthCacheHelper(
    DataContext dbContext,
    DbSet<ServiceHealthCache> serviceHealthCacheTable,
    DbSet<HealthCheckCache> healthCheckCacheTable,
    ILogger<ServiceHealthCacheHelper> logger) {

    this._dbContext = dbContext;
    this._serviceHealthCacheTable = serviceHealthCacheTable;
    this._healthCheckCacheTable = healthCheckCacheTable;
    this._logger = logger;
  }

  public async Task CreateUpdateCache(
    String environment,
    String tenant,
    String service,
    ServiceHealth value,
    ImmutableDictionary<String, HealthStatus> healthChecks,
    CancellationToken cancellationToken) {

    try {
      var serviceCacheId = this._dbContext.Database.SqlQuery<Guid>(
        $"insert into service_health_cache (id, environment, tenant, service, timestamp, aggregate_status) values ({Guid.NewGuid()}, {environment}, {tenant}, {service}, {value.Timestamp}, {value.AggregateStatus}) on conflict (environment, tenant, service) do update set timestamp = {value.Timestamp}, aggregate_status = {value.AggregateStatus} returning id"
      ).AsEnumerable().Single();

      foreach (var hc in healthChecks) {
        await this._dbContext.Database.ExecuteSqlInterpolatedAsync(
          $"insert into health_check_cache (id, service_health_id, health_check, status) values ({Guid.NewGuid()}, {serviceCacheId}, {hc.Key}, {hc.Value}) on conflict (service_health_id, health_check) do update set status = {value.AggregateStatus}",
          cancellationToken
        );
      }
    } catch (DbUpdateException dbException) {
      this._logger.LogWarning(
        message: "Error occurred while updating service health cache: {Message}",
        dbException.Message
      );
    }
  }

  public async Task<Dictionary<String, (DateTime Timestamp, HealthStatus Status)>> FetchServiceCache(
    String environment,
    String tenant,
    CancellationToken cancellationToken) {

    var existingServiceHealthCache =
      await this._serviceHealthCacheTable
        .Where(e => (e.Environment == environment)
          && (e.Tenant == tenant))
        .ToArrayAsync(cancellationToken);

    // Convert results
    var serviceStatuses =
      new Dictionary<String, (DateTime Timestamp, HealthStatus Status)>();

    foreach (var result in existingServiceHealthCache) {
      serviceStatuses.Add(result.Service, (result.Timestamp, result.AggregateStatus));
    }

    return serviceStatuses;
  }

  public async Task<Dictionary<(String Service, String HealthCheck), (DateTime Timestamp, HealthStatus Status)>>
    FetchHealthCheckCache(
      String environment,
      String tenant,
      CancellationToken cancellationToken) {

    var existingCachedValues = await this._serviceHealthCacheTable
      .Where(e => (e.Environment == environment) &&
        (e.Tenant == tenant))
      .LeftJoin(
        this._healthCheckCacheTable,
        leftKeySelector: sh => sh.Id,
        rightKeySelector: hc => hc.ServiceHealthId,
        resultSelector: (serviceHealthCache, healthCheckCache) => new {
          ServiceHealthCache = serviceHealthCache,
          HealthCheckCache = healthCheckCache
        })
      .ToArrayAsync(cancellationToken);

    var groupedResults = existingCachedValues.GroupBy(
      p => new {
        Id = p.ServiceHealthCache.Id,
        Service = p.ServiceHealthCache.Service,
        Timestamp = p.ServiceHealthCache.Timestamp
      },
      checks => checks.HealthCheckCache,
      (key, g) => new { ServiceId = key.Id, Service = key.Service, Timestamp = key.Timestamp, Checks = g.ToList() });

    // Convert results
    var healthCheckStatuses =
      new Dictionary<(String Service, String HealthCheck), (DateTime Timestamp, HealthStatus Status)>();

    foreach (var service in groupedResults) {
      foreach (var healthCheck in service.Checks) {
        if (healthCheck != null) {
          healthCheckStatuses.Add(
            (service.Service, healthCheck.HealthCheck),
            (service.Timestamp, healthCheck.Status));
        }
      }
    }

    return healthCheckStatuses;
  }
}
