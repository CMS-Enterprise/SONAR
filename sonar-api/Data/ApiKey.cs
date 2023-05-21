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
  public Guid Id { get; init; }

  [StringLength(44)] // Base64 encoded String for 32 bytes
  public String Key { get; init; }
  public ApiKeyType Type { get; set; }
  public Guid? EnvironmentId { get; set; }
  public Guid? TenantId { get; set; }

  public ApiKey(
    Guid id,
    String key,
    ApiKeyType type,
    Guid? environmentId,
    Guid? tenantId) {

    this.Id = id;
    this.Key = key;
    this.Type = type;
    this.EnvironmentId = environmentId;
    this.TenantId = tenantId;
  }

  public static ApiKey New(
    Guid id,
    String key,
    ApiKeyType type,
    Guid? environmentId,
    Guid? tenantId) =>
    new ApiKey(id,  key, type, environmentId, tenantId);
}
