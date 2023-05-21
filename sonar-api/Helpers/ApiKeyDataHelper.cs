using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Helpers;

public class ApiKeyDataHelper {
  private readonly IConfiguration _configuration;
  private readonly TenantDataHelper _tenantDataHelper;
  private readonly DbSet<ApiKey> _apiKeysTable;
  private readonly DbSet<Environment> _environmentsTable;

  public ApiKeyDataHelper(
    IConfiguration configuration,
    TenantDataHelper tenantDataHelper,
    DbSet<ApiKey> apiKeysTable,
    DbSet<Environment> environmentsTable) {

    this._configuration = configuration;
    this._tenantDataHelper = tenantDataHelper;
    this._apiKeysTable = apiKeysTable;
    this._environmentsTable = environmentsTable;
  }

  public async Task<Boolean> ValidateAdminPermission(
    [NotNull]
    String? headerApiKey,
    Boolean global,
    String adminActivity,
    CancellationToken cancellationToken) {

    if (String.IsNullOrEmpty(headerApiKey)) {
      throw new UnauthorizedException($"Authentication is required to {adminActivity}.");
    }

    var existingApiKey = await this.TryMatchApiKeyAsync(headerApiKey, cancellationToken);

    if (existingApiKey == null) {
      throw new UnauthorizedException($"Invalid authentication credential provided attempting to {adminActivity}.");
    }

    return (existingApiKey.Type == ApiKeyType.Admin) && (!global || !existingApiKey.EnvironmentId.HasValue);
  }

  public async Task<Boolean> ValidateEnvPermission(
    String headerApiKey,
    String environmentName,
    CancellationToken cancellationToken) {

    // Check if API key is associated with the specified Environment
    var existingApiKey = await this.TryMatchApiKeyAsync(headerApiKey, cancellationToken);

    Environment? specifiedEnv =
      await this._environmentsTable
        .Where(e => e.Name == environmentName)
        .SingleOrDefaultAsync(cancellationToken);

    if ((specifiedEnv == null) || (existingApiKey?.EnvironmentId != specifiedEnv.Id)) {
      return false;
    }
    return true;
  }

  public async Task ValidateTenantPermission(
    String? headerApiKey,
    String environment,
    String tenant,
    String activity,
    CancellationToken cancellationToken) {

    if (String.IsNullOrEmpty(headerApiKey)) {
      throw new UnauthorizedException($"Authentication is required to {activity}.");
    }

    var existingApiKey = await this.TryMatchApiKeyAsync(headerApiKey, cancellationToken);

    if (existingApiKey == null) {
      throw new UnauthorizedException(
        $"Invalid authentication credential provided attempting to {activity}."
      );
    }

    var tenantEntity = await this._tenantDataHelper.FetchExistingTenantAsync(environment, tenant, cancellationToken);
    if ((existingApiKey.Type != ApiKeyType.Admin) &&
        (existingApiKey.TenantId != tenantEntity.Id)) {
      throw new ForbiddenException(
        $"The authentication credential provided is not authorized to {activity}."
      );
    }
  }

  public async Task<ApiKey?> TryMatchApiKeyAsync(String headerApiKey, CancellationToken cancellationToken) {
    return this.MatchDefaultApiKey(headerApiKey) ??
      await this._apiKeysTable
        .Where(k => k.Key == headerApiKey)
        .SingleOrDefaultAsync(cancellationToken);
  }

  private ApiKey? MatchDefaultApiKey(String headerApiKey) {
    var defaultApiKey = this._configuration.GetValue<String>("ApiKey");
    if (!String.IsNullOrEmpty(defaultApiKey) && String.Equals(defaultApiKey, headerApiKey, StringComparison.Ordinal)) {
      return new ApiKey(
        Guid.Empty,
        defaultApiKey,
        ApiKeyType.Admin,
        environmentId: null,
        tenantId: null
      );
    } else {
      return null;
    }
  }
}
