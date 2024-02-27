using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.ServiceConfig;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;
using Xunit;

namespace Cms.BatCave.Sonar.Agent.Tests;

public class ServiceConfigMergerUnitTest {
  private const String ExistingServiceName = "existingService";
  private const String ExistingHealthCheckOneName = "hc-one";
  private const String ExistingHealthCheckTwoName = "hc-two";
  private const String ExistingChildServiceName = "existingChildService";

  private static readonly HealthCheckModel ExistingHealthCheckOne = new(
    name: ExistingHealthCheckOneName,
    description: "Health Check One",
    HealthCheckType.PrometheusMetric,
    new MetricHealthCheckDefinition(
      TimeSpan.FromSeconds(42),
      expression: "not actually promql",
      ImmutableList.Create(
        new MetricHealthCondition(HealthOperator.Equal, threshold: 9, HealthStatus.Offline)
      )
    ),
    null
  );

  private static readonly HealthCheckModel ExistingHealthCheckTwo = new(
    name: ExistingHealthCheckTwoName,
    description: "Health Check Two",
    HealthCheckType.HttpRequest,
    new HttpHealthCheckDefinition(
      new Uri("http://healthcheck/url"),
      new HttpHealthCheckCondition[] {
        new StatusCodeCondition(new UInt16[] { 200 }, HealthStatus.Online)
      }
    ),
    null
  );

  private static readonly ServiceConfiguration ExistingService = new(
    ExistingServiceName,
    displayName: "Display Name",
    description: "Long Form Description",
    url: new Uri("http://host/path"),
    healthChecks: ImmutableList.Create(ExistingHealthCheckOne, ExistingHealthCheckTwo),
    children: ImmutableHashSet<String>.Empty.Add(ExistingChildServiceName)
  );

  private static readonly ServiceConfiguration ExistingChildService = new(
    ExistingChildServiceName,
    displayName: "Child Display Name",
    healthChecks: ImmutableArray<HealthCheckModel>.Empty
  );

  private static readonly ServiceHierarchyConfiguration BaseConfig = new(
    ImmutableList.Create(
      ExistingService,
      ExistingChildService
    ),
    ImmutableHashSet<String>.Empty.Add(ExistingServiceName),
    null
  );

  // Add new ServiceConfiguration
  [Fact]
  public void MergeConfigs_AddServiceConfiguration() {
    var testName = $"{Guid.NewGuid()}";
    var overlay = JsonServiceConfigSerializer.Deserialize(SerializeConfigObject(new {
      services = new[] {
        new {
          name = testName,
          displayName = "New DisplayName"
        }
      }
    }));

    var result = ServiceConfigMerger.MergeConfigurations(BaseConfig, overlay);

    Assert.Equal(expected: 3, result.Services.Count);
    Assert.Single(result.Services.Where(svc => svc.Name == ExistingServiceName));
    Assert.Single(result.Services.Where(svc => svc.Name == ExistingChildServiceName));
    Assert.Single(result.Services.Where(svc => svc.Name == testName));

    Assert.Equal(new[] { ExistingServiceName }, result.RootServices);
  }

  public static IEnumerable<Object[]> ServiceConfigurationUpdates {
    get {
      yield return new Object[] {
        new {
          name = ExistingServiceName,
          displayName = "An alternate display name"
        },
        ExistingService with { DisplayName = "An alternate display name" }
      };
      yield return new Object[] {
        new {
          name = ExistingServiceName,
          description = "An alternate description"
        },
        ExistingService with { Description = "An alternate description" }
      };
      yield return new Object[] {
        new {
          name = ExistingServiceName,
          url = new Uri("http://alternate-url/")
        },
        ExistingService with { Url = new Uri("http://alternate-url/") }
      };
    }
  }

  // Update ServiceConfiguration
  //    DisplayName
  //    Description
  //    Url
  [Theory]
  [MemberData(nameof(ServiceConfigurationUpdates))]
  public void MergeConfigs_UpdateServiceConfiguration(
    Object overlayService,
    ServiceConfiguration expectedResult) {

    var overlay = JsonServiceConfigSerializer.Deserialize(SerializeConfigObject(new {
      services = new[] { overlayService }
    }));

    var result = ServiceConfigMerger.MergeConfigurations(BaseConfig, overlay);

    Assert.Equal(expected: 2, result.Services.Count);
    var match = Assert.Single(result.Services.Where(svc => svc.Name == ExistingServiceName));
    Assert.Equal(expectedResult, match);
  }

