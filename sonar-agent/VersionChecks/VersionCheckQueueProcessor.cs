using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Configuration;
using Cms.BatCave.Sonar.Agent.Helpers;
using Cms.BatCave.Sonar.Agent.VersionChecks.Models;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Agent.VersionChecks;

public sealed class VersionCheckQueueProcessor : IDisposable {

  private readonly IOptions<AgentConfiguration> _agentConfig;
  private readonly ConcurrentDictionary<Type, RoundRobinQueue<VersionCheckJob>> _queues;
  private readonly ConcurrentDictionary<Type, SemaphoreSlim> _workAvailable;

  public VersionCheckQueueProcessor(IOptions<AgentConfiguration> agentConfig) {
    this._agentConfig = agentConfig;
    this._queues = new ConcurrentDictionary<Type, RoundRobinQueue<VersionCheckJob>>();
    this._workAvailable = new ConcurrentDictionary<Type, SemaphoreSlim>();
  }

  public Task<VersionResponse> QueueVersionCheck(String tenant, VersionCheckModel model) {
    if (!this._queues.ContainsKey(model.Definition.GetType())) {
      return Task.FromException<VersionResponse>(new NotSupportedException(
        $"{model.Definition.GetType()} queue processor is not running."));
    }

    var job = new VersionCheckJob {
      Tenant = tenant,
      Model = model,
      Response = new TaskCompletionSource<VersionResponse>()
    };

    this._queues[model.Definition.GetType()].Enqueue(job);
    this._workAvailable[model.Definition.GetType()].Release();

    return job.Response.Task;
  }

  public async Task StartAsync<T>(
    IVersionRequester<T> versionRequester,
    CancellationToken cancellationToken)
    where T : VersionCheckDefinition {

    var createdSemaphore = this._workAvailable.TryAdd(typeof(T), new SemaphoreSlim(0));
    var createdQueue = this._queues.TryAdd(typeof(T), new RoundRobinQueue<VersionCheckJob>());

    if (!createdQueue || !createdSemaphore) {
      this._queues.TryRemove(typeof(T), out _);
      this._workAvailable.TryRemove(typeof(T), out _);
      throw new ApplicationException($"Failed to start queue processor task for ${typeof(T)}.");
    }

    using var workSlot = new SemaphoreSlim(this._agentConfig.Value.MaximumConcurrency);

    while (!cancellationToken.IsCancellationRequested) {
      await workSlot.WaitAsync(cancellationToken);
      await this._workAvailable[typeof(T)].WaitAsync(cancellationToken);

      if (this._queues[typeof(T)].TryDequeue(out var job)) {
        var _ = this.ProcessJobAsync(job, versionRequester, workSlot, cancellationToken);
      } else {
        workSlot.Release();
      }
    }
  }

  private async Task ProcessJobAsync<T>(
    VersionCheckJob job,
    IVersionRequester<T> versionRequester,
    SemaphoreSlim workSlot,
    CancellationToken cancellationToken)
    where T : VersionCheckDefinition {

    try {
      if (job.Model.Definition is T definition) {
        job.Response.SetResult(
          await versionRequester.GetVersionAsync(definition, cancellationToken));
      } else {
        job.Response.SetException(new ApplicationException(
          $"Dequeued {job.Model.Definition.GetType()}, but {typeof(T)} was expected."));
      }
    } catch (OperationCanceledException) {
      job.Response.SetCanceled(cancellationToken);
    } catch (Exception e) {
      job.Response.SetException(e);
    } finally {
      workSlot.Release();
    }
  }

  public void Dispose() {
    foreach (var (_, semaphore) in this._workAvailable) {
      semaphore.Dispose();
    }
  }

  private record VersionCheckJob : IRoundRobinQueueItem {
    public Object QueueKey => this.Tenant;
    public String Tenant { get; init; } = String.Empty;
    public VersionCheckModel Model { get; init; } = default!;
    public TaskCompletionSource<VersionResponse> Response { get; init; } = default!;
  }
}
