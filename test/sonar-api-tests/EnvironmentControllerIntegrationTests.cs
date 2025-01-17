using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Helpers.Maintenance;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Tests;

public class EnvironmentControllerIntegrationTests : ApiControllerTestsBase {
  public EnvironmentControllerIntegrationTests(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper outputHelper) :
    base(fixture, outputHelper) {
  }

  private readonly String TestScheduledMaintenanceEnvName = "env-test";

  // List Environments Anonymously
  [Fact]
  public async Task ListEnvironments_Success() {
    var (env, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var response = await
      this.Fixture.Server.CreateRequest($"api/v2/environments")
        .GetAsync();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<EnvironmentHealth[]>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(1, body.Count(e => e.EnvironmentName == env));
  }

  // Get Environment Anonymously
  [Fact]
  public async Task GetEnvironment_Success() {
    var (env, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var response = await
      this.Fixture.Server.CreateRequest($"api/v2/environments/{env}")
        .GetAsync();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<EnvironmentHealth>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(env, body.EnvironmentName);
  }

  // Create Environment Anon - 401
  [Fact]
  public async Task CreateEnvironment_Anonymous_Unauthorized() {
    var testName = $"{Guid.NewGuid()}";

    var response = await
      this.Fixture.Server.CreateRequest("api/v2/environments")
        .And(req => {
          req.Content = JsonContent.Create(new {
            Name = testName
          });
        })
        .PostAsync();

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  // Create Environment Non-Admin - 403
  [Fact]
  public async Task CreateEnvironment_Standard_Forbidden() {
    var testName = $"{Guid.NewGuid()}";

    var response = await
      this.Fixture.CreateAuthenticatedRequest("api/v2/environments", PermissionType.Standard)
        .And(req => {
          req.Content = JsonContent.Create(new {
            Name = testName
          });
        })
        .PostAsync();

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  // Create Environment Scoped Admin - 403
  [Fact]
  public async Task CreateEnvironment_ScopedAdmin_Forbidden() {
    var (env, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var testName = $"{Guid.NewGuid()}";

    var response = await
      this.Fixture.CreateAuthenticatedRequest("api/v2/environments", PermissionType.Admin, env)
        .And(req => {
          req.Content = JsonContent.Create(new {
            Name = testName
          });
        })
        .PostAsync();

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  // Create Environment Global Admin
  [Fact]
  public async Task CreateEnvironment_Admin_Success() {
    var testName = $"{Guid.NewGuid()}";

    var response = await
      this.Fixture.CreateAuthenticatedRequest("api/v2/environments", PermissionType.Admin)
        .And(req => {
          req.Content = JsonContent.Create(new {
            Name = testName
          });
        })
        .PostAsync();

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<EnvironmentModel>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(testName, body.Name);
  }

  // Create Environment With Scheduled Maintenance Config
  [Fact]
  public async Task CreateEnvironment_WithScheduledMaintenanceConfig_Success() {
    var scheduledMaintenances = ImmutableList<ScheduledMaintenanceConfiguration>.Empty
      .Add(new ScheduledMaintenanceConfiguration(
        scheduleExpression: "* 20 4 * ?",
        durationMinutes: 60
      ));

    var environmentToCreate = new EnvironmentModel(
      name: "test-environment-1",
      isNonProd: true,
      scheduledMaintenances: scheduledMaintenances
    );

    var response = await
      this.Fixture.CreateAuthenticatedRequest(url: "api/v2/environments", PermissionType.Admin)
        .And(req => {
          req.Content = JsonContent.Create(environmentToCreate);
        })
        .PostAsync();

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var createdEnvironment = await response.Content.ReadFromJsonAsync<EnvironmentModel>(
      SerializerOptions
    );

    Assert.NotNull(createdEnvironment);
    Assert.Equal(environmentToCreate.Name, createdEnvironment.Name);
    Assert.Equivalent(scheduledMaintenances, createdEnvironment.ScheduledMaintenances);

  }

  // Update Environment with scheduled maintenance config
  [Fact]
  public async Task UpdateEnvironment_WithScheduledMaintenanceConfig_Success() {
    var scheduledMaintenances = ImmutableList<ScheduledMaintenanceConfiguration>.Empty
      .Add(new ScheduledMaintenanceConfiguration(
        scheduleExpression: "* 12 * * ?",
        durationMinutes: 40
      ));

    var environmentToCreate = new EnvironmentModel(
      name: TestScheduledMaintenanceEnvName,
      isNonProd: false,
      scheduledMaintenances: scheduledMaintenances
    );

    var creation = await
      this.Fixture.CreateAdminRequest($"api/v2/environments")
        .And(req => {
          req.Content = JsonContent.Create(environmentToCreate);
        })
        .PostAsync();

    AssertHelper.Precondition(
      creation.StatusCode == HttpStatusCode.Created,
      "Unable to create environment"
    );

    var expectedScheduledMaintenanceConfig = new ScheduledMaintenanceConfiguration(
      scheduleExpression: "* 10 11 * ?",
      durationMinutes: 60);

    var newScheduledMaintenances = ImmutableList<ScheduledMaintenanceConfiguration>.Empty
      .Add(expectedScheduledMaintenanceConfig);

    var updatedEnvironmentModel = new EnvironmentModel(
      name: TestScheduledMaintenanceEnvName,
      isNonProd: false,
      scheduledMaintenances: newScheduledMaintenances
    );
    // then request for an update
    var response = await
      this.Fixture.CreateAuthenticatedRequest(url: $"api/v2/environments/{this.TestScheduledMaintenanceEnvName}", PermissionType.Admin)
        .And(req => {
          req.Content = JsonContent.Create(updatedEnvironmentModel);
        })
        .SendAsync("PUT");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var createdEnvironment = await response.Content.ReadFromJsonAsync<EnvironmentModel>(
      SerializerOptions
    );

    Assert.NotNull(createdEnvironment);
    Assert.Equal(environmentToCreate.Name, createdEnvironment.Name);

    var updatedMaintenanceConfig = Assert.Single(createdEnvironment.ScheduledMaintenances);

    Assert.Equal(expectedScheduledMaintenanceConfig, updatedMaintenanceConfig);
  }

  // Delete Environment Anon - 401
  [Fact]
  public async Task DeleteEnvironment_Anonymous_Unauthorized() {
    var (env, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var response = await
      this.Fixture.Server.CreateRequest($"api/v2/environments/{env}")
        .SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  // Delete Environment Non-Admin - 403
  [Fact]
  public async Task DeleteEnvironment_Standard_Forbidden() {
    var (env, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var response = await
      this.Fixture.CreateAuthenticatedRequest($"api/v2/environments/{env}", PermissionType.Standard)
        .SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  // Delete Environment Incorrect Scoped Admin - 403
  [Fact]
  public async Task DeleteEnvironment_OtherScopedAdmin_Forbidden() {
    var (env, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var (env2, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var response = await
      this.Fixture.CreateAuthenticatedRequest($"api/v2/environments/{env}", PermissionType.Admin, env2)
        .SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  // Delete Environment Global Admin, Tenant Config Exists - 400
  [Fact]
  public async Task DeleteEnvironment_TenantConfigExists_BadRequest() {
    var (env, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var response = await
      this.Fixture.CreateAuthenticatedRequest($"api/v2/environments/{env}", PermissionType.Admin, env)
        .SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  // Delete Environment Scoped Admin - Success
  [Fact]
  public async Task DeleteEnvironment_CorrectlyScopedAdmin_Success() {
    var env = $"{Guid.NewGuid()}";

    var creation = await
      this.Fixture.CreateAdminRequest($"api/v2/environments")
        .And(req => {
          req.Content = JsonContent.Create(new {
            Name = env
          });
        })
        .PostAsync();

    AssertHelper.Precondition(
      creation.StatusCode == HttpStatusCode.Created,
      "Unable to create environment"
    );

    var response = await
      this.Fixture.CreateAuthenticatedRequest($"api/v2/environments/{env}", PermissionType.Admin, env)
        .SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
  }

  // Delete Environment Global Admin - Success
  [Fact]
  public async Task DeleteEnvironment_GlobalAdmin_Success() {
    var env = $"{Guid.NewGuid()}";

    var creation = await
      this.Fixture.CreateAdminRequest($"api/v2/environments")
        .And(req => {
          req.Content = JsonContent.Create(new {
            Name = env
          });
        })
        .PostAsync();

    AssertHelper.Precondition(
      creation.StatusCode == HttpStatusCode.Created,
      "Unable to create environment"
    );

    var response = await
      this.Fixture.CreateAuthenticatedRequest($"api/v2/environments/{env}", PermissionType.Admin)
        .SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
  }
}