  // Add New HealthCheckModel
  [Fact]
  public void MergeConfigs_AddHealthCheck() {
    var testName = $"{Guid.NewGuid()}";
    var testHealthCheckDef = new MetricHealthCheckDefinition(
      TimeSpan.FromSeconds(37),
      "not actually logql",
      ImmutableList.Create(
        new MetricHealthCondition(
          HealthOperator.GreaterThan,
          threshold: 73,
          HealthStatus.AtRisk
        )
      )
    );

    var overlay = JsonServiceConfigSerializer.Deserialize(SerializeConfigObject(new {
      services = new[] {
        new {
          name = ExistingServiceName,
          healthChecks = new[] {
            new {
              name = testName,
              type = HealthCheckType.LokiMetric,
              definition = testHealthCheckDef
            }
          }
        }
      }
    }));

    var result = ServiceConfigMerger.MergeConfigurations(BaseConfig, overlay);

    Assert.Equal(expected: 2, result.Services.Count);
    var service = Assert.Single(result.Services.Where(svc => svc.Name == ExistingServiceName));

    Assert.NotNull(service.HealthChecks);
    Assert.Equal(expected: 3, service.HealthChecks.Count);

    var healthCheck = Assert.Single(service.HealthChecks.Where(hc => hc.Name == testName));
    Assert.Equal(HealthCheckType.LokiMetric, healthCheck.Type);
    Assert.Equal(testHealthCheckDef, healthCheck.Definition);
  }

  public static IEnumerable<Object[]> HealthCheckUpdates {
    get {
      yield return new Object[] {
        new {
          name = ExistingHealthCheckOneName,
          description = "An alternate description"
        },
        ExistingHealthCheckOne with { Description = "An alternate description" }
      };
      yield return new Object[] {
        new {
          name = ExistingHealthCheckOneName,
          type = HealthCheckType.LokiMetric
        },
        ExistingHealthCheckOne with { Type = HealthCheckType.LokiMetric }
      };

      var newDefinition = new HttpHealthCheckDefinition(
        new Uri("http://whatever/"),
        new HttpHealthCheckCondition[] {
          new ResponseTimeCondition(TimeSpan.FromSeconds(1013), HealthStatus.Degraded)
        }
      );
      yield return new Object[] {
        new {
          name = ExistingHealthCheckOneName,
          type = HealthCheckType.HttpRequest,
          definition = newDefinition
        },
        ExistingHealthCheckOne with {
          Type = HealthCheckType.HttpRequest,
          Definition = newDefinition
        }
      };
    }
  }

  // Update HealthCheckModel
  //    Description
  //    Type (compatible definition)
  //    Type + New Definition
  [Theory]
  [MemberData(nameof(HealthCheckUpdates))]
  public void MergeConfigs_UpdateHealthCheck(
    Object overlayHealthCheck,
    HealthCheckModel expectedResult) {

    var overlay = JsonServiceConfigSerializer.Deserialize(SerializeConfigObject(new {
      services = new[] {
        new {
          name = ExistingServiceName,
          healthChecks = new[] {
            overlayHealthCheck
          }
        }
      }
    }));

    var result = ServiceConfigMerger.MergeConfigurations(BaseConfig, overlay);

    Assert.Equal(expected: 2, result.Services.Count);
    var service = Assert.Single(result.Services.Where(svc => svc.Name == ExistingServiceName));

    Assert.NotNull(service.HealthChecks);
    Assert.Equal(expected: 2, service.HealthChecks.Count);

    var healthCheck =
      Assert.Single(service.HealthChecks.Where(hc => hc.Name == ExistingHealthCheckOneName));
    Assert.Equal(expectedResult, healthCheck);
  }

  // Error: Change HealthCheckModel Type with incompatible definition
  [Fact]
  public void MergeConfigs_IncompatibleHealthCheckType_ThrowsException() {
    var overlay = JsonServiceConfigSerializer.Deserialize(SerializeConfigObject(new {
      services = new[] {
        new {
          name = ExistingServiceName,
          healthChecks = new[] {
            new {
              name = ExistingHealthCheckOneName,
              // This changes the health check type without changing the health check definition to
              // a compatible type.
              type = HealthCheckType.HttpRequest
            }
          }
        }
      }
    }));

    var ex = Assert.Throws<InvalidConfigurationException>(
      () => ServiceConfigMerger.MergeConfigurations(BaseConfig, overlay)
    );

    Assert.Equal(InvalidConfigurationErrorType.IncompatibleHealthCheckType, ex.ErrorType);
  }

