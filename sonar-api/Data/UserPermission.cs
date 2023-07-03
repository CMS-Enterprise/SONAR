using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Data;

[Table("user_permission")]
public class UserPermission {
  [Key]
  public Guid Id { get; init; }

  public Guid UserId { get; init; }

  public Guid? EnvironmentId { get; init; }

  public Guid? TenantId { get; init; }

  public PermissionType Permission { get; set; }

  public UserPermission(
    Guid id,
    Guid userId,
    Guid? environmentId,
    Guid? tenantId,
    PermissionType permission) {

    this.Id = id;
    this.UserId = userId;
    this.EnvironmentId = environmentId;
    this.TenantId = tenantId;
    this.Permission = permission;
  }
}
