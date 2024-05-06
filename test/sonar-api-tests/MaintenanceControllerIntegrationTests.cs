using System;
using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Tests;

public class MaintenanceControllerIntegrationTests : ApiControllerTestsBase {
  private ITestOutputHelper _output;
  public MaintenanceControllerIntegrationTests(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper outputHelper) : base(fixture, outputHelper, true) {
    this._output = outputHelper;
  }

  [Fact]
  public async Task ToggleMaintenance_EnvironmentScoped_Success() {

    var user = await this.Fixture.CreateGlobalAdminUser();
    var env = $"{Guid.NewGuid()}";
    var creation = await
      this.Fixture.CreateAdminRequest($"api/v2/environments")
        .And(req => {
          req.Content = JsonContent.Create(new {
            Name = env
          });
        })
        .PostAsync();

    Assert.Equal(HttpStatusCode.Created, creation.StatusCode);

    var response = await this.Fixture.CreateFakeJwtRequest($"api/v2/maintenance/{env}/ad-hoc", user.Email)
      .And(req => {
        req.Content = JsonContent.Create(new {
          IsEnabled = true
        });
      }).SendAsync("PUT");

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ActiveAdHocMaintenanceView>(SerializerOptions);
    Assert.NotNull(body);
    Assert.Equal(expected: env, actual: body.Environment);
    Assert.Equal(expected: MaintenanceScope.Environment, actual: body.Scope);
    Assert.Equal(expected: user.FullName, actual: body.AppliedByUserName);
  }

  [Fact]
  public async Task ToggleMaintenance_TenantScoped_Success() {

    var user = await this.Fixture.CreateGlobalAdminUser();
    var (env, tenant) = await this.Fixture.CreateEmptyTestConfiguration();

    var response = await this.Fixture.CreateFakeJwtRequest($"api/v2/maintenance/{env}/tenants/{tenant}/ad-hoc", user.Email)
      .And(req => {
        req.Content = JsonContent.Create(new {
          IsEnabled = true
        });
      }).SendAsync("PUT");

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ActiveAdHocMaintenanceView>(SerializerOptions);
    Assert.NotNull(body);
    Assert.Equal(expected: env, actual: body.Environment);
    Assert.Equal(expected: MaintenanceScope.Tenant, actual: body.Scope);
    Assert.Equal(expected: user.FullName, actual: body.AppliedByUserName);
  }

