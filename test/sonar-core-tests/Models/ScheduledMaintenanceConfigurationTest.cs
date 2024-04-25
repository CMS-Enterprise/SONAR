using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Models.Validation;
using Xunit;

namespace Cms.BatCave.Sonar.Tests.Models;

public class ScheduledMaintenanceConfigurationTest {

  private readonly RecursivePropertyValidator _validator = new();

  [Theory]
  [InlineData("* 0 * * * *")]
  [InlineData("M-F * * * 0")]
  [InlineData("daily")]
  public void Validate_InvalidCronExpression_ReturnsValidationError(String invalidScheduleExpression) {
    var config = new ScheduledMaintenanceConfiguration(
      scheduleExpression: invalidScheduleExpression,
      durationMinutes: 60);

    var validationResults = new List<ValidationResult>();
    var isValid = this._validator.TryValidateObjectProperties(config, validationResults);

    Assert.False(isValid);
    Assert.NotEmpty(validationResults);
  }

  [Theory]
  [InlineData(-1)]
  [InlineData(0)]
  [InlineData(1441)]
  public void Validate_InvalidDuration_ReturnsValidationError(Int32 invalidDuration) {
    var config = new ScheduledMaintenanceConfiguration(
      scheduleExpression: "* * * * *",
      durationMinutes: invalidDuration);

    var validationResults = new List<ValidationResult>();
    var isValid = this._validator.TryValidateObjectProperties(config, validationResults);

    Assert.False(isValid);
    Assert.NotEmpty(validationResults);
  }

  [Theory]
  [InlineData("America/PoDunkNowhere")]
  [InlineData("Pacific Daylight Time")]
  [InlineData("Mountain Daylight Time")]
  [InlineData("Central Daylight Time")]
  [InlineData("Eastern Daylight Time")]
  [InlineData("US/Left_Coast")]
  public void Validate_InvalidTimeZone_ReturnsValidationError(String invalidTimeZone) {
    var config = new ScheduledMaintenanceConfiguration(
      scheduleExpression: "* * * * *",
      durationMinutes: 60,
      scheduleTimeZone: invalidTimeZone);

    var validationResults = new List<ValidationResult>();
    var isValid = this._validator.TryValidateObjectProperties(config, validationResults);

    Assert.False(isValid);
    Assert.NotEmpty(validationResults);
  }

  [Fact]
  public void NoTimeZoneSpecified_Default_UsEastern() {
    var usEasternTz = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    var config = new ScheduledMaintenanceConfiguration(
      scheduleExpression: "* * * * *",
      durationMinutes: 1);

    Assert.True(usEasternTz.HasSameRules(TimeZoneInfo.FindSystemTimeZoneById(config.ScheduleTimeZone)));
  }

  [Theory]
  [InlineData("Pacific Standard Time")]
  [InlineData("Mountain Standard Time")]
  [InlineData("Central Standard Time")]
  [InlineData("Eastern Standard Time")]
  [InlineData("US/Pacific")]
  [InlineData("US/Mountain")]
  [InlineData("US/Central")]
  [InlineData("US/Eastern")]
  [InlineData("America/Los_Angeles")]
  [InlineData("America/Denver")]
  [InlineData("America/Chicago")]
  [InlineData("America/New_York")]
  public void Validate_StandardTimeZones_NoValidationError(String timeZone) {
    var config = new ScheduledMaintenanceConfiguration(
      scheduleExpression: "* * * * *",
      durationMinutes: 60,
      scheduleTimeZone: timeZone);

    var validationResults = new List<ValidationResult>();
    var isValid = this._validator.TryValidateObjectProperties(config, validationResults);

    Assert.True(isValid);
    Assert.Empty(validationResults);
  }

  [Theory]
  [InlineData("0 0 ? * SUN,SAT", 1440, "US/Eastern")]
  public void Validate_ValidConfiguration_NoValidationError(
    String scheduleExpression,
    Int32 durationMinutes,
    String scheduleTimeZone) {

    var config = new ScheduledMaintenanceConfiguration(
      scheduleExpression,
      durationMinutes,
      scheduleTimeZone);

    var validationResults = new List<ValidationResult>();
    var isValid = this._validator.TryValidateObjectProperties(config, validationResults);

    Assert.True(isValid);
    Assert.Empty(validationResults);
  }
}
