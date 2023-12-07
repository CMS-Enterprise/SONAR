using System;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;


public sealed record FluxHelmReleaseVersionCheckDefinition : VersionCheckDefinition {
  public FluxHelmReleaseVersionCheckDefinition(
    String k8sNamespace,
    String helmRelease) {
    this.K8sNamespace = k8sNamespace;
    this.HelmRelease = helmRelease;
  }

  [Required]
  public String K8sNamespace { get; init; }

  [Required]
  public String HelmRelease { get; init; }
};

