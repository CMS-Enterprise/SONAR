using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Cms.BatCave.Sonar.Agent.Helpers;

public class RoundRobinQueue<T> : IProducerConsumerCollection<T> where T : IRoundRobinQueueItem {

  private readonly Object _lockObject = new();
  private readonly OrderedDictionary _queues = new();
  private Int32 _roundRobinIndex;

  public Int32 Count {
    get {
      lock (this._lockObject) {
        return this._queues.Cast<DictionaryEntry>().Sum(q => (q.Value as Queue<T>)!.Count);
      }
    }
  }

  public void Enqueue(T item) {
    lock (this._lockObject) {
      if (!this._queues.Contains(item.QueueKey)) {
        this._queues.Add(item.QueueKey, new Queue<T>());
      }
      (this._queues[item.QueueKey] as Queue<T>)!.Enqueue(item);
    }
  }

  public Boolean TryDequeue(out T item) {
    var nextItem = default(T);
    var found = false;

    lock (this._lockObject) {

      if (this._queues.Count != 0) {
        var startIndex = this._roundRobinIndex;
        do {
          var queue = (this._queues[this._roundRobinIndex] as Queue<T>)!;
          if (queue.Count != 0) {
            nextItem = queue.Dequeue();
            found = true;
          }
          this._roundRobinIndex = (this._roundRobinIndex + 1) % this._queues.Count;
        } while ((found == false) && (this._roundRobinIndex != startIndex));
      }

    }

    item = nextItem!;
    return found;
  }

  public Boolean TryAdd(T item) {
    this.Enqueue(item);
    return true;
  }

  public Boolean TryTake(out T item) {
    return this.TryDequeue(out item);
  }

  /// These are required by <see cref="IProducerConsumerCollection{T}"/> but aren't necessary to implement under the
  /// current use-case for this data structure.
  #region NotImplemented
  public IEnumerator<T> GetEnumerator() { throw new NotImplementedException(); }
  IEnumerator IEnumerable.GetEnumerator() { throw new NotImplementedException(); }
  void ICollection.CopyTo(Array array, Int32 index) { throw new NotImplementedException(); }
  public void CopyTo(T[] array, Int32 index) { throw new NotImplementedException(); }
  public T[] ToArray() { throw new NotImplementedException(); }
  #endregion

  /// Also required by IProducerConsumerCollection; use the same values as ConcurrentQueue.
  Boolean ICollection.IsSynchronized => false;
  Object ICollection.SyncRoot => throw new NotSupportedException();
}

public interface IRoundRobinQueueItem {
  Object QueueKey { get; }
}
