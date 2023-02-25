using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Configuration;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Agent.HealthChecks;

public sealed class HealthCheckQueueProcessor<TDefinition> : IDisposable
  where TDefinition : HealthCheckDefinition {

  private readonly SemaphoreSlim _concurrencyLimit;
  private readonly INotifyOptionsChanged<HealthCheckQueueProcessorConfiguration> _configuration;
  private readonly EventHandler<EventArgs> _configurationChangeHandler;

  private readonly IHealthCheckEvaluator<TDefinition> _healthCheckEvaluator;

  /// <summary>
  ///   Dictionary of tenant names to queue of health checks
  /// </summary>
  private readonly
    ConcurrentDictionary<String,
      ConcurrentQueue<(TaskCompletionSource<HealthStatus> Future, String Name, TDefinition Definition)>>
    _healthCheckQueues =
      new(StringComparer.OrdinalIgnoreCase);

  private readonly SemaphoreSlim _pendingChecks = new(0);
  private readonly ConcurrentBag<Task> _pendingConcurrencyLimitReductions = new();

  private readonly ConcurrentDictionary<Guid, Task> _runningHealthChecks = new();

  public HealthCheckQueueProcessor(
    IHealthCheckEvaluator<TDefinition> healthCheckEvaluator,
    INotifyOptionsChanged<HealthCheckQueueProcessorConfiguration> configuration) {
    this._healthCheckEvaluator = healthCheckEvaluator;
    this._configuration = configuration;

    var concurrencyLimitValue = configuration.Value.MaximumConcurrency;
    this._concurrencyLimit = new SemaphoreSlim(concurrencyLimitValue);
    configuration.OptionsChanged +=
      this._configurationChangeHandler = (_, _) => {
        var tmp = concurrencyLimitValue;
        concurrencyLimitValue = configuration.Value.MaximumConcurrency;
        var concurrencyChange = concurrencyLimitValue - tmp;
        if (concurrencyChange > 0) {
          for (var i = 0; i < concurrencyChange; i++) {
            this._concurrencyLimit.Release();
          }
        } else if (concurrencyChange < 0) {
          for (var i = 0; i > concurrencyChange; i--) {
            // Reduce the available concurrency slots in the background so we do not block this thread
            // that invokes this callback
            this._pendingConcurrencyLimitReductions.Add(this._concurrencyLimit.WaitAsync());
          }
        }
      };
  }

  public void Dispose() {
    this._concurrencyLimit.Dispose();
    this._pendingChecks.Dispose();
    this._configuration.OptionsChanged -= this._configurationChangeHandler;
    // The Tasks in _runningHealthChecks dispose themselves upon completion
  }

  public async Task Run(CancellationToken cancellationToken) {
    var roundRobinOffset = 0;

    while (!cancellationToken.IsCancellationRequested) {
      // Wait for there to be a concurrency slots available
      await this._concurrencyLimit.WaitAsync(cancellationToken);

      // Wait for there to be some work available
      await this._pendingChecks.WaitAsync(cancellationToken);

      var success = false;

      // Cycle through tenants until we find some work
      // Since tenants may be removed, it's possible we'll find no work, so only do a single pass
      var tenants = this._healthCheckQueues.Keys.ToList();
      for (var i = 0; i < tenants.Count; i++) {
        // This round robin strategy assumes relatively stable order for ConcurrentDictionary keys,
        // but tenants shouldn't been added and removed very frequently
        var tenant = tenants[(roundRobinOffset + i) % tenants.Count];
        if (this._healthCheckQueues.TryGetValue(tenant, out var queue) && queue.TryDequeue(out var check)) {
          var taskId = Guid.NewGuid();
          this._runningHealthChecks.TryAdd(
            taskId,
            this.RunHealthCheckAsync(taskId, check.Future, check.Name, check.Definition, cancellationToken)
          );

          success = true;
          break;
        }
      }

      if (!success) {
        // We didn't find the pending health check! That can only mean it was removed from the queue
        // due to tenant removal, which in turn mean there has been one extra call to _pendingChecks.Wait
        this._pendingChecks.Release();
        this._concurrencyLimit.Release();
      }

      roundRobinOffset++;
    }
  }

  public Task<HealthStatus> QueueHealthCheck(
    String tenant,
    String healthCheckName,
    TDefinition healthCheckDefinition) {

    var future = new TaskCompletionSource<HealthStatus>();
    this._healthCheckQueues.AddOrUpdate(
      tenant,
      addValueFactory: (_) => {
        var queue = new ConcurrentQueue<(TaskCompletionSource<HealthStatus>, String, TDefinition)>();
        queue.Enqueue((future, healthCheckName, healthCheckDefinition));
        return queue;
      },
      updateValueFactory: (_, queue) => {
        queue.Enqueue((future, healthCheckName, healthCheckDefinition));
        return queue;
      }
    );

    this._pendingChecks.Release();

    return future.Task;
  }

  public void RemoveTenant(String tenant) {
    if (this._healthCheckQueues.TryRemove(tenant, out var queue)) {
      while (queue.TryDequeue(out var entry)) {
        // Decrement the pending checks counter
        this._pendingChecks.Wait();
        entry.Future.SetCanceled();
      }
    }
  }
  private async Task RunHealthCheckAsync(
    Guid taskId,
    TaskCompletionSource<HealthStatus> future,
    String healthCheckName,
    TDefinition healthCheckDefinition,
    CancellationToken cancellationToken) {

    try {
      future.SetResult(
        await this._healthCheckEvaluator.EvaluateHealthCheckAsync(
          healthCheckName,
          healthCheckDefinition,
          cancellationToken
        )
      );
    } catch (OperationCanceledException) {
      future.SetCanceled(cancellationToken);
    } catch (Exception ex) {
      // This is a background task, notify the thread that is awaiting the future.
      future.SetException(ex);
    } finally {
      // Whether the check succeeds or fails, free up the concurrency slot
      this._concurrencyLimit.Release();
      if (this._runningHealthChecks.TryRemove(taskId, out var task)) {
        task.Dispose();
      }
    }
  }
}
