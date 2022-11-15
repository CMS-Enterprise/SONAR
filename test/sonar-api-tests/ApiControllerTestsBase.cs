using Xunit;

namespace Cms.BatCave.Sonar.Tests;

public class ApiControllerTestsBase : IClassFixture<ApiIntegrationTestFixture> {
  protected ApiIntegrationTestFixture Fixture { get; }

  protected ApiControllerTestsBase(ApiIntegrationTestFixture fixture) {
    this.Fixture = fixture;
  }
}
