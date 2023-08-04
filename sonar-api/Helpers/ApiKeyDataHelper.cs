using System;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Helpers;

public class ApiKeyDataHelper {
  private readonly IApiKeyRepository _apiKeyRepository;
  private readonly IOptions<SecurityConfiguration> _configuration;

  public ApiKeyDataHelper(
    IOptions<SecurityConfiguration> configuration,
    IApiKeyRepository apiKeyRepository) {

    this._apiKeyRepository = apiKeyRepository;
    this._configuration = configuration;
  }

  public async Task<ApiKey?> TryMatchApiKeyAsync(String headerApiKey, CancellationToken cancellationToken) {
    return this.MatchDefaultApiKey(headerApiKey) ??
      await this._apiKeyRepository.FindAsync(headerApiKey, cancellationToken);
  }

  private ApiKey? MatchDefaultApiKey(String headerApiKey) {
    var defaultApiKey = this._configuration.Value.DefaultApiKey;
    if (!String.IsNullOrEmpty(defaultApiKey) && String.Equals(defaultApiKey, headerApiKey, StringComparison.Ordinal)) {
      return new ApiKey(
        Guid.Empty,
        defaultApiKey,
        PermissionType.Admin,
        environmentId: null,
        tenantId: null
      );
    } else {
      return null;
    }
  }

  public async Task<Guid?> UpdateApiKeyUsageAsync(ApiKey apiKey, CancellationToken cancellationToken) {
    if (apiKey.Id == Guid.Empty) {
      return null;
    }
    return await this._apiKeyRepository.UpdateUsageAsync(apiKey.Id, cancellationToken);
  }
}
