using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cms.BatCave.Sonar.Threading.Tasks;

/// <summary>
///   An awaitable placeholder for data that may become available at some point in the future.
///   Consumers can poll, register a callback, or call the blocking <see cref="GetResult" /> method to
///   wait for completion.
/// </summary>
/// <typeparam name="T">The type of object this Future is a placeholder for.</typeparam>
public sealed class Future<T> : IAwaitable<T>, IAwaiter<T>, IDisposable {
  private readonly Object _sync = new();

  private T? _value;

  // Cross thread synchronization signal, raised when a value becomes available, otherwise disposed.
  private ManualResetEventSlim? _signal = new();

  /// <summary>
  ///   The <see cref="Exception" /> that caused the Future to become faulted.
  /// </summary>
  public Exception? Error { get; private set; }

  /// <summary>
  ///   Indicates whether or not the Future has successfully completed.
  /// </summary>
  public Boolean IsCompleted { get; private set; }

  /// <summary>
  ///   Indicates whether or not the Future was cancelled before it was successfully completed or
  ///   faulted.
  /// </summary>
  public Boolean IsCanceled => !this.IsCompleted && (this.Error == null) && (this._signal == null);

  /// <summary>
  ///   Indicates whether or not the Future has been marked as faulted.
  /// </summary>
  public Boolean IsFaulted => !this.IsCompleted && (this.Error != null);

  /// <summary>
  ///   An event that is invoked once when the Future is successfully completed.
  /// </summary>
  public event EventHandler<EventArgs>? Completed;

  private Future() {
  }

  /// <summary>
  ///   Disposes the Future. If the Future is not yet in the Completed, Faulted, or Canceled state when
  ///   it is disposed it will be transitioned into that state.
  /// </summary>
  /// <remarks>
  ///   As with the <see cref="Task.Dispose" /> method on <see cref="Task" />, this method is not thread
  ///   safe and should not be called before the Future is completed. However, no exception will be
  ///   raised in the event that Dispose is called before the Future is completed.
  /// </remarks>
  public void Dispose() {
    var tmp = this._signal;
    if (tmp != null) {
      // De-register continuations
      this.Completed = null;
      // Mark the Future as canceled (if it isn't already Completed or Faulted)
      this._signal = null;
      tmp.Dispose();
    }
  }

  /// <summary>
  ///   This method is required for the await keyword to be applied to the object.
  /// </summary>
  public IAwaiter<T> GetAwaiter() {
    return this;
  }

  /// <summary>
  ///   Registers a continuation callback that is invoked upon successfully completion. If the Future has
  ///   already completed, this callback will be invoked immediately.
  /// </summary>
  public void OnCompleted(Action continuation) {
    lock (this._sync) {
      if (!this.IsCompleted && (this.Error == null) && (this._signal != null)) {
        this.Completed += (_, _) => {
          continuation();
        };

        return;
      }
    }

    continuation();
  }

  /// <summary>
  ///   Blocks until the Future is completed or cancelled and returns the result value if completed.
  /// </summary>
  /// <exception cref="OperationCanceledException">
  ///   Thrown if the Future has been cancelled or is cancelled while waiting for completion.
  /// </exception>
  /// <exception cref="AggregateException">
  ///   The Future has been marked as faulted. See the <see cref="AggregateException.InnerException" />
  ///   for details.
  /// </exception>
  public T GetResult() {
    this._signal?.Wait();

    if (this.IsCanceled) {
      throw new OperationCanceledException();
    }

    if (this.IsFaulted) {
      // Wrap in an aggregate exception so that we do not disrupt the stack trace
      throw new AggregateException(this.Error!);
    }

    return this._value!;
  }

  /// <summary>
  ///   Creates a new Future instance and the corresponding <see cref="Provider" /> which can be used to
  ///   provide a value to consumers of that Future.
  /// </summary>
  public static (Future<T>, Provider) Create() {
    var future = new Future<T>();
    return (future, new Provider(future));
  }

  public class Provider {
    private readonly Future<T> _future;

    internal Provider(Future<T> future) {
      this._future = future;
    }

    /// <summary>
    ///   Provide a value to consumers of the associated <see cref="Future{T}" />.
    /// </summary>
    /// <remarks>
    ///   Calling this function after a <see cref="Future{T}" /> has been cancelled has no effect.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///   Thrown if the <see cref="Future{T}" /> has already been successfully completed or faulted.
    /// </exception>
    public void Provide(T value) {
      EventHandler<EventArgs>? continuations;
      var callContinuations = false;
      lock (this._future._sync) {
        if (this._future.IsCompleted) {
          throw new InvalidOperationException("A Future cannot be provided with a value multiple times.");
        } else if (this._future.Error != null) {
          throw new InvalidOperationException(
            "A Future cannot be provided with a value after it is in the faulted state.");
        }

        // Getting these as local variables to avoid a race condition where we successfully set the
        // signal, but are not successful in invoking the continuations.
        continuations = this._future.Completed;
        var signal = this._future._signal;
        if (signal != null) {
          try {
            this._future._value = value;
            this._future.IsCompleted = true;
            signal.Set();
            // Only perform the continuations if we successfully trigger the ManualResetEvent
            callContinuations = true;
          } catch (ObjectDisposedException) {
            // If the ManualResetEvent was disposed before we could trigger it, the Future is
            // cancelled instead of completed.
            this._future.IsCompleted = false;
          }
        }
      }
      // Only call continuations after releasing the lock. Even though locks are reentrant, if the
      // continuation blocks waiting for some other thread attempting to perform another operation
      // on this Future that would cause a deadlock.
      if (callContinuations) {
        continuations?.Invoke(this, EventArgs.Empty);
      }
    }

    /// <summary>
    ///   Cancels the <see cref="Future{T}" />.
    /// </summary>
    public void Cancel() {
      EventHandler<EventArgs>? continuations;
      Boolean callContinuations;
      lock (this._future._sync) {
        // Calling Dispose sets the Completed event handler to null
        continuations = this._future.Completed;
        this._future.Dispose();
        callContinuations = !this._future.IsCompleted && (this._future.Error == null);
      }
      // Only call continuations after releasing the lock.
      if (callContinuations) {
        continuations?.Invoke(this, EventArgs.Empty);
      }
    }

    /// <summary>
    ///   Marks the <see cref="Future{T}" /> as faulted.
    /// </summary>
    public void Fail(Exception ex) {
      if (ex == null) {
        throw new ArgumentNullException(nameof(ex));
      }

      EventHandler<EventArgs>? continuations;
      var callContinuations = false;
      lock (this._future._sync) {
        if (this._future.Error != null) {
          throw new InvalidOperationException("A Future cannot be faulted multiple times.");
        }

        // Getting these as local variables to avoid a race condition where we successfully set the
        // signal, but are not successful in invoking the continuations.
        continuations = this._future.Completed;
        var signal = this._future._signal;
        if (signal != null) {
          try {
            this._future.Error = ex;
            signal.Set();
            // Only perform the continuations if we successfully trigger the ManualResetEvent
            callContinuations = true;
          } catch (ObjectDisposedException) {
            // If the ManualResetEvent was disposed before we could trigger it, the Future is
            // cancelled instead of faulted.
            this._future.Error = null;
          }
        }
      }

      // Only call continuations after releasing the lock.
      if (callContinuations) {
        continuations?.Invoke(this, EventArgs.Empty);
      }
    }
  }
}
