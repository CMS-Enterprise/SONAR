using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;
using Environment = Cms.BatCave.Sonar.Data.Environment;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Tests;

public class HealthControllerIntegrationTests : ApiControllerTestsBase {
  private readonly ITestOutputHelper _testOutputHelper;
  private const String TestRootServiceName = "TestRootService";
  private const String TestChildServiceName = "TestChildService";
  private const String TestHealthCheckName = "TestHealthCheck";
  private const String TestGrandchildServiceName = "TestGrandchildService";

  // Tag inheritance vars
  private const String TestOverrideKey = "test-override-key";
  private const String TestOverrideInitialVal = "test-override-initial-val";
  private const String TestOverriddenVal = "test-overridden-val";
  private const String TestInheritedKey = "test-inherited-key";
  private const String TestInheritedVal = "test-inherited-val";
  private const String TestNullTagKey = "test-null-tag-key";
  private const String TestOverridenToNullKey = "test-overridden-to-null-key";
  private const String TestOverridenToNullVal = "test-overridden-to-null-val";
  private const String TestNewTagToBeOverridenByGrandchildKey = "test-new-tag-key";
  private const String TestNewTagToBeOverridenByGrandchildInitialVal = "test-new-tag-val";
  private const String TestNewTagToBeOverridenByGrandchildNewVal = "test-overridden-by-grandchild";

