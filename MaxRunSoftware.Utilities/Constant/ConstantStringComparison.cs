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

// ReSharper disable InconsistentNaming
public static partial class Constant
{
    /// <summary>
    /// List of String Comparisons from most restrictive to least
    /// </summary>
    public static readonly ImmutableArray<StringComparison> StringComparisons = ImmutableArray.Create(
        StringComparison.Ordinal,
        StringComparison.CurrentCulture,
        StringComparison.InvariantCulture,
        StringComparison.OrdinalIgnoreCase,
        StringComparison.CurrentCultureIgnoreCase,
        StringComparison.InvariantCultureIgnoreCase
    );

    /// <summary>
    /// Map of StringComparison to StringComparer
    /// </summary>
    public static readonly ImmutableDictionary<StringComparison, StringComparer> StringComparison_StringComparer = StringComparison_StringComparer_Create();

    private static ImmutableDictionary<StringComparison, StringComparer> StringComparison_StringComparer_Create()
    {
        var b = ImmutableDictionary.CreateBuilder<StringComparison, StringComparer>();
        b.TryAdd(StringComparison.CurrentCulture, StringComparer.CurrentCulture);
        b.TryAdd(StringComparison.CurrentCultureIgnoreCase, StringComparer.CurrentCultureIgnoreCase);
        b.TryAdd(StringComparison.InvariantCulture, StringComparer.InvariantCulture);
        b.TryAdd(StringComparison.InvariantCultureIgnoreCase, StringComparer.InvariantCultureIgnoreCase);
        b.TryAdd(StringComparison.Ordinal, StringComparer.Ordinal);
        b.TryAdd(StringComparison.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase);
        return b.ToImmutableDictionary();
    }
}
