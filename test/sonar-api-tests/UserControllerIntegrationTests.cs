using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

public class UserControllerIntegrationTests : ApiControllerTestsBase {
  private static readonly Guid User1Id = Guid.NewGuid();
  private static readonly User TestUser1 = new User(
    User1Id,
    "test1@test.com",
    "User1");
  private static readonly Guid User2Id = Guid.NewGuid();
  private static readonly User TestUser2 = new User(
    User2Id,
    "test2@test.com",
    "User2");

  private static readonly Guid Env1Id = Guid.NewGuid();
  private static readonly Guid Tenant1Id = Guid.NewGuid();
  private static readonly Environment Env1 = new Environment(Env1Id, "env1");
  private static readonly Tenant Tenant1 = new Tenant(Tenant1Id, Env1Id, "ten1");
  private static readonly PermissionType PermissionType1 = PermissionType.Admin;
  private static readonly UserPermission UserPermission1 = new UserPermission(
    Guid.NewGuid(),
    User1Id,
    Env1Id,
    Tenant1Id,
    PermissionType1
  );

  private static readonly Guid Env2Id = Guid.NewGuid();
  private static readonly Guid Tenant2Id = Guid.NewGuid();
  private static readonly Environment Env2 = new Environment(Env2Id, "env2");
  private static readonly Tenant Tenant2 = new Tenant(Tenant2Id, Env2Id, "ten2");
  private static readonly PermissionType PermissionType2 = PermissionType.Standard;
  private static readonly UserPermission UserPermission2 = new UserPermission(
    Guid.NewGuid(),
    User2Id,
    Env2Id,
    Tenant2Id,
    PermissionType2
  );

  private ITestOutputHelper _output;

  public UserControllerIntegrationTests(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper outputHelper) : base(fixture, outputHelper, true) {
    this._output = outputHelper;
  }

  [Fact]
  public async Task UpdateCurrentUser_BadRequest() {
    var response = await this.Fixture.CreateAdminRequest("/api/v2/user")
      .AddHeader(name: "Accept", value: "application/json")
      .PostAsync();

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task UpdateCurrentUser_Unauthorized() {
    var response = await this.Fixture.Server.CreateRequest("/api/v2/user")
      .AddHeader(name: "Accept", value: "application/json")
      .PostAsync();

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task GetUsers_Unauthorized() {
    var response = await this.Fixture.Server.CreateRequest("/api/v2/user")
      .AddHeader(name: "Accept", value: "application/json")
      .GetAsync();

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task GetUsers_Success() {
    // create users
    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var dbContext = services.GetRequiredService<DataContext>();
      var dbSet = services.GetRequiredService<DbSet<User>>();
      await dbSet.AddRangeAsync(new[] { TestUser1, TestUser2 }, cancellationToken);
      await dbContext.SaveChangesAsync(cancellationToken);
    });

    var response = await this.Fixture.CreateAdminRequest("/api/v2/user")
      .AddHeader(name: "Accept", value: "application/json")
      .GetAsync();
    var body = await response.Content.ReadFromJsonAsync<User[]>(SerializerOptions);

    // Verify that response contains only 2 elements
    // Verify that both TestUsers are included in response
    Assert.Equal(2, body.Length);
    Assert.Contains(body, user => user.Email == TestUser1.Email);
    Assert.Contains(body, user => user.Email == TestUser2.Email);
  }

  [Fact]
  public async Task GetUserAdminStatus_Success() {
    // create users and permissions
    await this.BuildUserPermissionDbTables();

    var response = await this.Fixture.CreateAdminRequest("/api/v2/user")
      .AddHeader(name: "Accept", value: "application/json")
      .GetAsync();
    var body = await response.Content.ReadFromJsonAsync<CurrentUserView[]>(SerializerOptions);
    Assert.Equal(2, body.Length);
    Assert.Contains(body, user => user.IsAdmin);
    Assert.Contains(body, user => !user.IsAdmin);
  }

  [Fact]
  public async Task GetUserPermissionView_Success() {
    // create users and permissions
    await this.BuildUserPermissionDbTables();

    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var permissionsRepo = services.GetRequiredService<IPermissionsRepository>();

      // Test first permission tree that contains User1 with Admin permissions for Env1/Tenant1
      var userPv1 = await permissionsRepo.GetUserPermissionsView(User1Id, cancellationToken);
      var permissionEntry = Assert.Single(userPv1.PermissionTree);
      Assert.Equal(permissionEntry.Key, Env1.Name);
      Assert.Single(permissionEntry.Value);
      Assert.Contains(permissionEntry.Value, permission => permission == Tenant1.Name);

      // Test second permission tree that should be empty due to standard permission
      var userPv2 = await permissionsRepo.GetUserPermissionsView(User2Id, cancellationToken);
      Assert.Empty(userPv2.PermissionTree);
    });
  }

  [Fact]
  public async Task UpdateCurrentUser_Success() {
    var user = await this.Fixture.CreateGlobalAdminUser();
    var res = await this.Fixture.CreateFakeJwtRequest("api/v2/user", user.Email)
      .And(req => {
        req.Content = JsonContent.Create(new {
          IsEnabled = true
        });
      }).PostAsync();
    Assert.Equal(expected: HttpStatusCode.Created, res.StatusCode);
    var body = await res.Content.ReadFromJsonAsync<CurrentUserView>(SerializerOptions);
    Assert.NotNull(body);
    Assert.Equal(expected: user.Email, actual: body.Email);
    // We don't include a 'name' claim in the fake auth scheme,
    // so the full name will default to the user email when handled by controller
    Assert.Equal(expected: user.Email, actual: body.FullName);
  }

  internal async Task BuildUserPermissionDbTables() {
    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var dbContext = services.GetRequiredService<DataContext>();
      var userDbSet = services.GetRequiredService<DbSet<User>>();
      var permissionDbSet = services.GetRequiredService<DbSet<UserPermission>>();
      var envDbSet = services.GetRequiredService<DbSet<Environment>>();
      var tenantDbSet = services.GetRequiredService<DbSet<Tenant>>();
      await envDbSet.AddRangeAsync(new[] { Env1, Env2 });
      await tenantDbSet.AddRangeAsync(new[] { Tenant1, Tenant2 });
      await userDbSet.AddRangeAsync(new[] { TestUser1, TestUser2 }, cancellationToken);
      await permissionDbSet.AddRangeAsync(new[] { UserPermission1, UserPermission2 }, cancellationToken);
      await dbContext.SaveChangesAsync(cancellationToken);
    });
  }
}
