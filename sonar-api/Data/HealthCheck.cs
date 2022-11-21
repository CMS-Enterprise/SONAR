using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("health_check")]
[Index(propertyNames: new[] { nameof(ServiceId), nameof(Name) }, IsUnique = true)]
public class HealthCheck {
  public Guid Id { get; init; }
  public Guid ServiceId { get; init; }

  [StringLength(100)] public String Name { get; init; }
  public String Description { get; init; }
  public HealthCheckType Type { get; init; }
  public String Definition { get; init; }

  public HealthCheck(
    Guid id,
    Guid serviceId,
    String name,
    String description,
    HealthCheckType type,
    String definition) {

    this.Id = id;
    this.ServiceId = serviceId;
    this.Name = name;
    this.Description = description;
    this.Type = type;
    this.Definition = definition;
  }

  public HealthCheckDefinition DeserializeDefinition() {
    return this.Type switch {
      HealthCheckType.PrometheusMetric =>
        JsonSerializer.Deserialize<PrometheusHealthCheckDefinition>(this.Definition) ??
        throw new InvalidOperationException("Definition deserialized to null."),
      _ => throw new NotSupportedException(
        $"Unable to deserialize definition. Unsupported health check type: {this.Type}")
    };
  }

  public static String SerializeDefinition(HealthCheckType type, HealthCheckDefinition def) {
    return type switch {
      HealthCheckType.PrometheusMetric => JsonSerializer.Serialize((PrometheusHealthCheckDefinition)def),
      _ => throw new ArgumentOutOfRangeException(nameof(type), type, $"Invalid value for {nameof(HealthCheckType)}")
    };
  }

  public static HealthCheck New(
    Guid serviceId,
    String name,
    String description,
    HealthCheckType type,
    String definition) =>
    new HealthCheck(Guid.Empty, serviceId, name, description, type, definition);
}
