using System.Collections.Generic;

namespace System;

public static class TupleComparer {
  public static IEqualityComparer<(T1, T2)> From<T1, T2>(IEqualityComparer<T1> comparer1, IEqualityComparer<T2> comparer2) {
    return new TupleComparer<T1, T2>(comparer1, comparer2);
  }
}

public class TupleComparer<T1, T2> : IEqualityComparer<(T1, T2)> {
  private readonly IEqualityComparer<T1> _comparer1;
  private readonly IEqualityComparer<T2> _comparer2;

  public TupleComparer(IEqualityComparer<T1> comparer1, IEqualityComparer<T2> comparer2) {
    this._comparer1 = comparer1;
    this._comparer2 = comparer2;
  }

  public Boolean Equals((T1, T2) x, (T1, T2) y) {
    return this._comparer1.Equals(x.Item1, y.Item1) && this._comparer2.Equals(x.Item2, y.Item2);
  }

  public Int32 GetHashCode((T1, T2) obj) {
    return HashCode.Combine(
      obj.Item1 != null ? this._comparer1.GetHashCode(obj.Item1) : 0,
      obj.Item2 != null ? this._comparer2.GetHashCode(obj.Item2) : 0
    );
  }
}
