using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Cms.BatCave.Sonar.Authentication;

public class IgnoreEnvironmentTenantScopeAuthorizationHandler :
  AuthorizationHandler<IgnoreEnvironmentTenantScopeRequirement> {

  protected override Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    IgnoreEnvironmentTenantScopeRequirement requirement) {

    // This requirement is a no-op it just overrides the default
    context.Succeed(requirement);
    return Task.CompletedTask;
  }
}
