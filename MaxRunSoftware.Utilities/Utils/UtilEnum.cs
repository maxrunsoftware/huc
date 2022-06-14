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

public static partial class Util
{
    private static readonly EnumCache enumCache = new();

    private sealed class EnumCache
    {
        private static readonly IBucketReadOnly<Type, IReadOnlyDictionary<string, object>> enums = new BucketCacheThreadSafeCopyOnWrite<Type, IReadOnlyDictionary<string, object>>(type => Enum.GetNames(type).ToDictionary(o => o, o => Enum.Parse(type, o)).AsReadOnly());

        private readonly object locker = new();
        private volatile IReadOnlyDictionary<DicKey, object> cache = new Dictionary<DicKey, object>().AsReadOnly();

        private readonly struct DicKey : IEquatable<DicKey>
        {
            private readonly int hashCode;
            private readonly Type enumType;
            private readonly string enumItemName;

            public DicKey(Type enumType, string enumItemName)
            {
                this.enumType = enumType;
                this.enumItemName = enumItemName;
                hashCode = GenerateHashCode(enumType, enumItemName);
            }

            public override int GetHashCode() => hashCode;

            public override bool Equals(object obj) => obj is DicKey key && Equals(key);

            public bool Equals(DicKey other) => hashCode == other.hashCode && enumType == other.enumType && enumItemName.Equals(other.enumItemName);
        }

        public bool TryGetEnumObject(Type enumType, string enumItemName, out object enumObject, bool throwExceptions)
        {
            enumItemName = enumItemName.TrimOrNull();
            if (throwExceptions)
            {
                enumType.CheckNotNull(nameof(enumType));
                enumItemName.CheckNotNull(nameof(enumItemName));
            }
            else
            {
                if (enumType == null || enumItemName == null)
                {
                    enumObject = null;
                    return false;
                }
            }

            if (!enumType.IsEnum)
            {
                var ut = Nullable.GetUnderlyingType(enumType);
                if (ut == null || !ut.IsEnum)
                {
                    if (throwExceptions) throw new ArgumentException("Type [" + enumType.FullNameFormatted() + "] is not an enum", nameof(enumType));

                    enumObject = null;
                    return false;
                }

                enumType = ut;
            }

            var key = new DicKey(enumType, enumItemName);

            if (cache.TryGetValue(key, out var eo))
            {
                enumObject = eo;
                return true;
            }

            lock (locker)
            {
                if (cache.TryGetValue(key, out eo))
                {
                    enumObject = eo;
                    return true;
                }

                var d = enums[enumType];
                foreach (var sc in Constant.STRINGCOMPARISONS)
                {
                    foreach (var kvp in d)
                    {
                        if (string.Equals(kvp.Key, enumItemName, sc))
                        {
                            eo = kvp.Value;
                            var c = new Dictionary<DicKey, object>();
                            foreach (var kvp2 in cache) c.Add(kvp2.Key, kvp2.Value);

                            c.Add(key, eo);
                            cache = c.AsReadOnly();

                            enumObject = eo;
                            return true;
                        }
                    }
                }

                if (throwExceptions)
                {
                    var itemNames = string.Join(", ", d.Keys.ToArray());
                    throw new ArgumentException("Type Enum [" + enumType.FullNameFormatted() + "] does not contain a member named '" + enumItemName + "', valid values are... " + itemNames, nameof(enumItemName));
                }

                enumObject = null;
                return false;
            }
        }
    }

    public static TEnum GetEnumItem<TEnum>(string name) where TEnum : struct, Enum => (TEnum)GetEnumItem(typeof(TEnum), name);

    public static object GetEnumItem(Type enumType, string name) => enumCache.TryGetEnumObject(enumType.CheckIsEnum(nameof(enumType)), name, out var o, true) ? o : null;

    /// <summary>
    /// Tries to parse a string to an Enum value, if not found return null
    /// </summary>
    /// <typeparam name="TEnum">The enum</typeparam>
    /// <param name="name">The enum item name</param>
    /// <returns>The enum item or null if not found</returns>
    public static TEnum? GetEnumItemNullable<TEnum>(string name) where TEnum : struct, Enum
    {
        if (name == null) return null;

        var o = GetEnumItemNullable(typeof(TEnum), name);
        if (o == null) return null;

        return (TEnum)o;
    }

    public static object GetEnumItemNullable(Type enumType, string name)
    {
        if (name == null) return null;

        if (enumCache.TryGetEnumObject(enumType.CheckIsEnum(nameof(enumType)), name, out var o, false)) return o;

        return null;
    }

    public static IReadOnlyList<TEnum> GetEnumItems<TEnum>() where TEnum : struct, Enum => (TEnum[])Enum.GetValues(typeof(TEnum));

    public static IReadOnlyList<object> GetEnumItems(Type enumType)
    {
        enumType.CheckIsEnum(nameof(enumType));
        var list = new List<object>();
        foreach (var item in Enum.GetValues(enumType)) list.Add(item);

        return list;
    }

    public static TEnum CombineEnumFlags<TEnum>(IEnumerable<TEnum> enums) where TEnum : struct, Enum => (TEnum)Enum.Parse(typeof(TEnum), string.Join(", ", enums.Select(o => o.ToString())));
}
