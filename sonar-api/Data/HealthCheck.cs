using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("health_table")]
[Index(propertyNames: new[] { nameof(ServiceId), nameof(Name) }, IsUnique = true)]
public class HealthCheck {
  public Guid Id { get; init; }
  public Guid ServiceId { get; init; }

  [StringLength(100)]
  public String Name { get; init; }
  public String Description { get; init; }
  public String Definition { get; init; }

  public HealthCheck(
    Guid id,
    Guid serviceId,
    String name,
    String description,
    String definition) {

    this.Id = id;
    this.ServiceId = serviceId;
    this.Name = name;
    this.Description = description;
    this.Definition = definition;
  }

  public HealthDefinition DeserializeDefinition() {
    return JsonSerializer.Deserialize<HealthDefinition>(this.Definition)?? throw new InvalidOperationException("Definition deserialized to null.");
  }

  public static String SerializeDefinition(HealthDefinition def) {
    return JsonSerializer.Serialize(def);
  }

  public static HealthCheck New(
    Guid serviceId,
    String name,
    String description,
    String definition) =>
    new HealthCheck(Guid.Empty, serviceId, name, description, definition);
}
