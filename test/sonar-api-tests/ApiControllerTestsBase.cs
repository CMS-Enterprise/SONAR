using System;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cms.BatCave.Sonar.Tests;

public class ApiControllerTestsBase : IClassFixture<ApiIntegrationTestFixture> {
  protected ApiIntegrationTestFixture Fixture { get; }

  protected ApiControllerTestsBase(ApiIntegrationTestFixture fixture) {
    this.Fixture = fixture;
  }

  protected RequestBuilder CreateAdminRequest(String url) {
    return this.Fixture.WithDependencies(provider => {
      var config = provider.GetRequiredService<IConfiguration>();
      var apiKey = config.GetValue<String>("ApiKey");
      return CreateAuthenticatedRequest(url, apiKey);
    });
  }

  protected RequestBuilder CreateAuthenticatedRequest(String url, String apiKey) {
    return this.Fixture.Server.CreateRequest(url)
      .And(req => {
        req.Headers.Add("ApiKey", apiKey);
      });
  }
}