  public static IEnumerable<Object[]> HealthCheckDefinitionUpdates {
    get {
      yield return new Object[] {
        ExistingHealthCheckOneName,
        new {
          name = ExistingHealthCheckOneName,
          // You must specify the health check type in the overlay, even if it isn't changing!
          type = HealthCheckType.PrometheusMetric,
          definition = new {
            duration = TimeSpan.FromSeconds(11235),
          }
        },
        ExistingHealthCheckOne with {
          Definition = ((MetricHealthCheckDefinition)ExistingHealthCheckOne.Definition) with {
            Duration = TimeSpan.FromSeconds(11235)
          }
        }
      };
      yield return new Object[] {
        ExistingHealthCheckOneName,
        new {
          name = ExistingHealthCheckOneName,
          // You must specify the health check type in the overlay, even if it isn't changing!
          type = HealthCheckType.PrometheusMetric,
          definition = new {
            expression = "alternate expression"
          }
        },
        ExistingHealthCheckOne with {
          Definition = ((MetricHealthCheckDefinition)ExistingHealthCheckOne.Definition) with {
            Expression = "alternate expression"
          }
        }
      };
      yield return new Object[] {
        ExistingHealthCheckOneName,
        new {
          name = ExistingHealthCheckOneName,
          // You must specify the health check type in the overlay, even if it isn't changing!
          type = HealthCheckType.PrometheusMetric,
          definition = new {
            conditions = new[] {
              new {
                @operator = "LessThan",
                threshold = 1,
                status = "Offline"
              }
            }
          }
        },
        ExistingHealthCheckOne with {
          Definition = ((MetricHealthCheckDefinition)ExistingHealthCheckOne.Definition) with {
            Conditions = ImmutableList.Create(
              new MetricHealthCondition(HealthOperator.LessThan, threshold: 1, HealthStatus.Offline)
            )
          }
        }
      };
    }
  }

  // Update HealthCheckModel.Definition
  // MetricHealthCheckDefinition
  //    Duration
  //    Expression
  //    Conditions (full replacement)
  // HttpHealthCheckDefinition
  //    Url
  //    FollowRedirects
  //    AuthorizationHeader
  //    SkipCertificateValidation
  //    Conditions (full replacement)
  [Theory]
  [MemberData(nameof(HealthCheckDefinitionUpdates))]
  public void MergeConfigs_UpdateHealthCheckDefinition_(
    String healthCheckName,
    Object overlayHealthCheck,
    HealthCheckModel expectedResult) {

    var overlay = JsonServiceConfigSerializer.Deserialize(SerializeConfigObject(new {
      services = new[] {
        new {
          name = ExistingServiceName,
          healthChecks = new[] {
            overlayHealthCheck
          }
        }
      }
    }));

    var result = ServiceConfigMerger.MergeConfigurations(BaseConfig, overlay);

    Assert.Equal(expected: 2, result.Services.Count);
    var service = Assert.Single(result.Services.Where(svc => svc.Name == ExistingServiceName));

    Assert.NotNull(service.HealthChecks);
    Assert.Equal(expected: 2, service.HealthChecks.Count);

    var healthCheck =
      Assert.Single(service.HealthChecks.Where(hc => hc.Name == healthCheckName));
    Assert.Equal(expectedResult, healthCheck);
  }

  // Add ServiceConfiguration.Children (set union)

  [Theory]
  [InlineData("foo")]
  [InlineData(ExistingChildServiceName, "bar")]
  public void MergeConfigs_AddServiceChild_ChildServiceListsUnion(params String[] children) {
    var overlay = JsonServiceConfigSerializer.Deserialize(SerializeConfigObject(new {
      services = new[] {
        new {
          name = ExistingServiceName,
          children
        }
      }
    }));

    var result = ServiceConfigMerger.MergeConfigurations(BaseConfig, overlay);

    Assert.Equal(expected: 2, result.Services.Count);
    var updatedService = Assert.Single(result.Services.Where(svc => svc.Name == ExistingServiceName));

    Assert.NotNull(ExistingService.Children);
    Assert.Equal(ExistingService.Children.Union(children), updatedService.Children);
  }

