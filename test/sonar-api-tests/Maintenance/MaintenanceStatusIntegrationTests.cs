using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Tests.Maintenance;

public class MaintenanceStatusIntegrationTests : MaintenanceStatusTestsBase {
  private const String ServiceName = "service";
  private const String ScheduledMaintenanceType = "scheduled";

  private static readonly ScheduledMaintenanceConfiguration AlwaysInScheduledMaintenanceConfiguration = new(
    "* * * * *",
    60,
    "US/Eastern");

  private static readonly ServiceHierarchyConfiguration ServiceConfigWithTenantScheduledMaintenance = new(
    rootServices: ImmutableHashSet.Create(ServiceName),
    services: ImmutableList.Create(
      new ServiceConfiguration(
        name: ServiceName,
        displayName: ServiceName)),
    scheduledMaintenances: ImmutableList.Create(AlwaysInScheduledMaintenanceConfiguration));

  private static readonly ServiceHierarchyConfiguration ServiceConfigWithServiceScheduledMaintenance = new(
    rootServices: ImmutableHashSet.Create(ServiceName),
    services: ImmutableList.Create(
      new ServiceConfiguration(
        name: ServiceName,
        displayName: ServiceName,
        scheduledMaintenances: ImmutableList.Create(AlwaysInScheduledMaintenanceConfiguration))));

  public MaintenanceStatusIntegrationTests(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper output)
    : base(fixture, output) { }

