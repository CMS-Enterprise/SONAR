using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ServiceVersion {
  public ServiceVersion(
    DateTime timestamp,
    IReadOnlyDictionary<VersionCheckType, String> versionChecks) {

    this.Timestamp = timestamp;
    this.VersionChecks = versionChecks;
  }

  [Required]
  public DateTime Timestamp { get; init; }

  [Required]
  public IReadOnlyDictionary<VersionCheckType, String> VersionChecks { get; init; }
}
