using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Tests;

public class UserControllerIntegrationTests : ApiControllerTestsBase {

  private static readonly User TestUser1 = new User(
    Guid.NewGuid(),
    "test1@test.com",
    "Test",
    "User1");
  private static readonly User TestUser2 = new User(
    Guid.NewGuid(),
    "test2@test.com",
    "Test",
    "User2");
  private ITestOutputHelper _output;

  public UserControllerIntegrationTests(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper outputHelper) : base(fixture, outputHelper) {
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

}
