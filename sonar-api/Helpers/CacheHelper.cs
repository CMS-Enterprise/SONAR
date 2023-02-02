using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Environment = Cms.BatCave.Sonar.Data.Environment;
using String = System.String;

namespace Cms.BatCave.Sonar.Helpers;

public class CacheHelper {
  private readonly DataContext _dbContext;
  private readonly DbSet<ServiceHealthCache> _serviceHealthCacheTable;
  private readonly DbSet<HealthCheckCache> _healthCheckCacheTable;

  public CacheHelper(
    DataContext dbContext,
    DbSet<ServiceHealthCache> serviceHealthCacheTable,
    DbSet<HealthCheckCache> healthCheckCacheTable) {

    this._dbContext = dbContext;
    this._serviceHealthCacheTable = serviceHealthCacheTable;
    this._healthCheckCacheTable = healthCheckCacheTable;
  }

  public async Task CreateUpdateCache(
    String environment,
    String tenant,
    String service,
    ServiceHealth value,
    ImmutableDictionary<String, HealthStatus> healthChecks,
    CancellationToken cancellationToken) {

    Console.WriteLine($"Fetching existing cache for {environment}/{tenant}/{service}");
    var existingCachedValues = await this._serviceHealthCacheTable
      .Where(e => (e.Environment == environment)
        && (e.Tenant == tenant)
        && (e.Service == service))
      .LeftJoin(
        this._healthCheckCacheTable,
        leftKeySelector: sh => sh.Id,
        rightKeySelector: hc => hc.ServiceHealthId,
        resultSelector: (serviceHealthCache, healthCheckCache) => new
          { ServiceHealthCache = serviceHealthCache, HealthCheckCache = healthCheckCache })
      .ToArrayAsync(cancellationToken);

    // initialize db transaction
    await using var tx =
      await this._dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

    try {
      // cache for this service doesn't exist, create
      if (existingCachedValues.Length == 0) {
        ServiceHealthCache serviceHealthCacheEntity;

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
        serviceHealthCacheEntity = createdServiceCache.Entity;
        Console.WriteLine($"Created new cache for {environment}/{tenant}/{service}");

        // create healthCheckCache using newly created serviceHealthCache
        await this._healthCheckCacheTable.AddAllAsync(
          healthChecks.Select(
            hc => new HealthCheckCache(
              Guid.Empty,
              serviceHealthCacheEntity.Id,
              hc.Key,
              hc.Value)),
          cancellationToken
        );

        Console.WriteLine($"Created new health check cache for {environment}/{tenant}/{service}");
      } else {
        // cache for this service exists, update

        var id = existingCachedValues[0].ServiceHealthCache.Id;
        // remove outdated healthCheckCache entries
        List<HealthCheckCache> healthChecksToRemove = new List<HealthCheckCache>();
        healthChecksToRemove = existingCachedValues.Select(obj => obj.HealthCheckCache).ToList();
        Console.WriteLine("Cache already exists... performing updates");
        this._healthCheckCacheTable.RemoveRange(healthChecksToRemove);

        // add new healthCheckCache entries
        await this._healthCheckCacheTable.AddAllAsync(healthChecks.Select(
          hc => new HealthCheckCache(
            Guid.Empty,
            id,
            hc.Key,
            hc.Value))
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
      Console.WriteLine("Data saved.");
    } catch (DbUpdateException dbException) {
      Console.WriteLine(dbException.Message);
    }
  }

  public async Task<ImmutableDictionary<String, HealthStatus>?> FetchCache(
    String environment,
    String tenant,
    String service,
    CancellationToken cancellationToken) {

    var existingCachedValues = await this._serviceHealthCacheTable
      .Where(e => (e.Environment == environment)
        && (e.Tenant == tenant)
        && (e.Service == service))
      .LeftJoin(
        this._healthCheckCacheTable,
        leftKeySelector: sh => sh.Id,
        rightKeySelector: hc => hc.ServiceHealthId,
        resultSelector: (serviceHealthCache, healthCheckCache) => new
          { ServiceHealthCache = serviceHealthCache, HealthCheckCache = healthCheckCache })
      .ToArrayAsync(cancellationToken);

    if (existingCachedValues.Length == 0) {
      return null;
    }

    return existingCachedValues.ToImmutableDictionary(
      x => x.HealthCheckCache.HealthCheck,
      x => x.HealthCheckCache.Status);
  }


}
