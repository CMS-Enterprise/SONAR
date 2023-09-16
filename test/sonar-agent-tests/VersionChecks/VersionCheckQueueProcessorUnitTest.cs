using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Configuration;
using Cms.BatCave.Sonar.Agent.VersionChecks;
using Cms.BatCave.Sonar.Agent.VersionChecks.Models;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Agent.Tests.VersionChecks;

public class VersionCheckQueueProcessorUnitTest {

  private readonly ITestOutputHelper _output;
  private readonly Mock<IVersionRequester<HttpResponseBodyVersionCheckDefinition>> _mockHttpVersionRequester = new();
  private readonly Mock<IOptions<AgentConfiguration>> _mockAgentConfigOptions = new();
  private readonly CancellationTokenSource _cts = new();

  public VersionCheckQueueProcessorUnitTest(ITestOutputHelper output) {
    this._output = output;
  }

  [Fact]
  public async Task VersionCheckQueueProcessor_ProcessesInParallelUpToMaxConcurrency() {
    const Int32 requestDelaySeconds = 1;

    this._mockHttpVersionRequester
      .Setup(requester =>
        requester.GetVersionAsync(
          It.IsAny<HttpResponseBodyVersionCheckDefinition>(),
          It.IsAny<CancellationToken>()))
      .Returns(async () => {
        await Task.Delay(TimeSpan.FromSeconds(requestDelaySeconds));
        return new VersionResponse(RequestTimestamp: DateTime.UtcNow, Version: "1.0");
      });

    this._mockAgentConfigOptions
      .SetupGet(options => options.Value)
      .Returns(new AgentConfiguration(
        DefaultTenant: "test",
        AgentInterval: 10,
        MaximumConcurrency: 3));

    var queueProcessor = new VersionCheckQueueProcessor(this._mockAgentConfigOptions.Object);

    var _ = queueProcessor.StartAsync(this._mockHttpVersionRequester.Object, this._cts.Token);

    const String tenant = "test";
    var model = new VersionCheckModel(
      versionCheckType: VersionCheckType.HttpResponseBody,
      definition: new HttpResponseBodyVersionCheckDefinition(
        url: "http://localhost:8080",
        path: "$.version",
        bodyType: HttpBodyType.Json));

    var startTime = DateTime.UtcNow;

    var taskBatch1 = Task.WhenAll(
      queueProcessor.QueueVersionCheck(tenant, model),
      queueProcessor.QueueVersionCheck(tenant, model),
      queueProcessor.QueueVersionCheck(tenant, model));

    var taskBatch2 = Task.WhenAll(
      queueProcessor.QueueVersionCheck(tenant, model),
      queueProcessor.QueueVersionCheck(tenant, model),
      queueProcessor.QueueVersionCheck(tenant, model));

    var responses1 = await taskBatch1;
    var responses2 = await taskBatch2;

    // Tasks in batch 1 should have all run in parallel, so they should all finish after requestDelaySeconds
    responses1.ToList().ForEach(response =>
      Assert.Equal(requestDelaySeconds, (response.RequestTimestamp - startTime).Seconds));

    // Tasks in batch 2 should also run in parallel but having to wait for the first batch,
    // so they should all finish after twice as long
    responses2.ToList().ForEach(response =>
      Assert.Equal(requestDelaySeconds * 2, (response.RequestTimestamp - startTime).Seconds));

    this._cts.Cancel();
  }

}
