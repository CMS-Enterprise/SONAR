using System;
using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Json;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Environment = Cms.BatCave.Sonar.Data.Environment;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Tests;

public class HealthControllerIntegrationTests : ApiControllerTestsBase {
  private const String TestRootServiceName = "TestRootService";
  private const String TestChildServiceName = "TestChildService";
  private const String TestHealthCheckName = "TestHealthCheck";

  private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions {
    Converters = { new JsonStringEnumConverter(), new ArrayTupleConverterFactory() },
    PropertyNameCaseInsensitive = true
  };

  private static readonly HealthCheckModel TestHealthCheck =
    new(
      HealthControllerIntegrationTests.TestHealthCheckName,
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
        HealthControllerIntegrationTests.TestRootServiceName,
        DisplayName: "Display Name",
        Description: null,
        Url: null,
        ImmutableList.Create(HealthControllerIntegrationTests.TestHealthCheck),
        Children: null)
    ),
    ImmutableHashSet<String>.Empty.Add(HealthControllerIntegrationTests.TestRootServiceName)
  );

  private static readonly ServiceHierarchyConfiguration TestRootChildConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        HealthControllerIntegrationTests.TestRootServiceName,
        DisplayName: "Display Name",
        Description: null,
        Url: null,
        ImmutableList.Create(HealthControllerIntegrationTests.TestHealthCheck),
        ImmutableHashSet<String>.Empty.Add(HealthControllerIntegrationTests.TestChildServiceName)),
      new ServiceConfiguration(
        HealthControllerIntegrationTests.TestChildServiceName,
        DisplayName: "Display Name",
        Description: null,
        Url: null,
        HealthChecks: ImmutableList.Create(HealthControllerIntegrationTests.TestHealthCheck),
        Children: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(HealthControllerIntegrationTests.TestRootServiceName)
  );

  public HealthControllerIntegrationTests(ApiIntegrationTestFixture fixture, ITestOutputHelper outputHelper) :
    base(fixture, outputHelper) {
  }

  // RecordServiceHealth scenarios
  //    Missing Environment - 404
  //    Missing Tenant - 404
  //    Missing Service - 404
  //    Basic successful request (root only) - 200
  //    Incorrect Health Check Name - 400
  //    Outdated Timestamp (> 2h) - 400
  //    Out of Order Timestamp - 400

  [Fact]
  public async Task RecordServiceHealth_MissingEnvironmentReturnsNotFound() {
    var missingEnvironmentName = Guid.NewGuid().ToString();
    var response = await
      this.CreateAdminRequest($"/api/v2/health/{missingEnvironmentName}/tenants/foo/services/bar")
        .AddHeader(name: "Accept", value: "application/json")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHealth(
            DateTime.Now,
            HealthStatus.Online,
            ImmutableDictionary<String, HealthStatus>.Empty
          ));
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ResourceNotFoundException.ProblemTypeName,
      actual: body.Type
    );
    Assert.Equal(
      expected: nameof(Environment),
      actual: HealthControllerIntegrationTests.GetExtensionValue<String>(body.Extensions["resourceType"])
    );
    Assert.Equal(
      expected: missingEnvironmentName,
      actual: HealthControllerIntegrationTests.GetExtensionValue<String>(body.Extensions["resourceId"])
    );
  }

  [Fact]
  public async Task RecordServiceHealth_MissingTenantReturnsNotFound() {
    var existingEnvironmentName = Guid.NewGuid().ToString();

    // Create existing Environment
    await this.Fixture.WithDependenciesAsync(async (provider, cancellationToken) => {
      var dbContext = provider.GetRequiredService<DataContext>();
      var environments = provider.GetRequiredService<DbSet<Environment>>();

      await environments.AddAsync(Environment.New(existingEnvironmentName), cancellationToken);
      await dbContext.SaveChangesAsync(cancellationToken);
    });

    var missingTenantName = Guid.NewGuid().ToString();

    var response = await
      this.CreateAdminRequest($"/api/v2/health/{existingEnvironmentName}/tenants/{missingTenantName}/services/foo")
        .AddHeader(name: "Accept", value: "application/json")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHealth(
            DateTime.Now,
            HealthStatus.Online,
            ImmutableDictionary<String, HealthStatus>.Empty
          ));
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ResourceNotFoundException.ProblemTypeName,
      actual: body.Type
    );
    Assert.Equal(
      expected: nameof(Tenant),
      actual: HealthControllerIntegrationTests.GetExtensionValue<String>(body.Extensions["resourceType"])
    );
    Assert.Equal(
      expected: missingTenantName,
      actual: HealthControllerIntegrationTests.GetExtensionValue<String>(body.Extensions["resourceId"])
    );
  }

  [Fact]
  public async Task RecordServiceHealth_MissingServiceReturnsNotFound() {
    // Create Service Configuration
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(HealthControllerIntegrationTests.TestRootOnlyConfiguration);
        })
        .PostAsync();

    // This should always succeed, This isn't what is being tested.
    AssertHelper.Precondition(
      createConfigResponse.IsSuccessStatusCode,
      message: "Failed to create test configuration."
    );

    var timestamp = DateTime.UtcNow;

    var missingServiceName = Guid.NewGuid().ToString();
    // Record health status
    var response = await
      this.CreateAdminRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}/services/{missingServiceName}")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHealth(
            timestamp,
            HealthStatus.Online,
            ImmutableDictionary<String, HealthStatus>.Empty
              .Add(HealthControllerIntegrationTests.TestHealthCheckName, HealthStatus.Online)
          ));
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ResourceNotFoundException.ProblemTypeName,
      actual: body.Type
    );
    Assert.Equal(
      expected: nameof(Service),
      actual: HealthControllerIntegrationTests.GetExtensionValue<String>(body.Extensions["resourceType"])
    );
    Assert.Equal(
      expected: missingServiceName,
      actual: HealthControllerIntegrationTests.GetExtensionValue<String>(body.Extensions["resourceId"])
    );
  }

  [Theory]
  [InlineData(HealthStatus.Unknown)]
  [InlineData(HealthStatus.Online)]
  [InlineData(HealthStatus.AtRisk)]
  [InlineData(HealthStatus.Degraded)]
  [InlineData(HealthStatus.Offline)]
  public async Task RecordServiceHealth_AllStatusTypes_Success(HealthStatus testStatus) {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthControllerIntegrationTests.TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow;

    // Record health status
    var response = await
      this.CreateAdminRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}/services/{HealthControllerIntegrationTests.TestRootServiceName}")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHealth(
            timestamp,
            testStatus,
            ImmutableDictionary<String, HealthStatus>.Empty
              .Add(HealthControllerIntegrationTests.TestHealthCheckName, testStatus)
          ));
        })
        .PostAsync();

    // 200, 201, 204 would all be ok
    Assert.True(
      response.IsSuccessStatusCode,
      userMessage: $"Expected a success response code (2xx). Actual: {(Int32)response.StatusCode}"
    );

    // Verify the data got created
    var getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/health/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyHealth[]>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);

    var rootServiceHealth = Assert.Single(body);

    Assert.Equal(HealthControllerIntegrationTests.TestRootServiceName, rootServiceHealth.Name);
    Assert.Equal(testStatus, rootServiceHealth.AggregateStatus);
    Assert.NotNull(rootServiceHealth.HealthChecks);
    var healthCheck = Assert.Single(rootServiceHealth.HealthChecks);
    Assert.Equal(HealthControllerIntegrationTests.TestHealthCheckName, healthCheck.Key);
    Assert.NotNull(healthCheck.Value);
    // Prometheus returns millisecond precision
    Assert.Equal(timestamp.TruncateNanoseconds(), healthCheck.Value.Value.Timestamp);
  }

  [Fact]
  public async Task RecordServiceHealth_IncorrectHealthCheckName() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthControllerIntegrationTests.TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow;

    // Record health status
    var response = await
      this.CreateAdminRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}/services/{HealthControllerIntegrationTests.TestRootServiceName}")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHealth(
            timestamp,
            HealthStatus.Online,
            ImmutableDictionary<String, HealthStatus>.Empty
              .Add(key: "incorrect", HealthStatus.Online)
          ));
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ProblemTypes.InconsistentData,
      actual: body.Type
    );
    Assert.Equal(
      expected: "incorrect",
      actual: HealthControllerIntegrationTests.GetExtensionValue<String>(body.Extensions["healthCheck"])
    );
  }

  [Fact]
  public async Task RecordServiceHealth_OutdatedTimestamp() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthControllerIntegrationTests.TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow.AddHours(-4);

    // Record health status
    var response = await
      this.CreateAdminRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}/services/{HealthControllerIntegrationTests.TestRootServiceName}")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHealth(
            timestamp,
            HealthStatus.Online,
            ImmutableDictionary<String, HealthStatus>.Empty
              .Add(HealthControllerIntegrationTests.TestHealthCheckName, HealthStatus.Online)
          ));
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ProblemTypes.InvalidData,
      actual: body.Type
    );
  }

  [Fact]
  public async Task RecordServiceHealth_OutOfOrderTimestamps() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthControllerIntegrationTests.TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow;

    // Record health status
    var response = await
      this.CreateAdminRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}/services/{HealthControllerIntegrationTests.TestRootServiceName}")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHealth(
            timestamp,
            HealthStatus.Online,
            ImmutableDictionary<String, HealthStatus>.Empty
              .Add(HealthControllerIntegrationTests.TestHealthCheckName, HealthStatus.Online)
          ));
        })
        .PostAsync();

    Assert.True(
      response.IsSuccessStatusCode,
      userMessage: $"Expected a success response code (2xx). Actual: {(Int32)response.StatusCode}"
    );

    // Attempt to record out of sequence health status
    response = await
      this.CreateAdminRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}/services/{HealthControllerIntegrationTests.TestRootServiceName}")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHealth(
            timestamp.AddMinutes(-10),
            HealthStatus.Online,
            ImmutableDictionary<String, HealthStatus>.Empty
              .Add(HealthControllerIntegrationTests.TestHealthCheckName, HealthStatus.Online)
          ));
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: response.StatusCode
    );

    var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ProblemTypes.InvalidData,
      actual: body.Type
    );
  }

  // GetServiceHierarchyHealth scenarios
  //    Missing Environment - 404
  //    Missing Tenant - 404
  //    No Services - Empty Array
  //    No Recorded Status - null values
  //    Child with no recorded status - Root has null aggregate
  //    Root & Child Status Resolutions:
  //      Child with explicitly recorded unknown status - Root has unknown aggregate
  //      Child online while root offline - Root has offline aggregate
  //      Child offline while root online - Root has offline aggregate

  [Fact]
  public async Task GetServiceHierarchyHealth_MissingEnvironmentReturnsNotFound() {
    var missingEnvironmentName = Guid.NewGuid().ToString();
    var response = await
      this.Fixture.Server.CreateRequest($"/api/v2/health/{missingEnvironmentName}/tenants/foo")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ResourceNotFoundException.ProblemTypeName,
      actual: body.Type
    );
    Assert.Equal(
      expected: nameof(Environment),
      actual: HealthControllerIntegrationTests.GetExtensionValue<String>(body.Extensions["resourceType"])
    );
    Assert.Equal(
      expected: missingEnvironmentName,
      actual: HealthControllerIntegrationTests.GetExtensionValue<String>(body.Extensions["resourceId"])
    );
  }

  [Fact]
  public async Task GetServiceHierarchyHealth_MissingTenantReturnsNotFound() {
    var existingEnvironmentName = Guid.NewGuid().ToString();

    // Create existing Environment
    await this.Fixture.WithDependenciesAsync(async (provider, cancellationToken) => {
      var dbContext = provider.GetRequiredService<DataContext>();
      var environments = provider.GetRequiredService<DbSet<Environment>>();

      await environments.AddAsync(Environment.New(existingEnvironmentName), cancellationToken);
      await dbContext.SaveChangesAsync(cancellationToken);
    });

    var missingTenantName = Guid.NewGuid().ToString();

    var response = await
      this.Fixture.Server.CreateRequest($"/api/v2/health/{existingEnvironmentName}/tenants/{missingTenantName}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ResourceNotFoundException.ProblemTypeName,
      actual: body.Type
    );
    Assert.Equal(
      expected: nameof(Tenant),
      actual: HealthControllerIntegrationTests.GetExtensionValue<String>(body.Extensions["resourceType"])
    );
    Assert.Equal(
      expected: missingTenantName,
      actual: HealthControllerIntegrationTests.GetExtensionValue<String>(body.Extensions["resourceId"])
    );
  }

  [Fact]
  public async Task GetServiceHierarchyHealth_NoServices() {
    // Create Tenant Configuration
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHierarchyConfiguration(
            ImmutableArray<ServiceConfiguration>.Empty,
            ImmutableHashSet<String>.Empty
          ));
        })
        .PostAsync();

    // This should always succeed, This isn't what is being tested.
    AssertHelper.Precondition(
      createConfigResponse.IsSuccessStatusCode,
      message: "Failed to create test configuration."
    );

    var getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/health/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyHealth[]>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Empty(body);
  }

  [Fact]
  public async Task GetServiceHierarchyHealth_NoStatus() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthControllerIntegrationTests.TestRootOnlyConfiguration);

    var getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/health/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyHealth[]>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    var serviceHealth = Assert.Single(body);

    Assert.NotNull(serviceHealth);
    Assert.Null(serviceHealth.Timestamp);
    Assert.Null(serviceHealth.AggregateStatus);
    Assert.NotNull(serviceHealth.HealthChecks);
    var healthCheck = Assert.Single(serviceHealth.HealthChecks);
    Assert.Equal(expected: HealthControllerIntegrationTests.TestHealthCheckName, actual: healthCheck.Key);
    Assert.Null(healthCheck.Value);
  }

  [Fact]
  public async Task GetServiceHierarchyHealth_NoChildStatus() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthControllerIntegrationTests.TestRootChildConfiguration);

    var timestamp = DateTime.UtcNow;
    await this.RecordServiceHealth(
      testEnvironment,
      testTenant,
      HealthControllerIntegrationTests.TestRootServiceName,
      HealthControllerIntegrationTests.TestHealthCheckName,
      timestamp,
      HealthStatus.Online);

    var getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/health/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyHealth[]>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    var serviceHealth = Assert.Single(body);

    Assert.NotNull(serviceHealth);
    Assert.NotNull(serviceHealth.Children);
    var childServiceHealth = Assert.Single(serviceHealth.Children);
    Assert.NotNull(childServiceHealth);
    // The child service is listed, but its status is unspecified
    Assert.Null(childServiceHealth.Timestamp);
    Assert.Null(childServiceHealth.AggregateStatus);

    // Aggregate is null because the child service's status is null
    Assert.Null(serviceHealth.Timestamp);
    Assert.Null(serviceHealth.AggregateStatus);
    Assert.NotNull(serviceHealth.HealthChecks);
    var healthCheck = Assert.Single(serviceHealth.HealthChecks);
    Assert.Equal(expected: HealthControllerIntegrationTests.TestHealthCheckName, actual: healthCheck.Key);
    Assert.Equal(expected: (timestamp.TruncateNanoseconds(), HealthStatus.Online), actual: healthCheck.Value);
  }

  /// <summary>
  ///   This test validates that the GetServiceHierarchyHealth endpoint is properly aggregating health
  ///   status when a root and child service have different statuses.
  /// </summary>
  /// <param name="rootStatus">The status to record for the root service and its health check.</param>
  /// <param name="childStatus">The status to record for the child service and its health check.</param>
  /// <param name="expectedAggregateStatus">
  ///   The expected aggregate status for the root service given the combination of its status and its
  ///   child service status.
  /// </param>
  [Theory]
  [InlineData(HealthStatus.Online, HealthStatus.Unknown, HealthStatus.Unknown)]
  [InlineData(HealthStatus.Online, HealthStatus.Offline, HealthStatus.Offline)]
  [InlineData(HealthStatus.Offline, HealthStatus.Online, HealthStatus.Offline)]
  public async Task GetServiceHierarchyHealth_RootStatusAggregation(
    HealthStatus rootStatus,
    HealthStatus childStatus,
    HealthStatus expectedAggregateStatus) {

    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthControllerIntegrationTests.TestRootChildConfiguration);

    var timestamp = DateTime.UtcNow;
    await this.RecordServiceHealth(
      testEnvironment,
      testTenant,
      HealthControllerIntegrationTests.TestRootServiceName,
      HealthControllerIntegrationTests.TestHealthCheckName,
      timestamp,
      rootStatus);

    await this.RecordServiceHealth(
      testEnvironment,
      testTenant,
      HealthControllerIntegrationTests.TestChildServiceName,
      HealthControllerIntegrationTests.TestHealthCheckName,
      timestamp,
      childStatus);

    var getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/health/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyHealth[]>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    var serviceHealth = Assert.Single(body);

    Assert.NotNull(serviceHealth);
    Assert.NotNull(serviceHealth.Children);
    var childServiceHealth = Assert.Single(serviceHealth.Children);

    // The child service and health check should have the recorded status
    var expectedTimestamp = timestamp.TruncateNanoseconds();
    Assert.NotNull(childServiceHealth);
    Assert.Equal(expectedTimestamp, childServiceHealth.Timestamp);
    Assert.Equal(childStatus, childServiceHealth.AggregateStatus);
    Assert.NotNull(childServiceHealth.HealthChecks);
    var childHealthCheck = Assert.Single(childServiceHealth.HealthChecks);
    Assert.Equal(expected: HealthControllerIntegrationTests.TestHealthCheckName, actual: childHealthCheck.Key);
    Assert.Equal(expected: (expectedTimestamp, childStatus), actual: childHealthCheck.Value);

    // The root service should have the expected aggregate status
    Assert.Equal(expectedTimestamp, serviceHealth.Timestamp);
    Assert.Equal(expectedAggregateStatus, serviceHealth.AggregateStatus);

    // The root health check should have the recorded status
    Assert.NotNull(serviceHealth.HealthChecks);
    var rootHealthCheck = Assert.Single(serviceHealth.HealthChecks);
    Assert.Equal(expected: HealthControllerIntegrationTests.TestHealthCheckName, actual: rootHealthCheck.Key);
    Assert.Equal(expected: (expectedTimestamp, rootStatus), actual: rootHealthCheck.Value);
  }

  private async Task<(String, String)> CreateTestConfiguration(ServiceHierarchyConfiguration configuration) {
    // Create Service Configuration
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(configuration);
        })
        .PostAsync();

    // This should always succeed, This isn't what is being tested.
    AssertHelper.Precondition(
      createConfigResponse.IsSuccessStatusCode,
      message: "Failed to create test configuration."
    );
    return (testEnvironment, testTenant);
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
      this.CreateAdminRequest(
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

  private static T? GetExtensionValue<T>(Object? extensionValue) {
    return extensionValue switch {
      null => default,
      T typedValue => typedValue,
      JsonElement element => HealthControllerIntegrationTests.GetElementValue<T>(element),
      _ => throw new ArgumentException(
        $"The {nameof(extensionValue)} argument was an unexpected type: {extensionValue.GetType().Name}",
        nameof(extensionValue))
    };
  }

  private static T GetElementValue<T>(JsonElement element) {
    if (typeof(T) == typeof(Int16)) {
      return (T)(Object)element.GetInt16();
    } else if (typeof(T) == typeof(Int32)) {
      return (T)(Object)element.GetInt32();
    } else if (typeof(T) == typeof(Int64)) {
      return (T)(Object)element.GetInt64();
    } else if (typeof(T) == typeof(UInt16)) {
      return (T)(Object)element.GetUInt16();
    } else if (typeof(T) == typeof(UInt32)) {
      return (T)(Object)element.GetUInt32();
    } else if (typeof(T) == typeof(UInt64)) {
      return (T)(Object)element.GetUInt64();
    } else if (typeof(T) == typeof(Single)) {
      return (T)(Object)element.GetSingle();
    } else if (typeof(T) == typeof(Double)) {
      return (T)(Object)element.GetDouble();
    } else if (typeof(T) == typeof(Decimal)) {
      return (T)(Object)element.GetDecimal();
    } else if (typeof(T) == typeof(String)) {
      return (T)(Object)(
        element.GetString() ??
        throw new JsonException($"Unable to convert JSON element of type {element.ValueKind} to a {nameof(String)}")
      );
    } else if (typeof(T) == typeof(DateTime)) {
      return (T)(Object)element.GetDateTime();
    } else if (typeof(T) == typeof(Guid)) {
      return (T)(Object)element.GetGuid();
    } else if (typeof(T) == typeof(Uri)) {
      return (T)(Object)new Uri(
        element.GetString() ??
        throw new JsonException($"Unable to convert JSON element of type {element.ValueKind} to a {nameof(Uri)}")
      );
    } else if (typeof(T) == typeof(TimeSpan)) {
      return (T)(Object)TimeSpan.Parse(
        element.GetString() ??
        throw new JsonException($"Unable to convert JSON element of type {element.ValueKind} to a {nameof(TimeSpan)}")
      );
    } else {
      throw new NotSupportedException($"The specified type is not supported: {typeof(T).Name}");
    }
  }
}
