using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record MetricHealthCondition {

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
}
