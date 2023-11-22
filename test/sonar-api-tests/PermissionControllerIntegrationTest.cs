using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Tests;

public class PermissionControllerIntegrationTest : ApiControllerTestsBase {
  private ITestOutputHelper _output;

  private static readonly Guid User1Guid = Guid.NewGuid();
  private static readonly Guid User2Guid = Guid.NewGuid();
  private static readonly User User1 = new User(User1Guid, "test1@test.com", "User1");
  private static readonly User User2 = new User(User2Guid, "test2@test.com", "User2");

  private static readonly Guid Env1Id = Guid.NewGuid();
  private static readonly Guid Tenant11Id = Guid.NewGuid();
  private static readonly Guid Tenant21Id = Guid.NewGuid();
  private static readonly Guid Tenant31Id = Guid.NewGuid();
  private static readonly Environment Env1 = new Environment(Env1Id, "foo1");
  private static readonly Tenant Tenant11 = new Tenant(Tenant11Id, Env1Id, "baz11");
  private static readonly Tenant Tenant21 = new Tenant(Tenant21Id, Env1Id, "baz21");
  private static readonly Tenant Tenant31 = new Tenant(Tenant31Id, Env1Id, "baz31");

  private static readonly Guid Env2Id = Guid.NewGuid();
  private static readonly Guid Tenant12Id = Guid.NewGuid();
  private static readonly Guid Tenant22Id = Guid.NewGuid();
  private static readonly Guid Tenant32Id = Guid.NewGuid();
  private static readonly Environment Env2 = new Environment(Env2Id, "foo2");
  private static readonly Tenant Tenant12 = new Tenant(Tenant12Id, Env2Id, "baz12");
  private static readonly Tenant Tenant22 = new Tenant(Tenant22Id, Env2Id, "baz22");
  private static readonly Tenant Tenant32 = new Tenant(Tenant32Id, Env2Id, "baz32");

  public PermissionControllerIntegrationTest(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper outputHelper) : base(fixture, outputHelper, true) {
    this._output = outputHelper;
  }

  #region Create User Permission
  [Theory]
  [MemberData(nameof(CreateUserPermissionData))]
  public async Task CreateUserPermission(
    PermissionDetails permissionUsedToCreate,
    PermissionDetails permissionCreating,
    HttpStatusCode expectedStatusCode
  ) {
    await this.BuildDbTables();
    if (expectedStatusCode == HttpStatusCode.Unauthorized) {
      var requestBuilder = this.Fixture.CreateAuthenticatedRequest("/api/v2/permissions", Guid.Empty, String.Empty);
      requestBuilder.And(msg => {
        msg.Content = JsonContent.Create(
          new {
            permission = permissionCreating.Permission,
            userEmail = permissionCreating.UserEmail,
            environment = permissionCreating.Environment,
            tenant = permissionCreating.Tenant
          }
        );
      });
      var response = await requestBuilder.PostAsync();
      Assert.Equal(expectedStatusCode, response.StatusCode);
    } else {
      var requestBuilder = this.Fixture.CreateAuthenticatedRequest("/api/v2/permissions", permissionUsedToCreate.Permission, permissionUsedToCreate.Environment, permissionUsedToCreate.Tenant);
      requestBuilder.And(msg => {
        msg.Content = JsonContent.Create(
          new {
            permission = permissionCreating.Permission,
            userEmail = permissionCreating.UserEmail,
            environment = permissionCreating.Environment,
            tenant = permissionCreating.Tenant
          }
        );
      });
      var response = await requestBuilder.PostAsync();
      Assert.Equal(expectedStatusCode, response.StatusCode);
      if (expectedStatusCode == HttpStatusCode.Created) {
        var body = await response.Content.ReadFromJsonAsync<PermissionConfiguration>(SerializerOptions);
        Assert.NotNull(body);
        Assert.Equal(permissionCreating.Permission, body.Permission);
        Assert.Equal(permissionCreating.UserEmail, body.UserEmail);
        Assert.Equal(permissionCreating.Environment, body.Environment);
        Assert.Equal(permissionCreating.Tenant, body.Tenant);
      }
    }
  }

