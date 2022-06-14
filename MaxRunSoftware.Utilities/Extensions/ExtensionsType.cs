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

using System.Linq.Expressions;

namespace MaxRunSoftware.Utilities;

public static class ExtensionsType
{
    public static bool IsNullable(this Type type, out Type underlyingType)
    {
        underlyingType = Nullable.GetUnderlyingType(type);
        return underlyingType != null;
    }

    public static bool IsNullable(this Type type) => Nullable.GetUnderlyingType(type) != null;

    /// <summary>Is this type a generic type</summary>
    /// <param name="type"></param>
    /// <returns>True if generic, otherwise False</returns>
    public static bool IsGeneric(this Type type) => type.IsGenericType && type.Name.Contains("`");

    //TODO: Figure out why IsGenericType isn't good enough and document (or remove) this condition
    public static Type AsNullable(this Type type)
    {
        // https://stackoverflow.com/a/23402284
        type.CheckNotNull(nameof(type));
        if (!type.IsValueType) return type;

        if (Nullable.GetUnderlyingType(type) != null) return type; // Already nullable

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) return type; // Already nullable

        return typeof(Nullable<>).MakeGenericType(type);
    }

    public static string FullNameFormatted(this Type type)
    {
        // https://stackoverflow.com/a/25287378
        if (type.IsGenericType)
        {
            var name = type.FullName ?? type.Name;
            return string.Format(
                "{0}<{1}>",
                name.Substring(0, name.LastIndexOf("`", StringComparison.InvariantCulture)),
                string.Join(", ", type.GetGenericArguments().Select(FullNameFormatted)));
        }

        return type.FullName;
    }

    public static string NameFormatted(this Type type)
    {
        // https://stackoverflow.com/a/25287378
        if (type.IsGenericType)
            return string.Format(
                "{0}<{1}>",
                type.Name.Substring(0, type.Name.LastIndexOf("`", StringComparison.InvariantCulture)),
                string.Join(", ", type.GetGenericArguments().Select(NameFormatted)));

        return type.Name;
    }

    public static IEnumerable<Type> GetTypesOf<T>(this Assembly assembly, bool allowAbstract = false, bool allowInterface = false, bool requireNoArgConstructor = false, bool namespaceSystem = false)
    {
        foreach (var t in assembly.GetTypes())
        {
            if (t.Namespace == null) continue;

            if (t.Namespace.StartsWith("System.", StringComparison.OrdinalIgnoreCase) && namespaceSystem == false) continue;

            if (t.IsInterface && allowInterface == false) continue;

            if (t.IsAbstract && t.IsInterface == false && allowAbstract == false) continue;

            if (requireNoArgConstructor && t.GetConstructor(Type.EmptyTypes) == null) continue;

            if (typeof(T).IsAssignableFrom(t) == false) continue;

            yield return t;
        }
    }

    #region EqualsAny

    public static bool Equals<T1>(this Type type) => typeof(T1) == type;

    public static bool Equals<T1, T2>(this Type type) => typeof(T1) == type || typeof(T2) == type;

    public static bool Equals<T1, T2, T3>(this Type type) => typeof(T1) == type || typeof(T2) == type || typeof(T3) == type;

    public static bool Equals<T1, T2, T3, T4>(this Type type) => typeof(T1) == type || typeof(T2) == type || typeof(T3) == type || typeof(T4) == type;

    public static bool Equals<T1, T2, T3, T4, T5>(this Type type) => typeof(T1) == type || typeof(T2) == type || typeof(T3) == type || typeof(T4) == type || typeof(T5) == type;

    public static bool Equals<T1, T2, T3, T4, T5, T6>(this Type type) => typeof(T1) == type || typeof(T2) == type || typeof(T3) == type || typeof(T4) == type || typeof(T5) == type || typeof(T6) == type;

    public static bool Equals<T1, T2, T3, T4, T5, T6, T7>(this Type type) => typeof(T1) == type || typeof(T2) == type || typeof(T3) == type || typeof(T4) == type || typeof(T5) == type || typeof(T6) == type || typeof(T7) == type;

    public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8>(this Type type) => typeof(T1) == type || typeof(T2) == type || typeof(T3) == type || typeof(T4) == type || typeof(T5) == type || typeof(T6) == type || typeof(T7) == type || typeof(T8) == type;

    public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Type type) => typeof(T1) == type || typeof(T2) == type || typeof(T3) == type || typeof(T4) == type || typeof(T5) == type || typeof(T6) == type || typeof(T7) == type || typeof(T8) == type || typeof(T9) == type;

    public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this Type type) => typeof(T1) == type || typeof(T2) == type || typeof(T3) == type || typeof(T4) == type || typeof(T5) == type || typeof(T6) == type || typeof(T7) == type || typeof(T8) == type || typeof(T9) == type || typeof(T10) == type;

    public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this Type type) => typeof(T1) == type || typeof(T2) == type || typeof(T3) == type || typeof(T4) == type || typeof(T5) == type || typeof(T6) == type || typeof(T7) == type || typeof(T8) == type || typeof(T9) == type || typeof(T10) == type || typeof(T11) == type;

    public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this Type type) => typeof(T1) == type || typeof(T2) == type || typeof(T3) == type || typeof(T4) == type || typeof(T5) == type || typeof(T6) == type || typeof(T7) == type || typeof(T8) == type || typeof(T9) == type || typeof(T10) == type || typeof(T11) == type || typeof(T12) == type;

    public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this Type type) => typeof(T1) == type || typeof(T2) == type || typeof(T3) == type || typeof(T4) == type || typeof(T5) == type || typeof(T6) == type || typeof(T7) == type || typeof(T8) == type || typeof(T9) == type || typeof(T10) == type || typeof(T11) == type || typeof(T12) == type || typeof(T13) == type;

    public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this Type type) => typeof(T1) == type || typeof(T2) == type || typeof(T3) == type || typeof(T4) == type || typeof(T5) == type || typeof(T6) == type || typeof(T7) == type || typeof(T8) == type || typeof(T9) == type || typeof(T10) == type || typeof(T11) == type || typeof(T12) == type || typeof(T13) == type || typeof(T14) == type;

    public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this Type type) => typeof(T1) == type || typeof(T2) == type || typeof(T3) == type || typeof(T4) == type || typeof(T5) == type || typeof(T6) == type || typeof(T7) == type || typeof(T8) == type || typeof(T9) == type || typeof(T10) == type || typeof(T11) == type || typeof(T12) == type || typeof(T13) == type || typeof(T14) == type || typeof(T15) == type;

    public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this Type type) => typeof(T1) == type || typeof(T2) == type || typeof(T3) == type || typeof(T4) == type || typeof(T5) == type || typeof(T6) == type || typeof(T7) == type || typeof(T8) == type || typeof(T9) == type || typeof(T10) == type || typeof(T11) == type || typeof(T12) == type || typeof(T13) == type || typeof(T14) == type || typeof(T15) == type || typeof(T16) == type;

    #endregion EqualsAny

    #region DefaultValue

    private static readonly IBucketReadOnly<Type, object> getDefaultValueCache = new DefaultValueCache();

    private sealed class DefaultValueCache : IBucketReadOnly<Type, object>
    {
        private readonly IBucketReadOnly<Type, object> bucket = new BucketCacheThreadSafeCopyOnWrite<Type, object>(Activator.CreateInstance);
        public IEnumerable<Type> Keys => bucket.Keys;
        public object this[Type key] => key == null ? null : key.IsValueType ? bucket[key] : null;
    }

    public static object GetDefaultValue(this Type type) => getDefaultValueCache[type];

    public static object GetDefaultValue2(this Type type)
    {
        var o = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Default(type), typeof(object))).Compile()();
        return o;
    }

    #endregion DefaultValue

    /*
    public static TAttribute[] GetCustomAttributes<TAttribute>(this FieldInfo field, bool inherit) where TAttribute : Attribute
    {
        var list = new List<TAttribute>();
        foreach (var attr in field.GetCustomAttributes(inherit))
        {
            if (attr is TAttribute a) list.Add(a);
        }
        return list.ToArray();
    }
    */

    public static TAttribute[] GetEnumItemAttributes<TAttribute>(this Type enumType, string enumItemName) where TAttribute : Attribute
    {
        enumType.CheckIsEnum(nameof(enumType));
        enumItemName = enumItemName.CheckNotNullTrimmed(nameof(enumItemName));

        foreach (var sc in Constant.STRINGCOMPARERS)
        {
            foreach (var name in enumType.GetEnumNames())
            {
                if (sc.Equals(name, enumItemName))
                {
                    var field = enumType.GetField(name, BindingFlags.Public | BindingFlags.Static);
                    if (field != null) return field.GetCustomAttributes<TAttribute>(false).ToArray();
                }
            }
        }

        return Array.Empty<TAttribute>();
    }


    public static TAttribute GetEnumItemAttribute<TAttribute>(this Type enumType, string enumItemName) where TAttribute : Attribute => GetEnumItemAttributes<TAttribute>(enumType, enumItemName).FirstOrDefault();

    public static bool IsStatic(this PropertyInfo info, bool nonPublic = false) => info.GetAccessors(nonPublic).Any(x => x.IsStatic);
    // https://stackoverflow.com/a/51441889
}
