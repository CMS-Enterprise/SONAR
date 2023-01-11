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
using Cms.BatCave.Sonar.Json;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Environment = Cms.BatCave.Sonar.Data.Environment;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Tests;

public class ConfigurationControllerIntegrationTests : ApiControllerTestsBase {
  private const String TestRootServiceName = "TestRootService";
  private const String TestChildServiceName = "TestChildService";
  private const String TestHealthCheckName = "TestHealthCheck";

  private const String TestRootServiceNameCasingMismatch = "tESTrOOTsERVICE";
  private const String TestServiceNameOver100Char =
    "ServiceNameServiceNameServiceNameServiceNameServiceNameServiceNameServiceNameServiceNameServiceNameService";
  private const String TestServiceNameNotUrlSafe = "Test_Service!";

  private const String TestNoRootServiceMatch =
    "One or more of the specified root services do not exist in the services array.";
  private const String TestNoChildServiceMatch =
    "One or more of the specified services contained a reference to a child service that did not exist in the services array.";
  private const String TestServiceNameMaxLength = "The field Name must be a string with a maximum length of 100.";

  private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions {
    Converters = { new JsonStringEnumConverter(), new ArrayTupleConverterFactory() },
    PropertyNameCaseInsensitive = true
  };

  private static readonly HealthCheckModel TestHealthCheck =
    new(
      ConfigurationControllerIntegrationTests.TestHealthCheckName,
      Description: "Health Check Description",
      HealthCheckType.PrometheusMetric,
      new PrometheusHealthCheckDefinition(
        TimeSpan.FromMinutes(1),
        Expression: "test_metric",
        ImmutableList.Create(
          new MetricHealthCondition(HealthOperator.GreaterThan, Threshold: 42.0m, HealthStatus.Offline)))
    );

