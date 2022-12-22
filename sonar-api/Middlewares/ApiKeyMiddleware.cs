using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Middlewares;

public class ApiKeyMiddleware {
  private readonly RequestDelegate _next;

  public ApiKeyMiddleware(RequestDelegate next) {
    _next = next;
  }

  public async Task InvokeAsync(HttpContext context, DbSet<ApiKey> apiKeysTable, DbSet<Tenant> tenantsTable) {
    if (!context.Request.Headers.TryGetValue("ApiKey", out var extractedApiKey)) {
      await this.WriteUnauthorizedMsg(context, "Missing API key in header.");
      return;
    }

    // Create cancellation source, token, new task
    var source = new CancellationTokenSource();
    CancellationToken cancellationToken = source.Token;

    if (context.Request.Headers["ApiKey"].Count > 1) {
      await this.WriteUnauthorizedMsg(context, "Multiple API keys in header.");
      return;
    }

    var headerApiKey = context.Request.Headers["ApiKey"].Single();

    try {
      // Check if header's API key is an existing API key
      var existingApiKey = await apiKeysTable
        .Where(k => k.Key == headerApiKey)
        .SingleOrDefaultAsync(cancellationToken);
      if (existingApiKey == null) {
        await this.WriteUnauthorizedMsg(context, "Invalid API key input.");
        return;
      }

      // If tenant is in request path, check if existing API key is associated with it
      String requestPath = context.Request.Path.ToString();
      if (requestPath.Contains("tenants")) {
        String tenantName = Regex.Replace(requestPath, ".*tenants/", "")
          .Split("/")[0];

        var existingTenant = await tenantsTable
          .Where(t => t.Name == tenantName)
          .SingleOrDefaultAsync(cancellationToken);

        if ((existingApiKey.Type != ApiKeyType.Admin) &&
            (existingApiKey.TenantId != existingTenant.Id)) {
          await this.WriteUnauthorizedMsg(context, $"API key is not authorized for {existingTenant.Name}");
        }
      }
    } catch {
      await this.WriteUnauthorizedMsg(context, "Invalid API key configuration.");
      return;
    }

    await _next(context);
  }

  private async Task WriteUnauthorizedMsg(HttpContext context, String errorMessage) {
    context.Response.StatusCode = (Int32)HttpStatusCode.Unauthorized;
    await context.Response.WriteAsync(errorMessage);
  }
}
