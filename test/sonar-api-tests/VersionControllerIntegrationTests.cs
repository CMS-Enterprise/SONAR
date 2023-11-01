using System;
using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Tests;

public class VersionControllerIntegrationTests : ApiControllerTestsBase {
  private const String TestRootServiceName = "TestRootService";
  private const String TestHealthCheckName = "TestHealthCheck";
  private const String TestVersion1Value = "version1";
  private const String TestVersion2Value = "version2";
  private const VersionCheckType TestFluxVersionCheckType = VersionCheckType.FluxKustomization;

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

  private static readonly VersionCheckModel TestVersionCheck =
    new(
      TestFluxVersionCheckType,
      new FluxKustomizationVersionCheckDefinition(k8sNamespace: "test", kustomization: "test")
    );

  private static readonly ServiceHierarchyConfiguration TestRootOnlyConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        TestRootServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        ImmutableList.Create(TestHealthCheck),
        ImmutableList.Create(TestVersionCheck),
        children: null)
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    null
  );

  public VersionControllerIntegrationTests(ApiIntegrationTestFixture fixture, ITestOutputHelper outputHelper) :
    base(fixture, outputHelper) {
  }

  // RecordServiceVersion scenarios
  //    Missing Environment - 404
  //    Missing Tenant - 404
  //    Missing Service - 404
  //    Basic successful request (root only) - 200
  //    Incorrect Version Check Type - 400
  //    Outdated Timestamp (> 2h) - 400
  //    Out of Order Timestamp - 400
  [Fact]
  public async Task RecordServiceVersion_MissingEnvironmentReturnsNotFound() {
    var missingEnvironmentName = Guid.NewGuid().ToString();
    var response = await
      this.Fixture.CreateAdminRequest($"/api/v2/version/{missingEnvironmentName}/tenants/foo/services/bar")
        .AddHeader(name: "Accept", value: "application/json")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceVersion(
            DateTime.Now,
            ImmutableDictionary<VersionCheckType, String>.Empty));
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ResourceNotFoundException.ProblemTypeName,
      actual: body.Type
    );
  }

  [Fact]
  public async Task GetServiceVersion_MissingTenantReturnsNotFound() {
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
      this.Fixture
        .CreateAdminRequest($"/api/v2/version/{existingEnvironmentName}/tenants/{missingTenantName}/services/bar")
        .AddHeader(name: "Accept", value: "application/json")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceVersion(
            DateTime.Now,
            ImmutableDictionary<VersionCheckType, String>.Empty));
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ResourceNotFoundException.ProblemTypeName,
      actual: body.Type
    );
  }

  [Fact]
  public async Task RecordServiceVersion_MissingServiceReturnsNotFound() {
    // Create Service Configuration
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(TestRootOnlyConfiguration);
        })
        .PostAsync();

    // This should always succeed, This isn't what is being tested.
    AssertHelper.Precondition(
      createConfigResponse.IsSuccessStatusCode,
      message: "Failed to create test configuration."
    );

    var timestamp = DateTime.UtcNow;

    var missingServiceName = Guid.NewGuid().ToString();

    var response = await
      this.Fixture
        .CreateAdminRequest($"/api/v2/version/{testEnvironment}/tenants/{testTenant}/services/{missingServiceName}")
        .AddHeader(name: "Accept", value: "application/json")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceVersion(
            timestamp,
            ImmutableDictionary<VersionCheckType, String>.Empty));
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ResourceNotFoundException.ProblemTypeName,
      actual: body.Type
    );
  }

  [Theory]
  [InlineData(VersionCheckType.FluxKustomization)]
  public async Task RecordServiceVersion_AllCheckTypes_Success(VersionCheckType testType) {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow;

    var response = await
      this.Fixture
        .CreateAdminRequest($"/api/v2/version/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}")
        .AddHeader(name: "Accept", value: "application/json")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceVersion(
            timestamp,
            ImmutableDictionary<VersionCheckType, String>.Empty
              .Add(testType, TestVersion1Value)));
        })
        .PostAsync();

    // 200, 201, 204 would all be ok
    Assert.True(
      response.IsSuccessStatusCode,
      userMessage: $"Expected a success response code (2xx). Actual: {(Int32)response.StatusCode}"
    );

    // Verify the data got created
    var getResponse = await
      this.Fixture.Server
        .CreateRequest($"/api/v2/version/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceVersionDetails[]>(
      SerializerOptions);

    Assert.NotNull(body);

    var rootServiceVersionDetails = Assert.Single(body);

    Assert.Equal(rootServiceVersionDetails.Timestamp, timestamp.TruncateNanoseconds());
    Assert.Equal(rootServiceVersionDetails.VersionType, testType);
    Assert.Equal(TestVersion1Value, rootServiceVersionDetails.Version);
  }

  [Fact]
  public async Task RecordServiceVersion_OutdatedTimestamp() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow.AddHours(-5);

    var response = await
      this.Fixture
        .CreateAdminRequest($"/api/v2/version/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}")
        .AddHeader(name: "Accept", value: "application/json")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceVersion(
            timestamp,
            ImmutableDictionary<VersionCheckType, String>.Empty
              .Add(VersionCheckType.FluxKustomization, TestVersion1Value)));
        })
        .PostAsync();

    Assert.Equal(expected: HttpStatusCode.BadRequest,
      actual: response.StatusCode);
  }

  [Fact]
  public async Task RecordServiceVersion_InvalidFutureTimestamp() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow.AddHours(1);

    var response = await
      this.Fixture
        .CreateAdminRequest($"/api/v2/version/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}")
        .AddHeader(name: "Accept", value: "application/json")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceVersion(
            timestamp,
            ImmutableDictionary<VersionCheckType, String>.Empty
              .Add(VersionCheckType.FluxKustomization, TestVersion1Value)));
        })
        .PostAsync();

    Assert.Equal(expected: HttpStatusCode.BadRequest,
      actual: response.StatusCode);
  }

  /// <summary>
  ///   This test validates that the GetSpecificServiceVersionDetails endpoint is properly selecting
  ///   the most recent version for a service
  /// </summary>
  [Fact]
  public async Task GetSpecificServiceVersionDetails_MostRecentVersion() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var earlierTimestamp = DateTime.UtcNow.AddMinutes(-2);
    var currentTimestamp = DateTime.UtcNow;

    await this.RecordServiceVersion(
      testEnvironment,
      testTenant,
      TestRootServiceName,
      TestFluxVersionCheckType,
      earlierTimestamp,
      TestVersion1Value);

    await this.RecordServiceVersion(
      testEnvironment,
      testTenant,
      TestRootServiceName,
      TestFluxVersionCheckType,
      currentTimestamp,
      TestVersion2Value);

    // Verify the data got created
    var getResponse = await
      this.Fixture.Server
        .CreateRequest($"/api/v2/version/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceVersionDetails[]>(
      SerializerOptions);

    Assert.NotNull(body);

    var rootServiceVersionDetails = Assert.Single(body);
    Assert.Equal(rootServiceVersionDetails.Timestamp, currentTimestamp.TruncateNanoseconds());
    Assert.Equal(TestFluxVersionCheckType, rootServiceVersionDetails.VersionType);
    Assert.Equal(TestVersion2Value, rootServiceVersionDetails.Version);
  }



  private async Task RecordServiceVersion(
      String testEnvironment,
      String testTenant,
      String serviceName,
      VersionCheckType versionCheckType,
      DateTime timestamp,
      String version) {

    // Record health status
    var response = await
      this.Fixture.CreateAdminRequest(
          $"/api/v2/version/{testEnvironment}/tenants/{testTenant}/services/{serviceName}")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceVersion(
            timestamp,
            ImmutableDictionary<VersionCheckType, String>.Empty
              .Add(versionCheckType, version)));
        })
        .PostAsync();

    AssertHelper.Precondition(
      response.IsSuccessStatusCode,
      message: "Failed to record service version"
    );
  }
}
