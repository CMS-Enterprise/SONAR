using System;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Microsoft.Extensions.Configuration;

namespace Cms.BatCave.Sonar.Helpers;

public class ApiKeyDataHelper {
  private readonly IConfiguration _configuration;
  private readonly IApiKeyRepository _apiKeyRepository;

  public ApiKeyDataHelper(
    IConfiguration configuration,
    IApiKeyRepository apiKeyRepository) {

    this._configuration = configuration;
    this._apiKeyRepository = apiKeyRepository;
  }

  public async Task<ApiKey?> TryMatchApiKeyAsync(String headerApiKey, CancellationToken cancellationToken) {
    return this.MatchDefaultApiKey(headerApiKey) ??
      await this._apiKeyRepository.FindAsync(headerApiKey, cancellationToken);
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
