using Xunit.Abstractions;
using Xunit;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using DateTime = System.DateTime;

namespace Cms.BatCave.Sonar.Tests;

public class HealthCheckHistoryControllerIntegrationTests : ApiControllerTestsBase {
  private readonly ITestOutputHelper _output;
  private const String RootServiceName = "TestRootService";
  private const String HealthCheckName1 = "TestHealthCheck";
  private const String HealthCheckName2 = "TestHealthCheck2";

  private static readonly HealthCheckModel TestHealthCheck1 =
    new(
      HealthCheckHistoryControllerIntegrationTests.HealthCheckName1,
      description: "Health Check Description",
      HealthCheckType.PrometheusMetric,
      new MetricHealthCheckDefinition(
        TimeSpan.FromMinutes(1),
        expression: "test_metric",
        ImmutableList.Create(
          new MetricHealthCondition(HealthOperator.GreaterThan, threshold: 42.0m, HealthStatus.Offline))),
      null
    );

  private static readonly HealthCheckModel TestHealthCheck2 =
    new(
      HealthCheckName2,
      description: "Health Check2 Description",
      HealthCheckType.PrometheusMetric,
      new MetricHealthCheckDefinition(
        TimeSpan.FromMinutes(1),
        expression: "test_expression",
        ImmutableList.Create(
          new MetricHealthCondition(HealthOperator.GreaterThan, threshold: 42.0m, HealthStatus.Offline))),
      null
    );

