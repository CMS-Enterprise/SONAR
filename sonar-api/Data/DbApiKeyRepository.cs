using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

public class DbApiKeyRepository : IApiKeyRepository {

  private readonly DataContext _dbContext;
  private readonly DbSet<ApiKey> _apiKeysTable;
  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;

  private const Int32 ApiKeyByteLength = 32;

  public DbApiKeyRepository(
    DataContext context,
    DbSet<ApiKey> apiKeysTable,
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable) {

    this._dbContext = context;
    this._apiKeysTable = apiKeysTable;
    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
  }

  public async Task<ApiKeyConfiguration> AddAsync(ApiKeyDetails apiKeyDetails, CancellationToken cancelToken) {
    ApiKeyConfiguration? apiKeyConfiguration = null;
    await using var tx =
      await this._dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancelToken);
    try {
      Environment? environment = null;
      Tenant? tenant = null;
      //See if the key details is requesting environment and tenant names.
      if (apiKeyDetails.Environment != null) {
        environment = this._environmentsTable.FirstOrDefault(env => env.Name == apiKeyDetails.Environment);
        if (environment == null) {
          throw new BadRequestException($"Environment does not exist {apiKeyDetails.Environment}");
        }
      }

      if ((environment != null) && (apiKeyDetails.Tenant != null)) {
        tenant = this._tenantsTable.FirstOrDefault(t =>
          ((t.Name == apiKeyDetails.Tenant) && (t.EnvironmentId == environment.Id)));
        if (tenant == null) {
          throw new BadRequestException($"Tenant does not exist {apiKeyDetails.Tenant}");
        }
      }


      var newKey = new ApiKey(Guid.Empty,  KeyHash.GenerateKey(), apiKeyDetails.ApiKeyType,
        environment?.Id, tenant?.Id);

      // Record new API key
      var createdApiKey = await this._apiKeysTable.AddAsync(newKey, cancelToken);
      await this._dbContext.SaveChangesAsync(cancelToken);
      await tx.CommitAsync(cancelToken);

      //Build and return data to client.
      apiKeyConfiguration = ToApiKeyConfig(environment, tenant, createdApiKey.Entity);
    } catch {
      await tx.RollbackAsync(cancelToken);
      throw;
    }

    return apiKeyConfiguration;
  }

  public async Task<Guid> DeleteAsync(Guid id, CancellationToken cancelToken) {
    await using var tx =
      await this._dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancelToken);
    try {
      // Get API key
      var existingApiKey = await this._apiKeysTable
        .Where(k => k.Id == id)
        .SingleOrDefaultAsync(cancelToken);

      if (existingApiKey == null) {
        throw new ResourceNotFoundException(nameof(ApiKey), id);
      }

      // Delete
      this._apiKeysTable.Remove(existingApiKey);
      // Save
      await this._dbContext.SaveChangesAsync(cancelToken);
      await tx.CommitAsync(cancelToken);
    } catch {
      await tx.RollbackAsync(cancelToken);
      throw;
    }

    return id;
  }

  public async Task<List<ApiKeyConfiguration>> GetKeysAsync(CancellationToken cancelToken) {
    return
      await this._apiKeysTable
        .LeftJoin(
          this._environmentsTable,
          api => api.EnvironmentId,
          env => env.Id,
          (key, env) => new {
            key,
            env
          })
        .LeftJoin(
          this._tenantsTable,
          mm => mm.key.TenantId,
          tenant => tenant.Id,
          (item1, item2) => new {
            KeyEnv = item1,
            tenant = item2
          })
        .Select(
          result => new ApiKeyConfiguration(
            result.KeyEnv.key.Id,
            result.KeyEnv.key.Key,
            result.KeyEnv.key.Type,
            (result.KeyEnv.env != null) ? result.KeyEnv.env.Name : null,
            result.tenant != null ? result.tenant.Name : null))
        .ToListAsync(cancellationToken: cancelToken);
  }

  public async Task<List<ApiKeyConfiguration>> GetEnvKeysAsync(Guid environmentId, CancellationToken cancelToken) {
    return
      await this._apiKeysTable
        // Get only those keys associated with the specified environment
        .Join(
          this._environmentsTable,
          api => api.EnvironmentId,
          env => env.Id,
          (key, env) => new { key, env })
        .Where(e => e.env.Id == environmentId)
        .LeftJoin(
          this._tenantsTable,
          mm => mm.key.TenantId,
          tenant => tenant.Id,
          (item1, tenant) => new {
            KeyEnv = item1,
            tenant
          })
        .Select(
          result => new ApiKeyConfiguration(
            result.KeyEnv.key.Id,
            // The Key itself is only provided during the initial creation
            String.Empty,
            result.KeyEnv.key.Type,
            result.KeyEnv.env.Name,
            result.tenant != null ? result.tenant.Name : null))
        .ToListAsync(cancellationToken: cancelToken);
  }

  public async Task<List<ApiKeyConfiguration>> GetTenantKeysAsync(ApiKey encKey, CancellationToken cancelToken) {
    return
      await this._apiKeysTable
        .Join(
          this._tenantsTable,
          api => api.TenantId,
          tenant => tenant.Id,
          (key, tenant) => new {
            key,
            tenant
          })
        .Where(keyTenant => keyTenant.tenant.Id == encKey.TenantId)
        .Join(this._environmentsTable,
          keyTenant => keyTenant.key.EnvironmentId,
          environment => environment.Id,
          (keyTenant, environment) => new {
            keyTenant.key,
            keyTenant.tenant,
            environment
          })
        .Select(
          result => new ApiKeyConfiguration(
            result.key.Id,
            // The Key itself is only provided during the initial creation
            String.Empty,
            result.key.Type,
            result.environment.Name,
            result.tenant.Name))
        .ToListAsync(cancelToken);
  }

  public async Task<ApiKey> GetApiKeyFromEncKeyAsync(String encKey, CancellationToken cancellationToken) {
    var result =
      await this._apiKeysTable
        .Where(e => e.Key == encKey)
        .SingleOrDefaultAsync(cancellationToken);

    if (result == null) {
      throw new ForbiddenException($"The API key provided doest not exist.");
    }

    return result;
  }

  public async Task<ApiKey> GetApiKeyFromApiKeyIdAsync(Guid keyId, CancellationToken cancellationToken) {

    var result =
      await this._apiKeysTable
        .Where(e => e.Id == keyId)
        .SingleOrDefaultAsync(cancellationToken);

    if (result == null) {
      throw new ResourceNotFoundException($"The API key provided does not exist.");
    }

    return result;
  }

  private static ApiKeyConfiguration ToApiKeyConfig(
    Environment? environment,
    Tenant? tenant,
    ApiKey entity) {

    return new ApiKeyConfiguration(
      entity.Id,
      entity.Key,
      entity.Type,
      environment?.Name,
      tenant?.Name
    );
  }
}
