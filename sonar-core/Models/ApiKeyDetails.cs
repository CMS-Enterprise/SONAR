using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ApiKeyDetails {

  public ApiKeyDetails(
    ApiKeyType apiKeyType,
    String? environment = null,
    String? tenant = null,
    Guid? environmentId = null,
    Guid? tenantId = null
    ) {

    this.ApiKeyType = apiKeyType;
    this.Environment = environment;
    this.EnvironmentId = environmentId;
    this.Tenant = tenant;
    this.TenantId = tenantId;
  }

  [Required]
  public ApiKeyType ApiKeyType { get; init; }

  public String? Environment { get; init; }
  public Guid? EnvironmentId { get; init; }

  public String? Tenant { get; init; }
  public Guid? TenantId { get; init; }

}
