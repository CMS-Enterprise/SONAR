using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Json;

namespace Cms.BatCave.Sonar.Models;

[JsonConverter(typeof(VersionCheckModelJsonConverter))]
public record VersionCheckModel {
  public VersionCheckModel(
    VersionCheckType versionCheckType,
    VersionCheckDefinition definition) {
    this.VersionCheckType = versionCheckType;
    this.Definition = definition;
  }

  [Required]
  public VersionCheckType VersionCheckType { get; init; }

  [Required]
  public VersionCheckDefinition Definition { get; init; }
}
