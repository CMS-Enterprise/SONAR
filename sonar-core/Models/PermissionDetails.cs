using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record PermissionDetails {
  public PermissionDetails(
    String userEmail,
    PermissionType permission,
    String? environment = null,
    String? tenant = null
  ) {

    this.UserEmail = userEmail;
    this.Permission = permission;
    this.Environment = environment;
    this.Tenant = tenant;
  }

  [Required]
  public PermissionType Permission { get; init; }

  [Required]
  public String UserEmail { get; init; }

  public String? Environment { get; init; }

  public String? Tenant { get; init; }
}
