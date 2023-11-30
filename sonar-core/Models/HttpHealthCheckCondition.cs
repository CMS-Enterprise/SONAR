using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Json;

namespace Cms.BatCave.Sonar.Models;

[JsonConverter(typeof(HttpHealthCheckConditionJsonConverter))]
public abstract record HttpHealthCheckCondition : IValidatableObject {

  protected HttpHealthCheckCondition(HealthStatus status, HttpHealthCheckConditionType type) {
    this.Status = status;
    this.Type = type;
  }

  [Required]
  public HealthStatus Status { get; init; }

  [Required]
  public HttpHealthCheckConditionType Type { get; init; }

  public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
    if (this.Status == HealthStatus.Maintenance) {
      yield return new ValidationResult(
        errorMessage: $"Invalid {nameof(this.Status)}: The {nameof(HealthStatus)} {nameof(HealthStatus.Maintenance)} is reserved and not a valid health check status."
      );
    }
  }
}
