using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Json;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Environment = Cms.BatCave.Sonar.Data.Environment;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Tests;

public class ConfigurationControllerIntegrationTests : ApiControllerTestsBase {
  private const String TestRootServiceName = "TestRootService";
  private const String TestChildServiceName = "TestChildService";
  private const String TestHealthCheckName = "TestHealthCheck";
  private const String TestHttpHealthCheckName = "TestHttpHealthCheck";

  private const String TestRootServiceNameCasingMismatch = "tESTrOOTsERVICE";
  private const String TestServiceNameOver100Char =
    "ServiceNameServiceNameServiceNameServiceNameServiceNameServiceNameServiceNameServiceNameServiceNameService";

  private const String TestHttpServiceName = "TestHttpService";
  private const String TestServiceNameNotUrlSafe = "Test_Service!";

  private const String TestNoRootServiceMatch =
    "One or more of the specified root services do not exist in the services array.";
  private const String TestNoChildServiceMatch =
    "One or more of the specified services contained a reference to a child service that did not exist in the services array.";
  private const String TestServiceNameMaxLength = "The field Name must be a string with a maximum length of 100.";
  private const String TestServiceNameDuplicated =
    "The specified list of services contained multiple services with the same name.";

  private const String TestFluxKustomizationVersionCheckK8sNamespace = "test";
  private const String TestFluxKustomizationVersionCheckKustomization = "test";

  private const String TestTenantTagKey = "test-tenant-tag-key";
  private const String TestTenantTagVal = "test-tenant-tag-val";
  private const String TestNullTenantTagKey = "test-null-tenant-tag-key";
  private const String TestServiceTagKey = "test-service-tag-key";
  private const String TestServiceTagVal = "test-service-tag-val";
  private const String TestNullServiceTagKey = "test-null-service-tag-key";

  private const String TestNewTenantTagKey = "test-new-tenant-tag-key";
  private const String TestNewTenantTagVal = "test-new-tenant-tag-val";
  private const String TestUpdatedTenantTagVal = "test-updated-tenant-tag-val";
  private const String TestUpdatedServiceTagVal = "test-updated-service-tag-val";
  private const String TestUpdateServiceTagToNullKey = "test-update-to-null-key";
  private const String TestUpdateServiceTagToNullVal = "test-update-to-null-val";
  private const String TestUpdateServiceTagNoChangeKey = "no-change-key";
  private const String TestUpdateServiceTagNoChangeVal = "no-change-val";
  private const String TestUpdateServiceTagRemoveKey = "removed-key";
  private const String TestUpdateServiceTagRemoveVal = "removed-val";

  private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions {
    Converters = { new JsonStringEnumConverter(), new ArrayTupleConverterFactory() },
    PropertyNameCaseInsensitive = true
  };

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

  private static readonly HealthCheckModel TestHttpHealthCheck =
    new(
      TestHttpHealthCheckName,
      description: "Http Health Check Description",
      HealthCheckType.HttpRequest,
      new HttpHealthCheckDefinition(
        url: new Uri("http://httpHealthCheck"),
        Array.Empty<HttpHealthCheckCondition>(),
        followRedirects: false,
        authorizationHeader: "Authorization Header Value",
        skipCertificateValidation: null
      ),
      null
    );

  private static readonly VersionCheckModel TestVersionCheck =
    new(
      VersionCheckType.FluxKustomization,
      new FluxKustomizationVersionCheckDefinition(
        TestFluxKustomizationVersionCheckK8sNamespace,
        TestFluxKustomizationVersionCheckKustomization)
    );

