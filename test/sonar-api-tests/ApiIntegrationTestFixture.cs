using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cms.BatCave.Sonar.Tests;

/// <summary>
///   Creates a single ASP.NET in-process TestServer that can be used for all the test methods in a
///   test class. A single database will be shared for all of the tests in the class.
/// </summary>
public sealed class ApiIntegrationTestFixture : IDisposable {
  private readonly Task _runTask;
  private readonly WebApplication _app;
  public TestServer Server { get; }
  public String FixtureId { get; }

  public ApiIntegrationTestFixture() {
    // Create a unique identifier for this test fixture instance
    this.FixtureId = Guid.NewGuid().ToString().Split("-").First();

    // Create the WebApplicationBuilder used to run the sonar-api but override
    // certain dependencies for testing purposes
    var builder =
      Program.CreateWebApplicationBuilder(
        Array.Empty<String>(),
        new TestDependencies($"sonar_test_{this.FixtureId}")
      );

    // Use the in-process ASP.NET TestServer instead of the normal HTTP server.
    builder.WebHost.UseTestServer();
    this._app = builder.Build();

    using (var scope = this._app.Services.CreateScope()) {
      var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
      dbContext.Database.EnsureCreated();
    }

    // This is unfortunately duplicated from Program.cs
    this._app.UseAuthorization();
    this._app.MapControllers();

    // Run the ASP.NET core application in a background thread.
    this._runTask = this._app.RunAsync();
    this.Server = this._app.GetTestServer();
  }

  public void WithDependencies(Action<IServiceProvider> action) {
    using var scope = this._app.Services.CreateScope();
    action(scope.ServiceProvider);
  }

  public async Task WithDependenciesAsync(
    Func<IServiceProvider, CancellationToken, Task> action,
    CancellationToken cancellationToken = default) {

    await using var scope = this._app.Services.CreateAsyncScope();
    await action(scope.ServiceProvider, cancellationToken);
  }

  public void Dispose() {
    this._app.StopAsync();
    this.Server.Dispose();
    // Wait for the ASP.NET core application to exit.
    this._runTask.ConfigureAwait(false).GetAwaiter().GetResult();
    this._runTask.Dispose();
  }
}
