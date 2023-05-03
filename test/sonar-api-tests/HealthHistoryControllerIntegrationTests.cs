using Xunit.Abstractions;
using Xunit;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Extensions;
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

  private static readonly ServiceHierarchyConfiguration TestRootChildConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        HealthHistoryControllerIntegrationTests.TestRootServiceName,
        DisplayName: "Display Name",
        Description: null,
        Url: null,
        ImmutableList.Create(HealthHistoryControllerIntegrationTests.TestHealthCheck),
        ImmutableHashSet<String>.Empty.Add(HealthHistoryControllerIntegrationTests.TestChildServiceName)),
      new ServiceConfiguration(
        HealthHistoryControllerIntegrationTests.TestChildServiceName,
        DisplayName: "Display Name",
        Description: null,
        Url: null,
        HealthChecks: ImmutableList.Create(HealthHistoryControllerIntegrationTests.TestHealthCheck),
        Children: null
      )
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

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyHealthHistory[]>(
      HealthControllerIntegrationTests.SerializerOptions
    );

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



  [Theory]
  [MemberData(nameof(RCTestData))]
  public async Task RootStatusAggregation(
    HealthStatus[] parentStatus,
    HealthStatus[] childStatus,
    HealthStatus[]? expectedStatus
  )
 {
   //Each status in the array (parentStatus, childStatus) will be recorded 6 times in one minute.
   //The time in between each recording is 10 seconds
   const Int32 numberRecordings = 6;
   const Int32 timeInSecondsBetweenRecordings = 10;
   //When querying prometheus the step will be a 30 second step.
   //Therefore, there will be 2 retrieved recordings for each minute
   const Int32 step = 30;

   var timespan = -expectedStatus?.Length ?? -10;
   var end = DateTime.UtcNow;
   var start = DateTime.UtcNow.AddMinutes(timespan);
   var timestamp = start;

    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthHistoryControllerIntegrationTests.TestRootChildConfiguration);

    //Record the status in Prometheus
    foreach (var hs in parentStatus) {
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

    timestamp = start;
    foreach (var hs in childStatus) {
      for (var i = 0; i < numberRecordings; i++) {
        await this.RecordServiceHealth(
          testEnvironment,
          testTenant,
          HealthHistoryControllerIntegrationTests.TestChildServiceName,
          HealthHistoryControllerIntegrationTests.TestHealthCheckName,
          timestamp,
          hs);
        timestamp = timestamp.AddSeconds(timeInSecondsBetweenRecordings);
      }
    }

    var getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/health-history/{testEnvironment}/tenants/{testTenant}?start={start.ToLocalTime()}&end={end.ToLocalTime()}&step={step}")
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
      var colStatus = serviceHealth.AggregateStatus.Select(x => x.AggregateStatus);
      var healthStatusEnumerable = colStatus as HealthStatus[] ?? colStatus.ToArray();

      var index = 0;
      foreach (var hs in expectedStatus) {
        //should have 2 items for each minute (30 second step).
        Assert.Equal(hs, healthStatusEnumerable[index]);
        Assert.Equal(hs, healthStatusEnumerable[index+1]);
        index += 2;
      }
   } else {
     //If no parent service metrics, check the child metrics
     if (childService.AggregateStatus != null) {
       var colStatus = childService.AggregateStatus.Select(x => x.AggregateStatus);
       var healthStatusEnumerable = colStatus as HealthStatus[] ?? colStatus.ToArray();

       var index = 0;
       foreach (var hs in expectedStatus) {
         //should have 2 items for each minute (30 second step).
         Assert.Equal(hs, healthStatusEnumerable[index]);
         Assert.Equal(hs, healthStatusEnumerable[index + 1]);
         index += 2;
       }
     }
   }
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

  public static IEnumerable<Object[]> RCTestData() {
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
