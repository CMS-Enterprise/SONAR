using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("tenant_tag")]
[Index(nameof(TenantId), nameof(Name), IsUnique = true)]
public class TenantTag {
  public Guid Id { get; init; }
  public Guid TenantId { get; init; }
  public String Name { get; init; }
  public String? Value { get; init; }

  public TenantTag(
    Guid id,
    Guid tenantId,
    String name,
    String? value) {

    this.Id = id;
    this.TenantId = tenantId;
    this.Name = name;
    this.Value = value;
  }

  public static TenantTag New(
    Guid tenantId,
    String name,
    String? value) =>
    new TenantTag(
      Guid.Empty,
      tenantId,
      name,
      value);
}
