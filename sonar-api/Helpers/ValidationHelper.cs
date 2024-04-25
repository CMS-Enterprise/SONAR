using System;
using System.Security.Claims;
using Cms.BatCave.Sonar.Authentication;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;

namespace Cms.BatCave.Sonar.Helpers;

public class ValidationHelper {
  private const Int32 MaxSecondsInFuture = 10;
  public static void ValidateTimestamp(DateTime timestamp, String timestampName = "timestamp") {
    if (timestamp.Kind != DateTimeKind.Utc) {
      throw new BadRequestException(
        message: $"Invalid value for {timestampName}: non-utc timestamp",
        ProblemTypes.InvalidData
      );
    }

    if (timestamp.Subtract(DateTime.UtcNow).TotalSeconds > MaxSecondsInFuture) {
      throw new BadRequestException(
        message: $"Invalid value for {timestampName}: timestamp provided is too far in the future",
        ProblemTypes.InvalidData
      );
    }
  }

  public static void ValidatePermissionScope(
    ClaimsPrincipal principal,
    Guid? environmentScope,
    Guid? tenantScope,
    String action) {

    var unauthorizedMsg = $"Current user is not authorized to {action}. Do not have Admin permissions for ";

    // Ensure API client has Admin access and proper permission for the activity's scope
    if (environmentScope.HasValue) {
      if (tenantScope.HasValue) {
        if (!principal.HasTenantAccess(environmentScope.Value, tenantScope.Value, PermissionType.Admin)) {
          throw new ForbiddenException($"{unauthorizedMsg} this Tenant's scope.");
        }
      } else if (!principal.HasEnvironmentAccess(environmentScope.Value, PermissionType.Admin)) {
        throw new ForbiddenException($"{unauthorizedMsg} this Environment's scope.");
      }
    } else {
      // Ensure authenticated API Key has global Admin scope
      if (!principal.HasGlobalAccess(PermissionType.Admin)) {
        throw new ForbiddenException($"{unauthorizedMsg} global scope.");
      }
    }
  }

  public static void ValidateTimestampHasTimezone(DateTime timestamp, String timestampName = "timestamp") {
    if (timestamp.Kind is DateTimeKind.Unspecified) {
      throw new BadRequestException($"Invalid value for {timestampName}: time zone must be specified.");
    }
  }
}
