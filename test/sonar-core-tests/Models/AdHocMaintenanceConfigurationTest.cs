using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Models.Validation;
using Xunit;

namespace Cms.BatCave.Sonar.Tests.Models;

public class AdHocMaintenanceConfigurationTest {

  private readonly RecursivePropertyValidator _validator = new();

  [Theory]
  [InlineData(-1)]
  [InlineData(9)]
  public void Validate_EndTimeNotFarEnoughOut_ReturnsValidationError(Int32 offsetMinutesFromUtcNow) {
    var config = new AdHocMaintenanceConfiguration(
      isEnabled: true,
      endTime: DateTime.UtcNow.Add(TimeSpan.FromMinutes(offsetMinutesFromUtcNow)));

    var validationResults = new List<ValidationResult>();
    var isValid = this._validator.TryValidateObjectProperties(config, validationResults);

    Assert.False(isValid);
    Assert.NotEmpty(validationResults);
  }

  [Fact]
  public void NoEndTimeSpecified_Default_2HoursFromNow() {
    var now = DateTime.UtcNow;

    var config = new AdHocMaintenanceConfiguration(isEnabled: true);

    Assert.Equal(now.AddHours(2), config.EndTime, TimeSpan.FromMilliseconds(1));
  }

  [Fact]
  public void Validate_ValidConfigurationEnabled_NoValidationError() {
    var config = new AdHocMaintenanceConfiguration(
      isEnabled: true,
      endTime: DateTime.UtcNow.AddMinutes(10));

    var validationResults = new List<ValidationResult>();
    var isValid = this._validator.TryValidateObjectProperties(config, validationResults);

    Assert.True(isValid);
    Assert.Empty(validationResults);
  }

  [Fact]
  public void Validate_ValidConfigurationDisabled_NoValidationError() {
    var config = new AdHocMaintenanceConfiguration(isEnabled: false, endTime: DateTime.UtcNow.AddMinutes(-1));

    var validationResults = new List<ValidationResult>();
    var isValid = this._validator.TryValidateObjectProperties(config, validationResults);

    Assert.True(isValid);
    Assert.Empty(validationResults);
  }

}
