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

namespace MaxRunSoftware.Utilities;

public abstract class ImmutableObjectBase : IComparable
{
    private readonly Lzy<int> hashCode;
    private readonly Lzy<string> toString;

    protected ImmutableObjectBase()
    {
        hashCode = CreateLazy(GetHashCode_Build);
        toString = CreateLazy(ToString_Build);
    }

    public sealed override int GetHashCode() => hashCode.Value;

    public sealed override string ToString() => toString.Value;

    public sealed override bool Equals(object? other)
    {
        if (ReferenceEquals(other, null)) return false;
        if (ReferenceEquals(other, this)) return true;
        if (GetHashCode() != other.GetHashCode()) return false;
        return Equals_Internal(other);
    }

    public int CompareTo(object? other)
    {
        if (ReferenceEquals(other, null)) return 1;
        if (ReferenceEquals(other, this)) return 0;
        return CompareTo_Internal(other);
    }

    #region Abstract

    protected abstract int GetHashCode_Build();

    protected abstract string ToString_Build();

    protected abstract bool Equals_Internal(object other);

    protected abstract int CompareTo_Internal(object other);

    #endregion Abstract

    #region Helpers

    protected static Lzy<T> CreateLazy<T>(Func<T> valueFactory) => new(valueFactory);

    /*
    #region IsEqual

    protected static bool IsEqual<T>(T? x, T? y) where T : IEquatable<T>
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
        return x.Equals(y);
    }

    protected static bool IsEqual<T>(T? x, T? y) where T : struct, IEquatable<T>
    {
        if (!x.HasValue && !y.HasValue) return true;
        if (!x.HasValue || !y.HasValue) return false;
        return x.Value.Equals(y.Value);
    }

    protected static bool IsEqual(string? x, string? y) => StringComparer.Ordinal.Equals(x, y);
    protected static bool IsEqualOrdinalIgnoreCase(string? x, string? y) => StringComparer.OrdinalIgnoreCase.Equals(x, y);

    #endregion IsEqual

    #region Compare

    protected static int Compare<T>(T? x, T? y) where T : IComparable<T>
    {
        if (ReferenceEquals(x, y)) return 0;
        if (ReferenceEquals(x, null)) return -1;
        if (ReferenceEquals(y, null)) return 1;
        return x.CompareTo(y);
    }

    protected static int Compare<T>(T? x, T? y) where T : struct, IComparable<T>
    {
        if (!x.HasValue && !y.HasValue) return 0;
        if (!x.HasValue) return -1;
        if (!y.HasValue) return 1;
        return x.Value.CompareTo(y.Value);
    }

    protected static int Compare(string? x, string? y)
    {
        int c;
        if ((c = CompareOrdinalIgnoreCase(x, y)) != 0) return c;
        if ((c = CompareOrdinal(x, y)) != 0) return c;
        return 0;
    }
    protected static int CompareOrdinal(string? x, string? y) => StringComparer.Ordinal.Compare(x, y);
    protected static int CompareOrdinalIgnoreCase(string? x, string? y) => StringComparer.OrdinalIgnoreCase.Compare(x, y);

    #endregion Compare
    */

    #endregion Helpers
}

public abstract class ImmutableObjectBase<T> : ImmutableObjectBase, IEquatable<T>, IComparable<T> where T : class
{
    #region ImmutableObjectBase

    protected sealed override bool Equals_Internal(object other) => Equals(other as T);
    protected sealed override int CompareTo_Internal(object other) => CompareTo(other as T);

    #endregion ImmutableObjectBase

    public bool Equals(T? other)
    {
        if (ReferenceEquals(other, null)) return false;
        if (ReferenceEquals(other, this)) return true;
        if (GetHashCode() != other.GetHashCode()) return false;
        return Equals_Internal(other);
    }

    public int CompareTo(T? other)
    {
        if (ReferenceEquals(other, null)) return 1;
        if (ReferenceEquals(other, this)) return 0;
        return CompareTo_Internal(other);
    }

    #region Abstract

    protected abstract bool Equals_Internal(T other);
    protected abstract int CompareTo_Internal(T other);

    #endregion Abstract
}
