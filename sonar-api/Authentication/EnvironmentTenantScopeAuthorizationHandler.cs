using System;
using System.Linq;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Authentication;

public class EnvironmentTenantScopeAuthorizationHandler :
  AuthorizationHandler<EnvironmentTenantScopeRequirement> {
  private readonly IHttpContextAccessor _contextAccessor;
  private readonly EnvironmentDataHelper _environmentDataHelper;
  private readonly TenantDataHelper _tenantDataHelper;
  private readonly ILogger<EnvironmentTenantScopeAuthorizationHandler> _logger;

  public EnvironmentTenantScopeAuthorizationHandler(
    IHttpContextAccessor contextAccessor,
    EnvironmentDataHelper environmentDataHelper,
    TenantDataHelper tenantDataHelper,
    ILogger<EnvironmentTenantScopeAuthorizationHandler> logger) {

    this._contextAccessor = contextAccessor;
    this._environmentDataHelper = environmentDataHelper;
    this._tenantDataHelper = tenantDataHelper;
    this._logger = logger;
  }

  protected override async Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    EnvironmentTenantScopeRequirement requirement) {

    if (context.Requirements.Any(r => r is IgnoreEnvironmentTenantScopeRequirement)) {
      // This action is explicitly annotated to override this requirement
      context.Succeed(requirement);
      return;
    }

    var httpContext = this._contextAccessor.HttpContext;
    if (httpContext == null) {
      throw new NotSupportedException(
        $"{nameof(EnvironmentTenantScopeAuthorizationHandler)} requires an {nameof(HttpContext)} instance to authorize requests."
      );
    }

    if (context.User.HasGlobalAccess(requirement.Permission)) {
      this._logger.LogDebug(
        "Principal {SubjectType}:{SubjectId} granted access to {Url} because they have global access",
        context.User.FindFirst(SonarIdentityClaims.SubjectType)?.Value,
        context.User.FindFirst(SonarIdentityClaims.SubjectId)?.Value,
        httpContext?.Request.Path
      );

      // The current user has global access
      context.Succeed(requirement);
    } else {
      // The user does not have global access, ensure that the request is scoped, and that the user
      // has access to the specified scope.

      if (httpContext.Request.RouteValues.TryGetValue("environment", out var environmentParam) &&
        environmentParam is String requestEnvironment) {

        if (httpContext.Request.RouteValues.TryGetValue("tenant", out var tenantParam) &&
          tenantParam is String requestTenant) {

          var (_, tenant) = await this._tenantDataHelper.TryFetchTenantAsync(
            requestEnvironment,
            requestTenant,
            httpContext.RequestAborted
          );

          // If the tenant does not exist, fall back to ensuring that the user has access to the environment
          if (tenant != null) {
            if (!httpContext.User.HasTenantAccess(tenant.EnvironmentId, tenant.Id, requirement.Permission)) {
              this._logger.LogInformation(
                "Principal {SubjectType}:{SubjectId} denied access to {Url} because they do not have access to tenant: {Environment}/{Tenant}",
                context.User.FindFirst(SonarIdentityClaims.SubjectType)?.Value,
                context.User.FindFirst(SonarIdentityClaims.SubjectId)?.Value,
                httpContext?.Request.Path,
                requestEnvironment,
                tenant.Name
              );
              context.Fail(new AuthorizationFailureReason(
                handler: this,
                message: $"The current user does not have access to the requested tenant: {tenant.Name}."
              ));
            } else {
              this._logger.LogDebug(
                "Principal {SubjectType}:{SubjectId} granted access to {Url} because they have access to the tenant: {Environment}/{Tenant}",
                context.User.FindFirst(SonarIdentityClaims.SubjectType)?.Value,
                context.User.FindFirst(SonarIdentityClaims.SubjectId)?.Value,
                httpContext?.Request.Path,
                requestEnvironment,
                tenant.Name
              );
              // The current user has access to the requested tenant
              context.Succeed(requirement);
            }

            return;
          }
        }

        var env = await this._environmentDataHelper.TryFetchEnvironmentAsync(
          requestEnvironment,
          httpContext.RequestAborted
        );

        // If the environment does not exist, fall back to ensuring that the user has global access
        if (env != null) {
          if (!httpContext.User.HasEnvironmentAccess(env.Id, requirement.Permission)) {
            this._logger.LogInformation(
              "Principal {SubjectType}:{SubjectId} denied access to {Url} because they do not have access to environment {Environment}",
              context.User.FindFirst(SonarIdentityClaims.SubjectType)?.Value,
              context.User.FindFirst(SonarIdentityClaims.SubjectId)?.Value,
              httpContext?.Request.Path,
              env.Name
            );
            context.Fail(new AuthorizationFailureReason(
              handler: this,
              message: $"The current user does not have access to the requested environment: {env.Name}."
            ));
          } else {
            this._logger.LogDebug(
              "Principal {SubjectType}:{SubjectId} granted access to {Url} because they have access to the environment: {Environment}",
              context.User.FindFirst(SonarIdentityClaims.SubjectType)?.Value,
              context.User.FindFirst(SonarIdentityClaims.SubjectId)?.Value,
              httpContext?.Request.Path,
              env.Name
            );
            // The current user has environment level access
            context.Succeed(requirement);
          }

          return;
        }
      }

      // The request URL is not scoped, but the user does not have global access
      this._logger.LogInformation(
        "Principal {SubjectType}:{SubjectId} denied access to {Url} because they do not have global API access",
        context.User.FindFirst(SonarIdentityClaims.SubjectType)?.Value,
        context.User.FindFirst(SonarIdentityClaims.SubjectId)?.Value,
        httpContext?.Request.Path
      );
      context.Fail(new AuthorizationFailureReason(
        handler: this,
        message:
        "The user's access is limited to a specific environment, but the requested URL requires global access."
      ));
    }
  }
}
