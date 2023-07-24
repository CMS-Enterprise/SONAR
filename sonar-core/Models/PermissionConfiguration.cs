using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public class PermissionConfiguration {

  public PermissionConfiguration(
    Guid id,
    String userEmail,
    PermissionType permission,
    String? environment = null,
    String? tenant = null) {

    this.Id = id;
    this.UserEmail = userEmail;
    this.Permission = permission;
    this.Environment = environment;
    this.Tenant = tenant;
  }
  public Guid Id { get; set; }
  public PermissionType Permission { get; init; }
  public String UserEmail { get; init; }
  public String? Environment { get; init; }
  public String? Tenant { get; init; }
}
