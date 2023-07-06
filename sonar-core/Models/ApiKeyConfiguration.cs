using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ApiKeyConfiguration {

  public ApiKeyConfiguration(
    Guid Id,
    String apiKey,
    PermissionType apiKeyType,
    String? environment = null,
    String? tenant = null) {

    this.Id = Id;
    this.ApiKey = apiKey;
    this.ApiKeyType = apiKeyType;
    this.Environment = environment;
    this.Tenant = tenant;
  }
  public Guid Id { get; set; }

  [StringLength(44)] // Base64 encoded String for 32 bytes
  [Required]
  public String ApiKey { get; init; }

  [Required]
  public PermissionType ApiKeyType { get; init; }

  public String? Environment { get; init; }

  public String? Tenant { get; init; }
}
