using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Json;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Builder;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Tests;

public class ApiControllerTestsBase : IClassFixture<ApiIntegrationTestFixture>, IDisposable {
  private readonly EventHandler<LogMessageEventArgs> _logHandler;

  protected static readonly JsonSerializerOptions SerializerOptions = new() {
    Converters = { new JsonStringEnumConverter(), new ArrayTupleConverterFactory() },
    PropertyNameCaseInsensitive = true
  };

  protected ApiIntegrationTestFixture Fixture { get; }

  protected ApiControllerTestsBase(ApiIntegrationTestFixture fixture, ITestOutputHelper outputHelper, Boolean resetDatabase = false) {
    this.Fixture = fixture;
    this.Fixture.LogMessageEvent +=
      this._logHandler = (_, args) => outputHelper.WriteLine($"{args.Level}: {args.Message}");

    this.Fixture.InitializeHost(this.OnInitializing, resetDatabase);
  }

  protected virtual void OnInitializing(WebApplicationBuilder builder) {
  }

  protected async Task<(String, String)> CreateTestConfiguration(
    ServiceHierarchyConfiguration configuration) {

    var testEnvironment = Guid.NewGuid().ToString();
    var testTenant = Guid.NewGuid().ToString();
    await this.CreateTestConfiguration(testEnvironment, testTenant, configuration);
    return (testEnvironment, testTenant);
  }

  protected async Task CreateTestConfiguration(
    String testEnvironment,
    String testTenant,
    ServiceHierarchyConfiguration configuration) {

    // Create Service Configuration
    var createConfigResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/config/{testEnvironment}/tenants/{testTenant}")
        .And(req => {
          req.Content = JsonContent.Create(configuration);
        })
        .PostAsync();

    // This should always succeed, This isn't what is being tested.
    AssertHelper.Precondition(
      createConfigResponse.IsSuccessStatusCode,
      message: "Failed to create test configuration."
    );
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
