using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Json;

namespace Cms.BatCave.Sonar.Models;

[JsonConverter(typeof(HealthCheckModelJsonConverter))]
public record HealthCheckModel {

  public HealthCheckModel(
    String name,
    String? description,
    HealthCheckType type,
    HealthCheckDefinition definition) {

    this.Name = name;
    this.Description = description;
    this.Type = type;
    this.Definition = definition;
  }

  [StringLength(100)]
  [Required]
  [RegularExpression("^[0-9a-zA-Z_-]+$")]
  public String Name { get; init; }

  public String? Description { get; init; }

  [Required]
  public HealthCheckType Type { get; init; }

  [Required]
  public HealthCheckDefinition Definition { get; init; }
}
