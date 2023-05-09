using Microsoft.AspNetCore.Authorization;

namespace Cms.BatCave.Sonar.Authentication;

/// <summary>
///   If the current user's identity is scoped to a specific
/// </summary>
public class EnvironmentTenantScopeRequirement : IAuthorizationRequirement {
}
