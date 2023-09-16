using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Agent.Tests.Helpers;

public class RoundRobinQueueUnitTest {
  private readonly ITestOutputHelper _output;

  public RoundRobinQueueUnitTest(ITestOutputHelper output) {
    this._output = output;
  }

  [Fact]
  public void RoundRobinQueue_MaintainsOrdering() {
    var rrq1 = new RoundRobinQueue<RrqItem>();
    var rrq2 = new RoundRobinQueue<RrqItem>();

    // Enqueue items in rrq1 in order by Value first then Tenant.
    for (var i = 0; i < 3; i++) {
      rrq1.Enqueue(new RrqItem { Tenant = "Tenant_1", Value = $"Value_{i}" });
      rrq1.Enqueue(new RrqItem { Tenant = "Tenant_2", Value = $"Value_{i}" });
      rrq1.Enqueue(new RrqItem { Tenant = "Tenant_3", Value = $"Value_{i}" });
    }

    // Enqueue items in rrq2 in order by Tenant first then Value.
    for (var i = 0; i < 3; i++) {
      rrq2.Enqueue(new RrqItem { Tenant = "Tenant_1", Value = $"Value_{i}" });
    }
    for (var i = 0; i < 3; i++) {
      rrq2.Enqueue(new RrqItem { Tenant = "Tenant_2", Value = $"Value_{i}" });
    }
    for (var i = 0; i < 3; i++) {
      rrq2.Enqueue(new RrqItem { Tenant = "Tenant_3", Value = $"Value_{i}" });
    }

    // Dequeue items from both queues simultaneously; they should come out in the same order, round robin by Tenant.
    while (rrq1.TryDequeue(out var rrq1Item) && rrq2.TryDequeue(out var rrq2Item)) {
      Assert.Equal(rrq1Item, rrq2Item);
    }
  }

  [Fact]
  public void RoundRobinQueue_IsThreadSafe() {
    var items = MakeTestRrqItems();

    var concurrentQueue = new ConcurrentQueue<RrqItem>(items);
    var roundRobinQueue = new RoundRobinQueue<RrqItem>();

    void Enqueuer(Int32 id, ConcurrentQueue<RrqItem> cQueue, RoundRobinQueue<RrqItem> rrQueue) {
      this._output.WriteLine($"enqueuer {id} started");

      var itemsEnqueued = 0;

      while (!cQueue.IsEmpty) {
        if (cQueue.TryDequeue(out var item)) {
          rrQueue.Enqueue(item);
          itemsEnqueued++;
        }
      }

      this._output.WriteLine($"enqueuer {id} finished, {itemsEnqueued} items");
    }

    Task.WaitAll(
      Task.Run(() => Enqueuer(id: 1, concurrentQueue, roundRobinQueue)),
      Task.Run(() => Enqueuer(id: 2, concurrentQueue, roundRobinQueue)),
      Task.Run(() => Enqueuer(id: 3, concurrentQueue, roundRobinQueue)));

    Assert.Empty(concurrentQueue);
    Assert.Equal(items.Count, roundRobinQueue.Count);

    var dequeuedItems1 = new List<RrqItem>();
    var dequeuedItems2 = new List<RrqItem>();

    void Dequeuer(Int32 id, RoundRobinQueue<RrqItem> rrQueue, ICollection<RrqItem> dequeuedItems) {
      this._output.WriteLine($"dequeuer {id} started");

      while (rrQueue.TryDequeue(out var item)) {
        dequeuedItems.Add(item);
      }

      this._output.WriteLine($"dequeuer {id} finished, {dequeuedItems.Count} items");
    }

    Task.WaitAll(
      Task.Run(() => Dequeuer(id: 1, roundRobinQueue, dequeuedItems1)),
      Task.Run(() => Dequeuer(id: 2, roundRobinQueue, dequeuedItems2)));

    Assert.Equal(dequeuedItems1.Count, dequeuedItems1.Distinct().Count());
    Assert.Equal(dequeuedItems2.Count, dequeuedItems2.Distinct().Count());
    Assert.Equal(items.Count, dequeuedItems1.Count + dequeuedItems2.Count);
    dequeuedItems1.ForEach(item => Assert.Contains(item, items));
    dequeuedItems2.ForEach(item => Assert.Contains(item, items));
  }

  private static IImmutableList<RrqItem> MakeTestRrqItems() {
    const Int32 numTenants = 25;
    const Int32 numValuesPerTenant = 1000;

    var items = new List<RrqItem>(numTenants * numValuesPerTenant);

    for (var t = 0; t < numTenants; t++) {
      for (var v = 0; v < numValuesPerTenant; v++) {
        items.Add(new RrqItem { Tenant = $"Tenant_{t}", Value = $"Value_{v}" });
      }
    }

    return items.ToImmutableList();
  }
}

public record RrqItem : IRoundRobinQueueItem {
  public String Tenant { get; init; } = String.Empty;
  public String Value { get; init; } = String.Empty;
  public Object QueueKey => this.Tenant;
}
