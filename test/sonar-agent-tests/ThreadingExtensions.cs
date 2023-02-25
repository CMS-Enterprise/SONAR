using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cms.BatCave.Sonar.Agent.Tests;

public static class ThreadingExtensions {
  public static Task<Boolean> WaitAsync(
    this ManualResetEventSlim resetEvent,
    TimeSpan timespan,
    CancellationToken cancellationToken = default) {

    return Task.Run(
      () => resetEvent.Wait(timespan, cancellationToken),
      cancellationToken
    );
  }

  public static Task<Boolean> WaitAsync(
    this ManualResetEventSlim resetEvent,
    Int32 timeout,
    CancellationToken cancellationToken = default) {

    return Task.Run(
      () => resetEvent.Wait(timeout, cancellationToken),
      cancellationToken
    );
  }
}
