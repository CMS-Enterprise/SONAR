using System;
using System.Collections.Generic;
using System.Linq;

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

  /// <summary>
  /// Checks whether <paramref name="enumerable"/> is null or empty.
  /// </summary>
  /// <remarks>
  /// This is shamelessly cribbed from <see cref="Microsoft.IdentityModel.Tokens.CollectionUtilities.IsNullOrEmpty{T}"/>
  /// for the sole purpose of avoiding the weird-seeming import of <c>Microsoft.IdentityModel.Tokens</c> where such a
  /// method is needed.
  /// </remarks>
  /// <typeparam name="T">The type of the <paramref name="enumerable"/>.</typeparam>
  /// <param name="enumerable">The <see cref="IEnumerable{T}"/> to be checked.</param>
  /// <returns>True if <paramref name="enumerable"/> is null or empty, false otherwise.</returns>
  public static Boolean IsNullOrEmpty<T>(this IEnumerable<T>? enumerable) {
    return (enumerable is null) || !enumerable.Any();
  }
}
