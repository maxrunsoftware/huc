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
    /// Case-Insensitive map of boolean string values to boolean values
    /// </summary>
    public static readonly ImmutableDictionary<string, bool> String_Bool = CreateDictionary(StringComparer.OrdinalIgnoreCase,
        ("1", true),
        ("T", true),
        ("TRUE", true),
        ("Y", true),
        ("YES", true),
        ("0", false),
        ("F", false),
        ("FALSE", false),
        ("N", false),
        ("NO", false)
    );


    /// <summary>
    /// Case-Insensitive hashset of boolean true values
    /// </summary>
    public static readonly ImmutableHashSet<string> Bool_True = CreateHashSet(StringComparer.OrdinalIgnoreCase, String_Bool.Where(o => o.Value).Select(o => o.Key).ToArray());

    /// <summary>
    /// Case-Insensitive hashset of boolean false values
    /// </summary>
    public static readonly ImmutableHashSet<string> Bool_False = CreateHashSet(StringComparer.OrdinalIgnoreCase, String_Bool.Where(o => !o.Value).Select(o => o.Key).ToArray());
}
