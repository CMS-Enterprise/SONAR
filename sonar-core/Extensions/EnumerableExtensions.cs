using System;
using System.Collections.Generic;

namespace Cms.BatCave.Sonar.Extensions;

public static class EnumerableExtensions {
  public static Boolean StartsWith<T>(this IEnumerable<T> source, IEnumerable<T> prefix) {
    return source.StartsWith(prefix, EqualityComparer<T>.Default);
  }

  public static Boolean StartsWith<T>(
    this IEnumerable<T> source,
    IEnumerable<T> prefix,
    IEqualityComparer<T> equalityComparer) {
    using var sourceIter = source.GetEnumerator();
    using var prefixIter = prefix.GetEnumerator();

    while (prefixIter.MoveNext()) {
      if (!sourceIter.MoveNext()) {
        // Source was shorter than prefix
        return false;
      }

      if (!equalityComparer.Equals(sourceIter.Current, prefixIter.Current)) {
        // Corresponding items are not equal
        return false;
      }
    }

    return true;
  }
}