  // Add RootServices (set union)
  [Theory]
  [InlineData("foo")]
  [InlineData(ExistingServiceName, "bar")]
  public void MergeConfigs_AddRootService_RootServiceListsUnion(params String[] rootServices) {
    var overlay = JsonServiceConfigSerializer.Deserialize(SerializeConfigObject(new {
      rootServices
    }));

    var result = ServiceConfigMerger.MergeConfigurations(BaseConfig, overlay);

    Assert.Equal(expected: 2, result.Services.Count);
    Assert.Equal(BaseConfig.RootServices.Union(rootServices), result.RootServices);
  }

  [Theory]
  [InlineData(
    "test-inputs/service-config-merging/version-check-merging/version-checks-base-0.json",
    "test-inputs/service-config-merging/version-check-merging/version-checks-overlay-0.json")]
  [InlineData(
    "test-inputs/service-config-merging/version-check-merging/version-checks-base-0.json",
    "test-inputs/service-config-merging/version-check-merging/version-checks-overlay-1.json")]
  [InlineData(
    "test-inputs/service-config-merging/version-check-merging/version-checks-base-0.json",
    "test-inputs/service-config-merging/version-check-merging/version-checks-overlay-0.json",
    "test-inputs/service-config-merging/version-check-merging/version-checks-overlay-1.json")]
  public async Task MergeConfigurations_AddsNewVersionChecks(String baseFile, params String[] overlayFiles) {
    var baseConfig = await LoadConfigFromFileAsync(baseFile);
    var overlayConfigs = overlayFiles.Select(LoadConfigFromFileAsync).Select(t => t.Result).ToArray();

    Assert.Null(baseConfig.Services.Single().VersionChecks);
    foreach (var overlayConfig in overlayConfigs) {
      Assert.Single(overlayConfig.Services.Single().VersionChecks!);
    }

    var mergedConfigs = overlayConfigs.Aggregate(baseConfig, ServiceConfigMerger.MergeConfigurations);

    Assert.NotEmpty(mergedConfigs.Services.Single().VersionChecks!);
    foreach (var overlayConfig in overlayConfigs) {
      Assert.Contains(
        overlayConfig.Services.Single().VersionChecks!.Single(),
        mergedConfigs.Services.Single().VersionChecks!);
    }
  }

  [Theory]
  [InlineData(
    "test-inputs/service-config-merging/version-check-merging/version-checks-base-1.json",
    "test-inputs/service-config-merging/version-check-merging/version-checks-overlay-0.json",
    "test-inputs/service-config-merging/version-check-merging/version-checks-overlay-1.json")]
  public async Task MergedConfigurations_ReplacesExistingVersionChecks(String baseFile, params String[] overlayFiles) {
    var baseConfig = await LoadConfigFromFileAsync(baseFile);
    var overlayConfigs = overlayFiles.Select(LoadConfigFromFileAsync).Select(t => t.Result).ToArray();

    Assert.Equal(expected: 2, baseConfig.Services.Single().VersionChecks!.Count);
    foreach (var overlayConfig in overlayConfigs) {
      Assert.DoesNotContain(
        overlayConfig.Services.Single().VersionChecks!.Single(),
        baseConfig.Services.Single().VersionChecks!);
    }

    var mergedConfigs = overlayConfigs.Aggregate(baseConfig, ServiceConfigMerger.MergeConfigurations);

    foreach (var baseVersionCheck in baseConfig.Services.Single().VersionChecks!) {
      Assert.DoesNotContain(
        baseVersionCheck,
        mergedConfigs.Services.Single().VersionChecks!);
    }

    foreach (var overlayConfig in overlayConfigs) {
      Assert.Contains(
        overlayConfig.Services.Single().VersionChecks!.Single(),
        mergedConfigs.Services.Single().VersionChecks!);
    }
  }

