using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Helpers;

public class ServiceVersionCacheHelper {
  private readonly DataContext _dbContext;
  private readonly DbSet<ServiceVersionCache> _serviceVersionCacheTable;
  private readonly ILogger<ServiceVersionCacheHelper> _logger;

  public ServiceVersionCacheHelper(
    DataContext dbContext,
    DbSet<ServiceVersionCache> serviceVersionCacheTable,
    ILogger<ServiceVersionCacheHelper> logger) {

    this._dbContext = dbContext;
    this._serviceVersionCacheTable = serviceVersionCacheTable;
    this._logger = logger;
  }

  public async Task CreateUpdateVersionCache(
    String environment,
    String tenant,
    String service,
    ServiceVersion value,
    CancellationToken cancellationToken) {

    try {
      foreach (var check in value.VersionChecks) {
        await this._dbContext.Database.ExecuteSqlInterpolatedAsync(
          $"insert into service_version_cache (id, environment, tenant, service, version_check_type, version, timestamp) values ({Guid.NewGuid()}, {environment}, {tenant}, {service}, {check.Key}, {check.Value}, {value.Timestamp}) on conflict (environment, tenant, service, version_check_type) do update set version = {check.Value}, timestamp = {value.Timestamp}",
          cancellationToken
        );
      }
    } catch (DbUpdateException dbException) {
      this._logger.LogWarning(
        message: "Error occurred while updating service version cache: {Message}",
        dbException.Message
      );
    }
  }

  public async Task<List<ServiceVersionDetails>> FetchServiceVersionCache(
    String environment,
    String tenant,
    String service,
    CancellationToken cancellationToken) {

    var existingServiceVersionCache =
      await this._serviceVersionCacheTable
        .Where(e => (e.Environment == environment) &&
          (e.Tenant == tenant) &&
          (e.Service == service))
        .ToArrayAsync(cancellationToken);

    return existingServiceVersionCache
      .Select(svc => new ServiceVersionDetails(
        svc.VersionCheckType,
        svc.Version,
        svc.Timestamp)).ToList();
  }
}
