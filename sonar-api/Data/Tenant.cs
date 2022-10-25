using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("tenant")]
[Index(propertyNames: new[] { nameof(EnvironmentId), nameof(Name) }, IsUnique = true)]
public class Tenant {
  public Guid Id { get; init; }
  public Guid EnvironmentId { get; init; }
  public String Name { get; init; }

  public Tenant(
    Guid id,
    Guid environmentId,
    String name) {

    this.Id = id;
    this.EnvironmentId = environmentId;
    this.Name = name;
  }

  public static Tenant New(Guid environmentId, String name) => new Tenant(Guid.Empty, environmentId, name);
}
