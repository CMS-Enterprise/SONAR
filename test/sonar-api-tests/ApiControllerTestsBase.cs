using System;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Tests;

public class ApiControllerTestsBase : IClassFixture<ApiIntegrationTestFixture>, IDisposable {
  private readonly EventHandler<LogMessageEventArgs> _logHandler;
  protected ApiIntegrationTestFixture Fixture { get; }

  protected ApiControllerTestsBase(ApiIntegrationTestFixture fixture, ITestOutputHelper outputHelper) {
    this.Fixture = fixture;
    this.Fixture.LogMessageEvent +=
      this._logHandler = (_, args) => outputHelper.WriteLine($"{args.Level}: {args.Message}");
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

  protected virtual void Dispose(Boolean disposing) {
    if (disposing) {
      this.Fixture.LogMessageEvent -= this._logHandler;
    }
  }

  public void Dispose() {
    this.Dispose(true);
    GC.SuppressFinalize(this);
  }
}
