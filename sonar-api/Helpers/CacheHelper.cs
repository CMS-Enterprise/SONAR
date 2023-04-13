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

public class CacheHelper {
  private readonly DataContext _dbContext;
  private readonly DbSet<ServiceHealthCache> _serviceHealthCacheTable;
  private readonly DbSet<HealthCheckCache> _healthCheckCacheTable;
  private readonly ILogger<CacheHelper> _logger;

  public CacheHelper(
    DataContext dbContext,
    DbSet<ServiceHealthCache> serviceHealthCacheTable,
    DbSet<HealthCheckCache> healthCheckCacheTable,
    ILogger<CacheHelper> logger) {

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

    var existingCachedValues = await this._serviceHealthCacheTable
      .Where(e => (e.Environment == environment) &&
        (e.Tenant == tenant) &&
        (e.Service == service))
      .LeftJoin(
        this._healthCheckCacheTable,
        leftKeySelector: sh => sh.Id,
        rightKeySelector: hc => hc.ServiceHealthId,
        resultSelector: (serviceHealthCache, healthCheckCache) => new {
          ServiceHealthCache = serviceHealthCache,
          HealthCheckCache = healthCheckCache
        })
      .ToArrayAsync(cancellationToken);

    // initialize db transaction
    await using var tx =
      await this._dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

    try {
      // cache for this service doesn't exist, create
      if (existingCachedValues.Length == 0) {
        var createdServiceCache = await this._serviceHealthCacheTable.AddAsync(
          new ServiceHealthCache(
            Guid.Empty,
            environment,
            tenant,
            service,
            value.Timestamp,
            value.AggregateStatus),
          cancellationToken
        );

        // create healthCheckCache using newly created serviceHealthCache
        await this._healthCheckCacheTable.AddAllAsync(
          healthChecks.Select(
            hc => new HealthCheckCache(
              Guid.Empty,
              createdServiceCache.Entity.Id,
              hc.Key,
              hc.Value)),
          cancellationToken
        );
      } else {
        // cache for this service exists, update
        var id = existingCachedValues[0].ServiceHealthCache.Id;

        // remove outdated healthCheckCache entries
        var healthChecksToRemove =
          existingCachedValues.Select(obj => obj.HealthCheckCache)
            .Where(hcc => hcc != null)
            .Cast<HealthCheckCache>()
            .ToList();
        this._healthCheckCacheTable.RemoveRange(healthChecksToRemove);

        // add new healthCheckCache entries
        await this._healthCheckCacheTable.AddAllAsync(
          healthChecks.Select(
            hc => new HealthCheckCache(
              Guid.Empty,
              id,
              hc.Key,
              hc.Value)),
          cancellationToken: cancellationToken
        );

        // update serviceHealthCache entry
        this._serviceHealthCacheTable.Update(new ServiceHealthCache(
          id,
          environment,
          tenant,
          service,
          value.Timestamp,
          value.AggregateStatus)
        );
      }

      await this._dbContext.SaveChangesAsync(cancellationToken);
      await tx.CommitAsync(cancellationToken);
    } catch (DbUpdateException dbException) {
      this._logger.LogError(
        message: "Error occurred while updating cache: {Message}",
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
