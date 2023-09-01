using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ServiceVersionDetails {

  public ServiceVersionDetails(
    VersionCheckType versionType,
    String version,
    DateTime timestamp) {

    this.VersionType = versionType;
    this.Version = version;
    this.Timestamp = timestamp;
  }

  [Required]
  public VersionCheckType VersionType { get; init; }

  [Required]
  public String Version { get; init; }

  [Required]
  public DateTime Timestamp { get; init; }
}


