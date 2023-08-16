using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions> {
  public const String SchemeName = "ApiKey";

  private readonly IOptions<DatabaseConfiguration> _dbConfiguration;
  private readonly IOptions<SecurityConfiguration> _securityConfiguration;
  private readonly ILoggerFactory _loggerFactory;
  private readonly KeyHashHelper _keyHashHelper;

  public ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    IOptions<DatabaseConfiguration> dbConfiguration,
    IOptions<SecurityConfiguration> securityConfiguration,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    ISystemClock clock,
    KeyHashHelper keyHashHelper) : base(options, loggerFactory, encoder, clock) {
    this._dbConfiguration = dbConfiguration;
    this._securityConfiguration = securityConfiguration;
    this._loggerFactory = loggerFactory;
    this._keyHashHelper = keyHashHelper;
  }

  protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
    if (this.Context.Request.Headers.TryGetValue("Authorization", out var authHeader)) {
      if (authHeader.Count > 1) {
        AuthenticateResult.Fail("Authentication failed. Multiple Authorization Headers were specified.");
      }

      var authHeaderValue = authHeader.SingleOrDefault();

      if ((authHeaderValue != null) && authHeaderValue.StartsWith("ApiKey ")) {
        return await this.AuthenticateApiKey(authHeaderValue[6..].Trim());
      }
    }

    if (this.Context.Request.Headers.TryGetValue("ApiKey", out var extractedApiKey)) {
      // Legacy/Deprecated ApiKey Header support (Prefer Authorization: ApiKey xxx)
      // If an ApiKey was specified, validate that API key
      if (extractedApiKey.Count > 1) {
        AuthenticateResult.Fail("Authentication failed. Multiple ApiKey Headers were specified.");
      }

      return await this.AuthenticateApiKey(extractedApiKey.SingleOrDefault());
    }

    // No matching authentication credential provided
    return AuthenticateResult.NoResult();
  }

  private async Task<AuthenticateResult> AuthenticateApiKey(String? headerApiKey) {
    // Use a short lived DataContext for ApiKey authentication and updates.
    await using var dataContext = new DataContext(this._dbConfiguration, this._loggerFactory);
    var repository =
      new DbApiKeyRepository(
        dataContext,
        dataContext.Set<ApiKey>(),
        dataContext.Set<Environment>(),
        dataContext.Set<Tenant>(),
        this._keyHashHelper,
        this._loggerFactory.CreateLogger<DbApiKeyRepository>()
      );
    var apiKeyHelper =
      new ApiKeyDataHelper(
        this._securityConfiguration,
        repository,
        this._loggerFactory.CreateLogger<ApiKeyDataHelper>()
      );
    // Use a single shared transaction for reading and updating ApiKeys
    // Check if header's API key is an existing API key
    var existingApiKey =
      headerApiKey != null ?
        await apiKeyHelper.TryMatchApiKeyAsync(headerApiKey, this.Context.RequestAborted) :
        null;

    if (existingApiKey == null) {
      return AuthenticateResult.Fail("The specified ApiKey is not valid.");
    } else {
      await apiKeyHelper.UpdateApiKeyUsageAsync(existingApiKey, this.Context.RequestAborted);
      return AuthenticateResult.Success(new AuthenticationTicket(
        new ClaimsPrincipal(new SonarIdentity(existingApiKey)),
        SchemeName
      ));
    }
  }
}
