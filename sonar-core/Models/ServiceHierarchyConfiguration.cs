using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;

public record ServiceHierarchyConfiguration(
  [Required]
  IImmutableDictionary<String, ServiceConfiguration> Services,
  [Required]
  IImmutableSet<String> RootServices
);
