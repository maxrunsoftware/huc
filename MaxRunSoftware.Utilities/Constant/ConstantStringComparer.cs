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
    public static readonly ImmutableArray<StringComparer> StringComparers = ImmutableArray.Create(
        StringComparer.Ordinal,
        StringComparer.CurrentCulture,
        StringComparer.InvariantCulture,
        StringComparer.OrdinalIgnoreCase,
        StringComparer.CurrentCultureIgnoreCase,
        StringComparer.InvariantCultureIgnoreCase
    );

    /// <summary>
    /// Map of StringComparer to StringComparison
    /// </summary>
    public static readonly ImmutableDictionary<StringComparer, StringComparison> StringComparer_StringComparison = StringComparer_StringComparison_Create();

    private static ImmutableDictionary<StringComparer, StringComparison> StringComparer_StringComparison_Create()
    {
        var b = ImmutableDictionary.CreateBuilder<StringComparer, StringComparison>();
        b.TryAdd(StringComparer.CurrentCulture, StringComparison.CurrentCulture);
        b.TryAdd(StringComparer.CurrentCultureIgnoreCase, StringComparison.CurrentCultureIgnoreCase);
        b.TryAdd(StringComparer.InvariantCulture, StringComparison.InvariantCulture);
        b.TryAdd(StringComparer.InvariantCultureIgnoreCase, StringComparison.InvariantCultureIgnoreCase);
        b.TryAdd(StringComparer.Ordinal, StringComparison.Ordinal);
        b.TryAdd(StringComparer.OrdinalIgnoreCase, StringComparison.OrdinalIgnoreCase);
        return b.ToImmutableDictionary();
    }
}
