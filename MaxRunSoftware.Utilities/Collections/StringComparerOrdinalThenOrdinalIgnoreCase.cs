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

public sealed class StringComparerOrdinalThenOrdinalIgnoreCase : StringComparer
{
    public static readonly StringComparerOrdinalThenOrdinalIgnoreCase INSTANCE = new();

    private StringComparerOrdinalThenOrdinalIgnoreCase() { }

    private readonly StringComparer ordinal = Ordinal;
    private readonly StringComparer ordinalIgnoreCase = OrdinalIgnoreCase;

    public override int Compare(string? x, string? y)
    {
        var c = ordinal.Compare(x, y);
        return c != 0 ? c : ordinalIgnoreCase.Compare(x, y);
    }
    public override bool Equals(string? x, string? y) => ordinal.Equals(x, y);
    public override int GetHashCode(string obj) => ordinal.GetHashCode(obj);
}

public static class StringComparerOrdinalThenOrdinalIgnoreCaseExtensions
{
    public static IOrderedEnumerable<string> OrderByOrdinalThenOrdinalIgnoreCase(this IEnumerable<string> obj) => obj.OrderBy(o => o, StringComparerOrdinalThenOrdinalIgnoreCase.INSTANCE);
    public static IOrderedEnumerable<T> OrderByOrdinalThenOrdinalIgnoreCase<T>(this IEnumerable<T> obj, Func<T, string> keySelector) => obj.OrderBy(keySelector, StringComparerOrdinalThenOrdinalIgnoreCase.INSTANCE);
}
