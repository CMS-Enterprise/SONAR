using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Configuration;
using Cms.BatCave.Sonar.Agent.HealthChecks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cms.BatCave.Sonar.Agent.Tests;

public class HealthCheckQueueProcessorUnitTest {
  private readonly Random _rand = new Random();

  // Queue work for multiple tenants, start processing, ensure work started in parallel
  [Fact]
  public async Task ProcessQueuedWorkInParallel() {
    var maximumConcurrency = 3;

    var testEvaluator = new TestHealthCheckEvaluator();
    var mockConfiguration = new Mock<INotifyOptionsChanged<HealthCheckQueueProcessorConfiguration>>();
    mockConfiguration.SetupGet(o => o.Value).Returns(new HealthCheckQueueProcessorConfiguration(maximumConcurrency));

    var processor = new HealthCheckQueueProcessor<TestHealthCheckDefinition>(
      testEvaluator,
      mockConfiguration.Object,
      Mock.Of<ILogger<HealthCheckQueueProcessor<TestHealthCheckDefinition>>>()
    );

    // Queue work before processing starts, verify round-robin execution
    var healthChecks =
      Enumerable.Range(start: 0, count: maximumConcurrency)
        .Select(i =>
          (Tenant: Guid.NewGuid().ToString(),
            Name: $"HealthCheck{i}",
            Definition: new TestHealthCheckDefinition(RandomHealthStatus())))
        .ToList();

    var futures = healthChecks.Select(hc => processor.QueueHealthCheck(hc.Tenant, hc.Name, hc.Definition)).ToList();

    var cancellation = new CancellationTokenSource();

    var processorTask = processor.Run(cancellation.Token);

    var evaluationStarted = await Task.WhenAll(
      healthChecks.Select(hc =>
        hc.Definition.EvaluationStarted.WaitAsync(TimeSpan.FromSeconds(1))
      )
    );

    // Verify that all three health checks started processing
    Assert.All(evaluationStarted, Assert.True);

    // Allow processing to complete
    foreach (var hc in healthChecks) {
      hc.Definition.EvaluationTrigger.Release();
    }

    // Verify that all three futures complete
    var results = await await Task.WhenAny(
      Task.WhenAll(futures.Select(async f => await f)),
      Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(_ => Array.Empty<HealthStatus>())
    );

    Assert.Equal(
      healthChecks.Select(hc => hc.Definition.TestResult),
      results
    );

    cancellation.Cancel();

    await Assert.ThrowsAsync<OperationCanceledException>(() => processorTask);
  }

  // Verify max concurrency setting is not exceeded
  [Fact]
  public async Task VerifyMaximumConcurrencyIsNotExceeded() {
    var maximumConcurrency = 2;
    var testEvaluator = new TestHealthCheckEvaluator();
    var mockConfiguration = new Mock<INotifyOptionsChanged<HealthCheckQueueProcessorConfiguration>>();
    mockConfiguration.SetupGet(o => o.Value).Returns(new HealthCheckQueueProcessorConfiguration(maximumConcurrency));

    var processor = new HealthCheckQueueProcessor<TestHealthCheckDefinition>(
      testEvaluator,
      mockConfiguration.Object,
      Mock.Of<ILogger<HealthCheckQueueProcessor<TestHealthCheckDefinition>>>()
    );

    // Queue work before processing starts, verify round-robin execution
    var healthChecks =
      Enumerable.Range(start: 0, count: maximumConcurrency + 2)
        .Select(i =>
          (Tenant: Guid.NewGuid().ToString(),
            Name: $"HealthCheck{i}",
            Definition: new TestHealthCheckDefinition(RandomHealthStatus())))
        .ToList();

    var futures = healthChecks.Select(hc => processor.QueueHealthCheck(hc.Tenant, hc.Name, hc.Definition)).ToList();

    var cancellation = new CancellationTokenSource();

    var processorTask = processor.Run(cancellation.Token);

    // Count the health checks for which evaluation has started.
    var evaluationStarted = await Task.WhenAll(
      healthChecks.Select(hc =>
        hc.Definition.EvaluationStarted.WaitAsync(0)
      )
    );

    // Verify that only the maximumConcurrency count of heath checks have begun evaluation
    Assert.Equal(maximumConcurrency, evaluationStarted.Count(x => x));

    // Allow processing to complete
    foreach (var hc in healthChecks) {
      hc.Definition.EvaluationTrigger.Release();
    }

    // re-count the health checks for which evaluation has started.
    evaluationStarted = await Task.WhenAll(
      healthChecks.Select(hc =>
        hc.Definition.EvaluationStarted.WaitAsync(TimeSpan.FromSeconds(1))
      )
    );

    // Verify that *all* health check evaluation starts
    Assert.All(evaluationStarted, Assert.True);

    // Verify that all three futures complete
    var results = await await Task.WhenAny(
      Task.WhenAll(futures.Select(async f => await f)),
      Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(_ => Array.Empty<HealthStatus>())
    );

    Assert.Equal(
      healthChecks.Select(hc => hc.Definition.TestResult),
      results
    );

    cancellation.Cancel();

    await Assert.ThrowsAsync<OperationCanceledException>(() => processorTask);
  }

  private static readonly HealthStatus[] HealthStatusValues = Enum.GetValues<HealthStatus>();

  private HealthStatus RandomHealthStatus() {
    return HealthStatusValues[this._rand.Next(minValue: 0, HealthStatusValues.Length)];
  }

  private class TestHealthCheckEvaluator : IHealthCheckEvaluator<TestHealthCheckDefinition> {
    public async Task<HealthStatus> EvaluateHealthCheckAsync(
      String name,
      TestHealthCheckDefinition definition,
      CancellationToken cancellationToken = default) {

      definition.EvaluationStarted.Set();
      await definition.EvaluationTrigger.WaitAsync(cancellationToken);
      return definition.TestResult;
    }
  }

  public record TestHealthCheckDefinition(HealthStatus TestResult) : HealthCheckDefinition {
    public ManualResetEventSlim EvaluationStarted { get; } = new ManualResetEventSlim();
    public SemaphoreSlim EvaluationTrigger { get; } = new SemaphoreSlim(0);
  }
}
