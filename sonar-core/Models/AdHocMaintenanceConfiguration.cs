using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;

public record AdHocMaintenanceConfiguration : IValidatableObject {

  public AdHocMaintenanceConfiguration(
    Boolean isEnabled,
    DateTime endTime = default) {

    this.IsEnabled = isEnabled;
    this.EndTime = endTime == default ? DateTime.UtcNow.AddHours(2) : endTime;
  }

  /// <summary>
  /// Whether ad-hoc maintenance is enabled (true) or disabled (false).
  /// </summary>
  [Required]
  public Boolean IsEnabled { get; init; }

  /// <summary>
  /// If <see cref="IsEnabled"/> is true, this is the time the ad-hoc maintenance window ends, ignored otherwise.
  /// Must be more than 10 minutes in the future, and defaults to 2 hours from DateTime.UtcNow if not supplied.
  /// </summary>
  public DateTime EndTime { get; init; }

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
    var validationResults = new List<ValidationResult>();

    // If enabling ad-hoc maintenance, the end time must be more than 10 minutes in the future.
    // Allow a small fudge factor of 1s to account for tiny delay between object creation and validation.
    if (this.IsEnabled) {
      var almost10Minutes = TimeSpan.FromMinutes(10).Subtract(TimeSpan.FromSeconds(1));
      if ((this.EndTime - DateTime.UtcNow) < almost10Minutes) {
        validationResults.Add(new ValidationResult(
          errorMessage: "End time must be more than ten minutes in the future.",
          memberNames: new[] { nameof(this.EndTime) }));
      }
    }

    return validationResults;
  }
}
