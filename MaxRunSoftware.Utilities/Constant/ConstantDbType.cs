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

using System.Net;
using System.Numerics;

namespace MaxRunSoftware.Utilities;

// ReSharper disable InconsistentNaming
public static partial class Constant
{
    /// <summary>
    /// Map of DotNet types to DbType
    /// </summary>
    public static readonly ImmutableDictionary<Type, DbType> Type_DbType = Type_DbType_Create();

    private static ImmutableDictionary<Type, DbType> Type_DbType_Create()
    {
        var b = ImmutableDictionary.CreateBuilder<Type, DbType>();

        b.TryAdd(typeof(bool), DbType.Boolean);
        b.TryAdd(typeof(bool?), DbType.Boolean);

        b.TryAdd(typeof(byte), DbType.Byte);
        b.TryAdd(typeof(byte?), DbType.Byte);
        b.TryAdd(typeof(sbyte), DbType.SByte);
        b.TryAdd(typeof(sbyte?), DbType.SByte);

        b.TryAdd(typeof(short), DbType.Int16);
        b.TryAdd(typeof(short?), DbType.Int16);
        b.TryAdd(typeof(ushort), DbType.UInt16);
        b.TryAdd(typeof(ushort?), DbType.UInt16);

        b.TryAdd(typeof(char), DbType.StringFixedLength);
        b.TryAdd(typeof(char?), DbType.StringFixedLength);
        b.TryAdd(typeof(char[]), DbType.StringFixedLength);

        b.TryAdd(typeof(int), DbType.Int32);
        b.TryAdd(typeof(int?), DbType.Int32);
        b.TryAdd(typeof(uint), DbType.UInt32);
        b.TryAdd(typeof(uint?), DbType.UInt32);

        b.TryAdd(typeof(long), DbType.Int64);
        b.TryAdd(typeof(long?), DbType.Int64);
        b.TryAdd(typeof(ulong), DbType.UInt64);
        b.TryAdd(typeof(ulong?), DbType.UInt64);

        b.TryAdd(typeof(float), DbType.Single);
        b.TryAdd(typeof(float?), DbType.Single);
        b.TryAdd(typeof(double), DbType.Double);
        b.TryAdd(typeof(double?), DbType.Double);
        b.TryAdd(typeof(decimal), DbType.Decimal);
        b.TryAdd(typeof(decimal?), DbType.Decimal);

        b.TryAdd(typeof(byte[]), DbType.Binary);

        b.TryAdd(typeof(Guid), DbType.Guid);
        b.TryAdd(typeof(Guid?), DbType.Guid);

        b.TryAdd(typeof(string), DbType.String);

        b.TryAdd(typeof(IPAddress), DbType.String);
        b.TryAdd(typeof(Uri), DbType.String);

        b.TryAdd(typeof(BigInteger), DbType.Decimal);
        b.TryAdd(typeof(BigInteger?), DbType.Decimal);

        b.TryAdd(typeof(DateTime), DbType.DateTime);
        b.TryAdd(typeof(DateTime?), DbType.DateTime);
        b.TryAdd(typeof(DateTimeOffset), DbType.DateTimeOffset);
        b.TryAdd(typeof(DateTimeOffset?), DbType.DateTimeOffset);

        b.TryAdd(typeof(object), DbType.Object);

        return b.ToImmutable();
    }


    /// <summary>
    /// Map of DbType to DotNet types
    /// </summary>
    public static readonly ImmutableDictionary<DbType, Type> DbType_Type = DbType_Type_Create();

    private static ImmutableDictionary<DbType, Type> DbType_Type_Create()
    {
        var b = ImmutableDictionary.CreateBuilder<DbType, Type>();

        b.TryAdd(DbType.AnsiString, typeof(string));
        b.TryAdd(DbType.AnsiStringFixedLength, typeof(char[]));
        b.TryAdd(DbType.Binary, typeof(byte[]));
        b.TryAdd(DbType.Boolean, typeof(bool));
        b.TryAdd(DbType.Byte, typeof(byte));
        b.TryAdd(DbType.Currency, typeof(decimal));
        b.TryAdd(DbType.Date, typeof(DateTime));
        b.TryAdd(DbType.DateTime, typeof(DateTime));
        b.TryAdd(DbType.DateTime2, typeof(DateTime));
        b.TryAdd(DbType.DateTimeOffset, typeof(DateTimeOffset));
        b.TryAdd(DbType.Decimal, typeof(decimal));
        b.TryAdd(DbType.Double, typeof(double));
        b.TryAdd(DbType.Guid, typeof(Guid));
        b.TryAdd(DbType.Int16, typeof(short));
        b.TryAdd(DbType.Int32, typeof(int));
        b.TryAdd(DbType.Int64, typeof(long));
        b.TryAdd(DbType.Object, typeof(object));
        b.TryAdd(DbType.SByte, typeof(sbyte));
        b.TryAdd(DbType.Single, typeof(float));
        b.TryAdd(DbType.String, typeof(string));
        b.TryAdd(DbType.StringFixedLength, typeof(char[]));
        b.TryAdd(DbType.Time, typeof(DateTime));
        b.TryAdd(DbType.UInt16, typeof(ushort));
        b.TryAdd(DbType.UInt32, typeof(uint));
        b.TryAdd(DbType.UInt64, typeof(ulong));
        b.TryAdd(DbType.VarNumeric, typeof(decimal));
        b.TryAdd(DbType.Xml, typeof(string));

        return b.ToImmutable();
    }

    public static readonly ImmutableHashSet<DbType> DbTypes_Numeric = ImmutableHashSet.Create(
        DbType.Byte,
        DbType.Currency,
        DbType.Decimal,
        DbType.Double,
        DbType.Int16,
        DbType.Int32,
        DbType.Int64,
        DbType.SByte,
        DbType.Single,
        DbType.UInt16,
        DbType.UInt32,
        DbType.UInt64,
        DbType.VarNumeric
    );


    public static readonly ImmutableHashSet<DbType> DbTypes_String = ImmutableHashSet.Create(
        DbType.AnsiString,
        DbType.AnsiStringFixedLength,
        DbType.String,
        DbType.StringFixedLength,
        DbType.Xml
    );

    public static readonly ImmutableHashSet<DbType> DbTypes_DateTime = ImmutableHashSet.Create(
        DbType.Date,
        DbType.DateTime,
        DbType.DateTime2,
        DbType.DateTimeOffset,
        DbType.Time
    );
}
