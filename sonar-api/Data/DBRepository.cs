using System;
using System.Collections.Generic;
using System.Threading;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Data;

public class DbRepository : IApiKeyRepository {

  private readonly DataContext _dbContext;
  private readonly DbSet<ApiKey> _apiKeysTable;
  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;

  private const Int32 ApiKeyByteLength = 32;

  public DbRepository(DataContext context, DbSet<ApiKey> apiKeysTable, DbSet<Environment> environmentsTable, DbSet<Tenant> tenantsTable) {
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

      var newKey = new ApiKey(Guid.Empty, GenerateApiKeyValue(), apiKeyDetails.ApiKeyType,
        apiKeyDetails.EnvironmentId, apiKeyDetails.TenantId);

      // Record new API key
      var createdApiKey = await this._apiKeysTable.AddAsync(newKey, cancelToken);
      await this._dbContext.SaveChangesAsync(cancelToken);
      await tx.CommitAsync(cancelToken);

      //Build and return data to client.
      var environmentName = apiKeyDetails.Environment ?? String.Empty;
      var envId = apiKeyDetails.EnvironmentId ?? Guid.Empty;
      var environment = new Environment(envId, environmentName);

      var tenantName = apiKeyDetails.Tenant ?? String.Empty;
      var tenantId = apiKeyDetails.TenantId ?? Guid.Empty;
      var tenant = new Tenant(tenantId, envId, tenantName);

      apiKeyConfiguration = ToApiKeyConfig(environment, tenant, createdApiKey.Entity);
    } catch {
      await tx.RollbackAsync(cancelToken);
      throw;
    }
    return apiKeyConfiguration;

  }
  public async Task<Guid> DeleteAsync(Guid id, CancellationToken cancelToken) {
    return await Task.Run(async () => {

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
    });
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
        .GroupJoin(
          this._tenantsTable,
          mm => mm.key.TenantId,
          tenant => tenant.Id,
          (item1, item2) => new {
            KeyEnv = item1,
            tenant = item2.FirstOrDefault()
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
  public async Task<List<ApiKeyConfiguration>> GetEnvKeysAsync(ApiKey encKey, CancellationToken cancelToken) {
    return
      await this._apiKeysTable
        .Join(
          this._environmentsTable,
          api => api.EnvironmentId,
          env => env.Id,
          (key, env) => new { key, env })
        .Where(e => e.env.Id == encKey.EnvironmentId)
        .GroupJoin(
          this._tenantsTable,
          mm => mm.key.TenantId,
          tenant => tenant.Id,
          (item1, item2) => new {
            KeyEnv = item1,
            tenant = item2.FirstOrDefault()
          })
        .Select(
          result => new ApiKeyConfiguration(
            result.KeyEnv.key.Id,
            result.KeyEnv.key.Key,
            result.KeyEnv.key.Type,
            result.KeyEnv.env.Name,
            result.tenant != null ? result.tenant.Name : null))
        .ToListAsync(cancellationToken: cancelToken);
  }
  public async Task<List<ApiKeyConfiguration>> GetTenantKeysAsync(ApiKey encKey, CancellationToken cancelToken) {

    return
      await this._apiKeysTable
      .LeftJoin(
        this._tenantsTable,
        api => api.TenantId,
        tenant => tenant.Id,
        (key, tenant) => new {
          key,
          tenant
        })
      .Where(mm => (mm.tenant != null) && (mm.tenant.Id == encKey.TenantId))
      .Join(this._environmentsTable,
        a => a.tenant!.EnvironmentId,
        b => b.Id,
        (ma, mb) => new {
          bb = ma,
          cc = mb
        })
      .Select(
        result => new ApiKeyConfiguration(
          result.bb.key.Id,
          result.bb.key.Key,
          result.bb.key.Type,
          result.cc.Name,
          (result.bb.tenant != null) ? result.bb.tenant.Name : null)).ToListAsync();
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
  public ApiKeyDetails GetKeyDetails(ApiKeyType apiKeyType, String? environmentName, String? tenantName) {

    ApiKeyDetails? result = null;

    Guid? envId = null;
    Guid? tenantId = null;

    //See if the key details is requesting environment and tenant names.
    if (environmentName != null) {
      var env = this._environmentsTable.FirstOrDefault(env => env.Name == environmentName);
      if (env == null) {
        throw new BadRequestException($"Environment does not exist {environmentName}");
      }
      envId = env.Id;
    }
    if (tenantName != null) {
      var tenant = this._tenantsTable.FirstOrDefault(tenant => ((tenant.Name == tenantName) && (tenant.EnvironmentId == envId)));
      if (tenant == null) {
        throw new BadRequestException($"Tenant does not exist {tenantName}");
      }
      tenantId = tenant.Id;
    }

    result = new ApiKeyDetails(apiKeyType, environmentName, tenantName, envId, tenantId);
    return result;
  }
  public async Task<ApiKeyDetails> GetKeyDetailsFromKeyIdAsync(Guid keyId, CancellationToken cancelToken) {

    var apiKey = await this.GetApiKeyFromApiKeyIdAsync(keyId, cancelToken);

    String? envName = null;
    String? tenantName = null;

    if (apiKey.EnvironmentId != null) {
      var env = this._environmentsTable.FirstOrDefault(env => env.Id == apiKey.EnvironmentId);
      if (env == null) {
        throw new BadRequestException($"Environment does not exist {env}");
      }
      envName = env.Name;
    }
    if (apiKey.TenantId != null) {
      var tenant = this._tenantsTable.FirstOrDefault(tenant => (tenant.Id == apiKey.TenantId) && (tenant.EnvironmentId == apiKey.EnvironmentId));
      if (tenant == null) {
        throw new BadRequestException($"Tenant does not exist {tenant}");
      }
      tenantName = tenant.Name;
    }
    return new ApiKeyDetails(apiKey.Type, envName, tenantName, apiKey.EnvironmentId, apiKey.TenantId);
  }
  private static String GenerateApiKeyValue() {
    var apiKey = new Byte[ApiKeyByteLength];
    String encodedApiKey = "";

    using (var rng = RandomNumberGenerator.Create()) {
      // Generate API key
      rng.GetBytes(apiKey);

      // Encode API key
      encodedApiKey = Convert.ToBase64String(apiKey);
    }
    return encodedApiKey;
  }
  private static ApiKeyConfiguration ToApiKeyConfig(
    Data.Environment? environment, Tenant? tenant, ApiKey entity) {
    return new ApiKeyConfiguration(
      entity.Id,
      entity.Key,
      entity.Type,
      environment?.Name,
      tenant?.Name
    );
  }
}
