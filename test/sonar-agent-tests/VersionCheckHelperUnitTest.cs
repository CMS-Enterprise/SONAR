using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Configuration;
using Cms.BatCave.Sonar.Agent.VersionChecks;
using Cms.BatCave.Sonar.Agent.VersionChecks.Models;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Agent.Tests;

public class VersionCheckHelperUnitTest {

  private ITestOutputHelper _output;
  private readonly Mock<ILogger<VersionCheckHelper>> _mockLogger = new();
  private readonly Mock<IOptions<AgentConfiguration>> _mockAgentConfigOptions = new();
  private readonly Mock<IOptions<ApiConfiguration>> _mockApiConfigOptions = new();
  private readonly Mock<ISonarClient> _mockSonarClient = new();
  private readonly Mock<IVersionRequester<HttpResponseBodyVersionCheckDefinition>> _mockHttpVersionRequester = new();
  private readonly CancellationTokenSource _cts = new();
  private readonly VersionCheckQueueProcessor _queueProcessor;

  public VersionCheckHelperUnitTest(ITestOutputHelper output) {
    this._output = output;

    this._mockAgentConfigOptions
      .SetupGet(options => options.Value)
      .Returns(new AgentConfiguration(
        DefaultTenant: "test",
        MaximumConcurrency: 1));

    this._mockApiConfigOptions
      .SetupGet(options => options.Value)
      .Returns(new ApiConfiguration(
        Environment: "test",
        BaseUrl: "http://localhost:8080",
        ApiKey: "test",
        ApiKeyId: new Guid()));

    this._queueProcessor = new VersionCheckQueueProcessor(this._mockAgentConfigOptions.Object);
  }

  [Fact]
  public async Task VersionCheckHelper_HappyPathTest() {
    this._mockHttpVersionRequester
      .Setup(requester =>
        requester.GetVersionAsync(
          It.IsAny<HttpResponseBodyVersionCheckDefinition>(),
          It.IsAny<CancellationToken>()))
      .ReturnsAsync(new VersionResponse(
        RequestTimestamp: DateTime.UtcNow,
        Version: "1.0"));

    var _ = this._queueProcessor.StartAsync(this._mockHttpVersionRequester.Object, this._cts.Token);

    var tenantConfig = this.CreateTenantConfig();

    this._mockSonarClient
      .Setup(client =>
        client.GetTenantAsync(
          It.IsAny<String>(),
          It.IsAny<String>(),
          It.IsAny<CancellationToken>()))
      .ReturnsAsync(tenantConfig);

    var reportedServiceVersions =
      new List<(String environment, String tenant, String service, ServiceVersion version)>();

    this._mockSonarClient
      .Setup(client =>
        client.RecordServiceVersionAsync(
          It.IsAny<String>(),
          It.IsAny<String>(),
          It.IsAny<String>(),
          It.IsAny<ServiceVersion>()))
      .Callback<String, String, String, ServiceVersion>(
        (environment, tenant, service, version) =>
          reportedServiceVersions.Add((environment, tenant, service, version)));

    var helper = new VersionCheckHelper(
      this._mockLogger.Object,
      this._mockAgentConfigOptions.Object,
      this._mockApiConfigOptions.Object,
      this._queueProcessor,
      this._mockSonarClient.Object);

    this._cts.CancelAfter(TimeSpan.FromSeconds(1));

    await helper.RunScheduledVersionChecks(tenant: "test", this._cts, this._cts.Token);

    var totalVersionChecks = tenantConfig.Services.Select(service => service.VersionChecks?.Count ?? 0).Sum();
    Assert.Equal(totalVersionChecks, reportedServiceVersions.Count);
  }

  [Fact]
  public async Task VersionCheckHelper_SadPathTest() {
    this._mockHttpVersionRequester
      .SetupSequence(requester =>
        requester.GetVersionAsync(
          It.IsAny<HttpResponseBodyVersionCheckDefinition>(),
          It.IsAny<CancellationToken>()))
      .ThrowsAsync(new OperationCanceledException("Cancelled for testing."))
      .ReturnsAsync(new VersionResponse(
        RequestTimestamp: DateTime.UtcNow,
        Version: "1.0"));

    var _ = this._queueProcessor.StartAsync(this._mockHttpVersionRequester.Object, this._cts.Token);

    var tenantConfig = this.CreateTenantConfig();

    this._mockSonarClient
      .Setup(client =>
        client.GetTenantAsync(
          It.IsAny<String>(),
          It.IsAny<String>(),
          It.IsAny<CancellationToken>()))
      .ReturnsAsync(tenantConfig);

    var reportedServiceVersions =
      new List<(String environment, String tenant, String service, ServiceVersion version)>();

    this._mockSonarClient
      .Setup(client =>
        client.RecordServiceVersionAsync(
          It.IsAny<String>(),
          It.IsAny<String>(),
          It.IsAny<String>(),
          It.IsAny<ServiceVersion>()))
      .Callback<String, String, String, ServiceVersion>(
        (environment, tenant, service, version) =>
          reportedServiceVersions.Add((environment, tenant, service, version)));

    var helper = new VersionCheckHelper(
      this._mockLogger.Object,
      this._mockAgentConfigOptions.Object,
      this._mockApiConfigOptions.Object,
      this._queueProcessor,
      this._mockSonarClient.Object);

    this._cts.CancelAfter(TimeSpan.FromSeconds(1));

    await helper.RunScheduledVersionChecks(tenant: "test", this._cts, this._cts.Token);

    var totalVersionChecks = tenantConfig.Services.Select(service => service.VersionChecks?.Count ?? 0).Sum();
    Assert.Equal(totalVersionChecks - 1, reportedServiceVersions.Count);
  }

  private ServiceHierarchyConfiguration CreateTenantConfig() {
    return new ServiceHierarchyConfiguration(
      services: new List<ServiceConfiguration> {
        new ServiceConfiguration(
          name: "Service_1",
          displayName: "Service_1",
          versionChecks: new List<VersionCheckModel> {
            new VersionCheckModel(
              versionCheckType: VersionCheckType.HttpResponseBody,
              definition: new HttpResponseBodyVersionCheckDefinition(
                bodyType: HttpBodyType.Json,
                url: "http://localhost:8081",
                path: "$.version"))
          }.ToImmutableList()),
        new ServiceConfiguration(
          name: "Service_2",
          displayName: "Service_2",
          versionChecks: new List<VersionCheckModel> {
            new VersionCheckModel(
              versionCheckType: VersionCheckType.HttpResponseBody,
              definition: new HttpResponseBodyVersionCheckDefinition(
                bodyType: HttpBodyType.Json,
                url: "http://localhost:8082",
                path: "$.version"))
          }.ToImmutableList()),
        new ServiceConfiguration(
          name: "Service_3",
          displayName: "Service_3")
      }.ToImmutableList(),
      rootServices: new HashSet<String> { }.ToImmutableHashSet(),
      tags: null);
  }
}
