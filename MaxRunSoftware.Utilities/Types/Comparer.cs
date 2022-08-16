// Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Diagnostics.CodeAnalysis;

namespace MaxRunSoftware.Utilities;

public abstract class EqualityComparerBase<T> : IEqualityComparer<T>, IEqualityComparer
{
    protected virtual bool CheckTypeExact => false;

    public bool Equals(T x, T y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return true;
        if (ReferenceEquals(y, null)) return true;
        if (x == null && y == null) return true;
        if (x == null) return false;
        if (y == null) return false;
        if (CheckTypeExact && x.GetType() != y.GetType()) return false;
        //if (GetHashCode(x) != GetHashCode(y)) return false;
        return EqualsInternal(x, y);
    }

    protected abstract bool EqualsInternal([NotNull] T x, [NotNull] T y);

    public int GetHashCode(T obj)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        // ReSharper disable once HeuristicUnreachableCode
        if (obj == null) return 0;
        var v = new List<object>();
        if (CheckTypeExact) v.Add(obj.GetType());
        GetHashCodeValues(obj, v);
        if (v.Count == 0) return 0;
        return Util.HashEnumerable(v);
    }

    protected abstract void GetHashCodeValues([NotNull] T obj, [NotNull] List<object> v);

    protected int HashOrdinalIgnoreCase(string s) => s == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(s);
    protected int HashOrdinal(string s) => s == null ? 0 : StringComparer.Ordinal.GetHashCode(s);

    protected bool EqualsOrdinalIgnoreCase(string o1, string o2) => StringComparer.OrdinalIgnoreCase.Equals(o1, o2);
    protected bool EqualsOrdinal(string o1, string o2) => StringComparer.Ordinal.Equals(o1, o2);

    public new bool Equals(object x, object y)
    {
        if (x is T xt && y is T yt) return Equals(xt, yt);

        return false;
    }

    public int GetHashCode(object obj)
    {
        if (obj is T ot) return GetHashCode(ot);

        return 0;
    }
}

public abstract class ComparerBase<T> : EqualityComparerBase<T>, IComparer<T>, IComparer
{
    public int Compare(T x, T y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (ReferenceEquals(x, null)) return -1;
        if (ReferenceEquals(y, null)) return 1;
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        if (CheckTypeExact)
        {
            var c = new TypeSlim(x.GetType()).CompareTo(new TypeSlim(y.GetType()));
            if (c != 0) return c;
        }

        //if (GetHashCode(x) != GetHashCode(y)) return false;
        return CompareInternal(x, y);
    }

    protected abstract int CompareInternal([NotNull] T x, [NotNull] T y);

    protected int CompareOrdinalIgnoreCase(string o1, string o2) => StringComparer.OrdinalIgnoreCase.Compare(o1, o2);
    protected int CompareOrdinal(string o1, string o2) => StringComparer.Ordinal.Compare(o1, o2);

    public int Compare(object x, object y)
    {
        if (x is T xt) return y is T yt ? Compare(xt, yt) : 1;
        if (y is T) return -1;

        return 0;
    }
}

public abstract class ComparerListBase<T, TList> : ComparerBase<TList> where TList : IReadOnlyList<T>
{
    protected override bool EqualsInternal(TList x, TList y)
    {
        var count = x.Count;
        if (count != y.Count) return false;
        for (var i = 0; i < count; i++)
        {
            var xItem = x[i];
            var yItem = y[i];
            if (ReferenceEquals(xItem, yItem)) continue;
            if (ReferenceEquals(xItem, null)) return false;
            if (ReferenceEquals(yItem, null)) return false;
            if (!EqualsInternal(xItem, yItem)) return false;
        }

        return true;
    }

    protected abstract bool EqualsInternal([NotNull] T x, [NotNull] T y);


    protected override int CompareInternal(TList x, TList y)
    {
        var xCount = x.Count;
        var yCount = y.Count;
        var count = Math.Min(xCount, yCount);

        for (var i = 0; i < count; i++)
        {
            var xItem = x[i];
            var yItem = y[i];
            if (ReferenceEquals(xItem, yItem)) continue;
            if (ReferenceEquals(xItem, null)) return -1;
            if (ReferenceEquals(yItem, null)) return 1;
            var c = CompareInternal(xItem, yItem);
            if (c != 0) return c;
        }

        return xCount - yCount;
    }

    protected abstract int CompareInternal([NotNull] T x, [NotNull] T y);


    protected override void GetHashCodeValues(TList obj, List<object> v)
    {
        var count = obj.Count;
        if (count == 0) return;
        v.Add(count);
        foreach (var item in obj)
        {
            if (ReferenceEquals(item, null)) v.Add(0);
            else
            {
                var itemsToHash = new List<object>();
                GetHashCodeValues(item, itemsToHash);
                var itemsHashed = Util.Hash(itemsToHash);
                v.Add(itemsHashed);
            }
        }
    }

    protected abstract void GetHashCodeValues([NotNull] T obj, [NotNull] List<object> v);
}

public class ComparerListComparable<T, TList> : ComparerListBase<T, TList> where TList : IReadOnlyList<T> where T : IEquatable<T>, IComparable<T>
{
    protected override bool EqualsInternal(T x, T y) => x.Equals(y);
    protected override int CompareInternal(T x, T y) => x.CompareTo(y);
    protected override void GetHashCodeValues(T obj, List<object> v) => v.Add(obj.GetHashCode());
}
