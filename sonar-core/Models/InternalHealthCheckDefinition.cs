using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public sealed record InternalHealthCheckDefinition : HealthCheckDefinition {

  public InternalHealthCheckDefinition(
    String description,
    String? expression) {
    this.Description = description;
    this.Expression = expression;
  }

  [Required]
  public String Description { get; init; }
  public String? Expression { get; init; }

  public Boolean Equals(InternalHealthCheckDefinition? other) {
    return other is not null &&
      String.Equals(this.Description, other.Description) &&
      String.Equals(this.Expression, other.Expression);
  }

  public override Int32 GetHashCode() {
    return HashCode.Combine(
      this.Expression,
      HashCodes.From(this.Description));
  }
}
