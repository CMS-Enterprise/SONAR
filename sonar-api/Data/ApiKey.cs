using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cms.BatCave.Sonar.Enumeration;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("api_key")]
[Index(propertyNames: new[] { nameof(Key) })]
public class ApiKey {
  [Key]
  [StringLength(44)] // Base64 encoded String for 32 bytes
  public String Key { get; init; }
  public ApiKeyType Type { get; set; }
  public Guid? TenantId { get; set; }

  public ApiKey(
    String key,
    ApiKeyType type,
    Guid? tenantId) {

    this.Key = key;
    this.Type = type;
    this.TenantId = tenantId;
  }

  public static ApiKey New(
    String key,
    ApiKeyType type,
    Guid? tenantId) =>
    new ApiKey(key, type, tenantId);
}