  private static readonly ServiceHierarchyConfiguration TestRootChildConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        ConfigurationControllerIntegrationTests.TestRootServiceName,
        DisplayName: "Display Name",
        Description: null,
        Url: null,
        ImmutableList.Create(ConfigurationControllerIntegrationTests.TestHealthCheck),
        ImmutableHashSet<String>.Empty.Add(ConfigurationControllerIntegrationTests.TestChildServiceName)),
      new ServiceConfiguration(
        ConfigurationControllerIntegrationTests.TestChildServiceName,
        DisplayName: "Display Name",
        Description: null,
        Url: null,
        HealthChecks: ImmutableList.Create(ConfigurationControllerIntegrationTests.TestHealthCheck),
        Children: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(ConfigurationControllerIntegrationTests.TestRootServiceName)
  );

  private static readonly ServiceHierarchyConfiguration TestNoRootServiceMatchConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        ConfigurationControllerIntegrationTests.TestChildServiceName,
        DisplayName: "Display Name",
        Description: null,
        Url: null,
        ImmutableList.Create(ConfigurationControllerIntegrationTests.TestHealthCheck),
        Children: null)
    ),
    ImmutableHashSet<String>.Empty.Add(ConfigurationControllerIntegrationTests.TestRootServiceName)
  );

  private static readonly ServiceHierarchyConfiguration TestRootServiceCasingMismatchConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        ConfigurationControllerIntegrationTests.TestRootServiceNameCasingMismatch,
        DisplayName: "Display Name",
        Description: null,
        Url: null,
        HealthChecks: ImmutableList.Create(ConfigurationControllerIntegrationTests.TestHealthCheck),
        Children: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(ConfigurationControllerIntegrationTests.TestRootServiceName)
  );

  private static readonly ServiceHierarchyConfiguration TestDiffServiceDiffCasingConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        ConfigurationControllerIntegrationTests.TestRootServiceName,
        DisplayName: "Display Name",
        Description: null,
        Url: null,
        HealthChecks: ImmutableList.Create(ConfigurationControllerIntegrationTests.TestHealthCheck),
        Children: null
      ),
      new ServiceConfiguration(
        ConfigurationControllerIntegrationTests.TestRootServiceNameCasingMismatch,
        DisplayName: "Display Name",
        Description: null,
        Url: null,
        HealthChecks: ImmutableList.Create(ConfigurationControllerIntegrationTests.TestHealthCheck),
        Children: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(ConfigurationControllerIntegrationTests.TestRootServiceName)
  );

  private static readonly ServiceHierarchyConfiguration TestNoChildServiceMatchConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        ConfigurationControllerIntegrationTests.TestRootServiceName,
        DisplayName: "Display Name",
        Description: null,
        Url: null,
        ImmutableList.Create(ConfigurationControllerIntegrationTests.TestHealthCheck),
        ImmutableHashSet<String>.Empty.Add(ConfigurationControllerIntegrationTests.TestChildServiceName))
    ),
    ImmutableHashSet<String>.Empty.Add(ConfigurationControllerIntegrationTests.TestRootServiceName)
  );

  private static readonly ServiceHierarchyConfiguration TestDuplicateServiceNamesConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        ConfigurationControllerIntegrationTests.TestRootServiceName,
        DisplayName: "Display Name",
        Description: null,
        Url: null,
        ImmutableList.Create(ConfigurationControllerIntegrationTests.TestHealthCheck),
        ImmutableHashSet<String>.Empty.Add(ConfigurationControllerIntegrationTests.TestChildServiceName)),
      new ServiceConfiguration(
        ConfigurationControllerIntegrationTests.TestChildServiceName,
        DisplayName: "Display Name",
        Description: null,
        Url: null,
        HealthChecks: ImmutableList.Create(ConfigurationControllerIntegrationTests.TestHealthCheck),
        Children: null
      ),
      new ServiceConfiguration(
        ConfigurationControllerIntegrationTests.TestChildServiceName,
        DisplayName: "Display Name",
        Description: null,
        Url: null,
        HealthChecks: ImmutableList.Create(ConfigurationControllerIntegrationTests.TestHealthCheck),
        Children: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(ConfigurationControllerIntegrationTests.TestRootServiceName)
  );

  private static readonly ServiceHierarchyConfiguration TestServiceNameOver100CharConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        ConfigurationControllerIntegrationTests.TestServiceNameOver100Char,
        DisplayName: "Display Name",
        Description: null,
        Url: null,
        ImmutableList.Create(ConfigurationControllerIntegrationTests.TestHealthCheck),
        Children: null)
    ),
    ImmutableHashSet<String>.Empty.Add(ConfigurationControllerIntegrationTests.TestServiceNameOver100Char)
  );

  public ConfigurationControllerIntegrationTests(ApiIntegrationTestFixture fixture) : base(fixture) { }

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
      ConfigurationControllerIntegrationTests.SerializerOptions
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
    await this.Fixture.WithDependenciesAsync(async (provider, cancellationToken) =>
    {
      var dbContext = provider.GetRequiredService<DataContext>();
      var environments = provider.GetRequiredService<DbSet<Data.Environment>>();

      await environments.AddAsync(Data.Environment.New(existingEnvironmentName), cancellationToken);
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
      ConfigurationControllerIntegrationTests.SerializerOptions
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
        ImmutableHashSet<String>.Empty));

    var getResponse = await
      this.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyConfiguration>(
      ConfigurationControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Empty(body.Services);
    Assert.Empty(body.RootServices);
  }

  [Fact]
  public async Task GetConfiguration_ConfigWithServicesReturnsOk() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(ConfigurationControllerIntegrationTests.TestRootChildConfiguration);

    var getResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyConfiguration>(
      ConfigurationControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Single(body.RootServices);
    Assert.Equal(
      expected: ConfigurationControllerIntegrationTests.TestRootChildConfiguration.Services.Count,
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
      this.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            ConfigurationControllerIntegrationTests.TestNoRootServiceMatchConfiguration);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: createConfigResponse.StatusCode);

    var body = await createConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      ConfigurationControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ProblemTypes.InvalidConfiguration,
      actual: body.Type
    );
    Assert.Equal(
      expected: ConfigurationControllerIntegrationTests.TestNoRootServiceMatch,
      actual: body.Title
    );
  }

  [Fact]
  public async Task CreateConfiguration_NoChildServiceMatchReturnsBadRequest() {
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            ConfigurationControllerIntegrationTests.TestNoChildServiceMatchConfiguration);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: createConfigResponse.StatusCode);

    var body = await createConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      ConfigurationControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ProblemTypes.InvalidConfiguration,
      actual: body.Type
    );
    Assert.Equal(
      expected: ConfigurationControllerIntegrationTests.TestNoChildServiceMatch,
      actual: body.Title
    );
  }

  [Fact]
  public async Task CreateConfiguration_MultipleServicesWithSameNameReturnsBadRequest() {
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            ConfigurationControllerIntegrationTests.TestDuplicateServiceNamesConfiguration);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: createConfigResponse.StatusCode);

    var body = await createConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      ConfigurationControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: "The specified list of services contained multiple services with the same name.",
      actual: body.Title
    );
  }

  [Fact]
  public async Task CreateConfiguration_ServiceNameLengthExceededReturnsBadRequest() {
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            ConfigurationControllerIntegrationTests.TestServiceNameOver100CharConfiguration);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: createConfigResponse.StatusCode);

    var body = await createConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      ConfigurationControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.True(body.Extensions.ContainsKey("errors"));

    var errorsObj = Assert.IsType<JsonElement>(body.Extensions["errors"]);
    Assert.True(errorsObj.TryGetProperty("Services[0].Name", out var serviceNameErrorsVal));
    var serviceNameErrorsArray = Assert.IsType<JsonElement>(serviceNameErrorsVal);
    Assert.Equal(JsonValueKind.Array, serviceNameErrorsArray.ValueKind);
    Assert.Equal(1, serviceNameErrorsArray.GetArrayLength());
    Assert.Equal(
      expected: ConfigurationControllerIntegrationTests.TestServiceNameMaxLength,
      actual: ConfigurationControllerIntegrationTests.GetExtensionValue<String>(serviceNameErrorsArray[0]));
  }

  [Fact]
  public async Task CreateConfiguration_ServiceNameNotUrlSafeReturnsBadRequest() {
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.Fixture.Server.CreateRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            ConfigurationControllerIntegrationTests.TestServiceNameNotUrlSafe);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: createConfigResponse.StatusCode);
  }

  [Fact]
  public async Task CreateConfiguration_TenantAlreadyExistsReturnsConflict() {
    var (testEnvironment, testTenant) =
      await this.CreateEmptyTestConfiguration();

    // Attempt to create configuration with services for existing Tenant
    var createConfigResponse = await
      this.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            ConfigurationControllerIntegrationTests.TestRootChildConfiguration);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.Conflict,
      actual: createConfigResponse.StatusCode);

    var body = await createConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      ConfigurationControllerIntegrationTests.SerializerOptions
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
      this.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            ConfigurationControllerIntegrationTests.TestRootServiceCasingMismatchConfiguration);
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
      this.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            ConfigurationControllerIntegrationTests.TestDiffServiceDiffCasingConfiguration);
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: createConfigResponse.StatusCode);

    var body = await createConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      ConfigurationControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: "The specified list of services contained multiple services with the same name.",
      actual: body.Title
    );
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
      await this.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            ConfigurationControllerIntegrationTests.TestNoRootServiceMatchConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: updateConfigResponse.StatusCode);

    var body = await updateConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      ConfigurationControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ProblemTypes.InvalidConfiguration,
      actual: body.Type
    );
    Assert.Equal(
      expected: ConfigurationControllerIntegrationTests.TestNoRootServiceMatch,
      actual: body.Title
    );
  }

  [Fact]
  public async Task UpdateConfiguration_NoChildServiceMatchReturnsBadRequest() {
    var (testEnvironment, testTenant) =
      await this.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            ConfigurationControllerIntegrationTests.TestNoChildServiceMatchConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: updateConfigResponse.StatusCode);

    var body = await updateConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      ConfigurationControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ProblemTypes.InvalidConfiguration,
      actual: body.Type
    );
    Assert.Equal(
      expected: ConfigurationControllerIntegrationTests.TestNoChildServiceMatch,
      actual: body.Title
    );
  }

  [Fact]
  public async Task UpdateConfiguration_ServiceNameLengthExceededReturnsBadRequest() {
    var (existingEnvironment, existingTenant) =
      await this.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.CreateAdminRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            ConfigurationControllerIntegrationTests.TestServiceNameOver100CharConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: updateConfigResponse.StatusCode);

    var body = await updateConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      ConfigurationControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);
    Assert.True(body.Extensions.ContainsKey("errors"));
    var errorsObj = Assert.IsType<JsonElement>(body.Extensions["errors"]);
    Assert.True(errorsObj.TryGetProperty("Services[0].Name", out var serviceNameErrorsVal));
    var serviceNameErrorsArray = Assert.IsType<JsonElement>(serviceNameErrorsVal);
    Assert.Equal(JsonValueKind.Array, serviceNameErrorsArray.ValueKind);
    Assert.Equal(1, serviceNameErrorsArray.GetArrayLength());
    Assert.Equal(
      expected: ConfigurationControllerIntegrationTests.TestServiceNameMaxLength,
      actual: ConfigurationControllerIntegrationTests.GetExtensionValue<String>(serviceNameErrorsArray[0]));
  }

  [Fact]
  public async Task UpdateConfiguration_ServiceNameNotUrlSafeReturnsBadRequest() {
    var (existingEnvironment, existingTenant) =
      await this.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.CreateAdminRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            ConfigurationControllerIntegrationTests.TestServiceNameNotUrlSafe);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: updateConfigResponse.StatusCode);
  }

  [Fact]
  public async Task UpdateConfiguration_MissingEnvironmentReturnsNotFound() {
    var (existingEnvironment, existingTenant) =
      await this.CreateEmptyTestConfiguration();

    var missingEnvironmentName = Guid.NewGuid().ToString();
    var updateConfigResponse = await
      this.CreateAdminRequest($"/api/v2/config/{missingEnvironmentName}/tenants/foo")
        .And(req => {
          req.Content = JsonContent.Create(
            ConfigurationControllerIntegrationTests.TestRootChildConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: updateConfigResponse.StatusCode);

    var body = await updateConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      ConfigurationControllerIntegrationTests.SerializerOptions
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
      await this.CreateEmptyTestConfiguration();

    var missingTenantName = Guid.NewGuid().ToString();
    var updateConfigResponse = await
      this.CreateAdminRequest($"/api/v2/config/{existingEnvironment}/tenants/{missingTenantName}")
        .And(req => {
          req.Content = JsonContent.Create(
            ConfigurationControllerIntegrationTests.TestRootChildConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: updateConfigResponse.StatusCode);

    var body = await updateConfigResponse.Content.ReadFromJsonAsync<ProblemDetails>(
      ConfigurationControllerIntegrationTests.SerializerOptions
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
      await this.CreateEmptyTestConfiguration();

    var updateConfigResponse = await
      this.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            ConfigurationControllerIntegrationTests.TestRootServiceCasingMismatchConfiguration);
        })
        .SendAsync("PUT");

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: updateConfigResponse.StatusCode);
  }

  [Fact]
  public async Task UpdateConfiguration_SuccessfulUpdateReturnsOk() {
    var (existingEnvironment, existingTenant) =
      await this.CreateEmptyTestConfiguration();

    var getResponse = await
      this.CreateAdminRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    // This isn't what is being tested.
    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceHierarchyConfiguration>(
      ConfigurationControllerIntegrationTests.SerializerOptions
    );

    // Confirm POST request had no services
    Assert.NotNull(body);
    Assert.Empty(body.Services);
    Assert.Empty(body.RootServices);

    var updateConfigResponse = await
      this.CreateAdminRequest($"/api/v2/config/{existingEnvironment}/tenants/{existingTenant}")
        .And(req => {
          req.Content = JsonContent.Create(
            ConfigurationControllerIntegrationTests.TestRootChildConfiguration);
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
      ConfigurationControllerIntegrationTests.SerializerOptions
    );

    // Confirm PUT request had services
    Assert.NotNull(body);
    Assert.Single(body.RootServices);
    Assert.Equal(
      expected: ConfigurationControllerIntegrationTests.TestRootChildConfiguration.Services.Count,
      actual: body.Services.Count);
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

  private async Task<(String, String)> CreateEmptyTestConfiguration() {
    // Create Service Configuration
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
      this.Fixture.Server
        .CreateRequest(
          $"/api/v2/config/{testEnvironment}/tenants/{testTenant}/services/{serviceName}")
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
