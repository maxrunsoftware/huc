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

using System.Data;

namespace MaxRunSoftware.Utilities
{
    /// <summary>
    /// <see href="https://dev.mysql.com/doc/dev/connector-net/6.10/html/T_MySql_Data_MySqlClient_MySqlDbType.htm">MySql.Data.MySqlClient.MySqlDbType</see>
    /// <see href="https://dev.mysql.com/doc/refman/8.0/en/data-types.html">Data Types</see>
    /// </summary>
    public enum SqlMySqlType
    {
        /// <summary>
        /// A fixed precision and scale numeric value between -1038 -1 and 10 38 -1.
        /// </summary>
        [SqlType(DbType.Decimal, SqlTypeNames = nameof(Decimal) + ",DEC,NUMERIC,FIXED")]
        Decimal = 0,

        /// <summary>
        /// The signed range is -128 to 127. The unsigned range is 0 to 255.
        /// </summary>
        [SqlType(DbType.SByte, SqlTypeNames = "TINYINT")]
        Byte = 1,

        /// <summary>
        /// A 16-bit signed integer. The signed range is -32768 to 32767. The unsigned range is 0 to 65535
        /// </summary>
        [SqlType(DbType.Int16, SqlTypeNames = "SMALLINT")]
        Int16 = 2,

        /// <summary>
        /// Specifies a 24 (3 byte) signed or unsigned value.
        /// </summary>
        [SqlType(DbType.Int32, SqlTypeNames = "MEDIUMINT")]
        Int24 = 9,

        /// <summary>
        /// A 32-bit signed integer
        /// </summary>
        [SqlType(DbType.Int32, SqlTypeNames = "INT,INTEGER")]
        Int32 = 3,

        /// <summary>
        /// A 64-bit signed integer.
        /// </summary>
        [SqlType(DbType.Int64, SqlTypeNames = "BIGINT")]
        Int64 = 8,

        /// <summary>
        /// A small (single-precision) floating-point number. Allowable values are -3.402823466E+38 to -1.175494351E-38, 0, and 1.175494351E-38 to 3.402823466E+38.
        /// </summary>
        [SqlType(DbType.Single)]
        Float = 4,

        /// <summary>
        /// A normal-size (double-precision) floating-point number. Allowable values are -1.7976931348623157E+308 to -2.2250738585072014E-308, 0, and 2.2250738585072014E-308 to 1.7976931348623157E+308.
        /// </summary>
        [SqlType(DbType.Double, SqlTypeNames = nameof(Double) + ",DOUBLE PRECISION")]
        Double = 5,

        /// <summary>
        /// A timestamp. The range is '1970-01-01 00:00:00' to sometime in the year 2037
        /// </summary>
        [SqlType(DbType.DateTime)]
        Timestamp = 7,

        /// <summary>
        /// Date The supported range is '1000-01-01' to '9999-12-31'.
        /// </summary>
        [SqlType(DbType.Date)]
        Date = 10,

        /// <summary>
        /// The range is '-838:59:59' to '838:59:59'.
        /// </summary>
        [SqlType(DbType.Time)]
        Time = 11,

        /// <summary>
        /// The supported range is '1000-01-01 00:00:00' to '9999-12-31 23:59:59'.
        /// </summary>
        [SqlType(DbType.DateTime)]
        DateTime = 12,

        /// <summary>
        /// A year in 2- or 4-digit format (default is 4-digit). The allowable values are 1901 to 2155, 0000 in the 4-digit year format, and 1970-2069 if you use the 2-digit format (70-69).
        /// </summary>
        [SqlType(DbType.UInt16)]
        Year = 13,

        /// <summary>
        /// Obsolete Use Datetime or Date type
        /// </summary>
        [SqlType(DbType.DateTime)]
        Newdate = 14,

        /// <summary>
        /// A variable-length string containing 0 to 65535 characters
        /// </summary>
        [SqlType(DbType.String)]
        VarString = 15,

        /// <summary>
        /// Bit-field data type
        /// </summary>
        [SqlType(DbType.Byte)]
        Bit = 16,

        /// <summary>
        /// JSON
        /// </summary>
        [SqlType(DbType.String)]
        JSON = 245,

        /// <summary>
        /// New Decimal
        /// </summary>
        [SqlType(DbType.Decimal)]
        NewDecimal = 246,

        /// <summary>
        /// An enumeration. A string object that can have only one value, chosen from the list of values 'value1', 'value2', ..., NULL or the special "" error value. An ENUM can have a maximum of 65535 distinct values
        /// </summary>
        [SqlType(DbType.String)]
        Enum = 247,

        /// <summary>
        /// A set. A string object that can have zero or more values, each of which must be chosen from the list of values 'value1', 'value2', ... A SET can have a maximum of 64 members.
        /// </summary>
        [SqlType(DbType.String)]
        Set = 248,

        /// <summary>
        /// A binary column with a maximum length of 255 (2^8 - 1) characters
        /// </summary>
        [SqlType(DbType.Binary)]
        TinyBlob = 249,

        /// <summary>
        /// A binary column with a maximum length of 16777215 (2^24 - 1) bytes.
        /// </summary>
        [SqlType(DbType.Binary)]
        MediumBlob = 250,

