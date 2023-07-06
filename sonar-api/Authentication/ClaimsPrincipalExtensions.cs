using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Authentication;

public static class ClaimsPrincipalExtensions {
  public static Boolean HasGlobalAccess(
    this ClaimsPrincipal principal,
    PermissionType permissionType = PermissionType.Standard) {

    return principal.GetEffectivePermissions()
      .Any(p => !p.EnvironmentId.HasValue && !p.TenantId.HasValue && p.HasPermission(permissionType));
  }

  /// <summary>
  ///   Determines if the specified principal has access, either explicit or inherited from a parent
  ///   scope, to the specified environment.
  /// </summary>
  public static Boolean HasEnvironmentAccess(
    this ClaimsPrincipal principal,
    Guid environmentId,
    PermissionType permission = PermissionType.Standard) {

    return principal.GetEffectivePermissions()
      .Any(p =>
        p.HasPermission(permission) &&
        (p.IsGlobal() || ((p.EnvironmentId == environmentId) && !p.TenantId.HasValue)));
  }

  /// <summary>
  ///   Determines if the specified principal has access, either explicit or inherited from a parent
  ///   scope, to the specified tenant.
  /// </summary>
  public static Boolean HasTenantAccess(
    this ClaimsPrincipal principal,
    Guid environmentId,
    Guid tenantId,
    PermissionType permission = PermissionType.Standard) {


    return principal.GetEffectivePermissions()
      .Any(p =>
        p.HasPermission(permission) &&
        (p.IsGlobal() || (p.TenantId == tenantId) || (!p.TenantId.HasValue && (p.EnvironmentId == environmentId))));
  }

  /// <summary>
  ///   Gets a minimal sequence of <see cref="ScopedPermission" /> objects indicating the principal's
  ///   effective permission within each scope taking into account rules for how permissions applied to
  ///   one scope might take precedence over the other.
  /// </summary>
  /// <remarks>
  ///   When permission are specified for both a Tenant scope and the Environment scope that that Tenant
  ///   resides within, the Tenant scope permission is only applied if it is more permissive than what is
  ///   specified at the Environment scope. Likewise when a principal has been granted permissions at the
  ///   Global level, Environment and Tenant permissions will only be included in the sequence when they
  ///   are more permissive than the global scope.
  /// </remarks>
  /// <param name="principal">The principal to return the sequence of permissions for.</param>
  public static IEnumerable<ScopedPermission> GetEffectivePermissions(this ClaimsPrincipal principal) {
    ScopedPermission? globalAccess = null;
    var environmentAccess = new Dictionary<Guid, ScopedPermission>();
    var tenantAccess = new Dictionary<Guid, (Guid, ScopedPermission)>();

    var accessClaims =
      principal.FindAll(SonarIdentityClaims.Access).Select(v => ScopedPermission.Parse(v.Value));

    foreach (var accessClaim in accessClaims) {
      if (accessClaim.EnvironmentId.HasValue) {
        // This assumes there aren't multiple claims for the same Tenant/Environment. If there are,
        // which one takes precedence is undefined.
        if (accessClaim.TenantId.HasValue) {
          tenantAccess[accessClaim.TenantId.Value] = (accessClaim.EnvironmentId.Value, accessClaim);
        } else {
          environmentAccess[accessClaim.EnvironmentId.Value] = accessClaim;
        }
      } else {
        // If this is the first global access claim, or this claim subsumes any previous claim...
        if ((globalAccess == null) || accessClaim.HasPermission(globalAccess.Permission)) {
          globalAccess = accessClaim;
        }
      }
    }

    if (globalAccess != null) {
      yield return globalAccess;
    }

    foreach (var access in environmentAccess.Values) {
      // Only include environments where the permission level is greater than the global access
      if ((globalAccess == null) || !globalAccess.HasPermission(access.Permission)) {
        yield return access;
      }
    }

    foreach (var (environmentId, access) in tenantAccess.Values) {
      // Only include environments where the permission level is greater than the global access and
      // any access granted for the environment
      if ((globalAccess == null) || !globalAccess.HasPermission(access.Permission)) {
        if (!environmentAccess.TryGetValue(environmentId, out var envAccess) ||
          !envAccess.HasPermission(access.Permission)) {

          yield return access;
        }
      }
    }
  }
}
