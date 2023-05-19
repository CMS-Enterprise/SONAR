using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Xunit;

namespace Cms.BatCave.Sonar.Tests;

public class HashCodesTest {
  public static IEnumerable<Object[]> EmptyCollectionsData {
    get {
      // Different empty collections should have the same hashcode within the same process
      var expected = new HashCode().ToHashCode();

      yield return new Object[] {
        Array.Empty<Object>(),
        expected
      };

      yield return new Object[] {
        new List<Object>(),
        expected
      };

      yield return new Object[] {
        ImmutableHashSet<Object>.Empty,
        expected
      };
    }
  }

  [Theory]
  [MemberData(nameof(EmptyCollectionsData))]
  public void HashCodes_From_Empty(IEnumerable<Object> source, Int32 expected) {
    var result = HashCodes.From(source);
    Assert.Equal(expected, result);
  }


  public static IEnumerable<Object[]> OutOfOrderData {
    get {
      yield return new Object[] {
        new Object?[] { 1, 1, 2, 3, 5 },
        new Object?[] { 1, 2, 1, 3, 5 },
      };

      yield return new Object[] {
        new Object?[] { "foo", "bar", "baz" },
        new Object?[] { "foo", "baz", "bar" },
      };

      yield return new Object[] {
        new Object?[] { "foo", "bar", null },
        new Object?[] { "foo", null, "bar" },
      };
    }
  }

  /// <summary>
  ///   Verify that the same set of values in a different order results in a different hash.
  /// </summary>
  [Theory]
  [MemberData(nameof(OutOfOrderData))]
  public void HashCodes_From_OutOfOrder_DifferentHash(IEnumerable<Object?> original, IEnumerable<Object?> outOfOrder) {
    var hc1 = HashCodes.From(original);
    var hc2 = HashCodes.From(outOfOrder);

    Assert.NotEqual(hc1, hc2);
  }


  public static IEnumerable<Object[]> NullValuesData {
    get {
      yield return new Object[] {
        new Object?[] { 1, 1, 2, 3, 5 },
        new Object?[] { 1, 1, 2, 3, 5, null },
      };
      yield return new Object[] {
        new Object?[] { 1, 1, 2, 3, 5 },
        new Object?[] { 1, 1, null, 2, 3, 5 },
      };
      // The number of nulls is also significant
      yield return new Object[] {
        new Object?[] { 1, 1, null, 2, 3, 5 },
        new Object?[] { 1, 1, null, null, 2, 3, 5 },
      };
    }
  }

  /// <summary>
  ///   Verify that the having nulls interspersed in the sequence results in a different hash.
  /// </summary>
  [Theory]
  [MemberData(nameof(NullValuesData))]
  public void HashCodes_From_NullValues_DifferentHash(IEnumerable<Object?> original, IEnumerable<Object?> withNulls) {
    var hc1 = HashCodes.From(original);
    var hc2 = HashCodes.From(withNulls);

    Assert.NotEqual(hc1, hc2);
  }

  [Fact]
  public void HashCodes_From_BoxedValues_SameHash() {
    var values = new[] { 1, 1, 2, 3, 5 };
    var unboxedHc = HashCodes.From(values);
    var boxedHc = HashCodes.From(values.Cast<Object>().ToList());

    Assert.Equal(unboxedHc, boxedHc);
  }

  [Fact]
  public void HashCodes_From_StableAcrossThreads() {
    var values = new[] { 37, 73, 2701 };

    var crossThreadResult = -1;
    var thread = new Thread(
      () => {
        crossThreadResult = HashCodes.From(new[] { 37, 73, 37 * 73 });
      }
    );
    thread.Start();
    var localResult = HashCodes.From(values);
    thread.Join();

    Assert.Equal(localResult, crossThreadResult);
  }
}
