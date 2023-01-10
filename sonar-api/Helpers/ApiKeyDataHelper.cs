using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Extensions;
using Microsoft.EntityFrameworkCore;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Helpers;

public class ApiKeyDataHelper {
  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;
  private readonly DbSet<ApiKey> _apiKeysTable;

  public ApiKeyDataHelper(
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable,
    DbSet<ApiKey> apiKeysTable) {

    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
    this._apiKeysTable = apiKeysTable;
  }

  public async Task<Guid?> FetchExistingTenantId(
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

    return result.Tenant.Id;
  }

  public async Task ValidateAdminPermission(
    String headerApiKey,
    String adminActivity,
    CancellationToken cancellationToken) {

    var existingApiKey = await this._apiKeysTable
      .Where(k => k.Key == headerApiKey)
      .SingleOrDefaultAsync(cancellationToken);

    if (existingApiKey.Type != ApiKeyType.Admin) {
      throw new UnauthorizedException($"API key in header is not authorized to {adminActivity}.");
    }
  }

  public async Task ValidateUpdatePermission(
    String headerApiKey,
    String environment,
    String tenant,
    CancellationToken cancellationToken) {

    var existingApiKey = await this._apiKeysTable
      .Where(k => k.Key == headerApiKey)
      .SingleOrDefaultAsync(cancellationToken);

    Guid? tenantId = await this.FetchExistingTenantId(environment, tenant, cancellationToken);

    if ((existingApiKey.Type != ApiKeyType.Admin) &&
        (existingApiKey.TenantId != tenantId)) {
      throw new UnauthorizedException(
        "API key in header is not authorized to update a tenant's configuration.");
    }
  }
}