        /// <summary>
        /// A binary column with a maximum length of 4294967295 or 4G (2^32 - 1) bytes.
        /// </summary>
        [SqlType(DbType.Binary)]
        LongBlob = 251,

        /// <summary>
        /// A binary column with a maximum length of 65535 (2^16 - 1) bytes.
        /// </summary>
        [SqlType(DbType.Binary)]
        Blob = 252,

        /// <summary>
        /// A variable-length string containing 0 to 255 bytes.
        /// </summary>
        [SqlType(DbType.String)]
        VarChar = 253,

        /// <summary>
        /// A fixed-length string.
        /// </summary>
        [SqlType(DbType.StringFixedLength)]
        String = 254,

        /// <summary>
        /// Geometric (GIS) data type.
        /// </summary>
        [SqlType(DbType.String)]
        Geometry = 255,

        /// <summary>
        /// Unsigned 8-bit value.
        /// </summary>
        [SqlType(DbType.Byte, SqlTypeNames = "TINYINT UNSIGNED")]
        UByte = 501,

        /// <summary>
        /// Unsigned 16-bit value.
        /// </summary>
        [SqlType(DbType.UInt16, SqlTypeNames = "SMALLINT UNSIGNED")]
        UInt16 = 502,

        /// <summary>
        /// Unsigned 24-bit value.
        /// </summary>
        [SqlType(DbType.UInt32, SqlTypeNames = "MEDIUMINT UNSIGNED")]
        UInt24 = 509,

        /// <summary>
        /// Unsigned 32-bit value.
        /// </summary>
        [SqlType(DbType.UInt32, SqlTypeNames = "INT UNSIGNED,INTEGER UNSIGNED")]
        UInt32 = 503,

        /// <summary>
        /// Unsigned 64-bit value.
        /// </summary>
        [SqlType(DbType.UInt64, SqlTypeNames = "BIGINT UNSIGNED")]
        UInt64 = 508,

        /// <summary>
        /// Fixed length binary string.
        /// </summary>
        [SqlType(DbType.Binary)]
        Binary = 754,

        /// <summary>
        /// Variable length binary string.
        /// </summary>
        [SqlType(DbType.Binary)]
        VarBinary = 753,

        /// <summary>
        /// A text column with a maximum length of 255 (2^8 - 1) characters.
        /// </summary>
        [SqlType(DbType.String)]
        TinyText = 749,

        /// <summary>
        /// A text column with a maximum length of 16777215 (2^24 - 1) characters.
        /// </summary>
        [SqlType(DbType.String)]
        MediumText = 750,

        /// <summary>
        /// A text column with a maximum length of 4294967295 or 4G (2^32 - 1) characters.
        /// </summary>
        [SqlType(DbType.String)]
        LongText = 751,

        /// <summary>
        /// A text column with a maximum length of 65535 (2^16 - 1) characters.
        /// </summary>
        [SqlType(DbType.String)]
        Text = 752,

        /// <summary>
        /// A guid column.
        /// </summary>
        [SqlType(DbType.Guid)]
        Guid = 854,

    }

    public static class SqlMySqlTypeExtensions
    {
        public static SqlMySqlType ToSqlMySqlType(this DbType dbType) => dbType switch
        {
            DbType.AnsiString => SqlMySqlType.LongText,
            DbType.Binary => SqlMySqlType.LongBlob,
            DbType.Byte => SqlMySqlType.UByte, // TODO: Check on this
            DbType.Boolean => SqlMySqlType.Bit,
            DbType.Currency => SqlMySqlType.Decimal,
            DbType.Date => SqlMySqlType.Date,
            DbType.DateTime => SqlMySqlType.DateTime,
            DbType.Decimal => SqlMySqlType.Decimal,
            DbType.Double => SqlMySqlType.Double,
            DbType.Guid => SqlMySqlType.Guid,
            DbType.Int16 => SqlMySqlType.Int16,
            DbType.Int32 => SqlMySqlType.Int32,
            DbType.Int64 => SqlMySqlType.Int64,
            DbType.Object => SqlMySqlType.LongText,
            DbType.SByte => SqlMySqlType.Byte, // TODO: Check on this
            DbType.Single => SqlMySqlType.Float,
            DbType.String => SqlMySqlType.LongText,
            DbType.Time => SqlMySqlType.Time,
            DbType.UInt16 => SqlMySqlType.UInt16,
            DbType.UInt32 => SqlMySqlType.UInt32,
            DbType.UInt64 => SqlMySqlType.UInt64,
            DbType.VarNumeric => SqlMySqlType.Decimal,
            DbType.AnsiStringFixedLength => SqlMySqlType.LongText,
            DbType.StringFixedLength => SqlMySqlType.LongText,
            DbType.Xml => SqlMySqlType.LongText,
            DbType.DateTime2 => SqlMySqlType.DateTime,
            DbType.DateTimeOffset => SqlMySqlType.DateTime,
            _ => throw new System.NotImplementedException(dbType.GetType().FullNameFormatted() + "." + dbType.ToString()),
        };
    }

}
