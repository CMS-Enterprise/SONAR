using System;
using System.Security.Claims;

namespace Cms.BatCave.Sonar.Authentication;

public static class ClaimsPrincipalExtensions {
  public static Boolean IsGlobal(this ClaimsPrincipal principal) {
    return !principal.HasClaim(c => c.Type == SonarIdentityClaims.Environment) &&
      !principal.HasClaim(c => c.Type == SonarIdentityClaims.Tenant);
  }

  public static (Guid? EnvironmentId, Guid? TenantId) GetScope(this ClaimsPrincipal principal) {
    Guid? environmentId = null;
    Guid? tenantId = null;

    var envClaim = principal.FindFirst(c => c.Type == SonarIdentityClaims.Environment);
    if (envClaim != null) {
      environmentId = Guid.Parse(envClaim.Value);

      var tenantClaim = principal.FindFirst(c => c.Type == SonarIdentityClaims.Tenant);
      if (tenantClaim != null) {
        tenantId = Guid.Parse(tenantClaim.Value);
      }
    }

    return (environmentId, tenantId);
  }
}
