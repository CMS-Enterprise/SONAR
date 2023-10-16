using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Models.Validation;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Tests.Models;

public class HttpBodyHealthCheckConditionUnitTest {

  private readonly RecursivePropertyValidator _validator = new();

  [Fact]
  public void Validate_CoversAllConditionTypes() {
    // This is a safety net unit test - it's intended remind us to update HttpBodyHealthCheckCondition.Validate
    // whenever we add new enum values to HttpHealthCheckConditionType. If this unit test fails with an
    // InvalidOperationException mentioning a "Missing case for Type=XYZ validation...", that means a new
    // HttpHealthCheckConditionType enum value was added, but the new value wasn't accounted for in
    // HttpBodyHealthCheckCondition.Validate. To fix this error, add appropriate handling for the new enum value.

    // Iterate over each currently existing HttpHealthCheckConditionType value, construct a HttpBodyHealthCheckCondition
    // using it, and pass the HttpBodyHealthCheckCondition through validation; validation should either succeed without
    // exception, or throw only the expected exception.
    foreach (var type in Enum.GetValues<HttpHealthCheckConditionType>()) {
      var condition = new HttpBodyHealthCheckCondition(HealthStatus.Unknown, type, path: "foo", value: "bar");
      try {
        this._validator.TryValidateObjectProperties(condition, new List<ValidationResult>());
      } catch (InvalidConfigurationException e)
        when (e is { ErrorType: InvalidConfigurationErrorType.IncompatibleHttpHealthCheckConditionType }) {
        // InvalidConfigurationException with InvalidConfigurationErrorType of IncompatibleHttpHealthCheckConditionType
        // is expected for HttpHealthCheckConditionType values that are incompatible with HttpBodyHealthCheckCondition.
        // Any other exception type thrown by Validate is intended to make this unit test fail.
      }
    }
  }

}
