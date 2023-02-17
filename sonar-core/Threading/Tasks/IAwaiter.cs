using System;
using System.Runtime.CompilerServices;

namespace Cms.BatCave.Sonar.Threading.Tasks;

/// <summary>
///   This is the missing interface for the awaiter returned to facility the C# <c>await</c> keyword.
/// </summary>
public interface IAwaiter<out T> : INotifyCompletion {
  /// <summary>
  ///   Indicates whether the awaitable object has successfully completed or not.
  /// </summary>
  Boolean IsCompleted { get; }

  /// <summary>
  ///   Blocks until a value becomes available and than returns the provided value. If the task is
  ///   disposed, canceled, or becomes faulted before it successfully completes then this method should
  ///   raise the corresponding exception.
  /// </summary>
  /// <exception cref="ObjectDisposedException">
  ///   The associated awaitable object was disposed before a value was provided.
  /// </exception>
  /// <exception cref="OperationCanceledException">
  ///   The associated awaitable process was cancelled before a value was provided.
  /// </exception>
  /// <exception cref="AggregateException">
  ///   An exception was raised by the awaitable process.
  /// </exception>
  T GetResult();
}
