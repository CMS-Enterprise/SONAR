using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record StatusCodeCondition : HttpHealthCheckCondition {

  public StatusCodeCondition(UInt16[] statusCodes, HealthStatus status)
    : base(status, HttpHealthCheckConditionType.HttpStatusCode) {

    this.StatusCodes = statusCodes;
  }

  [Required]
  public UInt16[] StatusCodes { get; init; }
}