  private static readonly ServiceHierarchyConfiguration TestRootChildConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        TestRootServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        ImmutableList.Create(TestHealthCheck),
        ImmutableList.Create(TestVersionCheck),
        ImmutableHashSet<String>.Empty.Add(TestChildServiceName),
        null),
      new ServiceConfiguration(
        TestChildServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(TestHealthCheck),
        children: null,
        tags: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    null
  );

  private static readonly ServiceHierarchyConfiguration TestNoRootServiceMatchConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        TestChildServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        ImmutableList.Create(TestHealthCheck),
        children: null,
        tags: null)
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    null
  );

  private static readonly ServiceHierarchyConfiguration TestRootServiceCasingMismatchConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        TestRootServiceNameCasingMismatch,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(TestHealthCheck),
        children: null,
        tags: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    null
  );

  private static readonly ServiceHierarchyConfiguration TestDiffServiceDiffCasingConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        TestRootServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(TestHealthCheck),
        children: null,
        tags: null
      ),
      new ServiceConfiguration(
        TestRootServiceNameCasingMismatch,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(TestHealthCheck),
        children: null,
        tags: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    null
  );

  private static readonly ServiceHierarchyConfiguration TestNoChildServiceMatchConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        TestRootServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        ImmutableList.Create(TestHealthCheck),
        ImmutableList.Create(TestVersionCheck),
        ImmutableHashSet<String>.Empty.Add(TestChildServiceName),
        null)
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    null
  );

  private static readonly ServiceHierarchyConfiguration TestDuplicateServiceNamesConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        TestRootServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(TestHealthCheck),
        versionChecks: ImmutableList.Create(TestVersionCheck),
        children: ImmutableHashSet<String>.Empty.Add(TestChildServiceName),
        tags: null),
      new ServiceConfiguration(
        TestChildServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(TestHealthCheck),
        children: null
      ),
      new ServiceConfiguration(
        TestChildServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(TestHealthCheck),
        children: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    null
  );

  private static readonly ServiceHierarchyConfiguration TestServiceNameOver100CharConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        TestServiceNameOver100Char,
        displayName: "Display Name",
        description: null,
        url: null,
        ImmutableList.Create(TestHealthCheck),
        children: null)
    ),
    ImmutableHashSet<String>.Empty.Add(TestServiceNameOver100Char),
    null
  );

  private static readonly ServiceHierarchyConfiguration TestHttpConditionAuthConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        TestHttpServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        ImmutableList.Create(TestHttpHealthCheck),
        ImmutableList.Create(TestVersionCheck),
        children: null)
    ),
    ImmutableHashSet<String>.Empty.Add(TestHttpServiceName),
    null
  );

  private static readonly ServiceHierarchyConfiguration TestConfigurationWithoutVersionChecks = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        TestHttpServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(TestHttpHealthCheck),
        children: null)
    ),
    ImmutableHashSet<String>.Empty.Add(TestHttpServiceName),
    null
  );

  private static readonly ServiceHierarchyConfiguration TestCreateTagsConfiguration = new(
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
          {TestServiceTagKey, TestServiceTagVal},
          {TestNullServiceTagKey, null},
        }.ToImmutableDictionary()
        ),
      new ServiceConfiguration(
        TestChildServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(TestHealthCheck),
        children: null,
        tags: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    new Dictionary<String, String?> {
      {TestTenantTagKey, TestTenantTagVal},
      {TestNullTenantTagKey, null},
    }.ToImmutableDictionary()
  );

  private static readonly ServiceHierarchyConfiguration TestTagsInitialConfiguration = new(
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
          {TestServiceTagKey, TestServiceTagVal},
          {TestUpdateServiceTagNoChangeKey, TestUpdateServiceTagNoChangeVal},
          {TestUpdateServiceTagToNullKey, TestUpdateServiceTagToNullVal},
          {TestUpdateServiceTagRemoveKey, TestUpdateServiceTagRemoveVal},
        }.ToImmutableDictionary()
      ),
      new ServiceConfiguration(
        TestChildServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(TestHealthCheck),
        children: null,
        tags: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    new Dictionary<String, String?> {
      {TestTenantTagKey, TestTenantTagVal}
    }.ToImmutableDictionary()
  );

  private static readonly ServiceHierarchyConfiguration TestUpdatedTagsConfiguration = new(
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
          {TestServiceTagKey, TestUpdatedServiceTagVal},
          {TestUpdateServiceTagNoChangeKey, TestUpdateServiceTagNoChangeVal},
          {TestUpdateServiceTagToNullKey, null}
        }.ToImmutableDictionary()
      ),
      new ServiceConfiguration(
        TestChildServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(TestHealthCheck),
        children: null,
        tags: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    new Dictionary<String, String?> {
      {TestTenantTagKey, TestUpdatedTenantTagVal},
      {TestNewTenantTagKey, TestNewTenantTagVal},
    }.ToImmutableDictionary()
  );

  private static readonly ServiceHierarchyConfiguration TestTagsConfigurationInvalidTenantTags = new(
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
          {TestServiceTagKey, TestServiceTagVal},
          {TestNullServiceTagKey, null},
        }.ToImmutableDictionary()
      ),
      new ServiceConfiguration(
        TestChildServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(TestHealthCheck),
        children: null,
        tags: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    new Dictionary<String, String?> {
      {"", TestTenantTagVal},
      {TestNullTenantTagKey, null},
    }.ToImmutableDictionary()
  );

  private static readonly ServiceHierarchyConfiguration TestTagsConfigurationInvalidServiceTags = new(
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
          {" ", TestServiceTagVal},
          {TestNullServiceTagKey, null},
        }.ToImmutableDictionary()
      ),
      new ServiceConfiguration(
        TestChildServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(TestHealthCheck),
        children: null,
        tags: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    new Dictionary<String, String?> {
      {TestTenantTagKey, TestTenantTagVal},
      {TestNullTenantTagKey, null},
    }.ToImmutableDictionary()
  );

  private static readonly ServiceHierarchyConfiguration TestTagsConfigurationBatchUpdateInitial = new(
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
          {TestServiceTagKey, TestServiceTagVal},
        }.ToImmutableDictionary()
      ),
      new ServiceConfiguration(
        TestChildServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(TestHealthCheck),
        children: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    new Dictionary<String, String?> {
      {TestTenantTagKey, TestTenantTagVal},
      {TestNullTenantTagKey, null},
    }.ToImmutableDictionary()
  );

  private static readonly ServiceHierarchyConfiguration TestTagsConfigurationBatchUpdateUpdated = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        TestRootServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        ImmutableList.Create(TestHealthCheck),
        ImmutableList.Create(TestVersionCheck),
        ImmutableHashSet<String>.Empty.Add(TestChildServiceName),
        null
      ),
      new ServiceConfiguration(
        TestChildServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: ImmutableList.Create(TestHealthCheck),
        children: null,
        tags: new Dictionary<String, String?> {
          {TestServiceTagKey, TestServiceTagVal},
        }.ToImmutableDictionary()
      )
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    new Dictionary<String, String?> {
      {TestTenantTagKey, TestTenantTagVal},
      {TestNullTenantTagKey, null},
    }.ToImmutableDictionary()
  );

  public ConfigurationControllerIntegrationTests(ApiIntegrationTestFixture fixture, ITestOutputHelper outputHelper) :
    base(fixture, outputHelper) { }

  // GetConfiguration scenarios
  //    Missing Environment - 404
  //    Missing Tenant - 404
  //    Config found but no Service - 200
  //    Config found with Services - 200

  [Fact]
  public async Task GetConfiguration_MissingEnvironmentReturnsNotFound() {
    var missingEnvironmentName = Guid.NewGuid().ToString();
    var response = await
      this.Fixture.Server.CreateRequest($"/api/v2/config/{missingEnvironmentName}/tenants/foo")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

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
    Assert.Equal(
      expected: nameof(Environment),
      actual: body.Extensions["resourceType"].ToString()
    );
    Assert.Equal(
      expected: missingEnvironmentName,
      actual: body.Extensions["resourceId"].ToString()
    );
  }

  [Fact]
  public async Task GetConfiguration_MissingTenantReturnsNotFound() {
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
      this.Fixture.Server.CreateRequest($"/api/v2/config/{existingEnvironmentName}/tenants/{missingTenantName}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

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
    Assert.Equal(
      expected: nameof(Tenant),
      actual: body.Extensions["resourceType"].ToString()
    );
    Assert.Equal(
      expected: missingTenantName,
      actual: body.Extensions["resourceId"].ToString()
    );
  }

  [Fact]
  public async Task GetConfiguration_ConfigWithoutServiceReturnsOk() {
    // Create Tenant Configuration
    // var testEnvironment = Guid.NewGuid().ToString();
    // var testTenant = Guid.NewGuid().ToString();

    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(new ServiceHierarchyConfiguration(
        ImmutableArray<ServiceConfiguration>.Empty,
        ImmutableHashSet<String>.Empty,
        null));

    var getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyConfiguration>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Empty(body.Services);
    Assert.Empty(body.RootServices);
  }

  [Fact]
  public async Task GetConfiguration_ConfigWithServicesReturnsOk() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootChildConfiguration);

    var getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyConfiguration>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Single(body.RootServices);
    Assert.Equal(
      expected: TestRootChildConfiguration.Services.Count,
      actual: body.Services.Count);
  }

  // CreateConfiguration scenarios
  //    Root Service does not exist as a Service - 400
  //    Referenced Child Service does not exist as a Service - 400
  //    Duplicate names in the service array - 400
  //    Service name length exceeds limit - 400
  //    Service name character is not URL safe - 400
  //    Tenant already exists - 409
  //    Root Service list casing mismatch - 201
  //    Successful configuration - 201 (this is in unit tests that call CreateTestConfiguration method)

  [Fact]
  public async Task CreateConfiguration_NoRootServiceMatchReturnsBadRequest() {
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestNoRootServiceMatchConfiguration);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: createConfigResponse.StatusCode);

    var body = await createConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(expected: "One or more validation errors occurred.", actual: body.Title);
    Assert.Contains(body.Extensions, filter: kvp => kvp is { Key: "errors", Value: JsonElement });
    var errors = ((JsonElement)body.Extensions["errors"]!).Deserialize<Dictionary<String, String[]>>()!;
    Assert.Contains(errors, kvp => kvp.Key == "RootServices" && kvp.Value.Contains(TestNoRootServiceMatch));
  }

  [Fact]
  public async Task CreateConfiguration_NoChildServiceMatchReturnsBadRequest() {
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestNoChildServiceMatchConfiguration);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: createConfigResponse.StatusCode);

    var body = await createConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(expected: "One or more validation errors occurred.", actual: body.Title);
    Assert.Contains(body.Extensions, filter: kvp => kvp is { Key: "errors", Value: JsonElement });
    var errors = ((JsonElement)body.Extensions["errors"]!).Deserialize<Dictionary<String, String[]>>()!;
    Assert.Contains(errors, kvp => kvp.Key == "Services" && kvp.Value.Contains(TestNoChildServiceMatch));
  }

  [Fact]
  public async Task CreateConfiguration_MultipleServicesWithSameNameReturnsBadRequest() {
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestDuplicateServiceNamesConfiguration);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: createConfigResponse.StatusCode);

    var body = await createConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(expected: "One or more validation errors occurred.", actual: body.Title);
    Assert.Contains(body.Extensions, filter: kvp => kvp is { Key: "errors", Value: JsonElement });
    var errors = ((JsonElement)body.Extensions["errors"]!).Deserialize<Dictionary<String, String[]>>()!;
    Assert.Contains(errors, kvp => kvp.Key == "Services" && kvp.Value.Contains(TestServiceNameDuplicated));
  }

  [Fact]
  public async Task CreateConfiguration_ServiceNameLengthExceededReturnsBadRequest() {
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestServiceNameOver100CharConfiguration);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: createConfigResponse.StatusCode);

    var body = await createConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.True(body.Extensions.ContainsKey("errors"));

    var errorsObj = Assert.IsType<JsonElement>(body.Extensions["errors"]);
    Assert.True(errorsObj.TryGetProperty("Services[0].Name", out var serviceNameErrorsVal));
    var serviceNameErrorsArray = Assert.IsType<JsonElement>(serviceNameErrorsVal);
    Assert.Equal(JsonValueKind.Array, serviceNameErrorsArray.ValueKind);
    Assert.Equal(1, serviceNameErrorsArray.GetArrayLength());
    Assert.Equal(
      expected: TestServiceNameMaxLength,
      actual: GetExtensionValue<String>(serviceNameErrorsArray[0]));
  }

  [Fact]
  public async Task CreateConfiguration_ServiceNameNotUrlSafeReturnsBadRequest() {
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestServiceNameNotUrlSafe);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: createConfigResponse.StatusCode);
  }

  [Fact]
  public async Task CreateConfiguration_TenantAlreadyExistsReturnsConflict() {
    var (testEnvironment, testTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    // Attempt to create configuration with services for existing Tenant
    var createConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestRootChildConfiguration);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.Conflict,
      actual: createConfigResponse.StatusCode);

    var body = await createConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: $"The specified {nameof(Environment)} and {nameof(Tenant)} already exist. Use PUT to update.",
      actual: body.Title
    );
  }

  [Fact]
  public async Task CreateConfiguration_RootServiceCasingMismatchReturnsCreated() {
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestRootServiceCasingMismatchConfiguration);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.Created,
      actual: createConfigResponse.StatusCode);
  }

  [Fact]
  public async Task CreateConfiguration_DiffServicesDiffCasingReturnsBadRequest() {
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestDiffServiceDiffCasingConfiguration);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: createConfigResponse.StatusCode);

    var body = await createConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(expected: "One or more validation errors occurred.", actual: body.Title);
    Assert.Contains(body.Extensions, filter: kvp => kvp is { Key: "errors", Value: JsonElement });
    var errors = ((JsonElement)body.Extensions["errors"]!).Deserialize<Dictionary<String, String[]>>()!;
    Assert.Contains(errors, kvp => kvp.Key == "Services" && kvp.Value.Contains(TestServiceNameDuplicated));
  }

  // UpdateConfiguration scenarios
  //    Root Service does not exist as a Service - 400
  //    Referenced Child Service does not exist as a Service - 400
  //    Service name length exceeds limit - 400
  //    Service name character is not URL safe - 400
  //    Missing Environment - 404
  //    Missing Tenant - 404
  //    Root Service list casing mismatch - 200
  //    Config updated successfully - 200

  [Fact]
  public async Task UpdateConfiguration_NoRootServiceMatchReturnsBadRequest() {
    var (testEnvironment, testTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestNoRootServiceMatchConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: updateConfigResponse.StatusCode);

    var body = await updateConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(expected: "One or more validation errors occurred.", actual: body.Title);
    Assert.Contains(body.Extensions, filter: kvp => kvp is { Key: "errors", Value: JsonElement });
    var errors = ((JsonElement)body.Extensions["errors"]!).Deserialize<Dictionary<String, String[]>>()!;
    Assert.Contains(errors, kvp => kvp.Key == "RootServices" && kvp.Value.Contains(TestNoRootServiceMatch));
  }

  [Fact]
  public async Task UpdateConfiguration_NoChildServiceMatchReturnsBadRequest() {
    var (testEnvironment, testTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestNoChildServiceMatchConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: updateConfigResponse.StatusCode);

    var body = await updateConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(expected: "One or more validation errors occurred.", actual: body.Title);
    Assert.Contains(body.Extensions, filter: kvp => kvp is { Key: "errors", Value: JsonElement });
    var errors = ((JsonElement)body.Extensions["errors"]!).Deserialize<Dictionary<String, String[]>>()!;
    Assert.Contains(errors, kvp => kvp.Key == "Services" && kvp.Value.Contains(TestNoChildServiceMatch));
  }

  [Fact]
  public async Task UpdateConfiguration_ServiceNameLengthExceededReturnsBadRequest() {
    var (existingEnvironment, existingTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestServiceNameOver100CharConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: updateConfigResponse.StatusCode);

    var body = await updateConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.True(body.Extensions.ContainsKey("errors"));
    var errorsObj = Assert.IsType<JsonElement>(body.Extensions["errors"]);
    Assert.True(errorsObj.TryGetProperty("Services[0].Name", out var serviceNameErrorsVal));
    var serviceNameErrorsArray = Assert.IsType<JsonElement>(serviceNameErrorsVal);
    Assert.Equal(JsonValueKind.Array, serviceNameErrorsArray.ValueKind);
    Assert.Equal(1, serviceNameErrorsArray.GetArrayLength());
    Assert.Equal(
      expected: TestServiceNameMaxLength,
      actual: GetExtensionValue<String>(serviceNameErrorsArray[0]));
  }

  [Fact]
  public async Task UpdateConfiguration_ServiceNameNotUrlSafeReturnsBadRequest() {
    var (existingEnvironment, existingTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestServiceNameNotUrlSafe);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: updateConfigResponse.StatusCode);
  }

  [Fact]
  public async Task UpdateConfiguration_MissingEnvironmentReturnsNotFound() {
    var (existingEnvironment, existingTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var missingEnvironmentName = Guid.NewGuid().ToString();
    var updateConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{missingEnvironmentName}/tenants/foo")
        .And(req => {
          req.Content = JsonContent.Create(
            TestRootChildConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: updateConfigResponse.StatusCode);

    var body = await updateConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ResourceNotFoundException.ProblemTypeName,
      actual: body.Type
    );
    Assert.Equal(
      expected: nameof(Environment),
      actual: body.Extensions["resourceType"].ToString()
    );
    Assert.Equal(
      expected: missingEnvironmentName,
      actual: body.Extensions["resourceId"].ToString()
    );
  }

  [Fact]
  public async Task UpdateConfiguration_MissingTenantReturnsNotFound() {
    var (existingEnvironment, existingTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var missingTenantName = Guid.NewGuid().ToString();
    var updateConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{existingEnvironment}/tenants/{missingTenantName}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestRootChildConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: updateConfigResponse.StatusCode);

    var body = await updateConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ResourceNotFoundException.ProblemTypeName,
      actual: body.Type
    );
    Assert.Equal(
      expected: nameof(Tenant),
      actual: body.Extensions["resourceType"].ToString()
    );
    Assert.Equal(
      expected: missingTenantName,
      actual: body.Extensions["resourceId"].ToString()
    );
  }

  [Fact]
  public async Task UpdateConfiguration_RootServiceCasingMismatchReturnsOk() {
    var (testEnvironment, testTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestRootServiceCasingMismatchConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: updateConfigResponse.StatusCode);
  }

  [Fact]
  public async Task UpdateConfiguration_SuccessfulUpdateReturnsOk() {
    var (existingEnvironment, existingTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var getResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    // This isn't what is being tested.
    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyConfiguration>(
      SerializerOptions
    );

    // Confirm POST request had no services
    Assert.NotNull(body);
    Assert.Empty(body.Services);
    Assert.Empty(body.RootServices);

    var updateConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestRootChildConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: updateConfigResponse.StatusCode);

    getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyConfiguration>(
      SerializerOptions
    );

    // Confirm PUT request had services
    Assert.NotNull(body);
    Assert.Single(body.RootServices);
    Assert.Equal(
      expected: TestRootChildConfiguration.Services.Count,
      actual: body.Services.Count);
  }

  [Fact]
  public async Task GetConfiguration_ConfigWithAuthorizationHeaderReturnsOk() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestHttpConditionAuthConfiguration);

    var getConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getConfigResponse.StatusCode);

    var body = await getConfigResponse.Content.ReadFromJsonAsync<ServiceHierarchyConfiguration>(SerializerOptions);

    Assert.NotNull(body);
    var services = Assert.Single(body.Services);
    var httpHealthCheck = Assert.Single(services.HealthChecks);
    var httpDefinition = (HttpHealthCheckDefinition)Assert.Single(new[] { httpHealthCheck.Definition });

    Assert.Equal(
      expected: "Authorization Header Value",
      actual: httpDefinition.AuthorizationHeader
    );
  }

  [Fact]
  public async Task GetConfiguration_ConfigWithRedactedAuthorizationHeaderReturnsOk() {

    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestHttpConditionAuthConfiguration);

    var getConfigResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getConfigResponse.StatusCode);

    var body = await getConfigResponse.Content.ReadFromJsonAsync<ServiceHierarchyConfiguration>(
      SerializerOptions);

    Assert.NotNull(body);
    var services = Assert.Single(body.Services);
    var httpHealthCheck = Assert.Single(services.HealthChecks);
    var httpDefinition = (HttpHealthCheckDefinition)Assert.Single(new[] { httpHealthCheck.Definition });

    Assert.Equal(
      expected: new String(c: '*', count: 32),
      actual: httpDefinition.AuthorizationHeader
    );
  }

  #region Version Check Configuration Tests

  // Version Check Scenarios
  //    Successfully create service configuration with version check
  //    Create service configuration with no version check, proving backward compatibility

  [Fact]
  public async Task CreateVersionCheckConfiguration_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestHttpConditionAuthConfiguration);

    var getConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    var body = await getConfigResponse.Content.ReadFromJsonAsync<ServiceHierarchyConfiguration>(
      SerializerOptions);

    Assert.NotNull(body);
    var services = Assert.Single(body.Services);
    var versionCheck = Assert.Single(services.VersionChecks);
    var versionCheckDefinition = (FluxKustomizationVersionCheckDefinition)Assert.Single(new[] { versionCheck.Definition });
    Assert.Equal(TestFluxKustomizationVersionCheckK8sNamespace, versionCheckDefinition.K8sNamespace);
    Assert.Equal(TestFluxKustomizationVersionCheckKustomization, versionCheckDefinition.Kustomization);
  }

  [Fact]
  public async Task CreateConfigurationWithoutVersionChecks_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestConfigurationWithoutVersionChecks);

    var getConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    var body = await getConfigResponse.Content.ReadFromJsonAsync<ServiceHierarchyConfiguration>(
      SerializerOptions);

    Assert.NotNull(body);
    var services = Assert.Single(body.Services);
    Assert.Null(services.VersionChecks);
  }

  #endregion

  #region Tenant and Service Tag Tests

  [Fact]
  public async Task CreateConfigurationWithTags_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestCreateTagsConfiguration);

    var getResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyConfiguration>(
      SerializerOptions
    );
    Assert.NotNull(body);

    // Test tenant tags
    Assert.NotNull(body.Tags);

    var tenantTags = body.Tags;
    var tenantTagVal = Assert.Contains(TestTenantTagKey, tenantTags);
    Assert.Equal(expected: TestTenantTagVal, actual: tenantTagVal);
    var tenantTagNullVal = Assert.Contains(TestNullTenantTagKey, tenantTags);
    Assert.Null(tenantTagNullVal);

    // Test service tags
    Assert.NotNull(body.Services);
    var rootService = Assert.Single(body.Services.Where(s => s.Name == TestRootServiceName));
    Assert.NotNull(rootService.Tags);

    var rootServiceTags = rootService.Tags;
    var serviceTagVal = Assert.Contains(TestServiceTagKey, rootServiceTags);
    Assert.Equal(expected: TestServiceTagVal, actual: serviceTagVal);
    var serviceTagNullVal = Assert.Contains(TestNullServiceTagKey, rootServiceTags);
    Assert.Null(serviceTagNullVal);
  }

  // Update tags
  // Initial configuration tags:
  //  Tenant tags:
  //    TestTenantTagKey: TestTenantTagVal
  //  Root service tags:
  //    TestServiceTagKey: TestServiceTagVal
  //    TestUpdateServiceTagNoChangeKey: TestUpdateServiceTagNoChangeVal
  //    TestUpdateServiceTagToNullKey: TestUpdateServiceTagToNullVal
  //    TestUpdateServiceTagRemoveKey: TestUpdateServiceTagRemoveVal
  //  Updated configuration tags:
  //    Tenant tags:
  //      TestTenantTagKey: TestUpdatedTenantTagVal
  //      TestNewTenantTagKey: TestNewTenantTagVal
  //    Root service tags:
  //      TestServiceTagKey: TestUpdatedServiceTagVal
  //      TestUpdateServiceTagNoChangeKey: TestUpdateServiceTagNoChangeVal
  //      TestUpdateServiceTagToNullKey: null
  //  Scenarios covered (corresponding scenario number denoted in code):
  //    1. Normal update (same key -> updated value)
  //    2. Update to null value (same key/non-null value -> update to null value)
  //    3. Tag present in initial configuration -> removed in new configuration (kvp not present in new configuration)
  //    4. No update (same kvp present in both initial and new configuration)
  //    5. New tag added (kvp present in new configuration but not present in initial)
  [Fact]
  public async Task UpdateConfigurationWithTags_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestTagsInitialConfiguration);

    var getResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var updateConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestUpdatedTagsConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: updateConfigResponse.StatusCode);

    getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyConfiguration>(
      SerializerOptions
    );
    Assert.NotNull(body);

    // Test tenant tags
    Assert.NotNull(body.Tags);

    // scenario 1
    var tenantTags = body.Tags;
    var tenantTagVal = Assert.Contains(TestTenantTagKey, tenantTags);
    Assert.Equal(expected: TestUpdatedTenantTagVal, actual: tenantTagVal);
    // scenario 5
    var newTenantTagVal = Assert.Contains(TestNewTenantTagKey, tenantTags);
    Assert.Equal(expected: TestNewTenantTagVal, actual: newTenantTagVal);

    // Test service tags
    Assert.NotNull(body.Services);
    var rootService = Assert.Single(body.Services.Where(s => s.Name == TestRootServiceName));
    Assert.NotNull(rootService.Tags);
    var rootServiceTags = rootService.Tags;

    // scenario 1
    var newServiceTagVal = Assert.Contains(TestServiceTagKey, rootServiceTags);
    Assert.Equal(expected: TestUpdatedServiceTagVal, actual: newServiceTagVal);
    // scenario 4
    var noUpdateServiceTagVal = Assert.Contains(TestUpdateServiceTagNoChangeKey, rootServiceTags);
    Assert.Equal(expected: TestUpdateServiceTagNoChangeVal, actual: noUpdateServiceTagVal);
    // scenario 2
    var newNullServiceTagVal = Assert.Contains(TestUpdateServiceTagToNullKey, rootServiceTags);
    Assert.Null(newNullServiceTagVal);
    // scenario 3
    Assert.DoesNotContain(TestUpdateServiceTagRemoveKey, rootServiceTags);

  }

  // Testing full addition and removal of tags
  // Scenarios covered:
  //  1. Full removal
  //  2. Full addition
  [Fact]
  public async Task UpdateConfigurationAtomicTagUpdate_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestTagsConfigurationBatchUpdateInitial);

    var getResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var updateConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestTagsConfigurationBatchUpdateUpdated);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: updateConfigResponse.StatusCode);

    getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyConfiguration>(
      SerializerOptions
    );
    Assert.NotNull(body);

    // Test service tags
    Assert.NotNull(body.Services);
    var rootService = Assert.Single(body.Services.Where(s => s.Name == TestRootServiceName));
    // scenario 1
    Assert.NotNull(rootService.Tags);
    Assert.Empty(rootService.Tags);

    // scenario 2
    var childService = Assert.Single(body.Services.Where(s => s.Name == TestChildServiceName));
    Assert.NotNull(childService);
    Assert.NotNull(childService.Tags);
    var childServiceTags = childService.Tags;
    var childServiceTag = Assert.Single(childServiceTags);
    Assert.Equal(expected: TestServiceTagVal, actual: childServiceTag.Value);
  }

  // Tests for invalid tags.
  // An invalid tag is either a tag with an empty string key or a tag with a key that contains only whitespace.
  // When this is present in the request body's configuration, a bad request should be returned from endpoint after validation.
  [Fact]
  public async Task CreateConfigurationWithInvalidTenantTags_BadRequest() {
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestTagsConfigurationInvalidTenantTags);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: createConfigResponse.StatusCode);
  }

  [Fact]
  public async Task CreateConfigurationWithInvalidServiceTags_BadRequest() {
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestTagsConfigurationInvalidServiceTags);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: createConfigResponse.StatusCode);
  }

  [Fact]
  public async Task UpdateConfigurationWithInvalidTenantTags_BadRequest() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestTagsInitialConfiguration);

    var getResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var updateConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestTagsConfigurationInvalidTenantTags);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: updateConfigResponse.StatusCode);

    getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);
  }

  [Fact]
  public async Task UpdateConfigurationWithInvalidServiceTags_BadRequest() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestTagsInitialConfiguration);

    var getResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var updateConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestTagsConfigurationInvalidServiceTags);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: updateConfigResponse.StatusCode);

    getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);
  }

  #endregion

  #region Authentication and Authorization Tests

  //****************************************************************************
  //
  //                     Authentication and Authorization
  //
  //****************************************************************************

  [Fact]
  public async Task CreateConfiguration_GlobalAdmin_Success() {
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.Fixture
        .CreateAuthenticatedRequest(
          $"/api/v2/config/{testEnvironment}/tenants/{testTenant}",
          PermissionType.Admin)
        .And(req => {
          req.Content = JsonContent.Create(TestRootChildConfiguration);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.Created,
      actual: createConfigResponse.StatusCode);
  }

  [Fact]
  public async Task CreateConfiguration_EnvironmentAdmin_Success() {
    var testEnvironment = Guid.NewGuid().ToString();

    var createEnvResponse = await
      this.Fixture.CreateAdminRequest("/api/v2/environments")
        .And(req => {
          req.Content = JsonContent.Create(new { name = testEnvironment });
        })
        .PostAsync();

    AssertHelper.Precondition(createEnvResponse.IsSuccessStatusCode, "Error creating environment");

    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.Fixture
        .CreateAuthenticatedRequest(
          $"/api/v2/config/{testEnvironment}/tenants/{testTenant}",
          PermissionType.Admin,
          testEnvironment)
        .And(req => {
          req.Content = JsonContent.Create(TestRootChildConfiguration);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.Created,
      actual: createConfigResponse.StatusCode);
  }

  [Fact]
  public async Task CreateConfiguration_Anonymous_Unauthorized() {
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(TestRootChildConfiguration);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.Unauthorized,
      actual: createConfigResponse.StatusCode);
  }

  [Fact]
  public async Task CreateConfiguration_EnvironmentStandard_Forbidden() {
    var testEnvironment = Guid.NewGuid().ToString();

    var createEnvResponse = await
      this.Fixture.CreateAdminRequest("/api/v2/environments")
        .And(req => {
          req.Content = JsonContent.Create(new { name = testEnvironment });
        })
        .PostAsync();

    AssertHelper.Precondition(createEnvResponse.IsSuccessStatusCode, "Error creating environment");

    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.Fixture
        .CreateAuthenticatedRequest(
          $"/api/v2/config/{testEnvironment}/tenants/{testTenant}",
          PermissionType.Standard,
          testEnvironment)
        .And(req => {
          req.Content = JsonContent.Create(TestRootChildConfiguration);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.Forbidden,
      actual: createConfigResponse.StatusCode);
  }

  [Fact]
  public async Task CreateConfiguration_OtherEnvironmentAdmin_Forbidden() {
    var (otherEnvironment, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var testEnvironment = Guid.NewGuid().ToString();

    var createEnvResponse = await
      this.Fixture.CreateAdminRequest("/api/v2/environments")
        .And(req => {
          req.Content = JsonContent.Create(new { name = testEnvironment });
        })
        .PostAsync();

    AssertHelper.Precondition(createEnvResponse.IsSuccessStatusCode, "Error creating environment");

    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.Fixture
        .CreateAuthenticatedRequest(
          $"/api/v2/config/{testEnvironment}/tenants/{testTenant}",
          PermissionType.Admin,
          otherEnvironment)
        .And(req => {
          req.Content = JsonContent.Create(TestRootChildConfiguration);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.Forbidden,
      actual: createConfigResponse.StatusCode);
  }

  [Fact]
  public async Task UpdateConfiguration_Auth_BuiltInAdmin_Success() {
    var (existingEnvironment, existingTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestRootChildConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: updateConfigResponse.StatusCode);
  }

  [Fact]
  public async Task UpdateConfiguration_Auth_GlobalAdmin_Success() {
    var (existingEnvironment, existingTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.Fixture.CreateAuthenticatedRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}", PermissionType.Admin)
        .And(req => {
          req.Content = JsonContent.Create(
            TestRootChildConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: updateConfigResponse.StatusCode);
  }

  [Fact]
  public async Task UpdateConfiguration_Auth_EnvironmentAdmin_Success() {
    var (existingEnvironment, existingTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.Fixture.CreateAuthenticatedRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}", PermissionType.Admin, existingEnvironment)
        .And(req => {
          req.Content = JsonContent.Create(
            TestRootChildConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: updateConfigResponse.StatusCode);
  }

  [Fact]
  public async Task UpdateConfiguration_Auth_EnvironmentStandard_Forbidden() {
    var (existingEnvironment, existingTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.Fixture.CreateAuthenticatedRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}", PermissionType.Standard, existingEnvironment)
        .And(req => {
          req.Content = JsonContent.Create(
            TestRootChildConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.Forbidden,
      actual: updateConfigResponse.StatusCode);
  }

  [Fact]
  public async Task UpdateConfiguration_Auth_OtherEnvironmentAdmin_Forbidden() {
    var (existingEnvironment, existingTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();
    var (otherEnvironment, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.Fixture.CreateAuthenticatedRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}", PermissionType.Admin, otherEnvironment)
        .And(req => {
          req.Content = JsonContent.Create(
            TestRootChildConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.Forbidden,
      actual: updateConfigResponse.StatusCode);
  }

  [Fact]
  public async Task UpdateConfiguration_Auth_Anonymous_Unauthorized() {
    var (existingEnvironment, existingTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestRootChildConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.Unauthorized,
      actual: updateConfigResponse.StatusCode);
  }

  [Fact]
  public async Task DeleteConfiguration_Auth_BuiltInAdmin_Success() {
    var (existingEnvironment, existingTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestRootChildConfiguration);
        })
        .SendAsync("DELETE");

    Assert.Equal(
      expected: HttpStatusCode.NoContent,
      actual: updateConfigResponse.StatusCode);
  }

  [Fact]
  public async Task DeleteConfiguration_Auth_GlobalAdmin_Success() {
    var (existingEnvironment, existingTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.Fixture.CreateAuthenticatedRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}", PermissionType.Admin)
        .And(req => {
          req.Content = JsonContent.Create(
            TestRootChildConfiguration);
        })
        .SendAsync("DELETE");

    Assert.Equal(
      expected: HttpStatusCode.NoContent,
      actual: updateConfigResponse.StatusCode);
  }

  [Fact]
  public async Task DeleteConfiguration_Auth_EnvironmentAdmin_Success() {
    var (existingEnvironment, existingTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.Fixture.CreateAuthenticatedRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}", PermissionType.Admin, existingEnvironment)
        .And(req => {
          req.Content = JsonContent.Create(
            TestRootChildConfiguration);
        })
        .SendAsync("DELETE");

    Assert.Equal(
      expected: HttpStatusCode.NoContent,
      actual: updateConfigResponse.StatusCode);
  }

  [Fact]
  public async Task DeleteConfiguration_Auth_EnvironmentStandard_Forbidden() {
    var (existingEnvironment, existingTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.Fixture.CreateAuthenticatedRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}", PermissionType.Standard, existingEnvironment)
        .And(req => {
          req.Content = JsonContent.Create(
            TestRootChildConfiguration);
        })
        .SendAsync("DELETE");

    Assert.Equal(
      expected: HttpStatusCode.Forbidden,
      actual: updateConfigResponse.StatusCode);
  }

  [Fact]
  public async Task DeleteConfiguration_Auth_OtherEnvironmentAdmin_Forbidden() {
    var (existingEnvironment, existingTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();
    var (otherEnvironment, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.Fixture.CreateAuthenticatedRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}", PermissionType.Admin, otherEnvironment)
        .And(req => {
          req.Content = JsonContent.Create(
            TestRootChildConfiguration);
        })
        .SendAsync("DELETE");

    Assert.Equal(
      expected: HttpStatusCode.Forbidden,
      actual: updateConfigResponse.StatusCode);
  }

  [Fact]
  public async Task DeleteConfiguration_Auth_Anonymous_Unauthorized() {
    var (existingEnvironment, existingTenant) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            TestRootChildConfiguration);
        })
        .SendAsync("Delete");

    Assert.Equal(
      expected: HttpStatusCode.Unauthorized,
      actual: updateConfigResponse.StatusCode);
  }

  [Fact]
  public async Task GetConfiguration_Auth_EnvironmentStandard_Success() {
    // Create Tenant Configuration
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootChildConfiguration);

    var getResponse = await
      this.Fixture.CreateAuthenticatedRequest(
          $"/api/v2/config/{testEnvironment}/tenants/{testTenant}",
          PermissionType.Standard,
          testEnvironment)
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);
  }

  [Fact(Skip = "BATAPI-: GetConfiguration should enforce ApiKey scope")]
  public async Task GetConfiguration_Auth_OtherEnvironmentStandard_Forbidden() {
    // Create Tenant Configuration
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootChildConfiguration);
    var (otherEnvironment, _) =
      await this.Fixture.CreateEmptyTestConfiguration();

    var getResponse = await
      this.Fixture.CreateAuthenticatedRequest(
          $"/api/v2/config/{testEnvironment}/tenants/{testTenant}",
          PermissionType.Standard,
          otherEnvironment)
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.Forbidden,
      actual: getResponse.StatusCode);
  }

  #endregion

  private static T? GetExtensionValue<T>(Object? extensionValue) {
    return extensionValue switch {
      null => default,
      T typedValue => typedValue,
      JsonElement element => ConfigurationControllerIntegrationTests.GetElementValue<T>(element),
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
