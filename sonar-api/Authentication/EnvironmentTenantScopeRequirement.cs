using Cms.BatCave.Sonar.Enumeration;
using Microsoft.AspNetCore.Authorization;

namespace Cms.BatCave.Sonar.Authentication;

/// <summary>
///   If the current user's identity is scoped to a specific
/// </summary>
public class EnvironmentTenantScopeRequirement : IAuthorizationRequirement {
  public PermissionType Permission { get; }

  public EnvironmentTenantScopeRequirement(
    PermissionType permission = PermissionType.Standard) {

    this.Permission = permission;
  }
}
