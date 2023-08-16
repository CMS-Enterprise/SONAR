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
      var apiKeyHeaderParts = headerApiKey.Split(':');
      try {
        var apiKeyId = Guid.Parse(apiKeyHeaderParts[0]);
        if (apiKeyId == Guid.Empty) {
          return this.MatchDefaultApiKey(apiKeyHeaderParts[1]);
        } else {
          var apiKeyDb = await this._apiKeyRepository.FindAsync(apiKeyId, cancellationToken);
          if (KeyHashHelper.ValidatePassword(apiKeyHeaderParts[1], apiKeyDb.Key)) {
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