  [Fact]
  public async Task GetEnvironments_ScheduledMaintenance_ReturnsExpectedState() {
    var envName = new Guid().ToString();
    var environmentWithScheduledMaintenance = new EnvironmentModel(
      name: envName,
      isNonProd: true,
      scheduledMaintenances: ImmutableList.Create(AlwaysInScheduledMaintenanceConfiguration));
    var creation = await
      this.Fixture.CreateAuthenticatedRequest(url: "api/v2/environments", PermissionType.Admin)
        .And(req => {
          req.Content = JsonContent.Create(environmentWithScheduledMaintenance);
        })
        .PostAsync();

    AssertHelper.Precondition(
      creation.StatusCode == HttpStatusCode.Created,
      "Unable to create environment"
    );

    this.MockPrometheusService
      .Setup(p => p.GetScopedCurrentMaintenanceStatus(
        envName,
        null,
        null,
        MaintenanceScope.Environment,
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(
        (true, ScheduledMaintenanceType))
      .Verifiable();

    var response = await this.Fixture.Server
      .CreateRequest($"api/v2/environments")
      .GetAsync();

    var environments = await response.Content.ReadFromJsonAsync<ImmutableList<EnvironmentHealth>>(SerializerOptions);

    this.MockPrometheusService.Verify();
    Assert.NotNull(environments);
    var envWithMaintenanceStatus = Assert.Single(environments
      .Where(e => e.EnvironmentName == envName));
    Assert.True(envWithMaintenanceStatus.IsInMaintenance);
    var envMaintenanceTypes = envWithMaintenanceStatus.InMaintenanceTypes;
    Assert.NotNull(envMaintenanceTypes);
    Assert.Equal(expected: ScheduledMaintenanceType, actual: envMaintenanceTypes);
  }

  [Fact]
  public async Task GetEnvironment_ScheduledMaintenance_ReturnsExpectedState() {
    var envName = new Guid().ToString();
    var environmentWithScheduledMaintenance = new EnvironmentModel(
      name: envName,
      isNonProd: true,
      scheduledMaintenances: ImmutableList.Create(AlwaysInScheduledMaintenanceConfiguration));
    var creation = await
      this.Fixture.CreateAuthenticatedRequest(url: $"api/v2/environments", PermissionType.Admin)
        .And(req => {
          req.Content = JsonContent.Create(environmentWithScheduledMaintenance);
        })
        .PostAsync();

    AssertHelper.Precondition(
      creation.StatusCode == HttpStatusCode.Created,
      "Unable to create environment"
    );

    this.MockPrometheusService
      .Setup(p => p.GetScopedCurrentMaintenanceStatus(
        envName,
        null,
        null,
        MaintenanceScope.Environment,
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(
        (true, ScheduledMaintenanceType))
      .Verifiable();

    var response = await this.Fixture.Server
      .CreateRequest($"api/v2/environments/{envName}")
      .GetAsync();

    var environment = await response.Content.ReadFromJsonAsync<EnvironmentHealth>(SerializerOptions);

    this.MockPrometheusService.Verify();
    Assert.NotNull(environment);

    Assert.True(environment.IsInMaintenance);
    var envMaintenanceTypes = environment.InMaintenanceTypes;
    Assert.NotNull(envMaintenanceTypes);
    Assert.Equal(expected: ScheduledMaintenanceType, actual: envMaintenanceTypes);
  }

  [Fact]
  public async Task GetTenants_ScheduledMaintenance_ReturnsExpectedState() {
    var (environmentName, tenantName) = await this
      .CreateTestConfiguration(ServiceConfigWithTenantScheduledMaintenance);

    this.MockPrometheusService
      .Setup(p => p.GetScopedCurrentMaintenanceStatus(
        environmentName,
        tenantName,
        null,
        MaintenanceScope.Tenant,
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(
        (true, "scheduled"))
      .Verifiable();

    var response =
      await this.Fixture.Server
        .CreateRequest($"/api/v2/tenants?environment={environmentName}&tenant={tenantName}")
        .GetAsync();

    var tenants = await response.Content
      .ReadFromJsonAsync<TenantInfo[]>(SerializerOptions);

    this.MockPrometheusService.Verify();
    Assert.NotNull(tenants);
    var tenantWithMaintenanceStatus = Assert.Single(tenants);
    Assert.True(tenantWithMaintenanceStatus.IsInMaintenance);
    var tenantMaintenanceTypes = tenantWithMaintenanceStatus.InMaintenanceTypes;
    Assert.NotNull(tenantMaintenanceTypes);
    Assert.Equal(expected: ScheduledMaintenanceType, actual: tenantMaintenanceTypes);
  }

  [Fact]
  public async Task GetServiceHierarchyHealth_ScheduledMaintenance_ReturnsExpectedState() {
    var (environmentName, tenantName) = await this
      .CreateTestConfiguration(ServiceConfigWithServiceScheduledMaintenance);

    this.MockPrometheusService
      .Setup(p => p.GetScopedCurrentMaintenanceStatus(
        environmentName,
        tenantName,
        ServiceName,
        MaintenanceScope.Service,
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(
        (true, "scheduled"))
      .Verifiable();

    var response =
      await this.Fixture.Server
        .CreateRequest($"/api/v2/health/{environmentName}/tenants/{tenantName}")
        .GetAsync();

    var serviceHierarchyHealths = await response.Content
      .ReadFromJsonAsync<ServiceHierarchyHealth[]>(SerializerOptions);

    this.MockPrometheusService.Verify();
    Assert.NotNull(serviceHierarchyHealths);
    var serviceHealth = Assert.Single(serviceHierarchyHealths);
    Assert.True(serviceHealth.IsInMaintenance);
    var tenantMaintenanceTypes = serviceHealth.InMaintenanceTypes;
    Assert.NotNull(tenantMaintenanceTypes);
    Assert.Equal(expected: ScheduledMaintenanceType, actual: tenantMaintenanceTypes);
  }

  [Fact]
  public async Task GetServiceHierarchyHealth_MaintenanceStatusInheritedFromTenantScopedMaintenance_ReturnsExpectedState() {
    var (environmentName, tenantName) = await this
      .CreateTestConfiguration(ServiceConfigWithTenantScheduledMaintenance);

    this.MockPrometheusService
      .Setup(p => p.GetScopedCurrentMaintenanceStatus(
        environmentName,
        tenantName,
        ServiceName,
        MaintenanceScope.Service,
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(
        (true, "scheduled"))
      .Verifiable();

    var response =
      await this.Fixture.Server
        .CreateRequest($"/api/v2/health/{environmentName}/tenants/{tenantName}")
        .GetAsync();

    var serviceHierarchyHealths = await response.Content
      .ReadFromJsonAsync<ServiceHierarchyHealth[]>(SerializerOptions);

    this.MockPrometheusService.Verify();
    Assert.NotNull(serviceHierarchyHealths);
    var serviceHealth = Assert.Single(serviceHierarchyHealths);
    Assert.True(serviceHealth.IsInMaintenance);
    var tenantMaintenanceTypes = serviceHealth.InMaintenanceTypes;
    Assert.NotNull(tenantMaintenanceTypes);
    Assert.Equal(expected: ScheduledMaintenanceType, actual: tenantMaintenanceTypes);
  }
}
