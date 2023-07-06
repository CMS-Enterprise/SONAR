using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ApiKeyDetails {

  public ApiKeyDetails(
    PermissionType apiKeyType,
    String? environment = null,
    String? tenant = null
    ) {

    this.ApiKeyType = apiKeyType;
    this.Environment = environment;
    this.Tenant = tenant;
  }

  [Required]
  public PermissionType ApiKeyType { get; init; }

  public String? Environment { get; init; }

  public String? Tenant { get; init; }
}
