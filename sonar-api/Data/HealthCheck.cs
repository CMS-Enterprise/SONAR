using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("health_check")]
[Index(nameof(ServiceId), nameof(Name), IsUnique = true)]
public class HealthCheck {
  public Guid Id { get; init; }
  public Guid ServiceId { get; init; }

  [StringLength(100)] public String Name { get; init; }
  public String? Description { get; init; }
  public HealthCheckType Type { get; init; }
  public String Definition { get; init; }
  public Int16? SmoothingTolerance { get; init; }

  public HealthCheck(
    Guid id,
    Guid serviceId,
    String name,
    String? description,
    HealthCheckType type,
    String definition,
    Int16? smoothingTolerance) {

    this.Id = id;
    this.ServiceId = serviceId;
    this.Name = name;
    this.Description = description;
    this.Type = type;
    this.Definition = definition;
    this.SmoothingTolerance = smoothingTolerance;
  }

  public HealthCheckDefinition DeserializeDefinition() {
    return this.Type switch {
      HealthCheckType.PrometheusMetric =>
        JsonSerializer.Deserialize<MetricHealthCheckDefinition>(this.Definition, DefinitionSerializerOptions) ??
        throw new InvalidOperationException("Definition deserialized to null."),
      HealthCheckType.LokiMetric =>
        JsonSerializer.Deserialize<MetricHealthCheckDefinition>(this.Definition, DefinitionSerializerOptions) ??
        throw new InvalidOperationException("Definition deserialized to null."),
      HealthCheckType.HttpRequest =>
        JsonSerializer.Deserialize<HttpHealthCheckDefinition>(this.Definition, DefinitionSerializerOptions) ??
        throw new InvalidOperationException("Definition deserialized to null."),
      HealthCheckType.Internal =>
        JsonSerializer.Deserialize<HttpHealthCheckDefinition>(this.Definition, DefinitionSerializerOptions) ??
        throw new InvalidOperationException("Definition deserialized to null."),

      _ => throw new NotSupportedException(
        $"Unable to deserialize definition. Unsupported health check type: {this.Type}")
    };
  }

  public static String SerializeDefinition(HealthCheckType type, HealthCheckDefinition def) {
    return type switch {
      HealthCheckType.PrometheusMetric => JsonSerializer.Serialize((MetricHealthCheckDefinition)def, DefinitionSerializerOptions),
      HealthCheckType.LokiMetric => JsonSerializer.Serialize((MetricHealthCheckDefinition)def, DefinitionSerializerOptions),
      HealthCheckType.HttpRequest => JsonSerializer.Serialize((HttpHealthCheckDefinition)def, DefinitionSerializerOptions),
      HealthCheckType.Internal => JsonSerializer.Serialize((HttpHealthCheckDefinition)def, DefinitionSerializerOptions),

      _ => throw new ArgumentOutOfRangeException(nameof(type), type, $"Invalid value for {nameof(HealthCheckType)}")
    };
  }

  public static readonly JsonSerializerOptions DefinitionSerializerOptions = new JsonSerializerOptions {
    Converters = { new JsonStringEnumConverter() },
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  public static HealthCheck New(
    Guid serviceId,
    String name,
    String description,
    HealthCheckType type,
    String definition,
    Int16 smoothingTolerance) =>
    new HealthCheck(Guid.Empty, serviceId, name, description, type, definition, smoothingTolerance);
}
