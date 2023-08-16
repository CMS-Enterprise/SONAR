using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Tests;

public class ApiKeyControllerIntegrationTests : ApiControllerTestsBase {
  public ApiKeyControllerIntegrationTests(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper outputHelper) : base(fixture, outputHelper) {
  }

  [Fact]
  public async Task CreateGlobalApiKey_BuiltInAdmin_Success() {
    var request =
      this.Fixture
        .CreateAdminRequest("/api/v2/keys")
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString()
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task CreateGlobalApiKey_GlobalAdmin_Success() {
    var request =
      this.Fixture
        .CreateAuthenticatedRequest("/api/v2/keys", PermissionType.Admin)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString()
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task CreateGlobalApiKey_Anonymous_Unauthorized() {
    var request =
      this.Fixture.Server
        .CreateRequest("/api/v2/keys")
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString()
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task CreateEnvironmentApiKey_GlobalAdmin_Success() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAuthenticatedRequest("/api/v2/keys", PermissionType.Admin)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task CreateEnvironmentApiKey_BuiltInAdmin_Success() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAdminRequest("/api/v2/keys")
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task CreateEnvironmentApiKey_EnvironmentAdmin_Success() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAuthenticatedRequest("/api/v2/keys", PermissionType.Admin, environment)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task CreateEnvironmentApiKey_GlobalStandard_Forbidden() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAuthenticatedRequest("/api/v2/keys", PermissionType.Standard)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreateEnvironmentApiKey_EnvironmentStandard_Forbidden() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAuthenticatedRequest("/api/v2/keys", PermissionType.Standard, environment)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreateEnvironmentApiKey_OtherEnvironmentAdmin_Forbidden() {
    var (environment1, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var (environment2, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAuthenticatedRequest("/api/v2/keys", PermissionType.Admin, environment1)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment2
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreateTenantApiKey_GlobalAdmin_Success() {
    var (environment, tenant) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAuthenticatedRequest("/api/v2/keys", PermissionType.Admin)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment,
            tenant
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task CreateTenantApiKey_BuiltInAdmin_Success() {
    var (environment, tenant) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAdminRequest("/api/v2/keys")
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment,
            tenant
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task CreateTenantApiKey_Anonymous_Unauthorized() {
    var (environment, tenant) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture.Server
        .CreateRequest("/api/v2/keys")
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment,
            tenant
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task CreateTenantApiKey_GlobalStandard_Forbidden() {
    var (environment, tenant) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAuthenticatedRequest("/api/v2/keys", PermissionType.Standard)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment,
            tenant
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreateTenantApiKey_EnvironmentAdmin_Success() {
    var (environment, tenant) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAuthenticatedRequest("/api/v2/keys", PermissionType.Admin, environment)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment,
            tenant
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task CreateTenantApiKey_OtherEnvironmentAdmin_Forbidden() {
    var (environment1, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var (environment2, tenant2) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAuthenticatedRequest("/api/v2/keys", PermissionType.Admin, environment1)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment2,
            tenant2
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task GetApiKeys_GlobalAdmin_Success() {
    var request =
      this.Fixture.CreateAuthenticatedRequest("/api/v2/keys", PermissionType.Admin);

    var response = await request.GetAsync();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task GetApiKeys_BuiltInAdmin_Success() {
    var request = this.Fixture.CreateAdminRequest("/api/v2/keys");

    var response = await request.GetAsync();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task GetApiKeys_Anonymous_Unauthorized() {
    var request = this.Fixture.Server.CreateRequest("/api/v2/keys");

    var response = await request.GetAsync();

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task GetApiKeys_GlobalStandard_Forbidden() {
    var request =
      this.Fixture.CreateAuthenticatedRequest("/api/v2/keys", PermissionType.Standard);

    var response = await request.GetAsync();

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task GetApiKeys_EnvironmentAdmin_Success() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture.CreateAuthenticatedRequest("/api/v2/keys", PermissionType.Admin, environment);

    var response = await request.GetAsync();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task DeleteApiKey_GlobalAdmin_Success() {
    var existingApiKey = this.Fixture.CreateApiKey(PermissionType.Admin);

    var request =
      this.Fixture.CreateAuthenticatedRequest($"/api/v2/keys/{existingApiKey.Id}", PermissionType.Admin);

    var response = await request.SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
  }

  [Fact]
  public async Task DeleteApiKey_BuiltInAdmin_Success() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var existingApiKey = this.Fixture.CreateApiKey(PermissionType.Standard, environment);

    var request =
      this.Fixture.CreateAdminRequest($"/api/v2/keys/{existingApiKey.Id}");

    var response = await request.SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
  }

  [Fact]
  public async Task DeleteApiKey_Anonymous_Unauthorized() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var existingApiKey = this.Fixture.CreateApiKey(PermissionType.Standard, environment);

    var request =
      this.Fixture.Server.CreateRequest($"/api/v2/keys/{existingApiKey.Id}");

    var response = await request.SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task DeleteApiKey_GlobalStandard_Forbidden() {
    var existingApiKey = this.Fixture.CreateApiKey(PermissionType.Standard);

    var request =
      this.Fixture.CreateAuthenticatedRequest($"/api/v2/keys/{existingApiKey.Id}", PermissionType.Standard);

    var response = await request.SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task DeleteEnvironmentApiKey_EnvironmentAdmin_Success() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var existingApiKey = this.Fixture.CreateApiKey(PermissionType.Standard, environment);

    var request =
      this.Fixture.CreateAuthenticatedRequest(
        $"/api/v2/keys/{existingApiKey.Id}",
        PermissionType.Admin,
        environment
      );

    var response = await request.SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
  }

  [Fact]
  public async Task DeleteEnvironmentApiKey_GlobalStandard_Forbidden() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var existingApiKey = this.Fixture.CreateApiKey(PermissionType.Standard, environment);

    var request =
      this.Fixture.CreateAuthenticatedRequest(
        $"/api/v2/keys/{existingApiKey.Id}",
        PermissionType.Standard
      );

    var response = await request.SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task DeleteEnvironmentApiKey_EnvironmentStandard_Forbidden() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var existingApiKey = this.Fixture.CreateApiKey(PermissionType.Standard, environment);

    var request =
      this.Fixture.CreateAuthenticatedRequest(
        $"/api/v2/keys/{existingApiKey.Id}",
        PermissionType.Standard,
        environment
      );

    var response = await request.SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task DeleteEnvironmentApiKey_OtherEnvironmentAdmin_Forbidden() {
    var (environment1, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var (environment2, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var existingApiKey = this.Fixture.CreateApiKey(PermissionType.Standard, environment1);

    var request =
      this.Fixture
        .CreateAuthenticatedRequest(
          $"/api/v2/keys/{existingApiKey.Id}",
          PermissionType.Admin,
          environment2
        );

    var response = await request.SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  // Test for updated ApiKey format <ApiKeyId>:<ApiKey>
  [Fact]
  public async Task CreateGlobalApiKey_BuiltInAdmin_Success_Updated() {
    var request =
      this.Fixture
        .CreateAdminRequestUpdated("/api/v2/keys")
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString()
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task CreateGlobalApiKey_GlobalAdmin_Success_Updated() {
    var request =
      this.Fixture
        .CreateAuthenticatedRequestUpdated("/api/v2/keys", PermissionType.Admin)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString()
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task CreateEnvironmentApiKey_GlobalAdmin_Success_Updated() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAuthenticatedRequestUpdated("/api/v2/keys", PermissionType.Admin)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task CreateEnvironmentApiKey_BuiltInAdmin_Success_Updated() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAdminRequestUpdated("/api/v2/keys")
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task CreateEnvironmentApiKey_EnvironmentAdmin_Success_Updated() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAuthenticatedRequestUpdated("/api/v2/keys", PermissionType.Admin, environment)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task CreateEnvironmentApiKey_GlobalStandard_Forbidden_Updated() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAuthenticatedRequestUpdated("/api/v2/keys", PermissionType.Standard)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreateEnvironmentApiKey_EnvironmentStandard_Forbidden_Updated() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAuthenticatedRequestUpdated("/api/v2/keys", PermissionType.Standard, environment)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreateEnvironmentApiKey_OtherEnvironmentAdmin_Forbidden_Updated() {
    var (environment1, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var (environment2, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAuthenticatedRequestUpdated("/api/v2/keys", PermissionType.Admin, environment1)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment2
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreateTenantApiKey_GlobalAdmin_Success_Updated() {
    var (environment, tenant) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAuthenticatedRequestUpdated("/api/v2/keys", PermissionType.Admin)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment,
            tenant
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task CreateTenantApiKey_BuiltInAdmin_Success_Updated() {
    var (environment, tenant) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAdminRequestUpdated("/api/v2/keys")
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment,
            tenant
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }
  [Fact]
  public async Task CreateTenantApiKey_GlobalStandard_Forbidden_Updated() {
    var (environment, tenant) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAuthenticatedRequestUpdated("/api/v2/keys", PermissionType.Standard)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment,
            tenant
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreateTenantApiKey_EnvironmentAdmin_Success_Updated() {
    var (environment, tenant) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAuthenticatedRequestUpdated("/api/v2/keys", PermissionType.Admin, environment)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment,
            tenant
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task CreateTenantApiKey_OtherEnvironmentAdmin_Forbidden_Updated() {
    var (environment1, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var (environment2, tenant2) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture
        .CreateAuthenticatedRequestUpdated("/api/v2/keys", PermissionType.Admin, environment1)
        .And(msg => {
          msg.Content = JsonContent.Create(new {
            apiKeyType = PermissionType.Admin.ToString(),
            environment2,
            tenant2
          });
        });

    var response = await request.PostAsync();

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task GetApiKeys_GlobalAdmin_Success_Updated() {
    var request =
      this.Fixture.CreateAuthenticatedRequestUpdated("/api/v2/keys", PermissionType.Admin);

    var response = await request.GetAsync();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task GetApiKeys_BuiltInAdmin_Success_Updated() {
    var request = this.Fixture.CreateAdminRequestUpdated("/api/v2/keys");

    var response = await request.GetAsync();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task GetApiKeys_GlobalStandard_Forbidden_Updated() {
    var request =
      this.Fixture.CreateAuthenticatedRequestUpdated("/api/v2/keys", PermissionType.Standard);

    var response = await request.GetAsync();

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task GetApiKeys_EnvironmentAdmin_Success_Updated() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();

    var request =
      this.Fixture.CreateAuthenticatedRequestUpdated("/api/v2/keys", PermissionType.Admin, environment);

    var response = await request.GetAsync();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task DeleteApiKey_GlobalAdmin_Success_Updated() {
    var existingApiKey = this.Fixture.CreateApiKey(PermissionType.Admin);

    var request =
      this.Fixture.CreateAuthenticatedRequestUpdated($"/api/v2/keys/{existingApiKey.Id}", PermissionType.Admin);

    var response = await request.SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
  }

  [Fact]
  public async Task DeleteApiKey_BuiltInAdmin_Success_Updated() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var existingApiKey = this.Fixture.CreateApiKey(PermissionType.Standard, environment);

    var request =
      this.Fixture.CreateAdminRequestUpdated($"/api/v2/keys/{existingApiKey.Id}");

    var response = await request.SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
  }


  [Fact]
  public async Task DeleteApiKey_GlobalStandard_Forbidden_Updated() {
    var existingApiKey = this.Fixture.CreateApiKey(PermissionType.Standard);

    var request =
      this.Fixture.CreateAuthenticatedRequestUpdated($"/api/v2/keys/{existingApiKey.Id}", PermissionType.Standard);

    var response = await request.SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task DeleteEnvironmentApiKey_EnvironmentAdmin_Success_Updated() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var existingApiKey = this.Fixture.CreateApiKey(PermissionType.Standard, environment);

    var request =
      this.Fixture.CreateAuthenticatedRequestUpdated(
        $"/api/v2/keys/{existingApiKey.Id}",
        PermissionType.Admin,
        environment
      );

    var response = await request.SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
  }

  [Fact]
  public async Task DeleteEnvironmentApiKey_GlobalStandard_Forbidden_Updated() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var existingApiKey = this.Fixture.CreateApiKey(PermissionType.Standard, environment);

    var request =
      this.Fixture.CreateAuthenticatedRequestUpdated(
        $"/api/v2/keys/{existingApiKey.Id}",
        PermissionType.Standard
      );

    var response = await request.SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task DeleteEnvironmentApiKey_EnvironmentStandard_Forbidden_Updated() {
    var (environment, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var existingApiKey = this.Fixture.CreateApiKey(PermissionType.Standard, environment);

    var request =
      this.Fixture.CreateAuthenticatedRequestUpdated(
        $"/api/v2/keys/{existingApiKey.Id}",
        PermissionType.Standard,
        environment
      );

    var response = await request.SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task DeleteEnvironmentApiKey_OtherEnvironmentAdmin_Forbidden_Updated() {
    var (environment1, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var (environment2, _) = await this.Fixture.CreateEmptyTestConfiguration();
    var existingApiKey = this.Fixture.CreateApiKey(PermissionType.Standard, environment1);

    var request =
      this.Fixture
        .CreateAuthenticatedRequestUpdated(
          $"/api/v2/keys/{existingApiKey.Id}",
          PermissionType.Admin,
          environment2
        );

    var response = await request.SendAsync("DELETE");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }
}
