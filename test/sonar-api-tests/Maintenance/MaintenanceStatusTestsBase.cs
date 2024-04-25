using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Helpers.Maintenance;
using Cms.BatCave.Sonar.Prometheus;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;
using Xunit.Abstractions;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Tests.Maintenance;

/// <summary>
/// Testing base class that provides functionality that is shared between tests for the maintenance status recording
/// classes, such as seeding the test database with relevant service hierarchy data (via xUnits IAsyncLifetime methods),
/// exposing the ids of the seeded entities, and a helper method for adding maintenance records to the test DB.
/// </summary>
public abstract class MaintenanceStatusTestsBase : ApiControllerTestsBase, IAsyncLifetime {

  /// <summary>
  /// A mock IPrometheusService instance that's used for tests inheriting this class, so that we don't require a
  /// running prometheus instance in the test host.
  /// </summary>
  protected readonly Mock<IPrometheusService> MockPrometheusService = new();

  /// <summary>
  /// A dictionary of the environment, tenant, and service guids created by <see cref="SetupTestServiceHierarchyAsync"/>.
  /// Keyed by the path to the entity delimited by forward slashes.
  /// <br/><br/>
  /// For example:
  /// <br/>TestServiceHierarchyIds["environment-1"] -> guid of environment-1
  /// <br/>TestServiceHierarchyIds["environment-2/tenant-1"] -> guid of tenant-1 of environment-2
  /// <br/>TestServiceHierarchyIds["environment-2/tenant-1/service-2"] -> guid of service-2 of tenant-1 of environment-2
  /// </summary>
  protected IImmutableDictionary<String, Guid> TestServiceHierarchyIds { get; private set; } =
    ImmutableDictionary<String, Guid>.Empty;

  /// <summary>
  /// A dictionary of the user ids created by <see cref="SetupTestUsersAsync"/>.
  /// Keyed by user full name.
  /// </summary>
  protected IImmutableDictionary<String, Guid> TestUserIds { get; private set; } =
    ImmutableDictionary<String, Guid>.Empty;

  protected MaintenanceStatusTestsBase(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper output)
    : base(fixture, output, resetDatabase: true) {
  }

  protected override void OnInitializing(WebApplicationBuilder builder) {
    base.OnInitializing(builder);

    // Replace the IPrometheusService instance in the DI container with a mock one for testing.
    builder.Services.RemoveAll<IPrometheusService>();
    builder.Services.AddScoped<IPrometheusService>(_ => this.MockPrometheusService.Object);
  }

  /// <summary>
  /// Implementation of xUnit IAsyncLifetime init method. This is where we seed the test database for each test run.
  /// </summary>
  public async Task InitializeAsync() {
    await this.SetupTestServiceHierarchyAsync();
    await this.SetupTestUsersAsync();
  }

  /// <summary>
  /// Test setup method that persists two environment records, each with two tenant records, each with two service
  /// records in the current test database. No additional data is persisted, only the bare minimum of fields required
  /// to create the entity records in the database.
  /// <br/><br/>
  /// Environments are named environment-1, environment-2.
  /// Tenants are named tenant-1, tenant-2.
  /// Services are named service-1, service-2.
  /// </summary>
  protected async Task SetupTestServiceHierarchyAsync() {
    var testServiceHierarchyIds = new Dictionary<String, Guid>();

    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      var environments = serviceProvider.GetRequiredService<DbSet<Environment>>();
      var tenants = serviceProvider.GetRequiredService<DbSet<Tenant>>();
      var services = serviceProvider.GetRequiredService<DbSet<Service>>();
      var serviceRelationships = serviceProvider.GetRequiredService<DbSet<ServiceRelationship>>();
      var context = serviceProvider.GetRequiredService<DataContext>();

      for (var e = 1; e <= 2; e++) {
        var environmentName = $"environment-{e}";
        var environment = environments.Add(Environment.New(name: environmentName));
        testServiceHierarchyIds[environmentName] = environment.Entity.Id;
        for (var t = 1; t <= 2; t++) {
          var tenantName = $"tenant-{t}";
          var tenant = tenants.Add(Tenant.New(environmentId: environment.Entity.Id, name: tenantName));
          testServiceHierarchyIds[$"{environmentName}/{tenantName}"] = tenant.Entity.Id;
          for (var s = 1; s <= 2; s++) {
            var serviceName = $"service-{s}";
            var service = services.Add(Service.New(
              tenantId: tenant.Entity.Id,
              name: serviceName,
              displayName: $"Service {s} of Environment {e}, Tenant {t}",
              isRootService: true,
              description: default,
              url: default));
            testServiceHierarchyIds[$"{environmentName}/{tenantName}/{serviceName}"] = service.Entity.Id;
            // To add some variation to the service hierarchy, add a child for the first service under a tenant, but
            // leave the other services childless.
            if (s == 1) {
              var childServiceName = $"service-{s}-child";
              var childService = services.Add(Service.New(
                tenantId: tenant.Entity.Id,
                name: childServiceName,
                displayName: $"Child service of Service {s} of Environment {e}, Tenant {t}",
                isRootService: false,
                description: default,
                url: default));
              testServiceHierarchyIds[$"{environmentName}/{tenantName}/{serviceName}/{childServiceName}"] =
                childService.Entity.Id;

              serviceRelationships.Add(new ServiceRelationship(
                parentServiceId: service.Entity.Id,
                serviceId: childService.Entity.Id));
            }
          }
        }
      }

      await context.SaveChangesAsync(ct);

      this.TestServiceHierarchyIds = testServiceHierarchyIds.ToImmutableDictionary();
    });
  }

  /// <summary>
  /// Test setup method that persists two records in the current test database. No additional data is persisted,
  /// only the bare minimum of fields required to create the entity records in the database.
  /// <br/><br/>
  /// User full names are user-1, user-2.
  /// </summary>
  protected async Task SetupTestUsersAsync() {
    var testUserIds = new Dictionary<String, Guid>();

    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      var users = serviceProvider.GetRequiredService<DbSet<User>>();
      var context = serviceProvider.GetRequiredService<DataContext>();

      for (var u = 1; u <= 2; u++) {
        var userFullName = $"user-{u}";
        var user = users.Add(User.New(email: $"{userFullName}@test.com", fullName: userFullName));
        testUserIds[userFullName] = user.Entity.Id;
      }

      await context.SaveChangesAsync(ct);
    });

    this.TestUserIds = testUserIds.ToImmutableDictionary();
  }

  /// <summary>
  /// Test helper method to add the given maintenance record to the current test database.
  /// </summary>
  /// <param name="record"></param>
  /// <typeparam name="TMaintenance"></typeparam>
  protected Task AddMaintenanceAsync<TMaintenance>(TMaintenance record) where TMaintenance : class, Data.Maintenance =>
    this.Fixture.WithDependenciesAsync((serviceProvider, ct) => serviceProvider
      .GetRequiredService<MaintenanceDataHelper<TMaintenance>>().AddAsync(record, ct));

  /// <summary>
  /// Implementation of xUnit IAsyncLifetime dispose method.
  /// </summary>
  public Task DisposeAsync() => Task.CompletedTask;
}
