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
    public static readonly ImmutableHashSet<char> PathDelimiters = PathDelimiters_Create();

    private static ImmutableHashSet<char> PathDelimiters_Create()
    {
        var hs = new HashSet<char>(new[] { '/', '\\', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
        var b = ImmutableHashSet.CreateBuilder<char>();
        foreach (var c in hs) b.Add(c);
        return b.ToImmutable();
    }

    public static readonly ImmutableHashSet<string> PathDelimiters_String = ImmutableHashSet.Create(PathDelimiters.Select(o => o.ToString()).ToArray());

    public static readonly bool Path_IsCaseSensitive = !OS_Windows;
    public static readonly StringComparer Path_StringComparer = Path_IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
    public static readonly StringComparison Path_StringComparison = Path_IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
}
