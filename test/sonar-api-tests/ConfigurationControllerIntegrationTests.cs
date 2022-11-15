using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Cms.BatCave.Sonar.Tests;

public class ConfigurationControllerIntegrationTests : ApiControllerTestsBase {
  public ConfigurationControllerIntegrationTests(ApiIntegrationTestFixture fixture) : base(fixture) {
  }

  [Fact]
  public async Task MissingEnvironmentReturnsNotFound() {
    var response = await
      this.Fixture.Server.CreateRequest($"/api/v2/config/{Guid.NewGuid()}/tenants/foo")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: response.StatusCode);
  }
}
