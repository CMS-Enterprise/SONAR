using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record MetricHealthCondition : IValidatableObject {

  public MetricHealthCondition(HealthOperator @operator, Decimal threshold, HealthStatus status) {
    this.Operator = @operator;
    this.Threshold = threshold;
    this.Status = status;
  }

  [Required]
  public HealthOperator Operator { get; init; }

  [Required]
  public Decimal Threshold { get; init; }

  [Required]
  public HealthStatus Status { get; init; }

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
    if (this.Status == HealthStatus.Maintenance) {
      yield return new ValidationResult(
        errorMessage: $"Invalid {nameof(this.Status)}: The {nameof(HealthStatus)} {nameof(HealthStatus.Maintenance)} is reserved and not a valid health check status."
      );
    }
  }
}
