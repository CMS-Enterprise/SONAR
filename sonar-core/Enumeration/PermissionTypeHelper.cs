using System;

namespace Cms.BatCave.Sonar.Enumeration;

public class PermissionTypeHelper {
  public static Boolean HasPermission(PermissionType actualPermission, PermissionType requiredPermission) {
    return requiredPermission switch {
      PermissionType.Admin => actualPermission is PermissionType.Admin,
      PermissionType.Standard => actualPermission is PermissionType.Standard or PermissionType.Admin,
      _ => throw new ArgumentOutOfRangeException(nameof(requiredPermission))
    };
  }
}