  private static readonly HealthCheckModel TestHealthCheck =
    new(
      HealthControllerIntegrationTests.TestHealthCheckName,
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
        HealthControllerIntegrationTests.TestRootServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        ImmutableList.Create(HealthControllerIntegrationTests.TestHealthCheck),
        children: null)
    ),
    ImmutableHashSet<String>.Empty.Add(HealthControllerIntegrationTests.TestRootServiceName),
    null
  );

  private static readonly ServiceHierarchyConfiguration TestRootChildConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        HealthControllerIntegrationTests.TestRootServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        ImmutableList.Create(HealthControllerIntegrationTests.TestHealthCheck),
        ImmutableList.Create(TestVersionCheck),
        ImmutableHashSet<String>.Empty.Add(HealthControllerIntegrationTests.TestChildServiceName)),
      new ServiceConfiguration(
        HealthControllerIntegrationTests.TestChildServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(HealthControllerIntegrationTests.TestHealthCheck),
        children: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(HealthControllerIntegrationTests.TestRootServiceName),
    null
  );

  private static readonly ServiceHierarchyConfiguration TestGrandchildConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        HealthControllerIntegrationTests.TestRootServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        ImmutableList.Create(HealthControllerIntegrationTests.TestHealthCheck),
        ImmutableList.Create(TestVersionCheck),
        ImmutableHashSet<String>.Empty.Add(HealthControllerIntegrationTests.TestChildServiceName)),
      new ServiceConfiguration(
        HealthControllerIntegrationTests.TestChildServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(HealthControllerIntegrationTests.TestHealthCheck),
        children: ImmutableHashSet<String>.Empty.Add(HealthControllerIntegrationTests.TestGrandchildServiceName)
    ),
      new ServiceConfiguration(
        HealthControllerIntegrationTests.TestGrandchildServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(HealthControllerIntegrationTests.TestHealthCheck),
        children: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(HealthControllerIntegrationTests.TestRootServiceName),
    null
  );

  private static readonly ServiceHierarchyConfiguration TestContainerParentService = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        HealthControllerIntegrationTests.TestRootServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        null,
        ImmutableList.Create(TestVersionCheck),
        ImmutableHashSet<String>.Empty.Add(HealthControllerIntegrationTests.TestChildServiceName)),
      new ServiceConfiguration(
        HealthControllerIntegrationTests.TestChildServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(HealthControllerIntegrationTests.TestHealthCheck),
        children: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(HealthControllerIntegrationTests.TestRootServiceName),
    null
  );

  private static readonly ServiceHierarchyConfiguration TestTagInheritanceConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        TestRootServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        ImmutableList.Create(TestHealthCheck),
        ImmutableList.Create(TestVersionCheck),
        ImmutableHashSet<String>.Empty.Add(TestChildServiceName),
        new Dictionary<String, String?> {
          {TestOverridenToNullKey, TestOverridenToNullVal},
          {TestOverrideKey, TestOverriddenVal}
        }.ToImmutableDictionary()
      ),
      new ServiceConfiguration(
        TestChildServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(TestHealthCheck),
        children: ImmutableHashSet<String>.Empty.Add(TestGrandchildServiceName),
        tags: new Dictionary<String, String?> {
          {TestOverridenToNullKey, null},
          {TestNewTagToBeOverridenByGrandchildKey, TestNewTagToBeOverridenByGrandchildInitialVal}
        }.ToImmutableDictionary()
      ),
      new ServiceConfiguration(
        TestGrandchildServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(TestHealthCheck),
        tags: new Dictionary<String, String?> {
          {TestNewTagToBeOverridenByGrandchildKey, TestNewTagToBeOverridenByGrandchildNewVal}
        }.ToImmutableDictionary())
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    new Dictionary<String, String?> {
      {TestOverrideKey, TestOverrideInitialVal},
      {TestInheritedKey, TestInheritedVal},
      {TestNullTagKey, null}
    }.ToImmutableDictionary()
  );

  public HealthControllerIntegrationTests(ApiIntegrationTestFixture fixture, ITestOutputHelper outputHelper, ITestOutputHelper testOutputHelper) :
    base(fixture, outputHelper) {
    this._testOutputHelper = testOutputHelper;
  }

  private static readonly String _testBaseUrl = "http://testUrl";

  protected override void OnInitializing(WebApplicationBuilder builder) {
    base.OnInitializing(builder);
    builder.Services.RemoveAll<IOptions<WebHostConfiguration>>();
    builder.Services.AddScoped<IOptions<WebHostConfiguration>>(provider => {
      return new OptionsWrapper<WebHostConfiguration>(
        new WebHostConfiguration(
          new[] { _testBaseUrl }));
    });
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
      this.Fixture.CreateAdminRequest($"/api/v2/health/{missingEnvironmentName}/tenants/foo/services/bar")
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
      this.Fixture.CreateAdminRequest($"/api/v2/health/{existingEnvironmentName}/tenants/{missingTenantName}/services/foo")
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
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
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
      this.Fixture.CreateAdminRequest(
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
      this.Fixture.CreateAdminRequest(
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
      this.Fixture.CreateAdminRequest(
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
      this.Fixture.CreateAdminRequest(
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
      expected: BadRequestException.DefaultProblemTypeName,
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
      this.Fixture.CreateAdminRequest(
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
      this.Fixture.CreateAdminRequest(
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
      expected: BadRequestException.DefaultProblemTypeName,
      actual: body.Type
    );
  }

  [Fact]
  public async Task RecordServiceHealth_InvalidFutureTimestamp() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthControllerIntegrationTests.TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow.AddHours(1);

    // Record health status
    var response = await
      this.Fixture.CreateAdminRequest(
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
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHierarchyConfiguration(
            ImmutableArray<ServiceConfiguration>.Empty,
            ImmutableHashSet<String>.Empty,
            null
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

  /// <summary>
  ///   This test validates that the GetServiceHierarchyHealth endpoint properly handles the case where
  ///   a parent service does not have any health checks, but its aggregate status is derived from
  ///   its children's health checks
  /// </summary>
  [Fact]
  public async Task GetServiceHierarchyHealth_ContainerParentServiceAggregation() {

    var expectedAggregateStatus = HealthStatus.Degraded;
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthControllerIntegrationTests.TestContainerParentService);

    var timestamp = DateTime.UtcNow;

    await this.RecordServiceHealth(
      testEnvironment,
      testTenant,
      HealthControllerIntegrationTests.TestChildServiceName,
      HealthControllerIntegrationTests.TestHealthCheckName,
      timestamp,
      expectedAggregateStatus);

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
    Assert.Equal(expectedAggregateStatus, childServiceHealth.AggregateStatus);
    Assert.NotNull(childServiceHealth.HealthChecks);
    var childHealthCheck = Assert.Single(childServiceHealth.HealthChecks);
    Assert.Equal(expected: HealthControllerIntegrationTests.TestHealthCheckName, actual: childHealthCheck.Key);
    Assert.Equal(expected: (expectedTimestamp, expectedAggregateStatus), actual: childHealthCheck.Value);

    // The root service should have the expected aggregate status
    Assert.Equal(expectedTimestamp, serviceHealth.Timestamp);
    Assert.Equal(expectedAggregateStatus, serviceHealth.AggregateStatus);

    // The root health check should no health checks
    Assert.Empty(serviceHealth.HealthChecks);
  }

  /// <summary>
  ///   This test validates that the GetServiceHierarchyHealth endpoint evaluates the root aggregate status when
  ///   the grandchild's reported status is the worst.
  /// </summary>
  [Fact]
  public async Task GetServiceHierarchyHealth_GrandchildStatusAggregation() {

    var rootStatus = HealthStatus.Online;
    var childStatus = HealthStatus.Degraded;
    var grandchildStatus = HealthStatus.Offline;

    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthControllerIntegrationTests.TestGrandchildConfiguration);

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

    await this.RecordServiceHealth(
      testEnvironment,
      testTenant,
      HealthControllerIntegrationTests.TestGrandchildServiceName,
      HealthControllerIntegrationTests.TestHealthCheckName,
      timestamp,
      grandchildStatus);

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
    var expectedTimestamp = timestamp.TruncateNanoseconds();

    // The grandchild service and health check should have the recorded status
    Assert.NotNull(childServiceHealth.Children);
    var grandchildServiceHealth = Assert.Single(childServiceHealth.Children);
    Assert.NotNull(grandchildServiceHealth);
    Assert.Equal(expectedTimestamp, grandchildServiceHealth.Timestamp);
    Assert.Equal(grandchildStatus, grandchildServiceHealth.AggregateStatus);
    Assert.NotNull(grandchildServiceHealth.HealthChecks);
    var grandchildHealthCheck = Assert.Single(grandchildServiceHealth.HealthChecks);
    Assert.Equal(expected: HealthControllerIntegrationTests.TestHealthCheckName, actual: grandchildHealthCheck.Key);
    Assert.Equal(expected: (expectedTimestamp, grandchildStatus), actual: grandchildHealthCheck.Value);

    // The child service
    Assert.NotNull(childServiceHealth);
    Assert.Equal(expectedTimestamp, childServiceHealth.Timestamp);
    Assert.Equal(grandchildStatus, childServiceHealth.AggregateStatus);

    // The root service should have the expected aggregate status
    Assert.Equal(expectedTimestamp, serviceHealth.Timestamp);
    Assert.Equal(grandchildStatus, serviceHealth.AggregateStatus);
  }

  #region Tag Inheritance and Resolution Tests

  //****************************************************************************
  //
  //                     Tag Inheritance and Resolution
  //
  //****************************************************************************

  // Update tags
  // Initial configuration tags:
  //  Tenant tags:
  //    TestOverrideKey: TestOverrideInitialVal
  //    TestInheritedKey: TestInheritedVal
  //    TestNullTagKey: null
  //  Root service tags:
  //    TestOverriddenToNullKey: TestOverriddenToNullVal
  //    TestOverrideKey: TestOverriddenVal
  //  Child service tags:
  //    TestOverriddenToNullKey: null,
  //    TestNewTagToBeOverridenByGrandchildKey: TestNewTagToBeOverridenByGrandchildInitialVal
  //  Grandchild service tags:
  //    TestNewTagToBeOverridenByGrandchildKey: TestNewTagToBeOverridenByGrandchildNewVal
  //  Resolved Tags
  //    Root service tags:
  //      TestOverrideKey: TestOverriddenVal
  //      TestInheritedKey: TestInheritedVal
  //      TestOverriddenToNullKey: TestOverriddenToNullVal
  //    Child service tags:
  //      TestInheritedKey: TestInheritedVal
  //      TestNewTagToBeOverridenByGrandchildKey: TestNewTagToBeOverridenByGrandchildInitialVal
  //    Grandchild service tags:
  //      TestInheritedKey: TestInheritedVal
  //      TestNewTagToBeOverridenByGrandchildKey: TestNewTagToBeOverridenByGrandchildNewVal
  //  Scenarios covered (corresponding scenario number denoted in code):
  //    1. Normal inheritance (TestInheritedKey present in tenant tags and inherited down to grandchild service)
  //    2. Normal override (TestOverrideKey present in tenant tags and overridden by root service)
  //    3. Tag removal (TestNullTagKey present in tenant keys and removed in root service and children)
  //    4. Tag overridden to null and removed (TestOverriddenToNullKey is non-null and present in root service but
  //       overridden to null in child service, consequently being removed during tag resolution)
  //    5. New tag added, overridden by descendent

  [Fact]
  public async Task GetServiceHierarchyHealth_TagInheritance_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestTagInheritanceConfiguration);

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
    var rootService = Assert.Single(body);

    // setup
    Assert.NotNull(rootService);
    Assert.NotNull(rootService.Children);
    var rootServiceTags = rootService.Tags;
    Assert.NotNull(rootServiceTags);
    var childService = Assert.Single(rootService.Children);
    Assert.NotNull(childService);
    Assert.NotNull(childService.Children);
    var childServiceTags = childService.Tags;
    Assert.NotNull(childServiceTags);
    var grandchildService = Assert.Single(childService.Children);
    Assert.NotNull(grandchildService);
    var grandchildServiceTags = grandchildService.Tags;
    Assert.NotNull(grandchildServiceTags);

    // scenario 1
    var inheritedTagRootService = Assert.Contains(TestInheritedKey, rootServiceTags);
    Assert.Equal(expected: TestInheritedVal, actual: inheritedTagRootService);
    var inheritedTagChildService = Assert.Contains(TestInheritedKey, childServiceTags);
    Assert.Equal(expected: TestInheritedVal, actual: inheritedTagChildService);
    var inheritedTagGrandchildService = Assert.Contains(TestInheritedKey, grandchildServiceTags);
    Assert.Equal(expected: TestInheritedVal, actual: inheritedTagGrandchildService);

    // scenario 2
    var overriddenTagRootServiceVal = Assert.Contains(TestOverrideKey, rootServiceTags);
    Assert.Equal(expected: TestOverriddenVal, actual: overriddenTagRootServiceVal);

    // scenario 3
    Assert.DoesNotContain(TestNullTagKey, rootServiceTags);

    // scenario 4
    var rootServiceNullTagVal = Assert.Contains(TestOverridenToNullKey, rootServiceTags);
    Assert.Equal(expected: TestOverridenToNullVal, actual: rootServiceNullTagVal);
    Assert.DoesNotContain(TestOverridenToNullKey, childServiceTags);

    // scenario 5
    // test that root service doesn't contain new tag, will be added by child and overridden by grandchild
    Assert.DoesNotContain(TestNewTagToBeOverridenByGrandchildKey, rootServiceTags);
    var newTagValChild = Assert.Contains(TestNewTagToBeOverridenByGrandchildKey, childServiceTags);
    Assert.Equal(expected: TestNewTagToBeOverridenByGrandchildInitialVal, actual: newTagValChild);
    var newTagValGrandchild =
      Assert.Contains(TestNewTagToBeOverridenByGrandchildKey, grandchildServiceTags);
    Assert.Equal(expected: TestNewTagToBeOverridenByGrandchildNewVal, actual: newTagValGrandchild);
  }

  // Query only the grandchild service, confirm tag resolution
  [Fact]
  public async Task GetSpecificServiceHierarchyHealth_TagInheritance_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestTagInheritanceConfiguration);

    var getResponse = await
      this.Fixture.Server.CreateRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}/{TestChildServiceName}/{TestGrandchildServiceName}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyHealth[]>(
      HealthControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    var grandchildService = Assert.Single(body);

    Assert.NotNull(grandchildService);
    var grandchildServiceTags = grandchildService.Tags;
    Assert.NotNull(grandchildServiceTags);

    var inheritedTagGrandchildService = Assert.Contains(TestInheritedKey, grandchildServiceTags);
    Assert.Equal(expected: TestInheritedVal, actual: inheritedTagGrandchildService);
  }

  #endregion

  #region Authentication and Authorization Tests

  //****************************************************************************
  //
  //                     Authentication and Authorization
  //
  //****************************************************************************

  [Fact]
  public async Task RecordServiceHealth_Auth_GlobalAdmin_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow;

    // Record health status
    var response = await
      this.Fixture.CreateAdminRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHealth(
            timestamp,
            HealthStatus.Online,
            ImmutableDictionary<String, HealthStatus>.Empty
              .Add(TestHealthCheckName, HealthStatus.Online)
          ));
        })
        .PostAsync();

    // 200, 201, 204 would all be ok
    Assert.True(
      response.IsSuccessStatusCode,
      userMessage: $"Expected a success response code (2xx). Actual: {(Int32)response.StatusCode}"
    );
  }

  [Fact]
  public async Task RecordServiceHealth_Auth_EnvironmentAdmin_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow;

    // Record health status
    var response = await
      this.Fixture.CreateAuthenticatedRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}",
          PermissionType.Admin,
          testEnvironment)
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHealth(
            timestamp,
            HealthStatus.Online,
            ImmutableDictionary<String, HealthStatus>.Empty
              .Add(TestHealthCheckName, HealthStatus.Online)
          ));
        })
        .PostAsync();

    // 200, 201, 204 would all be ok
    Assert.True(
      response.IsSuccessStatusCode,
      userMessage: $"Expected a success response code (2xx). Actual: {(Int32)response.StatusCode}"
    );
  }

  [Fact]
  public async Task RecordServiceHealth_Auth_TenantAdmin_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow;

    // Record health status
    var response = await
      this.Fixture.CreateAuthenticatedRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}",
          PermissionType.Admin,
          testEnvironment,
          testTenant)
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHealth(
            timestamp,
            HealthStatus.Online,
            ImmutableDictionary<String, HealthStatus>.Empty
              .Add(TestHealthCheckName, HealthStatus.Online)
          ));
        })
        .PostAsync();

    // 200, 201, 204 would all be ok
    Assert.True(
      response.IsSuccessStatusCode,
      userMessage: $"Expected a success response code (2xx). Actual: {(Int32)response.StatusCode}"
    );
  }

  [Fact]
  public async Task RecordServiceHealth_Auth_Anonymous_Unauthorized() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow;

    // Record health status
    var response = await
      this.Fixture.Server.CreateRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHealth(
            timestamp,
            HealthStatus.Online,
            ImmutableDictionary<String, HealthStatus>.Empty
              .Add(TestHealthCheckName, HealthStatus.Online)
          ));
        })
        .PostAsync();

    Assert.Equal(
      HttpStatusCode.Unauthorized,
      response.StatusCode
    );
  }

  [Fact]
  public async Task RecordServiceHealth_Auth_TenantStandard_Forbidden() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow;

    // Record health status
    var response = await
      this.Fixture.CreateAuthenticatedRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}",
          PermissionType.Standard,
          testEnvironment,
          testTenant)
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHealth(
            timestamp,
            HealthStatus.Online,
            ImmutableDictionary<String, HealthStatus>.Empty
              .Add(TestHealthCheckName, HealthStatus.Online)
          ));
        })
        .PostAsync();

    Assert.Equal(
      HttpStatusCode.Forbidden,
      response.StatusCode
    );
  }

  [Fact]
  public async Task RecordServiceHealth_Auth_OtherTenantAdmin_Forbidden() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);
    var otherTenant = Guid.NewGuid().ToString();
    await this.CreateTestConfiguration(testEnvironment, otherTenant, TestRootOnlyConfiguration);

    var timestamp = DateTime.UtcNow;

    // Record health status
    var response = await
      this.Fixture.CreateAuthenticatedRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}",
          PermissionType.Standard,
          testEnvironment,
          otherTenant)
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHealth(
            timestamp,
            HealthStatus.Online,
            ImmutableDictionary<String, HealthStatus>.Empty
              .Add(TestHealthCheckName, HealthStatus.Online)
          ));
        })
        .PostAsync();

    Assert.Equal(
      HttpStatusCode.Forbidden,
      response.StatusCode
    );
  }

  [Fact]
  public async Task GetServiceHierarchyHealth_Auth_Anonymous_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/health/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);
  }

  [Fact]
  public async Task GetServiceHierarchyHealth_Auth_EnvironmentStandard_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var getResponse = await
      this.Fixture.CreateAuthenticatedRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}",
          PermissionType.Standard,
          testEnvironment)
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);
  }

  [Fact(Skip = "BATAPI-: GetServiceHierarchyHealth should enforce ApiKey scope")]
  public async Task GetServiceHierarchyHealth_Auth_OtherEnvironmentStandard_Forbidden() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);
    var (otherEnvironment, _) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var getResponse = await
      this.Fixture.CreateAuthenticatedRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}",
          PermissionType.Standard,
          otherEnvironment)
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.Forbidden,
      actual: getResponse.StatusCode);
  }

  [Fact]
  public async Task GetSpecificServiceHierarchyHealth_Auth_Anonymous_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var getResponse = await
      this.Fixture.Server.CreateRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);
  }

  [Fact]
  public async Task GetSpecificServiceHierarchyHealth_Auth_EnvironmentStandard_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    var getResponse = await
      this.Fixture.CreateAuthenticatedRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}",
          PermissionType.Standard,
          testEnvironment)
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);
  }

  [Fact(Skip = "BATAPI-: GetServiceHierarchyHealth should enforce ApiKey scope")]
  public async Task GetSpecificServiceHierarchyHealth_Auth_OtherEnvironmentStandard_Forbidden() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);
    var (otherEnvironment, _) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var getResponse = await
      this.Fixture.CreateAuthenticatedRequest(
          $"/api/v2/health/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}",
          PermissionType.Standard,
          otherEnvironment)
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.Forbidden,
      actual: getResponse.StatusCode);
  }

  #endregion

  #region Dashboard Link Tests
  [Fact]
  public async Task GetServiceHierarchyHealth_DashboardLinkTest() {

    var rootStatus = HealthStatus.Online;
    var childStatus = HealthStatus.Degraded;
    var grandchildStatus = HealthStatus.Offline;

    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(HealthControllerIntegrationTests.TestGrandchildConfiguration);

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

    await this.RecordServiceHealth(
      testEnvironment,
      testTenant,
      HealthControllerIntegrationTests.TestGrandchildServiceName,
      HealthControllerIntegrationTests.TestHealthCheckName,
      timestamp,
      grandchildStatus);

    // build expected dashboard links
    var expectedRootServiceLink = $"{_testBaseUrl}/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}";
    var expectedChildServiceLink = $"{_testBaseUrl}/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}/{TestChildServiceName}";
    var expectedGrandchildServiceLink = $"{_testBaseUrl}/{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}/{TestChildServiceName}/{TestGrandchildServiceName}";

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
    Assert.Equal(expected: expectedRootServiceLink, actual: serviceHealth.DashboardLink);

    Assert.NotNull(serviceHealth.Children);
    var childServiceHealth = Assert.Single(serviceHealth.Children);

    // Test child service dashboard link
    Assert.NotNull(childServiceHealth);
    Assert.Equal(expected: expectedChildServiceLink, actual: childServiceHealth.DashboardLink);

    // Test grandchild service dashboard link
    Assert.NotNull(childServiceHealth.Children);
    var grandchildServiceHealth = Assert.Single(childServiceHealth.Children);
    Assert.Equal(expected: expectedGrandchildServiceLink, actual: grandchildServiceHealth.DashboardLink);

  }


  #endregion

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
