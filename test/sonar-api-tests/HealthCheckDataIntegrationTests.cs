using System;
using System.Collections.Immutable;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Xunit;
using Xunit.Abstractions;
using TimeSeries = System.Collections.Immutable.IImmutableList<(System.DateTime Timestamp, System.Double Value)>;

namespace Cms.BatCave.Sonar.Tests;

public class HealthCheckDataIntegrationTests : ApiControllerTestsBase {
  private const String TestRootServiceName = "TestRootService";
  private const String TestHealthCheckName = "TestHealthCheck";

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

  private static readonly ServiceHierarchyConfiguration TestRootOnlyConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        TestRootServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        ImmutableList.Create(TestHealthCheck),
        children: null)
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    null
  );

  public HealthCheckDataIntegrationTests(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper outputHelper) :
    base(fixture, outputHelper) {
  }

  [Fact]
  public async Task RecordHealthCheckData_Auth_GlobalAdmin_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow;

    // Record health status
    var response = await
      this.Fixture.CreateAdminRequest(
          $"/api/v2/health-check-data/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}")
        .And(req => {
          req.Content = ToJsonContent(new ServiceHealthData(
            ImmutableDictionary<String, TimeSeries>.Empty.Add(
              TestHealthCheckName,
              ImmutableList.Create<(DateTime, Double)>(
                (timestamp, 3.14159)
              ))
          ));
        })
        .PostAsync();

    Assert.True(
      response.IsSuccessStatusCode,
      userMessage: $"Expected a success response code (2xx). Actual: {(Int32)response.StatusCode}"
    );
  }

  [Fact]
  public async Task RecordHealthCheckData_Auth_EnvironmentAdmin_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow;

    // Record health status
    var response = await
      this.Fixture.CreateAuthenticatedRequest(
          $"/api/v2/health-check-data/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}",
          PermissionType.Admin,
          testEnvironment)
        .And(req => {
          req.Content = ToJsonContent(new ServiceHealthData(
            ImmutableDictionary<String, TimeSeries>.Empty.Add(
              TestHealthCheckName,
              ImmutableList.Create<(DateTime, Double)>(
                (timestamp, 3.14159)
              ))
          ));
        })
        .PostAsync();

    Assert.True(
      response.IsSuccessStatusCode,
      userMessage: $"Expected a success response code (2xx). Actual: {(Int32)response.StatusCode}"
    );
  }

  [Fact]
  public async Task RecordHealthCheckData_Auth_TenantAdmin_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow;

    // Record health status
    var response = await
      this.Fixture.CreateAuthenticatedRequest(
          $"/api/v2/health-check-data/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}",
          PermissionType.Admin,
          testEnvironment,
          testTenant)
        .And(req => {
          req.Content = ToJsonContent(new ServiceHealthData(
            ImmutableDictionary<String, TimeSeries>.Empty.Add(
              TestHealthCheckName,
              ImmutableList.Create<(DateTime, Double)>(
                (timestamp, 3.14159)
              ))
          ));
        })
        .PostAsync();

    Assert.True(
      response.IsSuccessStatusCode,
      userMessage: $"Expected a success response code (2xx). Actual: {(Int32)response.StatusCode}"
    );
  }

  [Fact]
  public async Task RecordHealthCheckData_Auth_StandardApiKey_Forbidden() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow;

    // Record health status
    var response = await
      this.Fixture.CreateAuthenticatedRequest(
          $"/api/v2/health-check-data/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}",
          PermissionType.Standard)
        .And(req => {
          req.Content = ToJsonContent(new ServiceHealthData(
            ImmutableDictionary<String, TimeSeries>.Empty.Add(
              TestHealthCheckName,
              ImmutableList.Create<(DateTime, Double)>(
                (timestamp, 3.14159)
              ))
          ));
        })
        .PostAsync();

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task RecordHealthCheckData_Auth_Anonymous_Unauthorized() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow;

    // Record health status
    var response = await
      this.Fixture.Server.CreateRequest(
          $"/api/v2/health-check-data/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}")
        .And(req => {
          req.Content = ToJsonContent(new ServiceHealthData(
            ImmutableDictionary<String, TimeSeries>.Empty.Add(
              TestHealthCheckName,
              ImmutableList.Create<(DateTime, Double)>(
                (timestamp, 3.14159)
              ))
          ));
        })
        .PostAsync();

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task GetHealthCheckData_Auth_GlobalAdmin_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow;

    // Record health status
    var response = await
      this.Fixture.CreateAdminRequest(
          $"/api/v2/health-check-data/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}")
        .And(req => {
          req.Content = ToJsonContent(new ServiceHealthData(
            ImmutableDictionary<String, TimeSeries>.Empty.Add(
              TestHealthCheckName,
              ImmutableList.Create<(DateTime, Double)>(
                (timestamp, 3.14159)
              ))
          ));
        })
        .PostAsync();

    AssertHelper.Precondition(response.IsSuccessStatusCode, "Error creating test data");

    response = await
      this.Fixture.CreateAdminRequest(
          $"/api/v2/health-check-data/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}/health-check/{TestHealthCheckName}")
        .GetAsync();

    Assert.True(
      response.IsSuccessStatusCode,
      $"GetHealthCheckData returned a non success status code: {response.StatusCode}"
    );
  }

  [Fact]
  public async Task GetHealthCheckData_Auth_TenantUser_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow;

    // Record health status
    var response = await
      this.Fixture.CreateAuthenticatedRequest(
          $"/api/v2/health-check-data/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}",
          PermissionType.Admin,
          testEnvironment,
          testTenant)
        .And(req => {
          req.Content = ToJsonContent(new ServiceHealthData(
            ImmutableDictionary<String, TimeSeries>.Empty.Add(
              TestHealthCheckName,
              ImmutableList.Create<(DateTime, Double)>(
                (timestamp, 3.14159)
              ))
          ));
        })
        .PostAsync();

    AssertHelper.Precondition(response.IsSuccessStatusCode, "Error creating test data");

    response = await
      this.Fixture.CreateAuthenticatedRequest(
          $"/api/v2/health-check-data/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}/health-check/{TestHealthCheckName}",
          PermissionType.Standard,
          testEnvironment,
          testTenant)
        .GetAsync();

    Assert.True(
      response.IsSuccessStatusCode,
      $"GetHealthCheckData returned a non success status code: {response.StatusCode}"
    );
  }

  [Fact]
  public async Task GetHealthCheckData_Auth_Anonymous_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow;

    // Record health status
    var response = await
      this.Fixture.CreateAdminRequest(
          $"/api/v2/health-check-data/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}")
        .And(req => {
          req.Content = ToJsonContent(new ServiceHealthData(
            ImmutableDictionary<String, TimeSeries>.Empty.Add(
              TestHealthCheckName,
              ImmutableList.Create<(DateTime, Double)>(
                (timestamp, 3.14159)
              ))
          ));
        })
        .PostAsync();

    AssertHelper.Precondition(response.IsSuccessStatusCode, "Error creating test data");

    response = await
      this.Fixture.Server.CreateRequest(
          $"/api/v2/health-check-data/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}/health-check/{TestHealthCheckName}")
        .GetAsync();

    Assert.True(
      response.IsSuccessStatusCode,
      $"GetHealthCheckData returned a non success status code: {response.StatusCode}"
    );
  }

  private static HttpContent ToJsonContent<T>(T obj) {
    return JsonContent.Create(
      obj,
      new MediaTypeHeaderValue("application/json"),
      SerializerOptions
    );
  }
}
