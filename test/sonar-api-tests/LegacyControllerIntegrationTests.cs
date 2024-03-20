using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Models.Legacy;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Tests;

public class LegacyControllerIntegrationTests : ApiControllerTestsBase {
  private const String TestEnvironment = "test-env";
  private const String OtherTestEnvironment = "other-env";
  private const String TestHealthCheckName = "fake-check";

  public Boolean ActiveChangeCallbacks => true;
  public Boolean HasChanged => false;

  private LegacyEndpointConfiguration? _config;

  private static readonly HealthCheckModel FakeHealthCheck = new(
    name: TestHealthCheckName,
    description: null,
    HealthCheckType.HttpRequest,
    new HttpHealthCheckDefinition(
      new Uri("about:blank"),
      Array.Empty<HttpHealthCheckCondition>(),
      followRedirects: default,
      authorizationHeader: default,
      skipCertificateValidation: default
    ),
    null
  );

  public LegacyControllerIntegrationTests(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper outputHelper) :
    base(fixture, outputHelper) {
  }

  protected override void OnInitializing(WebApplicationBuilder builder) {
    base.OnInitializing(builder);
    // Inject test configuration source
    // builder.Configuration.Sources.Add(this);
    builder.Services.AddTransient(this.GetTestConfig);
  }


  // Basic valid configuration test
  [Fact]
  public async Task ValidConfiguration_ServicesExist_SuccessResponse() {
    var testId = Guid.NewGuid().ToString();
    this.SetLegacyConfig(testId);
    // Create Tenant Configurations
    await this.CreateTestConfiguration(testId);

    var getResponse = await
      this.Fixture.Server.CreateRequest("/api/v1/services/")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

    var result = await getResponse.Content.ReadFromJsonAsync<LegacyServiceHierarchyHealth[]>(SerializerOptions);

    Assert.NotNull(result);
    Assert.Equal(expected: 2, result.Length);
    Assert.Equal(
      ImmutableHashSet.Create("synthetic-grouping", "legacy-root-service"),
      result.Select(svc => svc.Name).ToImmutableHashSet());

    var allServices =
      result.SelectMany(svc => svc.Children.Count > 0 ? svc.Children.Prepend(svc) : new[] { svc }).ToList();
    Assert.Equal(expected: 4, allServices.Count);
    // All services should be unresponsive
    Assert.True(
      allServices.All(svc => svc.Status == LegacyHealthStatus.Unresponsive)
    );
  }

  // Report service status and verify corresponding result is returned
  [Theory]
  [InlineData(HealthStatus.Degraded)]
  [InlineData(HealthStatus.AtRisk)]
  public async Task ReportStatus_ResponseIncludesStatus(HealthStatus degradedStatus) {
    var testId = Guid.NewGuid().ToString();
    this.SetLegacyConfig(testId);
    // Create Tenant Configurations
    await this.CreateTestConfiguration(testId);

    var timestamp = DateTime.UtcNow;

    await this.ReportServiceStatus(
      $"{TestEnvironment}-{testId}",
      tenant: "foo",
      service: "foo-service",
      timestamp,
      HealthStatus.Online);

    await this.ReportServiceStatus(
      $"{TestEnvironment}-{testId}",
      tenant: "bar",
      service: "bar-service",
      timestamp,
      degradedStatus);

    await this.ReportServiceStatus(
      $"{OtherTestEnvironment}-{testId}",
      tenant: "baz",
      service: "baz-service",
      timestamp,
      HealthStatus.Offline);

    var getResponse = await
      this.Fixture.Server.CreateRequest("/api/v1/services/")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

    var result = await getResponse.Content.ReadFromJsonAsync<LegacyServiceHierarchyHealth[]>(SerializerOptions);

    Assert.NotNull(result);
    Assert.Equal(expected: 2, result.Length);
    Assert.Equal(
      ImmutableHashSet.Create("synthetic-grouping", "legacy-root-service"),
      result.Select(svc => svc.Name).ToImmutableHashSet());

    var allServices =
      result.SelectMany(svc => svc.Children.Count > 0 ? svc.Children.Prepend(svc) : new[] { svc })
        .ToDictionary(svc => svc.Name);
    Assert.Equal(expected: 4, allServices.Count);
    // All services should be unresponsive

    Assert.Equal(LegacyHealthStatus.Operational, allServices["legacy-foo-service"].Status);
    Assert.Equal(LegacyHealthStatus.Degraded, allServices["legacy-bar-service"].Status);
    Assert.Equal(LegacyHealthStatus.Degraded, allServices["synthetic-grouping"].Status);
    Assert.Equal(LegacyHealthStatus.Unresponsive, allServices["legacy-root-service"].Status);
  }

