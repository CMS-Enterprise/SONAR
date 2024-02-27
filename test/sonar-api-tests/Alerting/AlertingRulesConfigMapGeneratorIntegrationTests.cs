using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Alerting;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Tests.Alerting;

public class AlertingRulesConfigMapGeneratorIntegrationTests : ApiControllerTestsBase {
  private const String TestRootServiceName = "TestRootService";
  private const String InvalidRootServiceName = "InvalidRootService";
  private const String TestChildServiceName = "TestChildService";
  private const String TestHealthCheckName = "TestHealthCheck";
  private const String TestAlertName = "TestAlert";
  private const String InvalidTestAlertName = "InvalidAlert";
  private const String TestReceiverName = "TestReceiver";

  private static readonly HealthCheckModel TestHealthCheck =
    new(
      TestHealthCheckName,
      description: "Health Check Description",
      HealthCheckType.PrometheusMetric,
      new MetricHealthCheckDefinition(
        TimeSpan.FromMinutes(1),
        expression: "test_metric",
        ImmutableList.Create(
          new MetricHealthCondition(HealthOperator.GreaterThan, threshold: 42.0m, HealthStatus.Offline))),
      null
    );

  private static readonly ServiceHierarchyConfiguration TestAlertingConfigWithHealthChecksOnChild = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        TestRootServiceName,
        displayName: "Display Name",
        children: ImmutableHashSet<String>.Empty.Add(TestChildServiceName),
        alertingRules: ImmutableArray<AlertingRuleConfiguration>.Empty
          .Add(new AlertingRuleConfiguration(
            TestAlertName,
            HealthStatus.Degraded,
            TestReceiverName,
            delay: 240))),
      new ServiceConfiguration(
        TestChildServiceName,
        displayName: "Display Name",
        healthChecks: ImmutableList.Create(TestHealthCheck)
      )
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    tags: null,
    alerting: new AlertingConfiguration(ImmutableList<AlertReceiverConfiguration>.Empty
      .Add(new AlertReceiverConfiguration(
        name: TestReceiverName,
        AlertReceiverType.Email,
        new AlertReceiverOptionsEmail("user@host.com")
      )))
  );

  private static readonly ServiceHierarchyConfiguration TestAlertingConfigOnChildService = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        TestRootServiceName,
        displayName: "Display Name",
        children: ImmutableHashSet<String>.Empty.Add(TestChildServiceName)),
      new ServiceConfiguration(
        TestChildServiceName,
        displayName: "Display Name",
        healthChecks: ImmutableList.Create(TestHealthCheck),
        alertingRules: ImmutableArray<AlertingRuleConfiguration>.Empty
          .Add(new AlertingRuleConfiguration(
            TestAlertName,
            HealthStatus.Degraded,
            TestReceiverName,
            delay: 240))
      )
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    tags: null,
    alerting: new AlertingConfiguration(ImmutableList<AlertReceiverConfiguration>.Empty
      .Add(new AlertReceiverConfiguration(
        name: TestReceiverName,
        AlertReceiverType.Email,
        new AlertReceiverOptionsEmail("user@host.com")
      )))
  );

  private static readonly ServiceHierarchyConfiguration InvalidAlertingConfigNoHealthChecks = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        TestRootServiceName,
        displayName: "Display Name",
        children: ImmutableHashSet<String>.Empty.Add(TestChildServiceName),
        alertingRules: ImmutableArray<AlertingRuleConfiguration>.Empty
          .Add(new AlertingRuleConfiguration(
            TestAlertName,
            HealthStatus.Degraded,
            TestReceiverName,
            delay: 240))),
      new ServiceConfiguration(
        InvalidRootServiceName,
        displayName: "Other Display Name",
        alertingRules: ImmutableArray<AlertingRuleConfiguration>.Empty
          .Add(new AlertingRuleConfiguration(
            InvalidTestAlertName,
            HealthStatus.Degraded,
            TestReceiverName,
            delay: 240))),
      new ServiceConfiguration(
        TestChildServiceName,
        displayName: "Display Name",
        healthChecks: ImmutableList.Create(TestHealthCheck)
      )
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName).Add(InvalidRootServiceName),
    tags: null,
    alerting: new AlertingConfiguration(ImmutableList<AlertReceiverConfiguration>.Empty
      .Add(new AlertReceiverConfiguration(
        name: TestReceiverName,
        AlertReceiverType.Email,
        new AlertReceiverOptionsEmail("user@host.com")
      )))
  );

  private static ServiceHierarchyConfiguration SimpleAlertingConfig(HealthStatus threshold) =>
    new(
      ImmutableList.Create(
        new ServiceConfiguration(
          TestRootServiceName,
          displayName: "Display Name",
          alertingRules: ImmutableArray<AlertingRuleConfiguration>.Empty
            .Add(new AlertingRuleConfiguration(
              TestAlertName,
              threshold,
              TestReceiverName,
              delay: 240)),
          healthChecks: ImmutableList.Create(TestHealthCheck)
        )
      ),
      ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
      tags: null,
      alerting: new AlertingConfiguration(ImmutableList<AlertReceiverConfiguration>.Empty
        .Add(new AlertReceiverConfiguration(
          name: TestReceiverName,
          AlertReceiverType.Email,
          new AlertReceiverOptionsEmail("user@host.com")
        )))
    );

  public AlertingRulesConfigMapGeneratorIntegrationTests(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper outputHelper) : base(fixture, outputHelper) {
  }

  [Theory]
  [InlineData(HealthStatus.AtRisk)]
  [InlineData(HealthStatus.Degraded)]
  [InlineData(HealthStatus.Offline)]
  [InlineData(HealthStatus.Unknown)]
  public async Task SimpleAlerting_GeneratesValidAlertingConfiguration(HealthStatus threshold) {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(SimpleAlertingConfig(threshold));

    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var generator = services.GetRequiredService<AlertingRulesConfigurationGenerator>();

      var (result, _) = await generator.GenerateAlertingRulesConfiguration(cancellationToken);

      var group = Assert.Single(result.Groups.Where(g => g.Name == $"{testEnvironment}_{testTenant}"));

      var rule = Assert.Single(group.Rules);
      Assert.Equal(4, rule.For.TotalMinutes);
      Assert.Contains("environment", (IReadOnlyDictionary<String, String>)rule.Labels);
      Assert.Contains("tenant", (IReadOnlyDictionary<String, String>)rule.Labels);
      Assert.Contains("service", (IReadOnlyDictionary<String, String>)rule.Labels);
      Assert.Contains("threshold", (IReadOnlyDictionary<String, String>)rule.Labels);

      Assert.Equal(testEnvironment, rule.Labels["environment"]);
      Assert.Equal(testTenant, rule.Labels["tenant"]);
      Assert.Equal(TestRootServiceName, rule.Labels["service"]);
      Assert.Equal(threshold.ToString(), rule.Labels["threshold"]);

      // Without actually running this expression it is hard to test that it is valid, but we can
      // test that it at least contains the expected service name
      Assert.Contains(TestRootServiceName, rule.Expression);
    });
  }

  [Fact]
  public async Task ParentServiceWithChild_ParentHasNoHealthChecks_GeneratesValidAlertingConfiguration() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestAlertingConfigWithHealthChecksOnChild);

    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var generator = services.GetRequiredService<AlertingRulesConfigurationGenerator>();

      var (result, _) = await generator.GenerateAlertingRulesConfiguration(cancellationToken);

      var group = Assert.Single(result.Groups.Where(g => g.Name == $"{testEnvironment}_{testTenant}"));

      var rule = Assert.Single(group.Rules);
      Assert.Equal(4, rule.For.TotalMinutes);
      Assert.Contains("environment", (IReadOnlyDictionary<String, String>)rule.Labels);
      Assert.Contains("tenant", (IReadOnlyDictionary<String, String>)rule.Labels);
      Assert.Contains("service", (IReadOnlyDictionary<String, String>)rule.Labels);
      Assert.Contains("threshold", (IReadOnlyDictionary<String, String>)rule.Labels);

      Assert.Equal(testEnvironment, rule.Labels["environment"]);
      Assert.Equal(testTenant, rule.Labels["tenant"]);
      Assert.Equal(TestRootServiceName, rule.Labels["service"]);
      Assert.Equal(HealthStatus.Degraded.ToString(), rule.Labels["threshold"]);

      Assert.Equal(
        $"http://example/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}",
        rule.Annotations["sonar_dashboard_uri"]
      );

      // Without actually running this expression it is hard to test that it is valid, but we can
      // test that it at least contains the expected service name
      Assert.Contains(TestChildServiceName, rule.Expression);
      // The root service doesn't have any health checks
      Assert.DoesNotContain(TestRootServiceName, rule.Expression);
    });
  }

  [Fact]
  public async Task ParentServiceWithChild_AlertingEnabledOnChild_GeneratesValidAlertingConfiguration() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestAlertingConfigOnChildService);

    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var generator = services.GetRequiredService<AlertingRulesConfigurationGenerator>();

      var (result, _) = await generator.GenerateAlertingRulesConfiguration(cancellationToken);

      var group = Assert.Single(result.Groups.Where(g => g.Name == $"{testEnvironment}_{testTenant}"));

      var rule = Assert.Single(group.Rules);
      Assert.Equal(4, rule.For.TotalMinutes);
      Assert.Contains("environment", (IReadOnlyDictionary<String, String>)rule.Labels);
      Assert.Contains("tenant", (IReadOnlyDictionary<String, String>)rule.Labels);
      Assert.Contains("service", (IReadOnlyDictionary<String, String>)rule.Labels);
      Assert.Contains("threshold", (IReadOnlyDictionary<String, String>)rule.Labels);

      Assert.Equal(testEnvironment, rule.Labels["environment"]);
      Assert.Equal(testTenant, rule.Labels["tenant"]);
      Assert.Equal(TestChildServiceName, rule.Labels["service"]);
      Assert.Equal(HealthStatus.Degraded.ToString(), rule.Labels["threshold"]);

      Assert.Contains("sonar_dashboard_uri", (IReadOnlyDictionary<String, String>)rule.Annotations);

      Assert.Equal(
        $"http://example/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}/{TestChildServiceName}",
        rule.Annotations["sonar_dashboard_uri"]
      );

      // Without actually running this expression it is hard to test that it is valid, but we can
      // test that it at least contains the expected service name
      Assert.Contains(TestChildServiceName, rule.Expression);
      // The root service doesn't have any health checks
      Assert.DoesNotContain(TestRootServiceName, rule.Expression);
    });
  }

  // No Health Checks: Warning
  // A warning should be generated for the invalid service, but the alerting configuration should
  // still be generated successfully
  [Fact]
  public async Task ConfigurationWithInvalidService_NoHealthChecks_GeneratesValidAlertingConfiguration() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(InvalidAlertingConfigNoHealthChecks);

    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var generator = services.GetRequiredService<AlertingRulesConfigurationGenerator>();

      var (result, _) = await generator.GenerateAlertingRulesConfiguration(cancellationToken);

      var group = Assert.Single(result.Groups.Where(g => g.Name == $"{testEnvironment}_{testTenant}"));
      var rule = Assert.Single(group.Rules);
      Assert.NotEqual(InvalidRootServiceName, rule.Labels["service"]);
    });

    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var tenantDataHelper = services.GetRequiredService<TenantDataHelper>();
      var errorReportHelper = services.GetRequiredService<ErrorReportsDataHelper>();

      var tenant =
        await tenantDataHelper.FetchExistingTenantAsync(
          testEnvironment,
          testTenant,
          cancellationToken
        );

      var errorReports = await errorReportHelper.GetFilteredErrorReportDetailsByEnvironment(
        tenant.EnvironmentId,
        tenant.Id,
        InvalidRootServiceName,
        null,
        null,
        null,
        DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(10)),
        DateTime.UtcNow,
        cancellationToken
      );

      var errorReport = Assert.Single(errorReports);
      Assert.Equal(AgentErrorLevel.Warning, errorReport.Level);
      Assert.Equal(AgentErrorType.Validation, errorReport.Type);
    });
  }

  // Invalid Threshold: Warning

  [Theory]
  [InlineData(HealthStatus.Online)]
  [InlineData(HealthStatus.Maintenance)]
  public async Task InvalidStatus_GeneratesErrorReport(HealthStatus threshold) {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(SimpleAlertingConfig(threshold));

    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var generator = services.GetRequiredService<AlertingRulesConfigurationGenerator>();

      var (result, _) = await generator.GenerateAlertingRulesConfiguration(cancellationToken);
      Assert.Empty(result.Groups.Where(g => g.Name == $"{testEnvironment}_{testTenant}"));
    });

    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var tenantDataHelper = services.GetRequiredService<TenantDataHelper>();
      var errorReportHelper = services.GetRequiredService<ErrorReportsDataHelper>();

      var tenant =
        await tenantDataHelper.FetchExistingTenantAsync(
          testEnvironment,
          testTenant,
          cancellationToken
        );

      var errorReports = await errorReportHelper.GetFilteredErrorReportDetailsByEnvironment(
        tenant.EnvironmentId,
        tenant.Id,
        TestRootServiceName,
        null,
        null,
        null,
        DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(10)),
        DateTime.UtcNow,
        cancellationToken
      );

      var errorReport = Assert.Single(errorReports);
      Assert.Equal(AgentErrorLevel.Warning, errorReport.Level);
      Assert.Equal(AgentErrorType.Validation, errorReport.Type);
    });
  }

  private static readonly JsonSerializerOptions TestSerializationOptions =
    new() {
      Converters = { new JsonStringEnumConverter() },
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

  [Fact]
  public void PrometheusAlertingConfiguration_JsonSerializationSanityCheck() {
    var testConfig = new PrometheusAlertingConfiguration(
      ImmutableList<PrometheusAlertingGroup>.Empty
        .Add(new PrometheusAlertingGroup(
          Name: "foo",
          ImmutableList<PrometheusAlertingRule>.Empty
            .Add(new PrometheusAlertingRule(
              "bar",
              "expr",
              TimeSpan.FromSeconds(3666),
              ImmutableDictionary<String, String>.Empty
                .Add("testing", "123"),
              ImmutableDictionary<String, String>.Empty
                .Add("test", "annotation")
            ))
        ))
    );

    var serialized = JsonSerializer.SerializeToDocument(testConfig, TestSerializationOptions);
    dynamic? deserialized = serialized.RootElement.DynamicDeserialize();

    Assert.NotNull(deserialized);
    Assert.NotNull(deserialized!.groups);
    var group = Assert.Single(deserialized.groups);
    Assert.NotNull(group);
    Assert.Equal("foo", group.name);
    Assert.NotNull(group.rules);
    var rule = Assert.Single(group.rules);
    Assert.NotNull(rule);
    Assert.Equal("bar", rule.alert);
    Assert.Equal("expr", rule.expr);
    Assert.Equal("1h1m6s", rule.@for);
    Assert.NotNull(rule.labels.testing);
    Assert.Equal("123", rule.labels.testing);
    Assert.Equal("annotation", rule.annotations.test);
  }
}
