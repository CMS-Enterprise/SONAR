using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ResponseTimeCondition : HttpHealthCheckCondition {

  public ResponseTimeCondition(TimeSpan responseTime, HealthStatus status)
    : base(status, HttpHealthCheckConditionType.HttpResponseTime) {

    this.ResponseTime = responseTime;
  }

  [Required]
  public TimeSpan ResponseTime { get; init; }
}
