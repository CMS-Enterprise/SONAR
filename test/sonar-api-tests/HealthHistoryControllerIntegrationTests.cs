using Xunit.Abstractions;
using Xunit;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Tests;

public class HealthHistoryControllerIntegrationTests: ApiControllerTestsBase  {
  private const String TestRootServiceName = "TestRootService";
  private const String TestChildServiceName = "TestChildService";
  private const String TestHealthCheckName = "TestHealthCheck";
  private ITestOutputHelper _output;

  private static readonly HealthCheckModel TestHealthCheck =
    new(
      HealthHistoryControllerIntegrationTests.TestHealthCheckName,
      Description: "Health Check Description",
      HealthCheckType.PrometheusMetric,
      new MetricHealthCheckDefinition(
        TimeSpan.FromMinutes(1),
        Expression: "test_metric",
        ImmutableList.Create(
          new MetricHealthCondition(HealthOperator.GreaterThan, Threshold: 42.0m, HealthStatus.Offline)))
    );

  private static readonly ServiceHierarchyConfiguration TestRootOnlyConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        HealthHistoryControllerIntegrationTests.TestRootServiceName,
        DisplayName: "Display Name", Description: null, Url: null,
        ImmutableList.Create(HealthHistoryControllerIntegrationTests.TestHealthCheck),
        Children: null)
    ),
    ImmutableHashSet<String>.Empty.Add(HealthHistoryControllerIntegrationTests.TestRootServiceName)
  );

  public HealthHistoryControllerIntegrationTests(ApiIntegrationTestFixture fixture, ITestOutputHelper outputHelper) :
    base(fixture, outputHelper)
  {
    this._output = outputHelper;
  }

  [Theory]
  [InlineData("/api/v2/environments")]
  [InlineData("/api/v2/health-history/{testEnvironment}/tenants/{testTenant}")]
  [InlineData($"/api/v2/health-history/{{testEnvironment}}/tenants/{{testTenant}}/services/{HealthHistoryControllerIntegrationTests.TestRootServiceName}")]
  public async Task HealthHistoryURLTest(
    string urlpath) {

    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthHistoryControllerIntegrationTests.TestRootOnlyConfiguration);

    var urlText = urlpath.Replace("{testEnvironment}", testEnvironment).Replace("{testTenant}", testTenant);
    var getResponse = await
      this.Fixture.Server.CreateRequest(urlText)
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    var body = await getResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    Assert.NotNull(body);
  }


 public static IEnumerable<Object[]> TestData() {
    yield return new Object[] {
      -10,
      new HealthStatus[] { HealthStatus.Online, HealthStatus.AtRisk, HealthStatus.Degraded },
      new HealthStatus[] { HealthStatus.Offline, HealthStatus.Unknown }
    };
    yield return new Object[] {
      -30,
      new HealthStatus[] { HealthStatus.Online, HealthStatus.AtRisk, HealthStatus.Degraded, HealthStatus.Offline },
      new HealthStatus[] { HealthStatus.Unknown }
    };
    yield return new Object[] {
      -30,
      new HealthStatus[] { HealthStatus.Online, HealthStatus.Degraded, HealthStatus.Offline },
      new HealthStatus[] { HealthStatus.Unknown, HealthStatus.AtRisk }
    };
  }

  [Theory]
  [MemberData(nameof(TestData))]
  public async Task RootServiceTest(
    Decimal minutes,
    HealthStatus[] RecordedStatus,
    HealthStatus[] NotRecordedStatus
    ) {

    const Int32 numberRecordings = 12;
    const Int32 timeInSecondsBetweenRecordings = 5;

    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthHistoryControllerIntegrationTests.TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow.AddMinutes(-10);
    var start = timestamp;
    var end = timestamp.AddMinutes(10);

    this._output.WriteLine($"UTC   start: {start} end: {end}");
    this._output.WriteLine($"Local start: {start.ToLocalTime()} end: {end.ToLocalTime()}");

    //Record the status in Prometheus
    foreach (var hs in RecordedStatus) {
      for (var i = 0; i < numberRecordings; i++) {
        await this.RecordServiceHealth(
          testEnvironment,
          testTenant,
          HealthHistoryControllerIntegrationTests.TestRootServiceName,
          HealthHistoryControllerIntegrationTests.TestHealthCheckName,
          timestamp,
          hs);
        timestamp = timestamp.AddSeconds(timeInSecondsBetweenRecordings);
      }
    }

    //Query Prometheus via the TestServer and collect the aggregate Status
    var getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/health-history/{testEnvironment}/tenants/{testTenant}?start={start.ToLocalTime()}&end={end.ToLocalTime()}&step=30")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyHealthHistory[]>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    Assert.NotNull(body);
    var serviceHealth = Assert.Single(body);

    if (serviceHealth.AggregateStatus != null) {
      var colStatus = serviceHealth.AggregateStatus.Select(x => x.AggregateStatus);

      var healthStatusEnumerable = colStatus as HealthStatus[] ?? colStatus.ToArray();

      foreach (var hs in RecordedStatus) {
        Assert.Contains(hs, healthStatusEnumerable);
      }
      foreach (var hs in NotRecordedStatus) {
        Assert.DoesNotContain(hs, healthStatusEnumerable);
      }
    }

  }


  [Theory]
  [InlineData(HealthStatus.Unknown)]
  [InlineData(HealthStatus.Online)]
  [InlineData(HealthStatus.AtRisk)]
  [InlineData(HealthStatus.Degraded)]
  [InlineData(HealthStatus.Offline)]
  public async Task AllStatusTypesTest(HealthStatus testStatus) {
    const Int32 numberRecordings = 12;
    const Int32 timeInSecondsBetweenRecordings = 5;

    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthHistoryControllerIntegrationTests.TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow.AddMinutes(-10);
    var start = timestamp;
    var end = timestamp.AddMinutes(10);
    for (var i = 0; i < numberRecordings; i++) {
      await this.RecordServiceHealth(
        testEnvironment,
        testTenant,
        HealthHistoryControllerIntegrationTests.TestRootServiceName,
        HealthHistoryControllerIntegrationTests.TestHealthCheckName,
        timestamp,
        testStatus);
      timestamp = timestamp.AddSeconds(timeInSecondsBetweenRecordings);
    }

    var getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/health-history/{testEnvironment}/tenants/{testTenant}?start={start.ToLocalTime()}&end={end.ToLocalTime()}&step=30")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyHealthHistory[]>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    Assert.NotNull(body);
    var serviceHealth = Assert.Single(body);

    if (serviceHealth.AggregateStatus != null) {
      var colStatus = serviceHealth.AggregateStatus.Select(x => x.AggregateStatus);

      var healthStatusEnumerable = colStatus as HealthStatus[] ?? colStatus.ToArray();
      Assert.Contains(testStatus, healthStatusEnumerable);
      Assert.Equal(HealthHistoryControllerIntegrationTests.TestRootServiceName, serviceHealth.Name);
    }

  }


  private async Task RecordServiceHealth(
    String testEnvironment,
    String testTenant,
    String serviceName,
    String healthCheckName,
    DateTime timestamp,
    HealthStatus status) {

    // Record health status
    var response = await
      this.Fixture.CreateAdminRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}/services/{serviceName}")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHealth(
            timestamp,
            status,
            ImmutableDictionary<String, HealthStatus>.Empty.Add(healthCheckName, status)
          ));
        })
        .PostAsync();

    AssertHelper.Precondition(
      response.IsSuccessStatusCode,
      message: "Failed to record service health"
    );
  }
}
