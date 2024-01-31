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
using DateTime = System.DateTime;

namespace Cms.BatCave.Sonar.Tests;

public class HealthHistoryControllerIntegrationTests : ApiControllerTestsBase {
  private const String RootServiceName = "TestRootService";
  private const String ChildServiceName = "TestChildService";
  private const String HealthCheckName = "TestHealthCheck";
  private ITestOutputHelper _output;

  //Each status in the array (parentStatus, childStatus) will be recorded 6 times in one minute.
  //The time in between each recording is 10 seconds
  private const Int32 NumberRecordings = 6;
  private const Int32 TimeInSecondsBetweenRecordings = 10;
  //When querying prometheus the step will be a 30 second step.
  //Therefore, there will be 2 retrieved recordings for each minute
  private const Int32 Step = 30;


  private static readonly HealthCheckModel TestHealthCheck =
    new(
      HealthHistoryControllerIntegrationTests.HealthCheckName,
      description: "Health Check Description",
      HealthCheckType.PrometheusMetric,
      new MetricHealthCheckDefinition(
        TimeSpan.FromMinutes(1),
        expression: "test_metric",
        ImmutableList.Create(
          new MetricHealthCondition(HealthOperator.GreaterThan, threshold: 42.0m, HealthStatus.Offline))),
      null
    );

  private static readonly VersionCheckModel TestVersionCheck =
    new(
      VersionCheckType.FluxKustomization,
      new FluxKustomizationVersionCheckDefinition(k8sNamespace: "test", kustomization: "test")
    );