  private static readonly ServiceHierarchyConfiguration TestRootOnlyConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        HealthCheckHistoryControllerIntegrationTests.RootServiceName,
        displayName: "Display Name", description: null, url: null,
        ImmutableList.Create(HealthCheckHistoryControllerIntegrationTests.TestHealthCheck1),
        children: null)
    ),
    ImmutableHashSet<String>.Empty.Add(HealthCheckHistoryControllerIntegrationTests.RootServiceName),
    null
  );

  private static readonly ServiceHierarchyConfiguration TestRootOnlyConfiguration2 = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        HealthCheckHistoryControllerIntegrationTests.RootServiceName,
        displayName: "Display Name", description: null, url: null,
        ImmutableList.Create(TestHealthCheck1, TestHealthCheck2),
        children: null)
    ),
    ImmutableHashSet<String>.Empty.Add(HealthCheckHistoryControllerIntegrationTests.RootServiceName),
    null
  );

  public HealthCheckHistoryControllerIntegrationTests(ApiIntegrationTestFixture fixture, ITestOutputHelper outputHelper) :
    base(fixture, outputHelper) {
    _output = outputHelper;
  }

  [Fact]
  public async Task HistoricalHealthCheckTest() {
    var testStatus = HealthStatus.Offline;
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthCheckHistoryControllerIntegrationTests.TestRootOnlyConfiguration);
    var sampleTimestamp = DateTime.UtcNow.AddMinutes(-10);
    var queryTimestamp = sampleTimestamp.AddMinutes(1);

    await this.RecordServiceHealth(
      testEnvironment,
      testTenant,
      HealthCheckHistoryControllerIntegrationTests.RootServiceName,
      sampleTimestamp,
      testStatus,
      new Dictionary<String, HealthStatus>() {
        { HealthCheckHistoryControllerIntegrationTests.HealthCheckName1, testStatus }
      });

    var getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/health-check-history/{testEnvironment}/tenants/{testTenant}/services/{RootServiceName}/health-check-result?timeQuery={queryTimestamp.ToString("yyyy-MM-ddTHH:mm:ssZ")}")
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
    Assert.Equal(HealthCheckName1, healthCheckHistory.Key);
    Assert.Equal(truncatedQueryTimestamp, healthCheckHistory.Value.Timestamp);
    Assert.Equal(testStatus, healthCheckHistory.Value.Status);
  }



  // Validate not found responses
  [Theory]
  [InlineData($"/api/v2/health-check-history/InvalidEnvironment/tenants/{{testTenant}}/services/{RootServiceName}/health-check-results?step=100")]
  [InlineData($"/api/v2/health-check-history/{{testEnvironment}}/tenants/InvalidTenant/services/{RootServiceName}/health-check-results?step=100")]
  [InlineData($"/api/v2/health-check-history/{{testEnvironment}}/tenants/{{testTenant}}/services/InvalidService/health-check-results?step=100")]
  public async Task HealthCheckHistory_NotFound(
    String incorrectPathToService) {
    // There are no validation
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration2);

    var urlText = incorrectPathToService
      .Replace("{testEnvironment}", testEnvironment)
      .Replace("{testTenant}", testTenant);

    // Record health with Timestamp
    var serviceHealthChecks1 = new Dictionary<String, HealthStatus>() {
      {HealthCheckName1, HealthStatus.Online},
      {HealthCheckName2, HealthStatus.AtRisk}
    };

    await this.RecordServiceHealth(
      testEnvironment,
      testTenant,
      RootServiceName,
      DateTime.UtcNow.AddMinutes(-5),
      HealthStatus.AtRisk, // Do not care for Aggregate Status
      serviceHealthChecks1); // Online, Degraded

    // Fetch time series response
    var getResponse = await
      this.Fixture.Server
        .CreateRequest(urlText)
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: getResponse.StatusCode);
  }

  // Validate the results from the HealthCheckHistoryController of the time series data.
  [Fact]
  public async Task HealthCheckHistoryTimeSeries_ValidateResults() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration2);

    var threeMinutesAgo = this.TruncateToSeconds(DateTime.UtcNow.AddMinutes(-3));
    var oneMinuteAgo = this.TruncateToSeconds(DateTime.UtcNow.AddMinutes(-1));

    // Record health with Timestamp 1
    var serviceHealthChecks1 = new Dictionary<String, HealthStatus>() {
      {HealthCheckName1, HealthStatus.Online},
      {HealthCheckName2, HealthStatus.AtRisk}
    };

    await this.RecordServiceHealth(
      testEnvironment,
      testTenant,
      RootServiceName,
      threeMinutesAgo,
      HealthStatus.AtRisk, // Do not care for Aggregate Status
      serviceHealthChecks1); // Online, Degraded

    // Record Health with Timestamp 2
    var serviceHealthChecks2 = new Dictionary<String, HealthStatus>() {
      {HealthCheckName1, HealthStatus.Degraded},
      {HealthCheckName2, HealthStatus.Offline}
    };

    await this.RecordServiceHealth(
      testEnvironment,
      testTenant,
      RootServiceName,
      oneMinuteAgo,
      HealthStatus.Offline, // Do not care for Aggregate Status
      serviceHealthChecks2); // AtRisk, Offline

    // Fetch time series response
    var getResponse = await
      this.Fixture.Server
        .CreateRequest($"/api/v2/health-check-history/{testEnvironment}/tenants/{testTenant}/services/{RootServiceName}/health-check-results?step=120&start={threeMinutesAgo:O}&end={oneMinuteAgo:O}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var result = await getResponse.Content.ReadFromJsonAsync<HealthCheckHistory>(SerializerOptions);
    Assert.NotNull(result);

    // Assert Keys by healthcheck name exists
    var healthChecks = result.HealthChecks;
    Assert.True(healthChecks.ContainsKey(HealthCheckName1));
    Assert.True(healthChecks.ContainsKey(HealthCheckName2));

    // Number of entry points
    Assert.Equal(2, healthChecks[HealthCheckName1].Count);
    Assert.Equal(2, healthChecks[HealthCheckName2].Count);

    // Create new timestamp without milliseconds since prometheus query doesn't return timestamps with ms.
    // Assert time stamps
    var resultSample1Timestamp = healthChecks[HealthCheckName1].Select(t => this.TruncateToSeconds(t.Item1));
    var resultSample2Timestamp = healthChecks[HealthCheckName2].Select(t => this.TruncateToSeconds(t.Item1));
    Assert.Contains(threeMinutesAgo, resultSample1Timestamp);
    Assert.Contains(oneMinuteAgo, resultSample1Timestamp);
    Assert.Contains(threeMinutesAgo, resultSample2Timestamp);
    Assert.Contains(oneMinuteAgo, resultSample2Timestamp);

    var healthCheck1Status = healthChecks[HealthCheckName1].Select(t => t.Item2); // Contains Online, Degraded
    var healthCheck2Status = healthChecks[HealthCheckName2].Select(t => t.Item2); // Contains AtRisk, Offline
    Assert.Contains(HealthStatus.Online, healthCheck1Status);
    Assert.Contains(HealthStatus.Degraded, healthCheck1Status);
    Assert.Contains(HealthStatus.AtRisk, healthCheck2Status);
    Assert.Contains(HealthStatus.Offline, healthCheck2Status);
  }

  private DateTime TruncateToSeconds(DateTime time) {
    return new DateTime(
      time.Ticks - (time.Ticks % TimeSpan.TicksPerSecond),
      time.Kind
    );
  }

  private async Task RecordServiceHealth(
    String testEnvironment,
    String testTenant,
    String serviceName,
    DateTime timestamp,
    HealthStatus aggregateStatus,
    Dictionary<String, HealthStatus> status) {

    // Record health status
    var response = await
      this.Fixture.CreateAdminRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}/services/{serviceName}")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHealth(
            timestamp,
            aggregateStatus,
            ImmutableDictionary<String, HealthStatus>.Empty.AddRange(status)
          ));
        })
        .PostAsync();

    AssertHelper.Precondition(
      response.IsSuccessStatusCode,
      message: "Failed to record service health"
    );
  }
}
