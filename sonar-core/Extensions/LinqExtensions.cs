using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Cms.BatCave.Sonar.Extensions;

public static class LinqExtensions {
  private static readonly MethodInfo AsQueryableMethod =
    new Func<IEnumerable<Object>, IQueryable<Object>>(Queryable.AsQueryable)
      .Method.GetGenericMethodDefinition();

  private static readonly MethodInfo DefaultIfEmptyMethod =
    new Func<IQueryable<Object>, IQueryable<Object?>>(Queryable.DefaultIfEmpty)
      .Method.GetGenericMethodDefinition();

  public static IQueryable<TResult> LeftJoin<TLeft, TRight, TKey, TResult>(
    this IQueryable<TLeft> leftSource,
    IEnumerable<TRight> rightSource,
    Expression<Func<TLeft, TKey>> leftKeySelector,
    Expression<Func<TRight, TKey>> rightKeySelector,
    Expression<Func<TLeft, TRight?, TResult>> resultSelector) {

    return ApplyResultSelector(
      leftSource.GroupJoin(rightSource, leftKeySelector, rightKeySelector,
        (left, rights) => new { Left = left, Rights = rights }),
      resultSelector
    );
  }

  private static IQueryable<TResult> ApplyResultSelector<TLeft, TRight, TGroup, TResult>(
    IQueryable<TGroup> groupJoin,
    Expression<Func<TLeft, TRight, TResult>> resultSelector) {

    var groupType = typeof(TGroup);

    var groupParam = Expression.Parameter(groupType, "group");
    var groupManySelector =
      Expression.Lambda<Func<TGroup, IEnumerable<TRight>>>(
        Expression.Call(
          DefaultIfEmptyMethod.MakeGenericMethod(typeof(TRight)),
          Expression.Call(
            AsQueryableMethod.MakeGenericMethod(typeof(TRight)),
            Expression.Property(groupParam, "Rights"))
        ),
        groupParam
      );

    var groupParam2 = Expression.Parameter(groupType, "group");
    var rightParam = Expression.Parameter(typeof(TRight), "right");
    var groupResultSelector =
      Expression.Lambda<Func<TGroup, TRight, TResult>>(
        Expression.Invoke(
          resultSelector,
          Expression.Property(groupParam2, "Left"),
          rightParam
        ),
        groupParam2,
        rightParam
      );

    return groupJoin
      .SelectMany(
        groupManySelector,
        groupResultSelector);
  }

  /// <summary>
  ///   Filter out nulls from an <see cref="IEnumerable{T}" />.
  /// </summary>
  /// <remarks>
  ///   This method is the equivalent of <c>source.Where(v => v != null)</c> except that with
  ///   <see cref="Enumerable.Where{T}(IEnumerable{T}, Func{T,Boolean})" /> the compiler is unable to
  ///   detect the type narrowing from a nullable to a non-null type.
  /// </remarks>
  /// <param name="source">The source collection.</param>
  /// <typeparam name="T">The type contained in the source collection.</typeparam>
  /// <returns>
  ///   An <see cref="IEnumerable{T}" /> containing only the non-null contents of the
  ///   <paramref name="source" />.
  /// </returns>
  public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> source) {
    foreach (var val in source) {
      if (val != null) {
        yield return val;
      }
    }
  }

  /// <summary>
  ///   Given an <see cref="IEnumerable{T}" /> <paramref name="source" />, either returns <c>null</c> if
  ///   that <see cref="IEnumerable{T}" /> is empty, or the original sequence if it is not.
  /// </summary>
  /// <remarks>
  ///   This method eagerly begins enumerating the <paramref name="source" />. If the
  ///   <see cref="IEnumerable{T} " /> that is returned is enumerated multiple times, the underlying
  ///   <paramref name="source" /> will also be re-enumerated.
  /// </remarks>
  /// <param name="source">The source sequence.</param>
  /// <typeparam name="T">The type contained by the source sequence.</typeparam>
  /// <returns>
  ///   Either <c>null</c> or an <see cref="IEnumerable{T}" /> that is equivalent to
  ///   <paramref name="source" />
  /// </returns>
  [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
  public static IEnumerable<T>? NullIfEmpty<T>(this IEnumerable<T> source) {
    var enumerator = source.GetEnumerator();
    try {
      return enumerator.MoveNext() ? new PeekedEnumerable<T>(source, enumerator) : null;
    } catch {
      enumerator.Dispose();
      throw;
    }
  }

  private class PeekedEnumerable<T> : IEnumerable<T> {
    private readonly IEnumerable<T> _source;
    private readonly IEnumerator<T> _enumerator;
    private Boolean _enumerated;

    public PeekedEnumerable(IEnumerable<T> source, IEnumerator<T> enumerator) {
      this._source = source;
      this._enumerator = enumerator;
    }

    public IEnumerator<T> GetEnumerator() {
      if (!this._enumerated) {
        this._enumerated = true;
        return new PeekedEnumerator(this._enumerator);
      } else {
        return this._source.GetEnumerator();
      }
    }

    private class PeekedEnumerator : IEnumerator<T> {
      private readonly IEnumerator<T> _enumerator;
      private Boolean _peeking = true;

      public T Current => this._enumerator.Current;

      Object IEnumerator.Current => this._enumerator.Current!;

      public PeekedEnumerator(IEnumerator<T> enumerator) {
        this._enumerator = enumerator;
      }

      public Boolean MoveNext() {
        if (this._peeking) {
          this._peeking = false;
          return true;
        } else {
          return this._enumerator.MoveNext();
        }
      }

      public void Reset() {
        this._peeking = false;
        this._enumerator.Reset();
      }

      public void Dispose() {
        this._enumerator.Dispose();
      }
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return this.GetEnumerator();
    }
  }
}
