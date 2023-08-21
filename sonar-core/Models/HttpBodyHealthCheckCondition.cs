using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record HttpBodyHealthCheckCondition : HttpHealthCheckCondition {

  public HttpBodyHealthCheckCondition(HealthStatus status, HttpHealthCheckConditionType type, String path, String value)
    : base(status, type) {
    this.Path = path;
    this.Value = value;
    this.Type = type;
  }

  [Required]
  public String Path { get; init; }

  [Required]
  public String Value { get; init; }

}