  // Referencing missing Environment/Tenant/Service does not cause errors
  [Fact]
  public async Task PartiallyValidConfiguration_SomeServicesExist_SuccessResponse() {
    var testId = Guid.NewGuid().ToString();
    this._config = new LegacyEndpointConfiguration(
      Enabled: true,
      ServiceMapping: new[] {
        new LegacyServiceMapping(
          LegacyName: "legacy-foo-service",
          Environment: $"test-env-{testId}",
          Tenant: "foo",
          Name: "foo-service"),
        new LegacyServiceMapping(
          LegacyName: "legacy-missing-service",
          DisplayName: "Override App Display Name",
          Environment: $"test-env-{testId}",
          Tenant: "bar",
          Name: "missing-service")
      },
      RootServices: new[] {
        "legacy-foo-service",
        "legacy-missing-service"
      });
    // Create Tenant Configurations
    await this.CreateTestConfiguration(testId);

    // Report the status for the service that exists
    await this.ReportServiceStatus(
      $"{TestEnvironment}-{testId}",
      tenant: "foo",
      service: "foo-service",
      DateTime.UtcNow,
      HealthStatus.Online);

    var getResponse = await
      this.Fixture.Server.CreateRequest("/api/v1/services/")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

    var result = await getResponse.Content.ReadFromJsonAsync<LegacyServiceHierarchyHealth[]>(SerializerOptions);

    Assert.NotNull(result);
    Assert.Equal(expected: 2, result.Length);
    Assert.Equal(
      ImmutableHashSet.Create("legacy-foo-service", "legacy-missing-service"),
      result.Select(svc => svc.Name).ToImmutableHashSet());

    var allServices =
      result.SelectMany(svc => svc.Children.Count > 0 ? svc.Children.Prepend(svc) : new[] { svc })
        .ToDictionary(svc => svc.Name);
    Assert.Equal(expected: 2, allServices.Count);
    Assert.Equal(LegacyHealthStatus.Operational, allServices["legacy-foo-service"].Status);
    Assert.Equal(LegacyHealthStatus.Unresponsive, allServices["legacy-missing-service"].Status);
  }

  // Invalid configuration causes 500:
  // Root service does not exist
  // (Not Tested) Duplicate service names
  // (Not Tested) Service references child that does not exist
  [Fact]
  public async Task InvalidConfiguration_MissingRootService() {

    var testId = Guid.NewGuid().ToString();
    this._config = new LegacyEndpointConfiguration(
      Enabled: true,
      ServiceMapping: new[] {
        new LegacyServiceMapping(
          LegacyName: "legacy-foo-service",
          Environment: $"test-env-{testId}",
          Tenant: "foo",
          Name: "foo-service")
      },
      RootServices: new[] {
        "legacy-foo-service",
        "legacy-missing-service"
      });
    // Create Tenant Configurations
    await this.CreateTestConfiguration(testId);

    await Assert.ThrowsAsync<InvalidOperationException>(async () => {
      await
        this.Fixture.Server.CreateRequest("/api/v1/services/")
          .AddHeader(name: "Accept", value: "application/json")
          .GetAsync();
    });

  }

  private void SetLegacyConfig(String testId) {
    this._config = new LegacyEndpointConfiguration(
      Enabled: true,
      ServiceMapping: new[] {
        new LegacyServiceMapping(
          LegacyName: "synthetic-grouping",
          DisplayName: "Legacy Metric Service Group",
          Children: new[] { "legacy-foo-service", "legacy-bar-service" }
        ),
        new LegacyServiceMapping(
          LegacyName: "legacy-foo-service",
          Environment: $"test-env-{testId}",
          Tenant: "foo",
          Name: "foo-service"),
        new LegacyServiceMapping(
          LegacyName: "legacy-bar-service",
          DisplayName: "Override App Display Name",
          Environment: $"test-env-{testId}",
          Tenant: "bar",
          Name: "bar-service"),
        new LegacyServiceMapping(
          LegacyName: "legacy-root-service",
          Environment: $"other-env-{testId}",
          Tenant: "baz",
          Name: "baz-service")
      },
      RootServices: new[] {
        "synthetic-grouping",
        "legacy-root-service"
      });
  }

  private IOptions<LegacyEndpointConfiguration> GetTestConfig(IServiceProvider arg) {
    return new OptionsWrapper<LegacyEndpointConfiguration>(
      this._config ?? throw new InvalidOperationException($"No {nameof(LegacyEndpointConfiguration)} specified.")
    );
  }

  private async Task ReportServiceStatus(
    String environment,
    String tenant,
    String service,
    DateTime timestamp,
    HealthStatus status) {

    // Record health status
    var response = await
      this.Fixture.CreateAdminRequest(
          $"/api/v2/health/{environment}/tenants/{tenant}/services/{service}")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHealth(
            timestamp,
            status,
            ImmutableDictionary<String, HealthStatus>.Empty
              .Add(TestHealthCheckName, status)
          ));
        })
        .PostAsync();

    // 200, 201, 204 would all be ok
    Assert.True(
      response.IsSuccessStatusCode,
      userMessage: $"Expected a success response code (2xx). Actual: {(Int32)response.StatusCode}"
    );
  }

  private async Task CreateTestConfiguration(String testId) {
    await this.CreateTestConfiguration(
      $"{TestEnvironment}-{testId}",
      "foo",
      new ServiceHierarchyConfiguration(
        ImmutableList.Create(
          new ServiceConfiguration(
            "foo-service",
            "Test Service Foo",
            healthChecks: ImmutableList.Create(FakeHealthCheck))
        ),
        ImmutableHashSet.Create("foo-service"),
        null)
    );
    await this.CreateTestConfiguration(
      $"{TestEnvironment}-{testId}",
      "bar",
      new ServiceHierarchyConfiguration(
        ImmutableList.Create(
          new ServiceConfiguration(
            "bar-service",
            "Test Service Bar",
            healthChecks: ImmutableList.Create(FakeHealthCheck))
        ),
        ImmutableHashSet.Create("bar-service"),
        null)
    );
    await this.CreateTestConfiguration(
      $"{OtherTestEnvironment}-{testId}",
      "baz",
      new ServiceHierarchyConfiguration(
        ImmutableList.Create(
          new ServiceConfiguration(
            "baz-service",
            "Test Service Bar",
            healthChecks: ImmutableList.Create(FakeHealthCheck))
        ),
        ImmutableHashSet.Create("baz-service"),
        null)
    );
  }
}
