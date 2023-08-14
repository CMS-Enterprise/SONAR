using System;
using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Helpers;

public class ApiKeyDataHelper {
  private readonly IApiKeyRepository _apiKeyRepository;
  private readonly IOptions<SecurityConfiguration> _configuration;
  private readonly ILogger<ApiKeyDataHelper> _logger;
  public ApiKeyDataHelper(
    IOptions<SecurityConfiguration> configuration,
    IApiKeyRepository apiKeyRepository,
    ILogger<ApiKeyDataHelper> logger) {

    this._apiKeyRepository = apiKeyRepository;
    this._configuration = configuration;
    this._logger = logger;
  }

  public async Task<ApiKey?> TryMatchApiKeyAsync(String headerApiKey, CancellationToken cancellationToken) {
    if (headerApiKey.Contains(':')) {
      var headerApi = headerApiKey.Split(':');

      try {
        //Checks API key to match Default API
        var defaultApiKey = this.MatchDefaultApiKey(headerApi[1]);
        //Checks default GUID of 00000000-0000-0000-0000-000000000000
        if (new Guid(headerApi[0]) == Guid.Empty) {
          return defaultApiKey;
        }
        //Non default key logic
        if (defaultApiKey == null) {
          var apiKeyDb = await this._apiKeyRepository.FindAsync(new Guid(headerApi[0]), cancellationToken);
          if (KeyHashHelper.ValidatePassword(headerApi[1], apiKeyDb.Key )) {
            return apiKeyDb;
          }
        }

      } catch (FormatException) {
        throw new BadRequestException("Invalid Guid. Guid should contain 32 hex-characters with 4 dashes (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).");
      }
      return null;
    }

    // Legacy/Deprecated ApiKey Header support (Prefer Authorization: ApiKey xxx)
    this._logger.LogWarning("Support of API key without Id specified is being deprecated. " +
      "Please update API Key to follow <ApiKeyId>:<ApiKey>");
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

  public async Task UpdateApiKeyUsageAsync(ApiKey apiKey, CancellationToken cancellationToken) {
    if (apiKey.Id == Guid.Empty) {
      return;
    }

    await this._apiKeyRepository.UpdateUsageAsync(apiKey, cancellationToken);
  }
}
