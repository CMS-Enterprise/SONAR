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

    // TODO: refactor this code to only use claims instead of SonarIdentity
    var identity =
      context.User.Identities.SingleOrDefault(ident => ident is SonarIdentity);
    if (identity is SonarIdentity { IsAuthenticated: true } sonarIdentity) {
      var httpContext = this._contextAccessor.HttpContext;
      if (sonarIdentity.EnvironmentId.HasValue) {
        // The current user is scoped to a particular environment
        if ((httpContext != null) &&
          httpContext.Request.RouteValues.TryGetValue("environment", out var environmentParam) &&
          (environmentParam is String requestEnvironment)) {

          var env = await this._environmentDataHelper.FetchExistingEnvAsync(
            requestEnvironment,
            httpContext.RequestAborted
          );

          if (sonarIdentity.EnvironmentId != env.Id) {
            this._logger.LogInformation(
              "User {User} denied access to {Url} because they do not have access to environment {Environment}",
              sonarIdentity.ApiKeyId,
              httpContext?.Request.Path,
              env.Name
            );
            context.Fail(new AuthorizationFailureReason(
              handler: this,
              message: $"The current user does not have access to the requested environment: {env.Name}."
            ));
          } else {
            if (sonarIdentity.TenantId.HasValue) {

              if (httpContext.Request.RouteValues.TryGetValue("tenant", out var tenantParam) &&
                (tenantParam is String requestTenant)) {

                var tenant = await this._tenantDataHelper.FetchExistingTenantAsync(
                  requestEnvironment,
                  requestTenant,
                  httpContext.RequestAborted
                );

                if (sonarIdentity.TenantId != tenant.Id) {
                  this._logger.LogInformation(
                    "User {User} denied access to {Url} because they do not have access to tenant: {Environment}/{Tenant}",
                    sonarIdentity.ApiKeyId,
                    httpContext?.Request.Path,
                    env.Name,
                    tenant.Name
                  );
                  context.Fail(new AuthorizationFailureReason(
                    handler: this,
                    message: $"The current user does not have access to the requested tenant: {tenant.Name}."
                  ));
                } else {
                  this._logger.LogDebug(
                    "User {User} granted access to {Url} because they have access to the tenant: {Environment}/{Tenant}",
                    sonarIdentity.ApiKeyId,
                    httpContext?.Request.Path,
                    env.Name,
                    tenant.Name
                  );
                  // The current user has access to the requested tenant
                  context.Succeed(requirement);
                }
              }
            } else {
              this._logger.LogDebug(
                "User {User} granted access to {Url} because they have access to the environment: {Environment}",
                sonarIdentity.ApiKeyId,
                httpContext?.Request.Path,
                env.Name
              );
              // The current user has environment level access
              context.Succeed(requirement);
            }
          }
        } else {
          this._logger.LogInformation(
            "User {User} denied access to {Url} because they do not have global API access",
            sonarIdentity.ApiKeyId,
            httpContext?.Request.Path
          );
          context.Fail(new AuthorizationFailureReason(
            handler: this,
            message:
            "The user's access is limited to a specific environment, but the requested URL requires global access."
          ));
        }
      } else {
        this._logger.LogDebug(
          "User {User} granted access to {Url} because they have global access",
          sonarIdentity.ApiKeyId,
          httpContext?.Request.Path
        );
        // The current user has global access
        context.Succeed(requirement);
      }
    } else {
      context.Fail(new AuthorizationFailureReason(
        handler: this,
        message: "The user is not authenticated."
      ));
    }
  }
}
