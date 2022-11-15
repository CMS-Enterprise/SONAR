using System;
using System.Dynamic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Tests;

public class HealthControllerIntegrationTests : ApiControllerTestsBase {
  public HealthControllerIntegrationTests(ApiIntegrationTestFixture fixture) : base(fixture) {
  }

  [Fact]
  public async Task MissingEnvironmentReturnsNotFound() {
    var response = await
      this.Fixture.Server.CreateRequest($"/api/v2/health/{Guid.NewGuid()}/tenants/foo")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: response.StatusCode);
  }

  [Fact]
  public async Task MissingTenantReturnsNotFound() {
    var existingEnvironmentName = Guid.NewGuid().ToString();

    // Create existing Environment
    await this.Fixture.WithDependenciesAsync(async (provider, cancellationToken) => {
      var dbContext = provider.GetRequiredService<DataContext>();
      var environments = provider.GetRequiredService<DbSet<Environment>>();

      await environments.AddAsync(Environment.New(existingEnvironmentName), cancellationToken);
      await dbContext.SaveChangesAsync(cancellationToken);
    });

    var response = await
      this.Fixture.Server.CreateRequest($"/api/v2/health/{existingEnvironmentName}/tenants/{Guid.NewGuid()}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();

    Assert.NotNull(body);
    Assert.Equal(
      expected: "Tenant Not Found",
      actual: body.Title);
  }
}
