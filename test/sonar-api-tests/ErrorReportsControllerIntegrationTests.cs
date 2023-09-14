using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Tests;

public class ErrorReportsControllerIntegrationTests : ApiControllerTestsBase {
  private const String TestRootServiceName = "TestRootService";
  private const String TestHealthCheckName = "TestHealthCheck";
  private const String TestFilteredHealthCheckName = "TestFilteredCheckName";
  private const AgentErrorLevel TestErrorLevel = AgentErrorLevel.Error;
  private const AgentErrorType TestErrorType = AgentErrorType.Unknown;
  private const String TestErrorMessage = "Error message";
  private const String TestErrorConfigurationString = "Test config";
  private const String TestErrorStackTrace = "Test stack trace";

  private static readonly ServiceHierarchyConfiguration TestConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        TestRootServiceName,
        displayName: "Display Name",
        description: null,
        url: null,
        healthChecks: null,
        children: null
      )
    ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName)
  );

  public ErrorReportsControllerIntegrationTests(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper outputHelper) :
    base(fixture, outputHelper) {
  }



  // CreateErrorReport scenarios
  //    Invalid Environment - 404
  //    Missing Tenant - 404
  //    Missing Service - 404
  //    Successful request (root only) - 200
  //    Unauthorized request (non-admin) - 401
  [Fact]
  public async Task CreateErrorReport_InvalidEnvReturnsNotFound() {
    var invalidEnvName = Guid.NewGuid().ToString();
    var response = await
      this.Fixture.CreateAdminRequest($"/api/v2/error-reports/{invalidEnvName}")
        .AddHeader(name: "Accept", value: "application/json")
        .And(req => {
          req.Content = JsonContent.Create(new ErrorReportDetails(
            DateTime.UtcNow,
            null,
            null,
            TestHealthCheckName,
            TestErrorLevel,
            TestErrorType,
            TestErrorMessage,
            TestErrorConfigurationString,
            TestErrorStackTrace
          ));
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ResourceNotFoundException.ProblemTypeName,
      actual: body.Type
    );
  }

  [Fact]
  public async Task CreateErrorReport_InvalidTenantReturnsNotFound() {
    var existingEnvironmentName = Guid.NewGuid().ToString();
    var missingTenantName = Guid.NewGuid().ToString();
    // Create existing Environment
    await this.Fixture.WithDependenciesAsync(async (provider, cancellationToken) => {
      var dbContext = provider.GetRequiredService<DataContext>();
      var environments = provider.GetRequiredService<DbSet<Environment>>();

      await environments.AddAsync(Environment.New(existingEnvironmentName), cancellationToken);
      await dbContext.SaveChangesAsync(cancellationToken);
    });

    var response = await
      this.Fixture.CreateAdminRequest($"/api/v2/error-reports/{existingEnvironmentName}")
        .AddHeader(name: "Accept", value: "application/json")
        .And(req => {
          req.Content = JsonContent.Create(new ErrorReportDetails(
            DateTime.UtcNow,
            missingTenantName,
            null,
            TestHealthCheckName,
            TestErrorLevel,
            TestErrorType,
            TestErrorMessage,
            TestErrorConfigurationString,
            TestErrorStackTrace
          ));
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ResourceNotFoundException.ProblemTypeName,
      actual: body.Type
    );
  }

  [Fact]
  public async Task CreateErrorReport_InvalidServiceReturnsNotFound() {
    // Create Service Configuration
    var (env, tenant) = await this.CreateTestConfiguration(TestConfiguration);
    var missingServiceName = Guid.NewGuid().ToString();

    var response = await
      this.Fixture.CreateAdminRequest($"/api/v2/error-reports/{env}")
        .AddHeader(name: "Accept", value: "application/json")
        .And(req => {
          req.Content = JsonContent.Create(new ErrorReportDetails(
            DateTime.UtcNow,
            tenant,
            missingServiceName,
            TestHealthCheckName,
            TestErrorLevel,
            TestErrorType,
            TestErrorMessage,
            TestErrorConfigurationString,
            TestErrorStackTrace
          ));
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(
      SerializerOptions
    );

    Assert.NotNull(body);
    Assert.Equal(
      expected: ResourceNotFoundException.ProblemTypeName,
      actual: body.Type
    );
  }

  [Fact]
  public async Task CreateErrorReport_Success() {
    // Create Service Configuration
    var (env, tenant) = await this.CreateTestConfiguration(TestConfiguration);

    var response = await
      this.Fixture.CreateAdminRequest($"/api/v2/error-reports/{env}")
        .AddHeader(name: "Accept", value: "application/json")
        .And(req => {
          req.Content = JsonContent.Create(new ErrorReportDetails(
            DateTime.UtcNow,
            tenant,
            TestRootServiceName,
            TestHealthCheckName,
            TestErrorLevel,
            TestErrorType,
            TestErrorMessage,
            TestErrorConfigurationString,
            TestErrorStackTrace
          ));
        })
        .PostAsync();

    Assert.True(
      response.IsSuccessStatusCode,
      userMessage: $"Expected a success response code (2xx). Actual: {(Int32)response.StatusCode}"
    );
  }

  [Fact]
  public async Task CreateErrorReport_AnonymousRequestReturnsUnauthorized() {
    // Create Service Configuration
    var (env, tenant) = await this.CreateTestConfiguration(TestConfiguration);

    var response = await
      this.Fixture.Server.CreateRequest($"/api/v2/error-reports/{env}")
        .AddHeader(name: "Accept", value: "application/json")
        .And(req => {
          req.Content = JsonContent.Create(new ErrorReportDetails(
            DateTime.UtcNow,
            tenant,
            TestRootServiceName,
            TestHealthCheckName,
            TestErrorLevel,
            TestErrorType,
            TestErrorMessage,
            TestErrorConfigurationString,
            TestErrorStackTrace
          ));
        })
        .PostAsync();

    Assert.Equal(
      expected: HttpStatusCode.Unauthorized,
      actual: response.StatusCode);
  }

  // ListErrorReport scenarios
  //    Invalid Environment - 404
  //    Invalid time range (start is later than end) - 400
  //    Invalid time range (range is too large) - 400
  //    Successful request (no filters) - 200
  //    Successful request (filter applied) - 200
  //    Unauthorized request (non-admin) - 401
  [Fact]
  public async Task ListErrorReport_InvalidEnvironmentReturnsNotFound() {
    var invalidEnvName = Guid.NewGuid().ToString();
    var response = await
      this.Fixture.CreateAdminRequest($"/api/v2/error-reports/{invalidEnvName}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: response.StatusCode);
  }

  [Fact]
  public async Task ListErrorReport_StartLaterThanEndReturnsBadRequest() {
    var (env, tenant) = await this.CreateTestConfiguration(TestConfiguration);
    // start date that is later than end date
    var invalidStart = DateTime.UtcNow;
    var invalidEnd = invalidStart.Subtract(TimeSpan.FromHours(1));

    var response = await
      this.Fixture.CreateAdminRequest($"/api/v2/error-reports/{env}?start={invalidStart}&end={invalidEnd}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: response.StatusCode);
  }

  [Fact]
  public async Task ListErrorReport_RangeGreaterThanMaxReturnsBadRequest() {
    var (env, tenant) = await this.CreateTestConfiguration(TestConfiguration);
    // start date that is later than end date
    var end = DateTime.UtcNow;
    var start = end.Subtract(TimeSpan.FromDays(6));

    var response = await
      this.Fixture.CreateAdminRequest($"/api/v2/error-reports/{env}?start={start}&end={end}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: response.StatusCode);
  }

  [Fact]
  public async Task ListErrorReport_SuccessWithoutFiltering() {
    var (env, tenant) = await this.CreateTestConfiguration(TestConfiguration);

    var response = await
      this.Fixture.CreateAdminRequest($"/api/v2/error-reports/{env}")
        .AddHeader(name: "Accept", value: "application/json")
        .And(req => {
          req.Content = JsonContent.Create(new ErrorReportDetails(
            DateTime.UtcNow,
            tenant,
            TestRootServiceName,
            TestHealthCheckName,
            TestErrorLevel,
            TestErrorType,
            TestErrorMessage,
            TestErrorConfigurationString,
            TestErrorStackTrace
          ));
        })
        .PostAsync();

    var getResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/error-reports/{env}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    var body = await getResponse.Content.ReadFromJsonAsync<List<ErrorReportDetails>>(
      SerializerOptions);

    Assert.NotNull(body);
    var requestResult = Assert.Single(body);
    Assert.Equal(TestRootServiceName, requestResult.Service);
    Assert.Equal(tenant, requestResult.Tenant);
    Assert.Equal(TestHealthCheckName, requestResult.HealthCheckName);
    Assert.Equal(TestErrorLevel, requestResult.Level);
    Assert.Equal(TestErrorType, requestResult.Type);
    Assert.Equal(TestErrorMessage, requestResult.Message);
    Assert.Equal(TestErrorConfigurationString, requestResult.Configuration);
    Assert.Equal(TestErrorStackTrace, requestResult.StackTrace);
  }

  [Fact]
  public async Task ListErrorReport_SuccessWithFiltering() {
    var (env, tenant) = await this.CreateTestConfiguration(TestConfiguration);

    // create two reports with different health check names
    await this.Fixture.CreateAdminRequest($"/api/v2/error-reports/{env}")
      .AddHeader(name: "Accept", value: "application/json")
      .And(req => {
        req.Content = JsonContent.Create(new ErrorReportDetails(
          DateTime.UtcNow,
          tenant,
          TestRootServiceName,
          TestHealthCheckName,
          TestErrorLevel,
          TestErrorType,
          TestErrorMessage,
          TestErrorConfigurationString,
          TestErrorStackTrace
        ));
      })
      .PostAsync();

    await this.Fixture.CreateAdminRequest($"/api/v2/error-reports/{env}")
      .AddHeader(name: "Accept", value: "application/json")
      .And(req => {
        req.Content = JsonContent.Create(new ErrorReportDetails(
          DateTime.UtcNow,
          tenant,
          TestRootServiceName,
          TestFilteredHealthCheckName,
          TestErrorLevel,
          TestErrorType,
          TestErrorMessage,
          TestErrorConfigurationString,
          TestErrorStackTrace
        ));
      })
      .PostAsync();

    var getResponse = await
      this.Fixture.CreateAdminRequest($"/api/v2/error-reports/{env}?healthCheckName={TestFilteredHealthCheckName}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    var body = await getResponse.Content.ReadFromJsonAsync<List<ErrorReportDetails>>(
      SerializerOptions);

    Assert.NotNull(body);
    var filteredResult = Assert.Single(body);
    Assert.Equal(TestFilteredHealthCheckName, filteredResult.HealthCheckName);
  }

  [Fact]
  public async Task ListErrorReport_AnonymousRequestReturnsUnauthorized() {
    var response = await
      this.Fixture.Server.CreateRequest($"/api/v2/error-reports/{Guid.NewGuid().ToString()}")
        .GetAsync();

    Assert.Equal(
      expected: HttpStatusCode.Unauthorized,
      actual: response.StatusCode);
  }

}
