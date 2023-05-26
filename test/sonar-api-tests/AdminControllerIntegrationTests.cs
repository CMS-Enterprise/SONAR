using System.Net;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Tests;

public class AdminControllerIntegrationTests : ApiControllerTestsBase {
  public AdminControllerIntegrationTests(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper outputHelper) : base(fixture, outputHelper) {
  }

  [Fact]
  public async Task InitializeDatabase_BuiltInAdmin_Success() {
    var request =
      this.Fixture.CreateAdminRequest("/api/admin/initialize");

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task InitializeDatabase_GlobalAdmin_Success() {
    var request =
      this.Fixture.CreateAuthenticatedRequest(
        "/api/admin/initialize",
        ApiKeyType.Admin
      );

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task InitializeDatabase_Unauthorized() {
    var request =
      this.Fixture.Server.CreateRequest("/api/admin/initialize");

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task InitializeDatabase_GlobalNonAdmin_Forbidden() {
    var request =
      this.Fixture.CreateAuthenticatedRequest(
        "/api/admin/initialize",
        ApiKeyType.Standard
      );

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task InitializeDatabase_EnvironmentScopedAdmin_Forbidden() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var request =
      this.Fixture.CreateAuthenticatedRequest(
        "/api/admin/initialize",
        ApiKeyType.Admin,
        environment
      );

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task InitializeDatabase_TenantScopedAdmin_Forbidden() {
    var (environment, tenant) = await this.Fixture.CreateEmptyTestConfiguration();
    var request =
      this.Fixture.CreateAuthenticatedRequest(
        "/api/admin/initialize",
        ApiKeyType.Admin,
        environment,
        tenant
      );

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }
}