  [Theory]
  [MemberData(nameof(CreatePermission_PermissionAlreadyExists_ReturnsClientError_Data))]
  public async Task CreatePermission_PermissionAlreadyExists_ReturnsClientError(
    String? environmentName,
    String? tenantName,
    PermissionType permissionType) {

    // Setup test data preconditions; we need the base DB values and a unique test user to already exist in the DB.
    await this.BuildDbTables();
    var user = this.CreateUniqueTestUserInDB();

    // A local function for creating the CreatePermission request that we are going to send multiple times.
    // Request instances can't be reused, hence the local factory function.
    RequestBuilder CreateCreatePermissionRequest() => this.Fixture.CreateAuthenticatedRequest(
      "/api/v2/permissions",
      PermissionType.Admin,
      environmentName,
      tenantName
    ).And(msg => msg.Content = JsonContent.Create(
      new PermissionDetails(
        user.Email,
        permissionType,
        environmentName,
        tenantName)
    ));

    // Send the request; it should succeed because we haven't added any permissions for this user yet.
    var firstResponse = await CreateCreatePermissionRequest().PostAsync();
    Assert.True(firstResponse.IsSuccessStatusCode);

    // Send the same request again; this time it should fail the unique constraint.
    var secondResponse = await CreateCreatePermissionRequest().PostAsync();
    Assert.False(secondResponse.IsSuccessStatusCode);
  }
  #endregion

  #region Delete UserPermission
  [Theory]
  [MemberData(nameof(DeleteUserPermissionData))]
  public async Task DeleteUserPermission(
    PermissionDetails permissionUsedToDelete,
    PermissionDetails permissionToDelete,
    HttpStatusCode expectedStatusCode
    ) {
    await this.BuildDbTables();

    //Create Deletion request
    var perm =
      await this.CreatePermission(JsonContent.Create(
        new {
          permission = permissionToDelete.Permission,
          userEmail = permissionToDelete.UserEmail,
          environment = permissionToDelete.Environment,
          tenant = permissionToDelete.Tenant
        }));

    if (expectedStatusCode == HttpStatusCode.Unauthorized) {
      //Delete with a anonymous user
      var requestBuilder = this.Fixture.Server.CreateRequest($"/api/v2/permissions/{perm!.Id.ToString()}");
      var response = await requestBuilder.SendAsync("DELETE");
      //Check results
      Assert.Equal(expectedStatusCode, response.StatusCode);
    } else {
      //Implement deletion of permission
      var requestBuilder = this.Fixture.CreateAuthenticatedRequest(
        $"/api/v2/permissions/{perm!.Id.ToString()}",
        permissionUsedToDelete.Permission,
        permissionUsedToDelete.Environment,
        permissionUsedToDelete.Tenant);
      var response = await requestBuilder.SendAsync("DELETE");
      //Check results
      Assert.Equal(expectedStatusCode, response.StatusCode);
    }
  }

  [Fact]
  public async Task DeleteUserPermission_IdNotFound() {
    // create users, environments, and tenants
    await this.BuildDbTables();

    //Create user permission
    var requestBuilder = this.Fixture.CreateAdminRequest("/api/v2/permissions");
    requestBuilder.And(msg => {
      msg.Content = JsonContent.Create(
        new {
          permission = "Admin",
          userEmail = User1.Email
        }
      );
    });

    var response = await requestBuilder.PostAsync();

    // Verify user permission created
    var body = await response.Content.ReadFromJsonAsync<PermissionConfiguration>(SerializerOptions);
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    Assert.NotNull(body);

    //Delete user permission with a Bad ID
    var requestBuilderDelete = this.Fixture.CreateAdminRequest($"/api/v2/permissions/E6F32AA4-EA14-45A8-85BD-1FBB1FE59BAD");
    var deleteResponse = await requestBuilderDelete.SendAsync("DELETE");

    // Verify response
    Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
  }

