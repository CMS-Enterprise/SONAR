using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("service")]
[Index(propertyNames: new[] { nameof(TenantId), nameof(Name) }, IsUnique = true)]
public class Service {
  public Guid Id { get; init; }
  public Guid TenantId { get; init; }

  [StringLength(100)]
  public String Name { get; init; }
  public String DisplayName { get; init; }
  public String? Description { get; init; }

  [StringLength(2048)]
  public Uri? Url { get; init; }
  public Boolean IsRootService { get; init; }

  public Service(
    Guid id,
    Guid tenantId,
    String name,
    String displayName,
    String description,
    Uri url,
    Boolean isRootService) {

    this.Id = id;
    this.TenantId = tenantId;
    this.Name = name;
    this.DisplayName = displayName;
    this.Description = description;
    this.Url = url;
    this.IsRootService = isRootService;
  }
}
