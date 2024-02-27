using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Models.Validation;
using Xunit;

namespace Cms.BatCave.Sonar.Tests.Models;

public class AlertingConfigurationTest {

  private readonly RecursivePropertyValidator _validator = new();

  [Fact]
  public void Validate_DuplicateAlertReceiverNames_ReturnsValidationError() {

    var alertingConfiguration =
      new AlertingConfiguration(
        receivers: ImmutableList.Create(
          new AlertReceiverConfiguration(
            name: "RECEIVER-1",
            receiverType: AlertReceiverType.Email,
            options: new AlertReceiverOptionsEmail(
              address: "user-1@host-1"
            )
          ),
          new AlertReceiverConfiguration(
            name: "receiver-1",
            receiverType: AlertReceiverType.Email,
            options: new AlertReceiverOptionsEmail(
              address: "user-1@host-1"
            )
          )
        )
      );

    var validationResults = new List<ValidationResult>();
    var isValid = this._validator.TryValidateObjectProperties(alertingConfiguration, validationResults);

    Assert.False(isValid);
    Assert.NotEmpty(validationResults);
  }

}
