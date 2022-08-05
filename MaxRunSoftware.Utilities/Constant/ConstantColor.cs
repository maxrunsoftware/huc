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

using System.Drawing;

namespace MaxRunSoftware.Utilities;

// ReSharper disable InconsistentNaming
public static partial class Constant
{
    /// <summary>
    /// Case-Sensitive map of Color names to Colors
    /// </summary>
    public static readonly ImmutableDictionary<string, Color> Colors = Colors_Create();

    private static ImmutableDictionary<string, Color> Colors_Create()
    {
        // https://stackoverflow.com/a/3821197

        var b = ImmutableDictionary.CreateBuilder<string, Color>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var colorType = typeof(Color);
            // We take only static property to avoid properties like Name, IsSystemColor ...
            var propInfos = colorType.GetProperties(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public);
            foreach (var propInfo in propInfos)
            {
                var colorGetMethod = propInfo.GetGetMethod();
                if (colorGetMethod == null) continue;

                var colorObject = colorGetMethod.Invoke(null, null);
                if (colorObject == null) continue;

                var colorObjectType = colorObject.GetType();
                if (colorObjectType != typeof(Color)) continue;

                var color = (Color)colorObject;
                var colorName = propInfo.Name;
                b.TryAdd(colorName, color);
            }
        }
        catch (Exception e) { LogError(e); }

        return b.ToImmutable();
    }
}
