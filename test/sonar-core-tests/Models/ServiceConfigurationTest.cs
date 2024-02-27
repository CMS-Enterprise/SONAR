using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Models.Validation;
using Xunit;

namespace Cms.BatCave.Sonar.Tests.Models;

public class ServiceConfigurationTest {

  private readonly RecursivePropertyValidator _validator = new();

  [Fact]
  public void Validate_DuplicateAlertingRuleNames_ReturnsValidationError() {

    var serviceConfiguration =
      new ServiceConfiguration(
        name: "service-1",
        displayName: "service-1",
        alertingRules: ImmutableList.Create(
          new AlertingRuleConfiguration(
            name: "SERVICE-1-ALERT-RULE-1",
            threshold: HealthStatus.Degraded,
            receiverName: "receiver-1"),
          new AlertingRuleConfiguration(
            name: "service-1-alert-rule-1",
            threshold: HealthStatus.Offline,
            receiverName: "receiver-2")
        )
      );

    var validationResults = new List<ValidationResult>();
    var isValid = this._validator.TryValidateObjectProperties(serviceConfiguration, validationResults);

    Assert.False(isValid);
    Assert.NotEmpty(validationResults);
  }

}
