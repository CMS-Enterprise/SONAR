using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;

public record ServiceHierarchyConfiguration(
  [Required]
  IImmutableList<ServiceConfiguration> Services,
  [Required]
  IImmutableSet<String> RootServices
);