  [Fact]
  public async Task DeleteUserPermission_BadRequest() {
    // create users, environments, and tenants
    await this.BuildDbTables();

    //Create user permission with bad user ID
    var requestBuilder = this.Fixture.CreateAdminRequest("/api/v2/permissions");
    requestBuilder.And(msg => {
      msg.Content = JsonContent.Create(
        new {
          permission = "Admin",
          userEmail = "Bademail@gmail.com"
        }
      );
    });

    var response = await requestBuilder.PostAsync();
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }
  #endregion

  #region Update UserPermission
  [Fact]
  public async Task UpdateUserPermission_Success() {
    // create users, environments, and tenants
    await this.BuildDbTables();

    //Create user permission
    var requestBuilder = this.Fixture.CreateAdminRequest("/api/v2/permissions");
    requestBuilder.And(msg => {
      msg.Content = JsonContent.Create(
        new {
          permission = "Admin",
          userEmail = User1.Email
        }
      );
    });

    var response = await requestBuilder.PostAsync();

    // Verify user permission created
    var body = await response.Content.ReadFromJsonAsync<PermissionConfiguration>(SerializerOptions);
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    Assert.NotNull(body);

    //Update user permission just created
    var requestBuilderPut = this.Fixture.CreateAdminRequest($"/api/v2/permissions/{body.Id.ToString()}");
    requestBuilderPut.And(msg => {
      msg.Content = JsonContent.Create(
        new {
          permission = body.Permission,
          userEmail = User2.Email
        }
      );
    });
    var putResponse = await requestBuilderPut.SendAsync("PUT");

    // Verify response
    Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);
  }
  [Fact]
  public async Task UpdateUserPermission_Unauthorized() {
    //Update user.  The Id here is fake, it is not in the database.  Will never make it to the controller to be checked.
    var requestBuilder = this.Fixture.Server.CreateRequest($"/api/v2/permissions/FDDAA3C9-B8BD-494B-8A48-27988AA03BCC");
    requestBuilder.And(msg => {
      msg.Content = JsonContent.Create(
        new {
          permission = PermissionType.Admin,
          userEmail = User2.Email
        }
      );
    });
    var response = await requestBuilder.SendAsync("PUT");
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }
  [Fact]
  public async Task UpdateUserPermission_Forbidden() {
    await this.BuildDbTables();
    var requestBuilder = this.Fixture.CreateAuthenticatedRequest($"/api/v2/permissions/FDDAA3C9-B8BD-494B-8A48-27988AA03BCC", PermissionType.Standard, Env1.Name, null);
    var response = await requestBuilder.SendAsync("PUT");
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }
  [Fact]
  public async Task UpdateUserPermission_BadRequest() {
    // create users, environments, and tenants
    await this.BuildDbTables();

    //Create user permission
    var requestBuilder = this.Fixture.CreateAdminRequest("/api/v2/permissions");
    requestBuilder.And(msg => {
      msg.Content = JsonContent.Create(
        new {
          permission = "Admin",
          userEmail = User1.Email
        }
      );
    });

    var response = await requestBuilder.PostAsync();

    // Verify user permission created
    var body = await response.Content.ReadFromJsonAsync<PermissionConfiguration>(SerializerOptions);
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    Assert.NotNull(body);

    //Update user permission with a Bad ID
    var requestBuilderUpdate = this.Fixture.CreateAdminRequest($"/api/v2/permissions/E6F32AA4-EA14-45A8-85BD-1FBB1FE59BAD");
    requestBuilderUpdate.And(msg => {
      msg.Content = JsonContent.Create(
        new {
          permission = "Admin",
          userEmail = User2.Email
        }
      );
    });

    var putResponse = await requestBuilderUpdate.SendAsync("PUT");

    // Verify response
    Assert.Equal(HttpStatusCode.NotFound, putResponse.StatusCode);
  }

  [Fact]
  public async Task UpdateUserPermission_BadParamters_BadRequest() {
    // create users, environments, and tenants
    await this.BuildDbTables();

    //Create user permission
    var requestBuilder = this.Fixture.CreateAdminRequest("/api/v2/permissions");
    requestBuilder.And(msg => {
      msg.Content = JsonContent.Create(
        new {
          permission = "Admin",
          userEmail = User1.Email
        }
      );
    });

    var response = await requestBuilder.PostAsync();

    // Verify user permission created
    var body = await response.Content.ReadFromJsonAsync<PermissionConfiguration>(SerializerOptions);
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    Assert.NotNull(body);

    //Update user permission with bad request in body - missing environment.
    var requestBuilderUpdate = this.Fixture.CreateAdminRequest($"/api/v2/permissions/{body.Id.ToString()}");
    requestBuilderUpdate.And(msg => {
      msg.Content = JsonContent.Create(
        new {
          permission = "Admin",
          userEmail = User2.Email,
          tenant = Tenant11.Name    //This will fail because there is no environment
        }
      );
    });

    var putResponse = await requestBuilderUpdate.SendAsync("PUT");

    // Verify response
    Assert.Equal(HttpStatusCode.BadRequest, putResponse.StatusCode);
  }
  #endregion

  #region Get Me UserPermission

  [Fact]
  public async Task GetCurrentUserPermissions_Global_Success() {
    // create users, environments, and tenants
    await this.BuildDbTables();

    var requestBuilder = this.Fixture.CreateAuthenticatedRequest("/api/v2/permissions/me", PermissionType.Admin, null, null);
    var response = await requestBuilder.GetAsync();

    // Verify user permission created
    var body = await response.Content.ReadFromJsonAsync<List<PermissionConfiguration>>(SerializerOptions);
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(body);
    Assert.Single(body);
    Assert.Contains(body, pc => pc.Environment == null);
    Assert.Contains(body, pc => pc.Tenant == null);
  }

  // Validate that the legacy API Key format still works
  [Fact]
  public async Task GetCurrentUserPermissions_LegacyApiKeyFormat_Success() {
    // create users, environments, and tenants
    await this.BuildDbTables();

    var requestBuilder = this.Fixture.CreateLegacyAuthenticatedRequest("/api/v2/permissions/me", PermissionType.Admin, null, null);
    var response = await requestBuilder.GetAsync();

    // Verify user permission created
    var body = await response.Content.ReadFromJsonAsync<List<PermissionConfiguration>>(SerializerOptions);
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(body);
    Assert.Single(body);
    Assert.Contains(body, pc => pc.Environment == null);
    Assert.Contains(body, pc => pc.Tenant == null);
  }
  [Fact]
  public async Task GetCurrentUserPermission_Unauthorized() {
    var requestBuilder = this.Fixture.Server.CreateRequest($"/api/v2/permissions/me");
    var response = await requestBuilder.GetAsync();
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }
  [Fact]
  public async Task GetCurrentUserPermissions_Environment_Success() {
    // create users, environments, and tenants
    await this.BuildDbTables();

    var requestBuilder = this.Fixture.CreateAuthenticatedRequest("/api/v2/permissions/me", PermissionType.Admin, Env1.Name, null);
    var response = await requestBuilder.GetAsync();

    // Verify user permission created
    var body = await response.Content.ReadFromJsonAsync<List<PermissionConfiguration>>(SerializerOptions);
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(body);
    Assert.Single(body);
    Assert.Contains(body, pc => ((pc.Environment == Env1.Name) && (pc.Tenant == null)));
  }

  [Fact]
  public async Task GetCurrentUserPermissions_Tenant_Success() {
    // create users, environments, and tenants
    await this.BuildDbTables();

    var requestBuilder = this.Fixture.CreateAuthenticatedRequest("/api/v2/permissions/me", PermissionType.Admin, Env1.Name, Tenant11.Name);
    var response = await requestBuilder.GetAsync();
    var body = await response.Content.ReadFromJsonAsync<List<PermissionConfiguration>>(SerializerOptions);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(body);
    Assert.Single(body);
    Assert.Contains(body, pc => ((pc.Environment == Env1.Name) && (pc.Tenant == Tenant11.Name)));
  }

  #endregion

  #region GetUserPermissions
  [Fact]
  public async Task GetUserPermissions_Global_Success() {
    // create users, environments, and tenants
    await this.BuildDbTables();

    await this.CreatePermission(JsonContent.Create(new { permission = "Admin", userEmail = User1.Email, environment = Env1.Name }));
    await this.CreatePermission(JsonContent.Create(new { permission = "Admin", userEmail = User2.Email, environment = Env2.Name }));

    var requestBuilder = this.Fixture.CreateAuthenticatedRequest("/api/v2/permissions", PermissionType.Admin, null, null);
    var response = await requestBuilder.GetAsync();

    // Verify user permission created
    var body = await response.Content.ReadFromJsonAsync<List<PermissionConfiguration>>(SerializerOptions);
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(body);
    Assert.Equal(2, body.Count);
    Assert.Contains(body, pc => pc.UserEmail == User1.Email);
    Assert.Contains(body, pc => pc.UserEmail == User2.Email);
  }
  [Fact]
  public async Task GetUserPermission_Unauthorized() {
    //Update user.  The Id here is fake, it is not in the database.  Will never make it to the controller to be checked.
    var requestBuilder = this.Fixture.Server.CreateRequest($"/api/v2/permissions");
    requestBuilder.And(msg => {
      msg.Content = JsonContent.Create(
        new {
          permission = PermissionType.Admin,
          userEmail = User2.Email
        }
      );
    });
    var response = await requestBuilder.GetAsync();
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }
  [Fact]
  public async Task GetUserPermissions_Environment_Success() {
    await this.BuildDbTables();

    await this.CreatePermission(JsonContent.Create(new { permission = "Admin", userEmail = User1.Email, environment = Env1.Name }));
    await this.CreatePermission(JsonContent.Create(new { permission = "Admin", userEmail = User1.Email, environment = Env2.Name }));
    await this.CreatePermission(JsonContent.Create(new { permission = "Admin", userEmail = User2.Email, environment = Env2.Name, tenant = Tenant11.Name }));

    var requestBuilder = this.Fixture.CreateAuthenticatedRequest("/api/v2/permissions", PermissionType.Admin, Env1.Name, null);
    var response = await requestBuilder.GetAsync();

    // Verify user permission created
    var body = await response.Content.ReadFromJsonAsync<List<PermissionConfiguration>>(SerializerOptions);
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(body);
    Assert.Single(body);
    Assert.Contains(body, pc => pc.UserEmail == User1.Email);
  }
  [Fact]
  public async Task GetUserPermissions_Tenant_Success() {
    await this.BuildDbTables();
    await this.CreatePermission(JsonContent.Create(new { permission = "Admin", userEmail = User1.Email, environment = Env1.Name }));
    await this.CreatePermission(JsonContent.Create(new { permission = "Admin", userEmail = User1.Email, environment = Env2.Name }));
    await this.CreatePermission(JsonContent.Create(new { permission = "Admin", userEmail = User2.Email, environment = Env2.Name, tenant = Tenant12.Name }));

    var requestBuilder = this.Fixture.CreateAuthenticatedRequest("/api/v2/permissions", PermissionType.Admin, Env2.Name, Tenant12.Name);
    var response = await requestBuilder.GetAsync();

    // Verify user permission created
    var body = await response.Content.ReadFromJsonAsync<List<PermissionConfiguration>>(SerializerOptions);
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(body);
    Assert.Single(body);
    Assert.Contains(body, pc => pc.UserEmail == User2.Email);
  }
  #endregion

  internal async Task BuildDbTables() {
    // create users, environments, and tenants
    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var dbContext = services.GetRequiredService<DataContext>();
      var dbSet = services.GetRequiredService<DbSet<User>>();
      var dbSetEnv = services.GetRequiredService<DbSet<Environment>>();
      var dbSetTenant = services.GetRequiredService<DbSet<Tenant>>();
      await dbSet.AddRangeAsync(new[] { User1, User2 }, cancellationToken);
      await dbSetEnv.AddRangeAsync(new[] { Env1, Env2 }, cancellationToken);
      await dbSetTenant.AddRangeAsync(new[] { Tenant11, Tenant21, Tenant31, Tenant12, Tenant22, Tenant32 }, cancellationToken);
      await dbContext.SaveChangesAsync(cancellationToken);
    });
  }
  internal async Task BuildDBTables_NoEnvironment_NoTenant() {
    // create users, environments, and tenants
    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var dbContext = services.GetRequiredService<DataContext>();
      var dbSet = services.GetRequiredService<DbSet<User>>();
      var dbSetEnv = services.GetRequiredService<DbSet<Environment>>();
      var dbSetTenant = services.GetRequiredService<DbSet<Tenant>>();
      await dbSet.AddRangeAsync(new[] { User1, User2 }, cancellationToken);
      await dbContext.SaveChangesAsync(cancellationToken);
    });
  }
  internal async Task BuildDBTables_NoUsers() {
    // create users, environments, and tenants
    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var dbContext = services.GetRequiredService<DataContext>();
      var dbSet = services.GetRequiredService<DbSet<User>>();
      var dbSetEnv = services.GetRequiredService<DbSet<Environment>>();
      var dbSetTenant = services.GetRequiredService<DbSet<Tenant>>();
      await dbSetEnv.AddRangeAsync(new[] { Env1 }, cancellationToken);
      await dbSetTenant.AddRangeAsync(new[] { Tenant11 }, cancellationToken);
      await dbContext.SaveChangesAsync(cancellationToken);
    });
  }

  internal User CreateUniqueTestUserInDB() {
    var userId = Guid.NewGuid();
    var user = new User(userId, $"{userId}@test.com", $"Test User {userId}");
    this.Fixture.WithDependencies(services => {
      var db = services.GetRequiredService<DataContext>();
      var users = services.GetRequiredService<DbSet<User>>();
      users.Add(user);
      db.SaveChanges();
    });
    return user;
  }

  internal async Task<PermissionConfiguration?> CreatePermission(HttpContent content) {
    //Create User Permission request
    var requestBuilder = this.Fixture.CreateAdminRequest("/api/v2/permissions");
    requestBuilder.And(msg => {
      msg.Content = content;
    });

    //Send Post Request
    var response = await requestBuilder.PostAsync();

    // Verify response that User Permission has been created
    if (response.StatusCode == HttpStatusCode.Created) {
      var body = await response.Content.ReadFromJsonAsync<PermissionConfiguration>(SerializerOptions);
      Assert.Equal(HttpStatusCode.Created, response.StatusCode);
      return body;
    } else {
      return null;
    }
  }


  public static IEnumerable<Object[]> CreateUserPermissionData() {
    yield return new Object[] {
      new PermissionDetails(User1.Email, PermissionType.Admin, Env1.Name, Tenant11.Name),
      new PermissionDetails(User1.Email, PermissionType.Standard, Env1.Name, Tenant11.Name),
      HttpStatusCode.Created
    };
    yield return new Object[] {
      new PermissionDetails(User1.Email, PermissionType.Admin, Env1.Name, null),
      new PermissionDetails(User1.Email, PermissionType.Admin, Env1.Name, Tenant11.Name),
      HttpStatusCode.Unauthorized
    };
    yield return new Object[] {
      new PermissionDetails(User1.Email, PermissionType.Admin, Env1.Name, Tenant11.Name),
      new PermissionDetails(User1.Email, PermissionType.Standard, Env1.Name, null),
      HttpStatusCode.Forbidden
    };
    yield return new Object[] {
      new PermissionDetails(User1.Email, PermissionType.Admin, Env1.Name, null),
      new PermissionDetails(User1.Email, PermissionType.Admin, Env2.Name, null),
      HttpStatusCode.Forbidden
    };
    yield return new Object[] {
      new PermissionDetails(User1.Email, PermissionType.Admin, null, null),
      new PermissionDetails(User1.Email, PermissionType.Admin, null, null),
      HttpStatusCode.Created
    };
    yield return new Object[] {
      new PermissionDetails(User1.Email, PermissionType.Admin, Env2.Name, null),
      new PermissionDetails(User1.Email, PermissionType.Standard, Env2.Name, null),
      HttpStatusCode.Created
    };
    yield return new Object[] {
      new PermissionDetails(User1.Email, PermissionType.Admin, null, null),
      new PermissionDetails(User1.Email, PermissionType.Standard, null, null),
      HttpStatusCode.Created
    };
    yield return new Object[] {
      new PermissionDetails(User1.Email, PermissionType.Standard, null, null),
      new PermissionDetails(User1.Email, PermissionType.Admin, null, null),
      HttpStatusCode.Forbidden
    };
    yield return new Object[] {
      new PermissionDetails(User1.Email, PermissionType.Admin, null, null),
      new PermissionDetails("UserDoesNotExist@gmail.com", PermissionType.Admin, Env1.Name, null),
      HttpStatusCode.BadRequest
    };
    yield return new Object[] {
      new PermissionDetails("UserDoesNotExist@gmail.com", PermissionType.Standard, null, null),
      new PermissionDetails(User1.Email, PermissionType.Admin, Env1.Name, Tenant11.Name),
      HttpStatusCode.Forbidden
    };
  }

  public static IEnumerable<Object?[]> CreatePermission_PermissionAlreadyExists_ReturnsClientError_Data =>
    new[] {
      // Tenant-scoped permissions
      new Object[] { Env1.Name, Tenant11.Name, PermissionType.Admin },
      new Object[] { Env1.Name, Tenant11.Name, PermissionType.Standard },
      // Environment-scoped permissions
      new Object?[] { Env1.Name, null, PermissionType.Admin },
      new Object?[] { Env1.Name, null, PermissionType.Standard },
      // Global-scoped permissions
      new Object?[] { null, null, PermissionType.Admin },
      new Object?[] { null, null, PermissionType.Standard },
    };

  //Here are the inputs and expected output for Deleting permissions
  //permission used to perform deletion
  //Permission created and that will be deleted
  //Expected status code
  public static IEnumerable<Object[]> DeleteUserPermissionData() {
    yield return new Object[] {
      new PermissionDetails(User1.Email, PermissionType.Standard, Env1.Name, Tenant11.Name),
      new PermissionDetails(User1.Email, PermissionType.Admin, Env1.Name, Tenant11.Name),
      HttpStatusCode.Forbidden
    };
    yield return new Object[] {
      new PermissionDetails(User1.Email, PermissionType.Admin, Env1.Name, Tenant11.Name),
      new PermissionDetails(User1.Email, PermissionType.Admin, Env1.Name, null),
      HttpStatusCode.Forbidden
    };
    yield return new Object[] {
      new PermissionDetails(User1.Email, PermissionType.Admin, Env1.Name, null),
      new PermissionDetails(User1.Email, PermissionType.Admin, Env1.Name, Tenant11.Name),
      HttpStatusCode.NoContent
    };
    yield return new Object[] {
      new PermissionDetails(User1.Email, PermissionType.Admin, Env1.Name, null),
      new PermissionDetails(User1.Email, PermissionType.Admin, null, null),
      HttpStatusCode.Forbidden
    };
    yield return new Object[] {
      new PermissionDetails(User1.Email, PermissionType.Admin, null, null),
      new PermissionDetails(User1.Email, PermissionType.Admin, null, null),
      HttpStatusCode.NoContent
    };
    yield return new Object[] {
      new PermissionDetails(User1.Email, PermissionType.Standard, null, null),
      new PermissionDetails(User1.Email, PermissionType.Admin, null, null),
      HttpStatusCode.Unauthorized
    };
  }


}
