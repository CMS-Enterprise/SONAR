using System;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;

public sealed record FluxKustomizationVersionCheckDefinition : VersionCheckDefinition {
  public FluxKustomizationVersionCheckDefinition(
    String path) {
    this.Path = path;
  }

  [Required]
  public String Path { get; init; }
};
