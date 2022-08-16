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

namespace MaxRunSoftware.Utilities.CommandLine;



public static class CommandUtil
{
    public static T? GetAttribute<T>(MemberInfo info) where T : Attribute => info.GetCustomAttributes(false).Select(o => o as T).WhereNotNull().FirstOrDefault() ?? info.GetCustomAttributes(true).Select(o => o as T).WhereNotNull().FirstOrDefault();

    public static string GetPropertyName(PropertyInfo info, bool fullTypeName)
    {
        return GetPropertyName(info.ReflectedType ?? info.DeclaringType, info, fullTypeName);
    }

    public static string GetPropertyName(TypeSlim type, PropertyInfo info, bool fullTypeName)
    {
        return GetPropertyName(type.Type, info, fullTypeName);
    }

    public static string GetPropertyName(Type? type, PropertyInfo info, bool fullTypeName)
    {
        var sb = new StringBuilder();

        if (type != null)
        {
            sb.Append(fullTypeName ? type.FullNameFormatted() : type.NameFormatted());
            sb.Append('.');
        }

        sb.Append(info.Name);

        return sb.ToString();
    }



    public static Func<object?, object?, int> GetCompareTo(PropertyInfo info)
    {
        var propertyType = Nullable.GetUnderlyingType(info.PropertyType) ?? info.PropertyType;
        if (propertyType.IsEnum) throw new ArgumentException($"Cannot create Comparer for Enum ");
        if (propertyType == typeof(string)) throw new ArgumentException($"Cannot define {nameof(Min)} or {nameof(Max)} for String");
    }
    public static MethodInfo GetCompareToMethod(PropertyInfo info)
    {
        if (info.PropertyType.IsAssignableTo(typeof(IComparable<>).MakeGenericType(info.PropertyType)))
        {
            // ReSharper disable once ReplaceWithSingleCallToFirst
            return info.PropertyType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(mi => mi.Name.Equals(nameof(IComparable.CompareTo)))
                .Where(mi => mi.ReturnType == typeof(int))
                .Where(mi => mi.GetParameters().Length == 1)
                .Where(mi => mi.GetParameters()[0].ParameterType.IsAssignableFrom(info.PropertyType))
                .First();
        }

        if (info.PropertyType.IsAssignableTo(typeof(IComparable)))
        {
            // ReSharper disable once ReplaceWithSingleCallToFirst
            return info.PropertyType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(mi => mi.Name.Equals(nameof(IComparable.CompareTo)))
                .Where(mi => mi.ReturnType == typeof(int))
                .Where(mi => mi.GetParameters().Length == 1)
                .Where(mi => mi.GetParameters()[0].ParameterType == typeof(object))
                .First();
        }

        throw new ArgumentException($"Type {info.PropertyType.Name} does not implement interface " + typeof(IComparable<>).MakeGenericType(info.PropertyType).NameFormatted() + " or interface " + typeof(IComparable).NameFormatted());

    }
}
