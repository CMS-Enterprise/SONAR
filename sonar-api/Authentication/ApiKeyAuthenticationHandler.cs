using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions> {
  public const String SchemeName = "ApiKey";

  private readonly ApiKeyDataHelper _apiKeyHelper;

  public ApiKeyAuthenticationHandler(
    ApiKeyDataHelper apiKeyHelper,
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISystemClock clock) : base(options, logger, encoder, clock) {

    this._apiKeyHelper = apiKeyHelper;
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
    // Check if header's API key is an existing API key
    var existingApiKey =
      headerApiKey != null ?
        await this._apiKeyHelper.TryMatchApiKeyAsync(headerApiKey, this.Context.RequestAborted) :
        null;

    if (existingApiKey == null) {
      return AuthenticateResult.Fail("The specified ApiKey is not valid.");
    } else {
      return AuthenticateResult.Success(new AuthenticationTicket(
        new ClaimsPrincipal(new SonarIdentity(existingApiKey)),
        SchemeName
      ));
    }
  }
}