  [Fact]
  public void MergeConfigurations_AlertingRules() {

    // Base config specifies two services, service-1 and service-2
    // service-1 has two alerts, service-1-alert-1 and service-1-alert-2
    // service-2 has no alerts
    var baseConfig =
      new ServiceHierarchyConfiguration(
        services: ImmutableList.Create(
          new ServiceConfiguration(
            name: "service-1",
            displayName: "service-1",
            alertingRules: ImmutableList.Create(
              new AlertingRuleConfiguration(
                name: "service-1-alert-1",
                threshold: HealthStatus.AtRisk,
                receiverName: "receiver-1"),
              new AlertingRuleConfiguration(
                name: "service-1-alert-2",
                threshold: HealthStatus.Offline,
                receiverName: "receiver-1"))),
          new ServiceConfiguration(
            name: "service-2",
            displayName: "service-2")),
        rootServices: ImmutableHashSet.Create(
          "service-1",
          "service-2"),
        alerting: new AlertingConfiguration(
          receivers: ImmutableList.Create(
            new AlertReceiverConfiguration(
              name: "receiver-1",
              receiverType: AlertReceiverType.Email,
              options: new AlertReceiverOptionsEmail(
                address: "user-1@host-1")))));

    // Overlay config has the same services defined as base config
    // service-1-alert-2 is modified in service-1 (the other one is left alone)
    // service-2-alert-1 is added to service-2
    var overlayConfig =
      new ServiceHierarchyConfiguration(
        services: ImmutableList.Create(
          new ServiceConfiguration(
            name: "service-1",
            displayName: "service-1",
            alertingRules: ImmutableList.Create(
              new AlertingRuleConfiguration(
                name: "service-1-alert-2",
                threshold: HealthStatus.Degraded,
                receiverName: "receiver-1"))),
          new ServiceConfiguration(
            name: "service-2",
            displayName: "service-2",
            alertingRules: ImmutableList.Create(
              new AlertingRuleConfiguration(
                name: "service-2-alert-1",
                threshold: HealthStatus.Degraded,
                receiverName: "receiver-1")))),
        rootServices: ImmutableHashSet.Create(
          "service-1",
          "service-2"),
        alerting: new AlertingConfiguration(
          receivers: ImmutableList.Create(
            new AlertReceiverConfiguration(
              name: "receiver-1",
              receiverType: AlertReceiverType.Email,
              options: new AlertReceiverOptionsEmail(
                address: "user-1@host-1")))));

    var mergedConfig = ServiceConfigMerger.MergeConfigurations(baseConfig, overlayConfig);

    AlertingRuleConfiguration GetAlertingRuleByName(ServiceHierarchyConfiguration config, String alertingRuleName) {
      return config.Services
        .SelectMany(s => s.AlertingRules ?? ImmutableList<AlertingRuleConfiguration>.Empty)
        .First(r => r.Name == alertingRuleName);
    }

    // service-1-alert-1 was not modified in the overlay config,
    // so merged config should have the same value as base config
    Assert.Equal(
      GetAlertingRuleByName(baseConfig, "service-1-alert-1"),
      GetAlertingRuleByName(mergedConfig, "service-1-alert-1"));

    // service-1-alert-2 was modified in the overlay config,
    // so merged config should have the same value as overlay config
    Assert.Equal(
      GetAlertingRuleByName(overlayConfig, "service-1-alert-2"),
      GetAlertingRuleByName(mergedConfig, "service-1-alert-2"));

    // service-2-alert-1 was added by the overlay config,
    // so merged config should have the same value as overlay config
    Assert.Equal(
      GetAlertingRuleByName(overlayConfig, "service-2-alert-1"),
      GetAlertingRuleByName(mergedConfig, "service-2-alert-1"));
  }

  private static String SerializeConfigObject(Object config) {
    return JsonSerializer.Serialize(config, JsonServiceConfigSerializer.ConfigSerializerOptions);
  }

  private static async Task<ServiceHierarchyConfiguration> LoadConfigFromFileAsync(String serviceConfigFilePath) {
    var configSource = new LocalFileServiceConfigSource(tenant: "test", filePaths: new[] { serviceConfigFilePath });
    var config = await configSource.GetConfigurationLayersAsync(tenant: "test", cancellationToken: default)
      .SingleAsync();

    try {
      ServiceConfigValidator.ValidateServiceConfig(config);
    } catch (InvalidConfigurationException e) {
      if (e.Data?["errors"] is List<ValidationResult> errors) {
        throw new InvalidConfigurationException(
          e.Message + "\nValidation errors:\n" + String.Join(separator: '\n', errors.Select(error => error.ToString())),
          InvalidConfigurationErrorType.DataValidationError);
      }
      throw;
    }

    return config;
  }
}
