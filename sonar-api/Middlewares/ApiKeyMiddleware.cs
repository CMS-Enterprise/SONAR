using System.Linq;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Microsoft.AspNetCore.Http;

namespace Cms.BatCave.Sonar.Middlewares;

public class ApiKeyMiddleware {
  private readonly RequestDelegate _next;

  public ApiKeyMiddleware(RequestDelegate next) {
    this._next = next;
  }

  public async Task InvokeAsync(HttpContext context, ApiKeyDataHelper apiKeyHelper) {
    if (context.Request.Headers.TryGetValue("ApiKey", out var extractedApiKey)) {
      // If an ApiKey was specified, validate that API key
      if (extractedApiKey.Count > 1) {
        throw new UnauthorizedException("Multiple API keys in header.");
      }

      var headerApiKey = extractedApiKey.Single();
      // Check if header's API key is an existing API key
      var existingApiKey = await apiKeyHelper.TryMatchApiKeyAsync(headerApiKey, context.RequestAborted);
      if (existingApiKey == null) {
        throw new UnauthorizedException("Invalid authentication credential provided.");
      }
    }

    await this._next(context);
  }
}