  private static readonly ServiceHierarchyConfiguration TestRootOnlyConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        HealthHistoryControllerIntegrationTests.RootServiceName,
        displayName: "Display Name", description: null, url: null,
        ImmutableList.Create(HealthHistoryControllerIntegrationTests.TestHealthCheck),
        children: null)
    ),
    ImmutableHashSet<String>.Empty.Add(HealthHistoryControllerIntegrationTests.RootServiceName),
    null
  );

  private static readonly ServiceHierarchyConfiguration TestRootChildConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        HealthHistoryControllerIntegrationTests.RootServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        ImmutableList.Create(HealthHistoryControllerIntegrationTests.TestHealthCheck),
        ImmutableList.Create(TestVersionCheck),
        ImmutableHashSet<String>.Empty.Add(HealthHistoryControllerIntegrationTests.ChildServiceName)),
      new ServiceConfiguration(
        HealthHistoryControllerIntegrationTests.ChildServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(HealthHistoryControllerIntegrationTests.TestHealthCheck),
        children: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(HealthHistoryControllerIntegrationTests.RootServiceName),
    null
  );

  public HealthHistoryControllerIntegrationTests(ApiIntegrationTestFixture fixture, ITestOutputHelper outputHelper) :
    base(fixture, outputHelper) {
    this._output = outputHelper;
  }

  [Theory]
  [InlineData("/api/v2/environments")]
  [InlineData("/api/v2/health-history/{testEnvironment}/tenants/{testTenant}")]
  [InlineData($"/api/v2/health-history/{{testEnvironment}}/tenants/{{testTenant}}/services/{HealthHistoryControllerIntegrationTests.RootServiceName}")]
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


  [Theory]
  [InlineData(HealthStatus.Unknown)]
  [InlineData(HealthStatus.Online)]
  [InlineData(HealthStatus.AtRisk)]
  [InlineData(HealthStatus.Degraded)]
  [InlineData(HealthStatus.Offline)]
  public async Task AllStatusTypesTest(HealthStatus testStatus) {

    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthHistoryControllerIntegrationTests.TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow.AddMinutes(-10);
    var start = timestamp;
    var end = timestamp.AddMinutes(10);
    for (var i = 0; i < NumberRecordings; i++) {
      await this.RecordServiceHealth(
        testEnvironment,
        testTenant,
        HealthHistoryControllerIntegrationTests.RootServiceName,
        HealthHistoryControllerIntegrationTests.HealthCheckName,
        timestamp,
        testStatus);
      timestamp = timestamp.AddSeconds(TimeInSecondsBetweenRecordings);
    }

    var getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/health-history/{testEnvironment}/tenants/{testTenant}?start={GetUniversalTimeString(start)}&end={GetUniversalTimeString(end)}&step={Step}")
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
    Assert.NotNull(serviceHealth.AggregateStatus);
    var colStatus = serviceHealth.AggregateStatus.Select(x => x.AggregateStatus);
    var healthStatusEnumerable = colStatus as HealthStatus[] ?? colStatus.ToArray();
    Assert.Contains(testStatus, healthStatusEnumerable);
    Assert.Equal(HealthHistoryControllerIntegrationTests.RootServiceName, serviceHealth.Name);
  }

  [Theory]
  [MemberData(nameof(HealthStatusData2))]
  public async Task RootStatusAggregation(
    HealthStatus[] parentStatus,
    HealthStatus[] expectedStatus
  ) {
    var end = DateTime.UtcNow;
    var start = DateTime.UtcNow.AddMinutes(-expectedStatus.Length);
    var timestamp = start;

    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthHistoryControllerIntegrationTests.TestRootOnlyConfiguration);

    //Record the status in Prometheus
    foreach (var hs in parentStatus) {
      for (var i = 0; i < NumberRecordings; i++) {
        await this.RecordServiceHealth(
          testEnvironment,
          testTenant,
          HealthHistoryControllerIntegrationTests.RootServiceName,
          HealthHistoryControllerIntegrationTests.HealthCheckName,
          timestamp,
          hs);
        timestamp = timestamp.AddSeconds(TimeInSecondsBetweenRecordings);
      }
    }

    var getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/health-history/{testEnvironment}/tenants/{testTenant}?start={GetUniversalTimeString(start)}&end={GetUniversalTimeString(end)}&step={Step}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyHealthHistory[]>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);

    // The parent and child service should have the recorded status
    var serviceHealth = Assert.Single(body);
    Assert.NotNull(serviceHealth);

    Assert.NotNull(serviceHealth.AggregateStatus);
    //Expecting 2 items for each minute(30 second step)
    Assert.Equal(serviceHealth.AggregateStatus.Count, expectedStatus.Length * 2);

    var index = 0;
    foreach (var hs in expectedStatus) {
      //should have 2 items for each minute (30 second step).
      Assert.Equal(hs, serviceHealth.AggregateStatus[index].AggregateStatus);
      Assert.Equal(hs, serviceHealth.AggregateStatus[index + 1].AggregateStatus);
      index += 2;
    }
  }

  [Theory]
  [MemberData(nameof(HealthStatusData))]
  public async Task RootChildStatusAggregation(
  HealthStatus[] parentStatus,
  HealthStatus[] childStatus,
  HealthStatus[] expectedStatus
  ) {
    var end = DateTime.UtcNow;
    var start = DateTime.UtcNow.AddMinutes(-expectedStatus.Length);
    var timestamp = start;

    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthHistoryControllerIntegrationTests.TestRootChildConfiguration);

    //Record the status in Prometheus
    foreach (var hs in parentStatus) {
      for (var i = 0; i < NumberRecordings; i++) {
        await this.RecordServiceHealth(
          testEnvironment,
          testTenant,
          HealthHistoryControllerIntegrationTests.RootServiceName,
          HealthHistoryControllerIntegrationTests.HealthCheckName,
          timestamp,
          hs);
        timestamp = timestamp.AddSeconds(TimeInSecondsBetweenRecordings);
      }
    }

    timestamp = start;
    foreach (var hs in childStatus) {
      for (var i = 0; i < NumberRecordings; i++) {
        await this.RecordServiceHealth(
          testEnvironment,
          testTenant,
          HealthHistoryControllerIntegrationTests.ChildServiceName,
          HealthHistoryControllerIntegrationTests.HealthCheckName,
          timestamp,
          hs);
        timestamp = timestamp.AddSeconds(TimeInSecondsBetweenRecordings);
      }
    }

    var getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/health-history/{testEnvironment}/tenants/{testTenant}?start={GetUniversalTimeString(start)}&end={GetUniversalTimeString(end)}&step={Step}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyHealthHistory[]>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);

    // The parent and child service should have the recorded status
    var serviceHealth = Assert.Single(body);
    Assert.NotNull(serviceHealth);
    Assert.NotNull(serviceHealth.Children);
    var childService = Assert.Single(serviceHealth.Children);

    if (serviceHealth.AggregateStatus != null) {
      //Expecting 2 items for each minute(30 second step)
      Assert.Equal(serviceHealth.AggregateStatus.Count, expectedStatus.Length * 2);

      var index = 0;
      foreach (var hs in expectedStatus) {
        //should have 2 items for each minute (30 second step).
        Assert.Equal(hs, serviceHealth.AggregateStatus[index].AggregateStatus);
        Assert.Equal(hs, serviceHealth.AggregateStatus[index + 1].AggregateStatus);
        index += 2;
      }

    } else {
      //If no parent service metrics, check the child metrics
      Assert.NotNull(childService.AggregateStatus);
      //Expecting 2 items for each minute(30 second step)
      Assert.Equal(childService.AggregateStatus.Count, expectedStatus.Length * 2);

      var index = 0;
      foreach (var hs in expectedStatus) {
        //should have 2 items for each minute (30 second step).
        Assert.Equal(hs, childService.AggregateStatus[index].AggregateStatus);
        Assert.Equal(hs, childService.AggregateStatus[index + 1].AggregateStatus);
        index += 2;
      }
    }
  }

  [Fact]
  public async Task HistoricalHealthCheckTest() {

    var testStatus = HealthStatus.Offline;
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthHistoryControllerIntegrationTests.TestRootOnlyConfiguration);
    var sampleTimestamp = DateTime.UtcNow.AddMinutes(-10);
    var queryTimestamp = sampleTimestamp.AddMinutes(1);

    await this.RecordServiceHealth(
      testEnvironment,
      testTenant,
      HealthHistoryControllerIntegrationTests.RootServiceName,
      HealthHistoryControllerIntegrationTests.HealthCheckName,
      sampleTimestamp,
      testStatus);

    var getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/health-history/{testEnvironment}/tenants/{testTenant}/services/{RootServiceName}/health-check-results?timeQuery={queryTimestamp.ToString("yyyy-MM-ddTHH:mm:ssZ")}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content
      .ReadFromJsonAsync<Dictionary<String, (DateTime Timestamp, HealthStatus Status)>>(
        HealthControllerIntegrationTests.SerializerOptions
      );

    Assert.NotNull(body);
    var healthCheckHistory = Assert.Single(body);

    // create new timestamp without milliseconds since prometheus query doesn't return timestamps with ms.
    var truncatedQueryTimestamp = new DateTime(queryTimestamp.Year, queryTimestamp.Month, queryTimestamp.Day,
      queryTimestamp.Hour, queryTimestamp.Minute, queryTimestamp.Second);
    Assert.Equal(HealthCheckName, healthCheckHistory.Key);
    Assert.Equal(truncatedQueryTimestamp, healthCheckHistory.Value.Timestamp);
    Assert.Equal(testStatus, healthCheckHistory.Value.Status);
  }

  /// <summary>
  ///   This test validates that the GetServiceHealthHistory endpoint is correctly
  ///   aggregating the worst status over a given step.
  /// </summary>
  [Fact]
  public async Task GetServiceHealthHistory_WorstStatusIsReturned() {

    var initialStatus = HealthStatus.Offline;
    var currentStatus = HealthStatus.Online;

    var end = DateTime.UtcNow;
    var start = end.AddMinutes(-10);
    var step = 600;

    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthHistoryControllerIntegrationTests.TestRootOnlyConfiguration);

    await this.RecordServiceHealth(
      testEnvironment,
      testTenant,
      HealthHistoryControllerIntegrationTests.RootServiceName,
      HealthHistoryControllerIntegrationTests.HealthCheckName,
      start.AddMinutes(1),
      initialStatus);

    await this.RecordServiceHealth(
      testEnvironment,
      testTenant,
      HealthHistoryControllerIntegrationTests.RootServiceName,
      HealthHistoryControllerIntegrationTests.HealthCheckName,
      start.AddMinutes(2),
      currentStatus);

    var getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/health-history/{testEnvironment}/tenants/{testTenant}/services/{RootServiceName}?start={GetUniversalTimeString(start)}&end={GetUniversalTimeString(end)}&step={step}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyHealthHistory>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    var aggregateStatus = body.AggregateStatus;

    Assert.NotNull(aggregateStatus);
    Assert.NotEmpty(aggregateStatus);

    // 1 item should be in response since the step was the same as the duration
    var aggregateStatusItem = Assert.Single(aggregateStatus);
    Assert.Equal(aggregateStatusItem.AggregateStatus, initialStatus);
  }

  #region Authentication and Authorization Tests

  //****************************************************************************
  //
  //                     Authentication and Authorization
  //
  //****************************************************************************

  [Fact]
  public async Task GetHealthHistory_Auth_GlobalAdmin_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow.AddMinutes(-10);
    var start = timestamp;
    var end = timestamp.AddMinutes(10);
    for (var i = 0; i < NumberRecordings; i++) {
      await this.RecordServiceHealth(
        testEnvironment,
        testTenant,
        RootServiceName,
        HealthCheckName,
        timestamp,
        HealthStatus.Online);
      timestamp = timestamp.AddSeconds(TimeInSecondsBetweenRecordings);
    }

    var getResponse = await
      this.Fixture.CreateAdminRequest(
          $"/api/v2/health-history/{testEnvironment}/tenants/{testTenant}?start={start}&end={end}&step={Step}")
        .GetAsync();

    Assert.True(
      getResponse.IsSuccessStatusCode,
      $"GetHealthHistory returned non-success status code: {getResponse.StatusCode}"
    );
  }

  [Fact]
  public async Task GetHealthHistory_Auth_TenantUser_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow.AddMinutes(-10);
    var start = timestamp;
    var end = timestamp.AddMinutes(10);
    for (var i = 0; i < NumberRecordings; i++) {
      await this.RecordServiceHealth(
        testEnvironment,
        testTenant,
        RootServiceName,
        HealthCheckName,
        timestamp,
        HealthStatus.Online);
      timestamp = timestamp.AddSeconds(TimeInSecondsBetweenRecordings);
    }

    var getResponse = await
      this.Fixture.CreateAuthenticatedRequest(
          $"/api/v2/health-history/{testEnvironment}/tenants/{testTenant}?start={start}&end={end}&step={Step}",
          PermissionType.Standard,
          testEnvironment,
          testTenant)
        .GetAsync();

    Assert.True(
      getResponse.IsSuccessStatusCode,
      $"GetHealthHistory returned non-success status code: {getResponse.StatusCode}"
    );
  }

  [Fact]
  public async Task GetHealthHistory_Auth_Anonymous_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow.AddMinutes(-10);
    var start = timestamp;
    var end = timestamp.AddMinutes(10);
    for (var i = 0; i < NumberRecordings; i++) {
      await this.RecordServiceHealth(
        testEnvironment,
        testTenant,
        RootServiceName,
        HealthCheckName,
        timestamp,
        HealthStatus.Online);
      timestamp = timestamp.AddSeconds(TimeInSecondsBetweenRecordings);
    }

    var getResponse = await
      this.Fixture.Server.CreateRequest(
          $"/api/v2/health-history/{testEnvironment}/tenants/{testTenant}?start={start}&end={end}&step={Step}")
        .GetAsync();

    Assert.True(
      getResponse.IsSuccessStatusCode,
      $"GetHealthHistory returned non-success status code: {getResponse.StatusCode}"
    );
  }

  #endregion

  public static IEnumerable<Object[]> HealthStatusData() {
    yield return new Object[] {
      new HealthStatus[] { HealthStatus.Online, HealthStatus.AtRisk, HealthStatus.Degraded },
      new HealthStatus[] { HealthStatus.Online, HealthStatus.Online, HealthStatus.Offline },
      new HealthStatus[] { HealthStatus.Online, HealthStatus.AtRisk, HealthStatus.Offline }
    };
    yield return new Object[] {
      new HealthStatus[] { HealthStatus.Online, HealthStatus.Online, HealthStatus.Online, HealthStatus.Online, HealthStatus.Unknown, HealthStatus.Unknown, HealthStatus.Unknown },
      new HealthStatus[] { HealthStatus.Offline, HealthStatus.Offline, HealthStatus.Offline, HealthStatus.Online, HealthStatus.Unknown, HealthStatus.Unknown, HealthStatus.Unknown},
      new HealthStatus[] { HealthStatus.Offline, HealthStatus.Offline, HealthStatus.Offline, HealthStatus.Online, HealthStatus.Unknown, HealthStatus.Unknown, HealthStatus.Unknown }
    };
    yield return new Object[] {
      new HealthStatus[] { HealthStatus.Online, HealthStatus.Online, HealthStatus.Degraded },
      new HealthStatus[] {  },
      new HealthStatus[] { HealthStatus.Online, HealthStatus.Online, HealthStatus.Degraded }
    };
    yield return new Object[] {
      new HealthStatus[] {  },
      new HealthStatus[] { HealthStatus.Online, HealthStatus.Online, HealthStatus.Degraded },
      new HealthStatus[] { HealthStatus.Online, HealthStatus.Online, HealthStatus.Degraded }
    };
  }

  public static IEnumerable<Object[]> HealthStatusData2() {
    yield return new Object[] {
      new HealthStatus[] { HealthStatus.Online, HealthStatus.AtRisk, HealthStatus.Degraded },
      new HealthStatus[] { HealthStatus.Online, HealthStatus.AtRisk, HealthStatus.Degraded }
    };
    yield return new Object[] {
      new HealthStatus[] { HealthStatus.Online, HealthStatus.Online, HealthStatus.Online, HealthStatus.Online, HealthStatus.Unknown, HealthStatus.Unknown, HealthStatus.Unknown },
      new HealthStatus[] { HealthStatus.Online, HealthStatus.Online, HealthStatus.Online, HealthStatus.Online, HealthStatus.Unknown, HealthStatus.Unknown, HealthStatus.Unknown },
    };
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

  private static String GetUniversalTimeString(DateTime timestamp) {
    return timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ");
  }
}
