using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Tests;

public class ReadinessControllerTests : ApiControllerTestsBase {
  public ReadinessControllerTests(ApiIntegrationTestFixture fixture, ITestOutputHelper outputHelper) :
    base(fixture, outputHelper) {
  }

  [Fact]
  public async Task ReadinessTest() {
    var response = await
      this.Fixture.Server.CreateRequest("/api/ready")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<JsonNode>();
    Assert.NotNull(body);
    Assert.Equal(
      expected: "Ok",
      actual: body["status"]?.GetValue<String>());
    Assert.True(
      !String.IsNullOrEmpty(body["version"]?.GetValue<String>()),
      userMessage: "Version information missing");
  }
}