  [Fact]
  public async Task ToggleMaintenance_ServiceScoped_Success() {

    var user = await this.Fixture.CreateGlobalAdminUser();
    // Create Service Configuration
    var env = Guid.NewGuid().ToString();
    var tenant = Guid.NewGuid().ToString();
    var rootService = "root-service";

    var createConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{env}/tenants/{tenant}")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHierarchyConfiguration(
            ImmutableArray<ServiceConfiguration>.Empty.Add(
              new ServiceConfiguration(rootService, rootService)),
            ImmutableHashSet<String>.Empty.Add(rootService),
            null
          ));
        })
        .PostAsync();

    AssertHelper.Precondition(
      createConfigResponse.IsSuccessStatusCode,
      message: "Failed to create test configuration."
    );

    var response = await this.Fixture.CreateFakeJwtRequest($"api/v2/maintenance/{env}/tenants/{tenant}/services/{rootService}/ad-hoc", user.Email)
      .And(req => {
        req.Content = JsonContent.Create(new {
          IsEnabled = true
        });
      }).SendAsync("PUT");

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ActiveAdHocMaintenanceView>(SerializerOptions);
    Assert.NotNull(body);
    Assert.Equal(expected: env, actual: body.Environment);
    Assert.Equal(expected: MaintenanceScope.Service, actual: body.Scope);
    Assert.Equal(expected: user.FullName, actual: body.AppliedByUserName);
  }

  [Fact]
  public async Task FetchActiveScheduledEnvironmentMaintenances_Success() {
    var expectedScheduleExpression = "* * * * *";
    var expectedTimezone = "US/Eastern";
    var expectedDuration = 20;
    var (env, _, _) = await this
      .CreateScheduledMaintenanceConfiguration(expectedScheduleExpression, expectedDuration, expectedTimezone);

    var response = await this.Fixture.Server.CreateRequest("api/v2/maintenance/environments/scheduled")
      .AddHeader(name: "Accept", value: "application/json")
      .GetAsync();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var body = await response.Content
      .ReadFromJsonAsync<ActiveScheduledMaintenanceView[]>(SerializerOptions);
    Assert.NotNull(body);
    var maintenanceView = Assert.Single(body);

    Assert.Equal(expected: MaintenanceScope.Environment, actual: maintenanceView.Scope);
    Assert.Equal(expected: env, actual: maintenanceView.Environment);
    Assert.Null(maintenanceView.Tenant);
    Assert.Null(maintenanceView.Service);
    Assert.Equal(expected: expectedScheduleExpression, actual: maintenanceView.ScheduleExpression);
    Assert.Equal(expected: expectedDuration, actual: maintenanceView.Duration);
    Assert.Equal(expected: expectedTimezone, actual: maintenanceView.TimeZone);
  }

  [Fact]
  public async Task FetchActiveScheduledTenantMaintenances_Success() {
    var expectedScheduleExpression = "* * * * *";
    var expectedTimezone = "US/Eastern";
    var expectedDuration = 20;
    var (_, tenant, _) = await this
      .CreateScheduledMaintenanceConfiguration(expectedScheduleExpression, expectedDuration, expectedTimezone);

    var response = await this.Fixture.Server.CreateRequest("api/v2/maintenance/tenants/scheduled")
      .AddHeader(name: "Accept", value: "application/json")
      .GetAsync();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var body = await response.Content
      .ReadFromJsonAsync<ActiveScheduledMaintenanceView[]>(SerializerOptions);
    Assert.NotNull(body);
    var maintenanceView = Assert.Single(body);

    Assert.Equal(expected: MaintenanceScope.Tenant, actual: maintenanceView.Scope);
    Assert.Equal(expected: tenant, actual: maintenanceView.Tenant);
    Assert.Null(maintenanceView.Service);
    Assert.Equal(expected: expectedScheduleExpression, actual: maintenanceView.ScheduleExpression);
    Assert.Equal(expected: expectedDuration, actual: maintenanceView.Duration);
    Assert.Equal(expected: expectedTimezone, actual: maintenanceView.TimeZone);
  }

  [Fact]
  public async Task FetchActiveScheduledServiceMaintenances_Success() {
    var expectedScheduleExpression = "* * * * *";
    var expectedTimezone = "US/Eastern";
    var expectedDuration = 20;
    var (_, _, service) = await this
      .CreateScheduledMaintenanceConfiguration(expectedScheduleExpression, expectedDuration, expectedTimezone);

    var response = await this.Fixture.Server.CreateRequest("api/v2/maintenance/services/scheduled")
      .AddHeader(name: "Accept", value: "application/json")
      .GetAsync();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var body = await response.Content
      .ReadFromJsonAsync<ActiveScheduledMaintenanceView[]>(SerializerOptions);
    Assert.NotNull(body);
    var maintenanceView = Assert.Single(body);

    Assert.Equal(expected: MaintenanceScope.Service, actual: maintenanceView.Scope);
    Assert.Equal(expected: service, actual: maintenanceView.Service);
    Assert.Equal(expected: expectedScheduleExpression, actual: maintenanceView.ScheduleExpression);
    Assert.Equal(expected: expectedDuration, actual: maintenanceView.Duration);
    Assert.Equal(expected: expectedTimezone, actual: maintenanceView.TimeZone);
  }

  private async Task<(String envName, String tenantName, String serviceName)> CreateScheduledMaintenanceConfiguration(
    String expectedScheduleExpression,
    Int32 expectedDuration,
    String expectedTimezone) {

    var envName = Guid.NewGuid().ToString();
    var tenantName = Guid.NewGuid().ToString();
    var serviceName = Guid.NewGuid().ToString();

    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var db = services.GetRequiredService<DataContext>();
      var environments = services.GetRequiredService<DbSet<Environment>>();
      var tenants = services.GetRequiredService<DbSet<Tenant>>();
      var servicesTable = services.GetRequiredService<DbSet<Service>>();
      var environmentMaintenances = services.GetRequiredService<DbSet<ScheduledEnvironmentMaintenance>>();
      var tenantMaintenances = services.GetRequiredService<DbSet<ScheduledTenantMaintenance>>();
      var serviceMaintenances = services.GetRequiredService<DbSet<ScheduledServiceMaintenance>>();

      var environment = await environments.AddAsync(
        new Environment(
          Guid.NewGuid(),
          envName),
        cancellationToken);

      var tenant = await tenants.AddAsync(new Tenant(
          Guid.NewGuid(),
          environment.Entity.Id,
          tenantName),
        cancellationToken);

      var service = await servicesTable.AddAsync(new Service(
        Guid.NewGuid(),
        tenant.Entity.Id,
        serviceName,
        serviceName,
        null,
        null,
        true));

      await environmentMaintenances.AddAsync(
        new ScheduledEnvironmentMaintenance(
          Guid.NewGuid(),
          expectedScheduleExpression,
          expectedTimezone,
          expectedDuration,
          environment.Entity.Id),
        cancellationToken);

      await tenantMaintenances.AddAsync(
        new ScheduledTenantMaintenance(
          Guid.NewGuid(),
          expectedScheduleExpression,
          expectedTimezone,
          expectedDuration,
          tenant.Entity.Id),
        cancellationToken);

      await serviceMaintenances.AddAsync(
        new ScheduledServiceMaintenance(
          Guid.NewGuid(),
          expectedScheduleExpression,
          expectedTimezone,
          expectedDuration,
          service.Entity.Id),
        cancellationToken);

      await db.SaveChangesAsync(cancellationToken);
    });

    return (envName, tenantName, serviceName);
  }
}
