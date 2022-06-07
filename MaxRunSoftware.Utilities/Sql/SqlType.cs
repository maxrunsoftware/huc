/*
Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

namespace MaxRunSoftware.Utilities;

public class SqlType
{
    public Type EnumType { get; }
    public string EnumName { get; }
    public object EnumObject { get; }

    public string SqlTypeName { get; private set; }

    public IReadOnlyList<string> SqlTypeNames { get; }

    public bool HasAttribute { get; }
    public DbType DbType { get; }
    public Type DotNetType { get; }
    public SqlType ActualItem { get; private set; }

    private readonly string actualItemName;

    private SqlType(Type enumType, string enumName)
    {
        EnumType = enumType;
        EnumName = enumName;

        EnumObject = Util.GetEnumItem(enumType, enumName);

        var attribute = enumType.GetEnumItemAttribute<SqlTypeAttribute>(enumName);
        HasAttribute = (attribute != null);

        var sqlTypeNames = new List<string>();
        if (attribute != null)
        {
            DbType = attribute.DbType;
            DotNetType = attribute.DotNetType;

            var names = attribute.SqlTypeNames.TrimOrNull();
            if (names != null)
            {
                sqlTypeNames.AddRange(names.Split(',', ';', '|').TrimOrNull().WhereNotNull());
            }

            if (attribute.ActualSqlType != null) actualItemName = attribute.ActualSqlType.ToString();
        }

        sqlTypeNames.Add(enumName);

        var sqlTypeNames2 = new List<string>();
        var sqlTypeNames2Set = new HashSet<string>();
        foreach (var n in sqlTypeNames) if (sqlTypeNames2Set.Add(n)) sqlTypeNames2.Add(n);

        SqlTypeNames = sqlTypeNames2.AsReadOnly();
    }

    private static readonly object locker = new();
    private static readonly Dictionary<Type, IReadOnlyList<SqlType>> cacheL = new();
    private static readonly Dictionary<Type, Dictionary<string, SqlType>> cacheD = new();

    public static IReadOnlyList<SqlType> GetEnumItems(Type enumType)
    {
        enumType.CheckIsEnum(nameof(enumType));
        lock (locker)
        {
            if (cacheL.TryGetValue(enumType, out var list)) return list;
            var l = Scan(enumType).AsReadOnly();
            cacheL.Add(enumType, l);
            return l;
        }
    }

    public static SqlType GetEnumItemBySqlName(Type enumType, string sqlName)
    {
        enumType.CheckIsEnum(nameof(enumType));
        sqlName = sqlName.CheckNotNullTrimmed(nameof(sqlName));

        lock (locker)
        {
            if (!cacheD.TryGetValue(enumType, out var dic))
            {
                dic = new Dictionary<string, SqlType>(StringComparer.OrdinalIgnoreCase);
                var list = GetEnumItems(enumType);
                foreach (var enumItem in list)
                {
                    foreach (var sqlTypeName in enumItem.SqlTypeNames)
                    {
                        dic.Add(sqlTypeName, enumItem);
                    }
                }
                cacheD.Add(enumType, dic);
            }
            return dic.GetValueOrDefault(sqlName);
        }
    }

    private static List<SqlType> Scan(Type enumType)
    {
        enumType.CheckIsEnum(nameof(enumType));

        var list = new List<SqlType>();
        foreach (var name in enumType.GetEnumNames().OrEmpty())
        {
            list.Add(new SqlType(enumType, name));
        }

        // Duplicate Sql Type name check
        var sqlTypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in list)
        {
            foreach (var sqlTypeName in item.SqlTypeNames)
            {
                if (!sqlTypeNames.Add(sqlTypeName))
                {
                    throw new InvalidOperationException(enumType.FullNameFormatted() + $" defines multiple SqlType names of '{sqlTypeName}'");
                }
            }
        }

        // Update ActualItem references
        var d = new Dictionary<string, SqlType>();
        foreach (var item in list) d[item.EnumName] = item;
        foreach (var item in list)
        {
            if (item.actualItemName == null) continue;
            if (d.TryGetValue(item.actualItemName, out var actualItem))
            {
                item.ActualItem = actualItem;
            }
            else
            {
                throw new InvalidOperationException(enumType.FullNameFormatted() + "." + item.EnumName + " references non-existant item " + item.actualItemName);
            }
        }

        // Update SqlTypeName for base objects
        foreach (var item in list.Where(o => o.ActualItem == null))
        {
            item.SqlTypeName = item.SqlTypeNames.First();
        }

        // Update SqlTypeName for other objects
        foreach (var item in list.Where(o => o.ActualItem == null))
        {
            var areadyCheckedItemNames = new HashSet<string>();
            var current = item;
            while (current.ActualItem != null)
            {
                if (!areadyCheckedItemNames.Add(current.EnumName))
                {
                    throw new InvalidOperationException($"Circular [{nameof(SqlTypeAttribute.ActualSqlType)}] reference detected in {item.EnumType.FullNameFormatted()} with items " + areadyCheckedItemNames.OrderBy(o => o.ToUpper().ToStringDelimited(", ")));
                }
                current = current.ActualItem;
            }
            item.SqlTypeName = current.SqlTypeName;
        }


        // Return results
        return list;
    }
}


