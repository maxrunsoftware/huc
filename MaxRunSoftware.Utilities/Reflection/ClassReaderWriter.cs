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

public sealed class ClassReaderWriter
{
    private static readonly IBucketReadOnly<Type, ClassReaderWriter> cache = new BucketCacheThreadSafeCopyOnWrite<Type, ClassReaderWriter>(t => new ClassReaderWriter(t));

    public Type Type { get; }
    public IReadOnlyDictionary<string, PropertyReaderWriter> Properties { get; }

    public ClassReaderWriter(Type type)
    {
        Type = type.CheckNotNull(nameof(type));

        var list = new List<PropertyReaderWriter>();
        foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) list.Add(new PropertyReaderWriter(propertyInfo, false));

        foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)) list.Add(new PropertyReaderWriter(propertyInfo, true));
        //list = list.Where(o => o.CanGet || o.CanSet).ToList();

        var d = new Dictionary<string, PropertyReaderWriter>();
        foreach (var prop in list) d.Add(prop.Name, prop);

        Properties = new DictionaryReadOnlyStringCaseInsensitive<PropertyReaderWriter>(d);
    }

    public static ClassReaderWriter Get(Type type) => cache[type.CheckNotNull(nameof(type))];

    public static IEnumerable<PropertyReaderWriter> GetProperties(
        Type type,
        bool? canGet = null,
        bool? canSet = null,
        bool isStatic = false,
        bool isInstance = false
    )
    {
        var list = new List<PropertyReaderWriter>();
        foreach (var prop in Get(type).Properties.Values)
        {
            if (canGet.HasValue && canGet == true && !prop.CanGet) continue;

            if (canGet.HasValue && canGet == false && prop.CanGet) continue;

            if (canSet.HasValue && canSet == true && !prop.CanSet) continue;

            if (canSet.HasValue && canSet == false && prop.CanSet) continue;

            if (!isStatic && !isInstance) continue; // don't include anything? maybe throw exception because you probably forgot something

            if (!isStatic && prop.IsStatic) continue; // don't include static

            if (isStatic && !isInstance && prop.IsInstance) continue; // don't include instance

            //if (isStatic && isInstance) ; // include everything

            list.Add(prop);
        }

        return list.OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase);
    }

    public static DictionaryReadOnlyStringCaseInsensitive<PropertyReaderWriter> GetPropertiesDictionary(
        Type type,
        bool? canGet = null,
        bool? canSet = null,
        bool isStatic = false,
        bool isInstance = false
    )
    {
        var d = new Dictionary<string, PropertyReaderWriter>();
        foreach (var prop in GetProperties(type, canGet, canSet, isStatic, isInstance)) d.Add(prop.Name, prop);

        return new DictionaryReadOnlyStringCaseInsensitive<PropertyReaderWriter>(d);
    }

    public static IEnumerable<KeyValuePair<string, object>> GetPropertiesValues(object source)
    {
        foreach (var prop in GetProperties(source.GetType(), true, isInstance: true)) yield return KeyValuePair.Create(prop.Name, prop.GetValue(source));
    }

    public static void CopyProperties(object source, object target, bool ignoreMissingPropertiesOnTarget = false)
    {
        // TODO: If CopyProperties is called a bunch of object this can be expensive, so cache the GetProperties calls on the Type

        var sourceType = source.GetType();
        var sourceProps = GetProperties(sourceType, true, isInstance: true).ToList();

        var targetType = target.GetType();
        var targetPropsD = GetPropertiesDictionary(targetType, canSet: true, isInstance: true);

        foreach (var sourceProp in sourceProps)
        {
            if (targetPropsD.TryGetValue(sourceProp.Name, out var targetProp))
            {
                var sourcePropObject = sourceProp.GetValue(source);
                targetProp.SetValue(target, sourcePropObject);
            }
            else
            {
                if (!ignoreMissingPropertiesOnTarget) throw new MissingMemberException(targetType.FullNameFormatted(), sourceProp.Name);
            }
        }
    }

    public static T CopyObject<T>(T source) where T : new()
    {
        var target = new T();
        CopyProperties(source, target);
        return target;
    }

    public static object CopyObject(object source)
    {
        var target = Util.CreateInstance(source.GetType());
        CopyProperties(source, target);
        return target;
    }
}
