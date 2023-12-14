using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public sealed record KubernetesVersionCheckDefinition : VersionCheckDefinition {

  public KubernetesVersionCheckDefinition(KubernetesResourceType resourceType, String resourceName, String containerName, String resourceNamespace) {
    this.ResourceType = resourceType;
    this.ResourceName = resourceName;
    this.ContainerName = containerName;
    this.ResourceNamespace = resourceNamespace;
  }

  [Required]
  public KubernetesResourceType ResourceType { get; init; }

  [Required]
  public String ResourceNamespace { get; init; }

  [Required]
  public String ResourceName { get; init; }

  [Required]
  public String ContainerName { get; init; }


}
