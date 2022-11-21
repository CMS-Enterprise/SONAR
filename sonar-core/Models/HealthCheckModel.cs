using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Json;

namespace Cms.BatCave.Sonar.Models;

[JsonConverter(typeof(HealthCheckModelJsonConverter))]
public record HealthCheckModel(
  [StringLength(100)]
  [Required]
  [RegularExpression("^[0-9a-zA-Z_-]+$")]
  String Name,
  String Description,
  HealthCheckType Type,
  HealthCheckDefinition Definition
);
