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

public static class ExtensionsEqualCompare
{
    #region IsEqual

    public static bool IsEqual<T>(this T? x, T? y) where T : IEquatable<T>
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
        return x.Equals(y);
    }

    public static bool IsEqual<T>(this T? x, T? y) where T : struct, IEquatable<T>
    {
        if (!x.HasValue && !y.HasValue) return true;
        if (!x.HasValue || !y.HasValue) return false;
        return x.Value.Equals(y.Value);
    }

    public static bool IsEqual(this object? x, object? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
        return x.Equals(y);
    }

    public static bool IsEqual(this string? x, string? y) => StringComparer.Ordinal.Equals(x, y);
    public static bool IsEqualOrdinalIgnoreCase(this string? x, string? y) => StringComparer.OrdinalIgnoreCase.Equals(x, y);

    #endregion IsEqual

    #region Compare

    public static int Compare<T>(this T? x, T? y) where T : IComparable<T>
    {
        if (ReferenceEquals(x, y)) return 0;
        if (ReferenceEquals(x, null)) return -1;
        if (ReferenceEquals(y, null)) return 1;
        return x.CompareTo(y);
    }

    public static int Compare<T>(this T? x, T? y) where T : struct, IComparable<T>
    {
        if (!x.HasValue && !y.HasValue) return 0;
        if (!x.HasValue) return -1;
        if (!y.HasValue) return 1;
        return x.Value.CompareTo(y.Value);
    }

    public static int Compare(this string? x, string? y)
    {
        int c;
        if ((c = CompareOrdinalIgnoreCase(x, y)) != 0) return c;
        if ((c = CompareOrdinal(x, y)) != 0) return c;
        return 0;
    }

    public static int CompareOrdinal(this string? x, string? y) => StringComparer.Ordinal.Compare(x, y);
    public static int CompareOrdinalIgnoreCase(this string? x, string? y) => StringComparer.OrdinalIgnoreCase.Compare(x, y);

    #endregion Compare
}
