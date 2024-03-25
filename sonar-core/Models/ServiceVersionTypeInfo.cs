using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ServiceVersionTypeInfo {

  public ServiceVersionTypeInfo(
    VersionCheckType versionType,
    String version) {

    this.VersionType = versionType;
    this.Version = version;
  }

  [Required]
  public VersionCheckType VersionType { get; init; }

  [Required]
  public String Version { get; init; }
}
