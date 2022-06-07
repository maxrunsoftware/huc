// /*
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
// */

namespace MaxRunSoftware.Utilities;

public sealed class ClassReaderWriter
{
    private static readonly IBucketReadOnly<Type, ClassReaderWriter> cache = new BucketCacheThreadSafeCopyOnWrite<Type, ClassReaderWriter>(t => new ClassReaderWriter(t));

    public Type Type { get; }
    public IReadOnlyDictionary<string, PropertyReaderWriter> Properties { get; }

    public ClassReaderWriter(Type type)
    {
        Type = type.CheckNotNull(nameof(type));

        var list = new List<PropertyReaderWriter>();
        foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            list.Add(new PropertyReaderWriter(propertyInfo, isStatic: false));
        }
        foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
        {
            list.Add(new PropertyReaderWriter(propertyInfo, isStatic: true));
        }
        list = list.Where(o => o.CanGet || o.CanSet).ToList();

        var d = new Dictionary<string, PropertyReaderWriter>();
        foreach (var prop in list)
        {
            d.Add(prop.Name, prop);
        }

        Properties = new DictionaryReadOnlyStringCaseInsensitive<PropertyReaderWriter>(d);
    }

    public static ClassReaderWriter Get(Type type) => cache[type.CheckNotNull(nameof(type))];

    public static IEnumerable<PropertyReaderWriter> GetProperties(
        Type type,
        bool canGet = false,
        bool canSet = false,
        bool isStatic = false,
        bool isInstance = false
        )
    {
        var list = new List<PropertyReaderWriter>();
        foreach (var prop in Get(type).Properties.Values)
        {
            if (canGet && !prop.CanGet) continue;
            if (canSet && !prop.CanSet) continue;
            if (!isStatic && !isInstance) continue; // don't include anything? maybe throw exception because you probably forgot something
            if (!isStatic && isInstance && prop.IsStatic) continue; // don't include static
            if (isStatic && !isInstance && prop.IsInstance) continue; // don't include instance

            //if (isStatic && isInstance) ; // include everything

            list.Add(prop);
        }
        return list.OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase);
    }
}
