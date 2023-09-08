using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("version_check")]
[Index(nameof(ServiceId), nameof(VersionCheckType), IsUnique = true)]
public class VersionCheck {
  [Key]
  public Guid Id { get; init; }
  public Guid ServiceId { get; set; }
  public VersionCheckType VersionCheckType { get; set; }
  public String Definition { get; set; }

  public VersionCheck(
    Guid id,
    Guid serviceId,
    VersionCheckType versionCheckType,
    String definition) {

    this.Id = id;
    this.ServiceId = serviceId;
    this.VersionCheckType = versionCheckType;
    this.Definition = definition;
  }

  public VersionCheckDefinition DeserializeDefinition() {
    return this.VersionCheckType switch {
      VersionCheckType.FluxKustomization =>
        JsonSerializer.Deserialize<FluxKustomizationVersionCheckDefinition>(
          this.Definition,
          DefinitionSerializerOptions) ??
        throw new InvalidOperationException("Definition deserialized to null."),

      VersionCheckType.HttpResponseBody =>
        JsonSerializer.Deserialize<HttpResponseBodyVersionCheckDefinition>(
          this.Definition,
          DefinitionSerializerOptions) ??
        throw new InvalidOperationException("Definition deserialized to null."),

      _ => throw new NotSupportedException(
        $"Unable to deserialize definition. Unsupported health check type: {this.VersionCheckType}")
    };
  }

  public static String SerializeDefinition(VersionCheckType type, VersionCheckDefinition def) {
    return type switch {
      VersionCheckType.FluxKustomization =>
        JsonSerializer.Serialize(
          (FluxKustomizationVersionCheckDefinition)def,
          DefinitionSerializerOptions),
      VersionCheckType.HttpResponseBody =>
        JsonSerializer.Serialize(
          (HttpResponseBodyVersionCheckDefinition)def,
          DefinitionSerializerOptions),
      _ => throw new ArgumentOutOfRangeException(nameof(type), type, $"Invalid value for {nameof(HealthCheckType)}")
    };
  }

  public static readonly JsonSerializerOptions DefinitionSerializerOptions = new JsonSerializerOptions {
    Converters = { new JsonStringEnumConverter() },
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  public static VersionCheck New(
    Guid serviceId,
    VersionCheckType versionCheckType,
    String definition) =>
    new VersionCheck(Guid.Empty, serviceId, versionCheckType, definition);
}
