using System;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Helpers;

public class ApiKeyDataHelper {
  private readonly IApiKeyRepository _apiKeyRepository;
  private readonly IOptions<SecurityConfiguration> _configuration;
  private readonly ILogger<ApiKeyDataHelper> _logger;
  private readonly IHttpContextAccessor _httpContextAccessor;

  public ApiKeyDataHelper(
    IOptions<SecurityConfiguration> configuration,
    IApiKeyRepository apiKeyRepository,
    ILogger<ApiKeyDataHelper> logger,
    IHttpContextAccessor httpContextAccessor) {

    this._apiKeyRepository = apiKeyRepository;
    this._configuration = configuration;
    this._logger = logger;
    this._httpContextAccessor = httpContextAccessor;
  }

  public async Task<ApiKey?> TryMatchApiKeyAsync(String headerApiKey, CancellationToken cancellationToken) {
    if (headerApiKey.Contains(':')) {
      var apiKeyHeaderParts = headerApiKey.Split(':');
      try {
        var apiKeyId = Guid.Parse(apiKeyHeaderParts[0]);
        if (apiKeyId == Guid.Empty) {
          return this.MatchDefaultApiKey(apiKeyHeaderParts[1]);
        } else {
          return await this._apiKeyRepository.FindAsync(apiKeyId, apiKeyHeaderParts[1], cancellationToken);
        }
      } catch (FormatException) {
        throw new BadRequestException("Invalid Guid. Guid should contain 32 hex-characters with 4 dashes (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).");
      }
    }

    var apiKey = this.MatchDefaultApiKey(headerApiKey) ??
      await this._apiKeyRepository.FindAsync(headerApiKey, cancellationToken);

    if (apiKey is not null) {
      this._logger.LogWarning(
        "Usage of valid API key with deprecated API key header format: " +
        "ApiKeyId={ApiKeyId}, EnvironmentId={EnvironmentId}, TenantId={TenantId}, " +
        "RequestMethod={RequestMethod}, RequestPath={RequestPath}",
        apiKey.Id,
        apiKey.EnvironmentId,
        apiKey.TenantId,
        this._httpContextAccessor.HttpContext?.Request.Method,
        this._httpContextAccessor.HttpContext?.Request.Path);
    } else {
      this._logger.LogWarning(
        "Usage of invalid API key with deprecated API key header format: " +
        "RequestMethod={RequestMethod}, RequestPath={RequestPath}",
        this._httpContextAccessor.HttpContext?.Request.Method,
        this._httpContextAccessor.HttpContext?.Request.Path);
    }

    return apiKey;
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
