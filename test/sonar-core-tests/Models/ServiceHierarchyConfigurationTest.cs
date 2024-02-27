using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Models.Validation;
using Xunit;

namespace Cms.BatCave.Sonar.Tests.Models;

public class ServiceHierarchyConfigurationTest {

  private readonly RecursivePropertyValidator _validator = new();

  [Fact]
  public void Validate_ServiceAlertingRulesReferenceValidAlertReceivers_ValidationSucceeds() {

    var serviceHierarchyConfiguration =
      new ServiceHierarchyConfiguration(
        services: ImmutableList.Create(
          new ServiceConfiguration(
            name: "service-1",
            displayName: "service-1",
            alertingRules: ImmutableList.Create(
              new AlertingRuleConfiguration(
                name: "service-1-alert-rule-1",
                threshold: HealthStatus.Degraded,
                receiverName: "RECEIVER-1")
            )
          ),
          new ServiceConfiguration(
            name: "service-2",
            displayName: "service-2",
            alertingRules: ImmutableList.Create(
              new AlertingRuleConfiguration(
                name: "service-2-alert-rule-1",
                threshold: HealthStatus.Degraded,
                receiverName: "receiver-1"),
              new AlertingRuleConfiguration(
                name: "service-2-alert-rule-2",
                threshold: HealthStatus.Offline,
                receiverName: "rEcEiVeR-2")
            )
          )
        ),
        rootServices: ImmutableHashSet.Create(
          "service-1"
        ),
        alerting: new AlertingConfiguration(
          receivers: ImmutableList.Create(
            new AlertReceiverConfiguration(
              name: "receiver-1",
              receiverType: AlertReceiverType.Email,
              options: new AlertReceiverOptionsEmail(
                address: "user-1@host-1"
              )
            ),
            new AlertReceiverConfiguration(
              name: "RECEIVER-2",
              receiverType: AlertReceiverType.Email,
              options: new AlertReceiverOptionsEmail(
                address: "user-2@host-1"
              )
            )
          )
        )
      );

    var validationResults = new List<ValidationResult>();
    var isValid = this._validator.TryValidateObjectProperties(serviceHierarchyConfiguration, validationResults);

    Assert.True(isValid);
    Assert.Empty(validationResults);
  }

  [Theory]
  [MemberData(nameof(ServiceAlertRuleReferencesInvalidAlertReceiverData))]
  public void Validate_ServiceAlertRuleReferencesInvalidAlertReceiver_ReturnsValidationError(
    ServiceHierarchyConfiguration serviceHierarchyConfiguration) {

    var validationResults = new List<ValidationResult>();
    var isValid = this._validator.TryValidateObjectProperties(serviceHierarchyConfiguration, validationResults);

    Assert.False(isValid);
    Assert.NotEmpty(validationResults);
  }

  public static IEnumerable<Object[]>
    ServiceAlertRuleReferencesInvalidAlertReceiverData =>
      new List<Object[]> {

        // Test case: Referenced alert receiver name is "receiver-2", but defined alert receiver name is "receiver-1".
        new Object[] {
          new ServiceHierarchyConfiguration(
            services: ImmutableList.Create(
              new ServiceConfiguration(
                name: "service-1",
                displayName: "service-1",
                alertingRules: ImmutableList.Create(
                  new AlertingRuleConfiguration(
                    name: "service-1-alert-rule-1",
                    threshold: HealthStatus.Degraded,
                    receiverName: "receiver-2")
                )
              )
            ),
            rootServices: ImmutableHashSet.Create(
              "service-1"
            ),
            alerting: new AlertingConfiguration(
              receivers: ImmutableList.Create(
                new AlertReceiverConfiguration(
                  name: "receiver-1",
                  receiverType: AlertReceiverType.Email,
                  options: new AlertReceiverOptionsEmail(
                    address: "user-1@host-1"
                  )
                )
              )
            )
          )
        },

        // Test case: Referenced alert receiver name is "receiver-1", but no alert receivers are defined.
        new Object[] {
          new ServiceHierarchyConfiguration(
            services: ImmutableList.Create(
              new ServiceConfiguration(
                name: "service-1",
                displayName: "service-1",
                alertingRules: ImmutableList.Create(
                  new AlertingRuleConfiguration(
                    name: "service-1-alert-rule-1",
                    threshold: HealthStatus.Degraded,
                    receiverName: "receiver-1")
                )
              )
            ),
            rootServices: ImmutableHashSet.Create(
              "service-1"
            )
          )
        }
      };

}
