using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Cms.BatCave.Sonar.Helpers;

public class ApiKeyDataHelper {
  private readonly IConfiguration _configuration;
  private readonly TenantDataHelper _tenantDataHelper;
  private readonly DbSet<ApiKey> _apiKeysTable;

  public ApiKeyDataHelper(
    IConfiguration configuration,
    TenantDataHelper tenantDataHelper,
    DbSet<ApiKey> apiKeysTable) {

    this._configuration = configuration;
    this._tenantDataHelper = tenantDataHelper;
    this._apiKeysTable = apiKeysTable;
  }

  public async Task ValidateAdminPermission(
    String? headerApiKey,
    String adminActivity,
    CancellationToken cancellationToken) {

    if (String.IsNullOrEmpty(headerApiKey)) {
      throw new UnauthorizedException($"Authentication is required to {adminActivity}.");
    }

    var existingApiKey = await this.TryMatchApiKeyAsync(headerApiKey, cancellationToken);

    if (existingApiKey == null) {
      throw new UnauthorizedException($"Invalid authentication credential provided attempting to {adminActivity}.");
    }

    if (existingApiKey.Type != ApiKeyType.Admin) {
      throw new ForbiddenException($"The authentication credential provided is not authorized to {adminActivity}.");
    }
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
        defaultApiKey,
        ApiKeyType.Admin,
        tenantId: null
      );
    } else {
      return null;
    }
  }
}
