using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
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
    )
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
    )
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
    ImmutableHashSet<String>.Empty.Add(ExistingServiceName)
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

  private static String SerializeConfigObject(Object config) {
    return JsonSerializer.Serialize(config, JsonServiceConfigSerializer.ConfigSerializerOptions);
  }
}