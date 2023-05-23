using System;
using System.Collections.Generic;
using System.Threading;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Controllers;

public class DBRepository : IApiKeyRepository {

  private readonly DataContext _dbContext;
  private readonly DbSet<ApiKey> _apiKeysTable;
  private readonly EnvironmentDataHelper _envDataHelper;
  private readonly TenantDataHelper _tenantDataHelper;
  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;

  private readonly CancellationToken _cancellationToken;

  private const Int32 ApiKeyByteLength = 32;

  public DBRepository(DataContext context, DbSet<ApiKey> apiKeysTable, EnvironmentDataHelper envDataHelper, TenantDataHelper tenantDataHelper,
    DbSet<Environment> environmentsTable, DbSet<Tenant> tenantsTable, CancellationToken token) {
    this._dbContext = context;
    this._apiKeysTable = apiKeysTable;
    this._cancellationToken = token;
    this._envDataHelper = envDataHelper;
    this._tenantDataHelper = tenantDataHelper;
    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
  }

  public async Task<ApiKeyConfiguration> AddAsync(ApiKeyDetails apiKeyDetails) {
    return await Task.Run(async () => {
      ApiKeyConfiguration? apiKeyConfiguration = null;
      await using var tx =
         await this._dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, this._cancellationToken);
      try {
        var environmentName = apiKeyDetails.Environment ?? String.Empty;
        var tenantName = apiKeyDetails.Tenant ?? String.Empty;
        var environment = await this._envDataHelper.FetchExistingEnvAsync(environmentName, this._cancellationToken);
        var tenant = await this._tenantDataHelper.FetchExistingTenantAsync(environmentName, tenantName, this._cancellationToken);
        var newKey = new ApiKey(Guid.Empty, GenerateApiKeyValue(), apiKeyDetails.ApiKeyType, environment.Id, tenant.Id);

        // Record new API key
        var createdApiKey = await this._apiKeysTable.AddAsync(newKey, this._cancellationToken);
        await this._dbContext.SaveChangesAsync(this._cancellationToken);
        await tx.CommitAsync(this._cancellationToken);
        apiKeyConfiguration = ToApiKeyConfig(environment, tenant, createdApiKey.Entity);
      } catch {
        await tx.RollbackAsync(this._cancellationToken);
        throw;
      }
      return apiKeyConfiguration;
    });
  }

  public async Task<IEnumerable<ApiKeyConfiguration>> GetKeysAsync() {
    return await Task.Run(() => {
        var apiKeyConfigurations =
          this._apiKeysTable
            .Join(
              this._environmentsTable,
              api => api.EnvironmentId,
              env => env.Id,
              (key, env) => new { key, env })
            .Join(
              this._tenantsTable,
              mm => mm.key.TenantId,
              tenant => tenant.Id,
              (KeyEnv, tenant) => new { KeyEnv, tenant })
            .Select(
              result => new ApiKeyConfiguration(
                result.KeyEnv.key.Id,
                result.KeyEnv.key.Key,
                result.KeyEnv.key.Type,
                result.KeyEnv.env.Name,
                result.tenant.Name))
            .ToList();
        return apiKeyConfigurations;
    });
  }

  public async Task<Guid> DeleteAsync(Guid id) {
    return await Task.Run(async () => {

      await using var tx =
        await this._dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, this._cancellationToken);
      try {
        // Get API key
        var existingApiKey = await this._apiKeysTable
              .Where(k => k.Id == id)
              .SingleOrDefaultAsync(this._cancellationToken);

        if (existingApiKey == null) {
          throw new ResourceNotFoundException(nameof(ApiKey), id);
        }

        // Delete
        this._apiKeysTable.Remove(existingApiKey);
        // Save
        await this._dbContext.SaveChangesAsync(this._cancellationToken);
        await tx.CommitAsync(this._cancellationToken);
      } catch {
        await tx.RollbackAsync(this._cancellationToken);
        throw;
      }
      return id;
    });
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
    Data.Environment? environment,
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
