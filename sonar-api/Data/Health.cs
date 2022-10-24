using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("health")]
[Index(propertyNames: new[] { nameof(ServiceId), nameof(Name) }, IsUnique = true)]
public class Health
{
  public Guid Id { get; init; }

  public Guid ServiceId { get; init; }

  [StringLength(100)]
  public String Name { get; init; }
  public String Description { get; init; }
  public HealthDefinition Definition { get; init; }

  public Health(
    Guid id,
    Guid serviceId,
    String name,
    String description,
    HealthDefinition definition)
  {

    this.Id = id;
    this.ServiceId = serviceId;
    this.Name = name;
    this.Description = description;
    this.Definition = definition;
  }
}
