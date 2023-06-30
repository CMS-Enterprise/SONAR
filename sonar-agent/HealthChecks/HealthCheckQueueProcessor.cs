using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Configuration;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Agent.HealthChecks;

/// <summary>
///   A Queue based processor for health check evaluation.
/// </summary>
/// <remarks>
///   <para>
///     The purpose of this class is to decouple the evaluation of individual health checks from the
///     periodic scheduling and reporting of service health. This class partitions health checks of a
///     particular type by tenant and processes them parallel, round robin fashion, ensuring that no
///     one tenant starves others for health check evaluation.
///   </para>
///   <para>
///     Note: the current round robin implementation still has some potential for short lived
///     starvation. If one tenant has a backlog of work while no other tenants do then that tenant will
///     max out the allowable parallelism and if at that point work is queued for another tenant it
///     will have to wait until the other active work is completed. It would be possible to improve on
///     the current implementation by implementing a preemption mechanism that terminates and re-queues
///     a tenants running health checks, beyond a single health check, when another tenant schedules
///     new work.
///   </para>
/// </remarks>
/// <typeparam name="TDefinition">
///   The type of health check definition handled by this processor.
/// </typeparam>
public sealed class HealthCheckQueueProcessor<TDefinition> : IDisposable
  where TDefinition : HealthCheckDefinition {

  private readonly SemaphoreSlim _concurrencyLimit;
  private readonly INotifyOptionsChanged<HealthCheckQueueProcessorConfiguration> _configuration;
  private readonly ILogger<HealthCheckQueueProcessor<TDefinition>> _logger;
  private readonly EventHandler<EventArgs> _configurationChangeHandler;

  private readonly IHealthCheckEvaluator<TDefinition> _healthCheckEvaluator;

  /// <summary>
  ///   Dictionary of tenant names to queue of health checks
  /// </summary>
  private readonly
    ConcurrentDictionary<String,
      ConcurrentQueue<(TaskCompletionSource<HealthStatus> Future, HealthCheckIdentifier HealthCheck, TDefinition Definition)>>
    _healthCheckQueues =
      new(StringComparer.OrdinalIgnoreCase);

  private readonly SemaphoreSlim _pendingChecks = new(0);
  private readonly ConcurrentBag<Task> _pendingConcurrencyLimitReductions = new();

  private readonly ConcurrentDictionary<Guid, Task> _runningHealthChecks = new();

  public HealthCheckQueueProcessor(
    IHealthCheckEvaluator<TDefinition> healthCheckEvaluator,
    INotifyOptionsChanged<HealthCheckQueueProcessorConfiguration> configuration,
    ILogger<HealthCheckQueueProcessor<TDefinition>> logger) {
    this._healthCheckEvaluator = healthCheckEvaluator;
    this._configuration = configuration;
    this._logger = logger;

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

  /// <summary>
  ///   Starts a background task that process pending health checks asynchronously. The
  ///   <see cref="Task" /> returned by this method will not complete until cancellation is requested via
  ///   the provided <see cref="CancellationToken" />.
  /// </summary>
  public async Task Run(CancellationToken cancellationToken) {
    var roundRobinOffset = 0;

    while (true) {
      // Wait for there to be a concurrency slots available
      await this._concurrencyLimit.WaitAsync(cancellationToken);
      this._logger.LogTrace(
        "[{ThreadId}] Concurrency capacity available...",
        Environment.CurrentManagedThreadId
      );

      // Wait for there to be some work available
      await this._pendingChecks.WaitAsync(cancellationToken);
      this._logger.LogTrace(
        "[{ThreadId}] Additional work available...",
        Environment.CurrentManagedThreadId
      );

      var success = false;

      // Cycle through tenants until we find some work
      // Since tenants may be removed, it's possible we'll find no work, so only do a single pass
      var tenants = this._healthCheckQueues.Keys.ToList();
      for (var i = 0; i < tenants.Count; i++) {
        // This round robin strategy assumes relatively stable order for ConcurrentDictionary keys,
        // but tenants shouldn't been added and removed very frequently
        var tenant = tenants[(roundRobinOffset + i) % tenants.Count];
        this._logger.LogTrace(
          "[{ThreadId}] Checking tenant \"{Tenant}\" for pending work...",
          Environment.CurrentManagedThreadId,
          tenant
        );
        if (this._healthCheckQueues.TryGetValue(tenant, out var queue) && queue.TryDequeue(out var check)) {
          var taskId = Guid.NewGuid();
          this._logger.LogTrace(
            "[{ThreadId}] Starting health check \"{HealthCheck}\" ({TaskId})",
            Environment.CurrentManagedThreadId,
            check.HealthCheck,
            taskId
          );
          this._runningHealthChecks.TryAdd(
            taskId,
            this.RunHealthCheckAsync(taskId, check.Future, check.HealthCheck, check.Definition, cancellationToken)
          );

          success = true;
          break;
        }
      }

      if (!success) {
        this._logger.LogTrace("No additional work found, a tenant must have been removed");
        // We didn't find the pending health check! That can only mean it was removed from the queue
        // due to tenant removal, which in turn mean there has been one extra call to _pendingChecks.Wait
        this._pendingChecks.Release();
        this._concurrencyLimit.Release();
      }

      roundRobinOffset++;
    }
  }

  /// <summary>
  ///   Queues a health check for evaluation.
  /// </summary>
  /// <returns>
  ///   An awaitable <see cref="Task{HealthStatus}" /> that will provide the result of the result of
  ///   the health check once it is evaluated.
  /// </returns>
  public Task<HealthStatus> QueueHealthCheck(
    String tenant,
    HealthCheckIdentifier healthCheck,
    TDefinition healthCheckDefinition) {

    var future = new TaskCompletionSource<HealthStatus>();
    this._healthCheckQueues.AddOrUpdate(
      tenant,
      addValueFactory: (_) => {
        var queue = new ConcurrentQueue<(TaskCompletionSource<HealthStatus>, HealthCheckIdentifier, TDefinition)>();
        queue.Enqueue((future, healthCheck, healthCheckDefinition));
        return queue;
      },
      updateValueFactory: (_, queue) => {
        queue.Enqueue((future, healthCheck, healthCheckDefinition));
        return queue;
      }
    );

    this._pendingChecks.Release();

    return future.Task;
  }

  /// <summary>
  ///   Removes and cancels all queued health checks for the specified tenant.
  /// </summary>
  public void RemoveTenant(String tenant) {
    if (this._healthCheckQueues.TryRemove(tenant, out var queue)) {
      while (queue.TryDequeue(out var entry)) {
        // Decrement the pending checks counter
        this._pendingChecks.Wait();
        entry.Future.SetCanceled();
      }
    }
  }

  /// <summary>
  ///   Calls the <see cref="IHealthCheckEvaluator{TDefinition}" />'s
  ///   <see cref="IHealthCheckEvaluator{TDefinition}.EvaluateHealthCheckAsync" /> method and uses the
  ///   returned value to complete the <see cref="Task{HealthStatus}" /> for the health check.
  /// </summary>
  private async Task RunHealthCheckAsync(
    Guid taskId,
    TaskCompletionSource<HealthStatus> future,
    HealthCheckIdentifier healthCheck,
    TDefinition healthCheckDefinition,
    CancellationToken cancellationToken) {

    try {
      this._logger.LogDebug(
        "[{ThreadId}] Evaluating Health Check Evaluation (HealthCheck: {HealthCheck}, TaskId: {TaskId})",
        Environment.CurrentManagedThreadId,
        healthCheck,
        taskId
      );
      var result = await this._healthCheckEvaluator.EvaluateHealthCheckAsync(
        healthCheck,
        healthCheckDefinition,
        cancellationToken
      );
      this._logger.LogDebug(
        "[{ThreadId}] Health Check Complete (HealthCheck: {HealthCheck}, Status: {Status}, TaskId: {TaskId})",
        Environment.CurrentManagedThreadId,
        healthCheck,
        result,
        taskId
      );
      future.SetResult(result);
    } catch (OperationCanceledException) {
      future.SetCanceled(cancellationToken);
      this._logger.LogDebug(
        "Health Check Canceled (HealthCheck: {HealthCheck}, TaskId: {TaskId})",
        healthCheck,
        taskId
      );
    } catch (Exception ex) {
      // This is a background task, notify the thread that is awaiting the future.
      future.SetException(ex);
    } finally {
      // Whether the check succeeds or fails, free up the concurrency slot
      this._concurrencyLimit.Release();
      this._runningHealthChecks.TryRemove(taskId, out _);
    }
  }
}
