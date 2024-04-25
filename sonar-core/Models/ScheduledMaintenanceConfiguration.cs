using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cronos;

namespace Cms.BatCave.Sonar.Models;

public record ScheduledMaintenanceConfiguration : IValidatableObject {

  public ScheduledMaintenanceConfiguration(
    String scheduleExpression,
    Int32 durationMinutes,
    String scheduleTimeZone = "US/Eastern") {

    this.ScheduleExpression = scheduleExpression;
    this.DurationMinutes = durationMinutes;
    this.ScheduleTimeZone = scheduleTimeZone;
  }

  /// <summary>
  /// The Cron expression that describes the start of the window for the maintenance schedule. May be down
  /// to one-minute resolution, no seconds.
  /// </summary>
  [Required]
  public String ScheduleExpression { get; init; }

  /// <summary>
  /// The duration in minutes that the maintenance window lasts each time.
  /// </summary>
  [Required]
  public Int32 DurationMinutes { get; init; }

  /// <summary>
  /// The time zone that <see cref="ScheduleExpression"/> is evaluated in.
  /// Defaults to <c>"US/Eastern"</c> if not supplied.
  /// </summary>
  public String ScheduleTimeZone { get; init; }

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
    var validationResults = new List<ValidationResult>();

    // Schedule expression must be parseable by Cron library.
    try {
      CronExpression.Parse(this.ScheduleExpression);
    } catch (Exception e) {
      validationResults.Add(new ValidationResult(
        errorMessage: e.Message,
        memberNames: new[] { nameof(this.ScheduleExpression) }));
    }

    // Duration must be between 1 minute and 24 hours.
    if (this.DurationMinutes is < 1 or > 24 * 60) {
      validationResults.Add(new ValidationResult(
        errorMessage: "Duration must be between 1 minute and 24 hours (inclusive).",
        memberNames: new[] { nameof(this.DurationMinutes) }));
    }

    // Schedule must be in a valid time zone.
    try {
      TimeZoneInfo.FindSystemTimeZoneById(this.ScheduleTimeZone);
    } catch (Exception e) {
      validationResults.Add(new ValidationResult(
        errorMessage: e.Message,
        memberNames: new[] { nameof(this.ScheduleTimeZone) }));
    }

    return validationResults;
  }
}
