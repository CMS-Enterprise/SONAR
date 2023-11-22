using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Controllers;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Tests;

/// <summary>
///   Creates a single ASP.NET in-process TestServer that can be used for all the test methods in a
///   test class. A single database will be shared for all of the tests in the class.
/// </summary>
public class ApiIntegrationTestFixture : IDisposable, ILoggerProvider {
  private Task? _runTask;
  private WebApplication? _app;
  public TestServer Server { get; private set; }
  public String FixtureId { get; }

  public event EventHandler<LogMessageEventArgs>? LogMessageEvent;

  public ApiIntegrationTestFixture() {
    // Create a unique identifier for this test fixture instance
    this.FixtureId = Guid.NewGuid().ToString().Split("-").First();
  }

  public void InitializeHost(Action<WebApplicationBuilder> build, Boolean resetDatabase = false) {

    // Create the WebApplicationBuilder used to run the sonar-api but override
    // certain dependencies for testing purposes
    var builder =
      Program.CreateWebApplicationBuilder(
        Array.Empty<String>(),
        new TestDependencies($"sonar_test_{this.FixtureId}", this)
      );

    build(builder);

    // Use the in-process ASP.NET TestServer instead of the normal HTTP server.
    builder.WebHost.UseTestServer();
    this._app = Program.BuildApplication(builder);

    using (var scope = this._app.Services.CreateScope()) {
      var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
      if (resetDatabase) {
        //This means all tests will have the database deleted and a new one will be created on each separate run.
        dbContext.Database.EnsureDeleted();
      }
      dbContext.Database.EnsureCreated();
    }

    // This is unfortunately duplicated from Program.cs
    this._app.UseAuthorization();
    this._app.MapControllers();

    // Run the ASP.NET core application in a background thread.
    this._runTask = this._app.RunAsync();
    this.Server = this._app.GetTestServer();
  }

  /// <summary>
  ///   Runs a function with an <see cref="IServiceProvider" /> that can be used to create dependencies
  ///   normally available in the SONAR API application.
  /// </summary>
  public void WithDependencies(Action<IServiceProvider> action) {
    using var scope = this._app.Services.CreateScope();
    action(scope.ServiceProvider);
  }

  /// <summary>
  ///   Runs a function with an <see cref="IServiceProvider" /> that can be used to create dependencies
  ///   normally available in the SONAR API application.
  /// </summary>
  public TResult WithDependencies<TResult>(Func<IServiceProvider, TResult> func) {
    using var scope = this._app.Services.CreateScope();
    return func(scope.ServiceProvider);
  }

  /// <summary>
  ///   Runs an asynchronous function with an <see cref="IServiceProvider" /> that can be used to create
  ///   dependencies normally available in the SONAR API application.
  /// </summary>
  public async Task WithDependenciesAsync(
    Func<IServiceProvider, CancellationToken, Task> action,
    CancellationToken cancellationToken = default) {

    await using var scope = this._app.Services.CreateAsyncScope();
    await action(scope.ServiceProvider, cancellationToken);
  }

  public ILogger CreateLogger(String categoryName) {
    return new TestLogger(this, categoryName);
  }

  public RequestBuilder CreateLegacyAuthenticatedRequest(String url, String apiKey) {
    return this.Server.CreateRequest(url)
      .And(req => {
        req.Headers.Add("ApiKey", apiKey);
      });
  }

  public RequestBuilder CreateLegacyAuthenticatedRequest(
    String url,
    PermissionType type,
    String? environment = null,
    String? tenant = null) {

    var apiKey = this.CreateApiKey(type, environment, tenant);

    return this.CreateLegacyAuthenticatedRequest(url, apiKey.ApiKey);
  }

  public RequestBuilder CreateAdminRequest(String url) {
    return this.WithDependencies(provider => {
      var config = provider.GetRequiredService<IOptions<SecurityConfiguration>>();
      return this.CreateAuthenticatedRequest(url, Guid.Empty, config.Value.DefaultApiKey ?? "");
    });
  }

  public RequestBuilder CreateAuthenticatedRequest(String url, Guid apiKeyId, String apiKey) {
    var updatedApiKey = apiKeyId + ":" + apiKey;

    return this.Server.CreateRequest(url)
      .And(req => {
        req.Headers.Add("ApiKey", updatedApiKey);
      });
  }

  public RequestBuilder CreateAuthenticatedRequest(
    String url,
    PermissionType type,
    String? environment = null,
    String? tenant = null) {

    var apiKey = this.CreateApiKey(type, environment, tenant);

    return this.CreateAuthenticatedRequest(url, apiKey.Id, apiKey.ApiKey);
  }

  public ApiKeyConfiguration CreateApiKey(
    PermissionType type,
    String? environment = null,
    String? tenant = null) {

    if (this._app == null) {
      throw new InvalidOperationException("This test fixture has not yet been initialized.");
    }

    using var scope = this._app.Services.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();

    return repository.AddAsync(new ApiKeyDetails(type, environment, tenant), CancellationToken.None).Result;
  }

  public async Task<(String, String)> CreateEmptyTestConfiguration() {
    // Create Service Configuration
    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();

    var createConfigResponse = await
      this.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(new ServiceHierarchyConfiguration(
            ImmutableArray<ServiceConfiguration>.Empty,
            ImmutableHashSet<String>.Empty,
            null
          ));
        })
        .PostAsync();

    // This should always succeed, This isn't what is being tested.
    AssertHelper.Precondition(
      createConfigResponse.IsSuccessStatusCode,
      message: "Failed to create test configuration."
    );
    return (testEnvironment, testTenant);
  }

  private class TestLogger : ILogger {
    private readonly ApiIntegrationTestFixture _fixture;
    private readonly String _categoryName;

    public TestLogger(ApiIntegrationTestFixture fixture, String categoryName) {
      this._fixture = fixture;
      this._categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) {
      return UnitDisposable.Instance;
    }

    public Boolean IsEnabled(LogLevel logLevel) {
      return true;
    }

    public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, String> formatter) {

      this._fixture.OnLogMessageEvent(new LogMessageEventArgs(
        logLevel,
        formatter(state, exception)
      ));
    }

    private class UnitDisposable : IDisposable {
      public static readonly UnitDisposable Instance = new UnitDisposable();

      private UnitDisposable() {
      }

      public void Dispose() {
      }
    }
  }

  protected virtual void Dispose(Boolean disposing) {
    if (disposing) {
      if (this._app is not null) {
        using (var scope = this._app.Services.CreateScope()) {
          var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
          dbContext.Database.EnsureDeleted();
        }

        this._app.StopAsync();
        this.Server.Dispose();
        // Wait for the ASP.NET core application to exit.
        this._runTask.ConfigureAwait(false).GetAwaiter().GetResult();
        this._runTask.Dispose();
      }
    }
  }

  public void Dispose() {
    this.Dispose(true);
    GC.SuppressFinalize(this);
  }

  public virtual async ValueTask DisposeAsync() {
    await using (var scope = this._app.Services.CreateAsyncScope()) {
      var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
      await dbContext.Database.EnsureDeletedAsync();
    }

    await this._app.StopAsync();
    this.Server.Dispose();
    // Wait for the ASP.NET core application to exit.
    await this._runTask;
    this._runTask.Dispose();
  }

  private void OnLogMessageEvent(LogMessageEventArgs e) {
    this.LogMessageEvent?.Invoke(this, e);
  }
}
