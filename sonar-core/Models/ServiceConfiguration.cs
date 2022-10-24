using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;

public record ServiceConfiguration(
  [StringLength(100)]
  [Required]
  String Name,
  [Required]
  String DisplayName,
  String? Description,
  Uri? Url,
  IImmutableSet<String>? Children
);
