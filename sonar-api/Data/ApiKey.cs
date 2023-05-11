using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cms.BatCave.Sonar.Enumeration;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("api_key")]
[Index(nameof(Key))]
public class ApiKey {
  [Key]
  [StringLength(44)] // Base64 encoded String for 32 bytes
  public String Key { get; init; }
  public ApiKeyType Type { get; set; }
  public Guid? EnvironmentId { get; set; }
  public Guid? TenantId { get; set; }

  public ApiKey() {
    this.Key = "";
    this.Type = ApiKeyType.Standard;
    this.EnvironmentId = null;
    this.TenantId = null;
  }

  public ApiKey(
    String key,
    ApiKeyType type,
    Guid? environmentId,
    Guid? tenantId) {

    this.Key = key;
    this.Type = type;
    this.EnvironmentId = environmentId;
    this.TenantId = tenantId;
  }

  public static ApiKey New(
    String key,
    ApiKeyType type,
    Guid? environmentId,
    Guid? tenantId) =>
    new ApiKey(key, type, environmentId, tenantId);
}
