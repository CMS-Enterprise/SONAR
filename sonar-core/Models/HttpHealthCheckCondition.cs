using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Json;

namespace Cms.BatCave.Sonar.Models;

[JsonConverter(typeof(HttpHealthCheckConditionJsonConverter))]
public abstract record HttpHealthCheckCondition {

  protected HttpHealthCheckCondition(HealthStatus status, HttpHealthCheckConditionType type) {
    this.Status = status;
    this.Type = type;
  }

  [Required]
  public HealthStatus Status { get; init; }

  [Required]
  public HttpHealthCheckConditionType Type { get; init; }
}
