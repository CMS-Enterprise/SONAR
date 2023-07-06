using System;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Authentication;

public record ScopedPermission {
  public PermissionType Permission { get; init; }

  // These should not be set-able outside of the constructor, even at initialization time, because
  // there is no way to validate correctness when making a non-destructive mutation.
  public Guid? EnvironmentId { get; }
  public Guid? TenantId { get; }

  public ScopedPermission(PermissionType permission, Guid? environmentId = null, Guid? tenantId = null) {
    this.Permission = permission;
    this.EnvironmentId = environmentId;
    this.TenantId = tenantId;

    if (tenantId.HasValue && !environmentId.HasValue) {
      throw new ArgumentException(
        $"The {nameof(environmentId)} parameter must be specified if a non-null {nameof(tenantId)} is specified.",
        nameof(environmentId)
      );
    }
  }

  public override String ToString() {
    return $"{this.Permission}:{this.EnvironmentId?.ToString() ?? "-"}/{this.TenantId?.ToString() ?? "-"}";
  }

  public static ScopedPermission Parse(String permission) {
    var parts = permission.Split(":");
    if (parts.Length != 2) {
      throw new FormatException("The specified string did not match the expected format permission:environmentId/tenantId");
    }
    var scopeParts = parts[1].Split("/");
    if (scopeParts.Length != 2) {
      throw new FormatException("The specified string did not match the expected format permission:environmentId/tenantId");
    }
    return new ScopedPermission(
      Enum.Parse<PermissionType>(parts[0]),
      scopeParts[0] == "-" ? null : Guid.Parse(scopeParts[0]),
      scopeParts[1] == "-" ? null : Guid.Parse(scopeParts[1])
    );
  }

  public Boolean IsGlobal() {
    return (this.EnvironmentId == null) && (this.TenantId == null);
  }

  public Boolean HasPermission(PermissionType permission) {
    return PermissionTypeHelper.HasPermission(this.Permission, permission);
  }
}
