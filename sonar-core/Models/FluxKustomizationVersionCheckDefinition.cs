using System;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;

public sealed record FluxKustomizationVersionCheckDefinition : VersionCheckDefinition {
  public FluxKustomizationVersionCheckDefinition(
    String k8sNamespace,
    String kustomization) {
    this.K8sNamespace = k8sNamespace;
    this.Kustomization = kustomization;
  }

  [Required]
  public String K8sNamespace { get; init; }

  [Required]
  public String Kustomization { get; init; }
};
