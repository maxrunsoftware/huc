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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

[assembly: System.Runtime.InteropServices.Guid(MaxRunSoftware.Utilities.Constant.ID)]

namespace MaxRunSoftware.Utilities
{
    public static class Constant
    {
        public const string ID = "461985d6-d681-4a0f-b110-547f3beaf967";

        private const string BOOL_TRUE = "1 T TRUE Y YES";
        private const string BOOL_FALSE = "0 F FALSE N NO";

        #region CHARS

        /// <summary>
        /// Characters A-Z
        /// </summary>
        public const string CHARS_A_Z_UPPER = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        /// <summary>
        /// Characters a-z
        /// </summary>
        public const string CHARS_A_Z_LOWER = "abcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// Numbers 0-9
        /// </summary>
        public const string CHARS_0_9 = "0123456789";

        /// <summary>
        /// First 128 characters
        /// </summary>
        public const string CHARS_00000_00128 =
                "\u0000" + "\u0001" + "\u0002" + "\u0003" + "\u0004" + "\u0005" + "\u0006" + "\u0007" + "\u0008" + "\u0009" + "\u000A" + "\u000B" + "\u000C" + "\u000D" + "\u000E" + "\u000F" +
                "\u0010" + "\u0011" + "\u0012" + "\u0013" + "\u0014" + "\u0015" + "\u0016" + "\u0017" + "\u0018" + "\u0019" + "\u001A" + "\u001B" + "\u001C" + "\u001D" + "\u001E" + "\u001F" +
                "\u0020" + "\u0021" + "\u0022" + "\u0023" + "\u0024" + "\u0025" + "\u0026" + "\u0027" + "\u0028" + "\u0029" + "\u002A" + "\u002B" + "\u002C" + "\u002D" + "\u002E" + "\u002F" +
                "\u0030" + "\u0031" + "\u0032" + "\u0033" + "\u0034" + "\u0035" + "\u0036" + "\u0037" + "\u0038" + "\u0039" + "\u003A" + "\u003B" + "\u003C" + "\u003D" + "\u003E" + "\u003F" +
                "\u0040" + "\u0041" + "\u0042" + "\u0043" + "\u0044" + "\u0045" + "\u0046" + "\u0047" + "\u0048" + "\u0049" + "\u004A" + "\u004B" + "\u004C" + "\u004D" + "\u004E" + "\u004F" +
                "\u0050" + "\u0051" + "\u0052" + "\u0053" + "\u0054" + "\u0055" + "\u0056" + "\u0057" + "\u0058" + "\u0059" + "\u005A" + "\u005B" + "\u005C" + "\u005D" + "\u005E" + "\u005F" +
                "\u0060" + "\u0061" + "\u0062" + "\u0063" + "\u0064" + "\u0065" + "\u0066" + "\u0067" + "\u0068" + "\u0069" + "\u006A" + "\u006B" + "\u006C" + "\u006D" + "\u006E" + "\u006F" +
                "\u0070" + "\u0071" + "\u0072" + "\u0073" + "\u0074" + "\u0075" + "\u0076" + "\u0077" + "\u0078" + "\u0079" + "\u007A" + "\u007B" + "\u007C" + "\u007D" + "\u007E" + "\u007F";

        public static readonly ImmutableArray<char> CHARS_A_Z_UPPER_ARRAY = ImmutableArray.Create(CHARS_A_Z_UPPER.ToCharArray());
        public static readonly ImmutableArray<char> CHARS_A_Z_LOWER_ARRAY = ImmutableArray.Create(CHARS_A_Z_LOWER.ToCharArray());
        public static readonly ImmutableArray<char> CHARS_0_9_ARRAY = ImmutableArray.Create(CHARS_0_9.ToCharArray());
        public static readonly ImmutableArray<char> CHARS_00000_00128_ARRAY = ImmutableArray.Create(CHARS_00000_00128.ToCharArray());

        #endregion CHARS

        public const int BUFFER_SIZE_MINIMUM = 1024 * 4;

        /// <summary>
        /// We pick a value that is the largest multiple of 4096 that is still smaller than the
        /// large object heap threshold (85K). The CopyTo/CopyToAsync buffer is short-lived and is
        /// likely to be collected at Gen0, and it offers a significant improvement in Copy performance.
        /// </summary>
        public const int BUFFER_SIZE_OPTIMAL = 81920;

        public const string NEWLINE_WINDOWS = "\r\n";
        public const string NEWLINE_UNIX = "\n";
        public const string NEWLINE_MAC = "\r";

        public const long BYTES_BYTE = 1L;
        public const long BYTES_KILO = 1000L;
        public const long BYTES_KIBI = 1024L;
        public const long BYTES_MEGA = 1000000L;
        public const long BYTES_MEBI = 1048576L;
        public const long BYTES_GIGA = 1000000000L;
        public const long BYTES_GIBI = 1073741824L;
        public const long BYTES_TERA = 1000000000000L;
        public const long BYTES_TEBI = 1099511627776L;
        public const long BYTES_PETA = 1000000000000000L;
        public const long BYTES_PEBI = 1125899906842624L;
        public const long BYTES_EXA = 1000000000000000000L;
        public const long BYTES_EXBI = 1152921504606846976L;

        public const bool ZERO_BOOL = false;
        public const byte ZERO_BYTE = byte.MinValue;
        public const sbyte ZERO_SBYTE = 0;
        public const decimal ZERO_DECIMAL = decimal.Zero;
        public const double ZERO_DOUBLE = 0;
        public const float ZERO_FLOAT = 0;
        public const int ZERO_INT = 0;
        public const uint ZERO_UINT = uint.MinValue;
        public const long ZERO_LONG = 0;
        public const ulong ZERO_ULONG = ulong.MinValue;
        public const short ZERO_SHORT = 0;
        public const ushort ZERO_USHORT = ushort.MinValue;

        /// <summary>
        /// List of String Comparisons from most restrictive to least
        /// </summary>
        public static readonly IReadOnlyList<StringComparison> LIST_StringComparison = new List<StringComparison> {
                StringComparison.Ordinal,
                StringComparison.CurrentCulture,
                StringComparison.InvariantCulture,
                StringComparison.OrdinalIgnoreCase,
                StringComparison.CurrentCultureIgnoreCase,
                StringComparison.InvariantCultureIgnoreCase
            }.AsReadOnly();

        /// <summary>
        /// List of String Comparisons from most restrictive to least
        /// </summary>
        public static readonly IReadOnlyList<StringComparer> LIST_StringComparer = new List<StringComparer> {
                StringComparer.Ordinal,
                StringComparer.CurrentCulture,
                StringComparer.InvariantCulture,
                StringComparer.OrdinalIgnoreCase,
                StringComparer.CurrentCultureIgnoreCase,
                StringComparer.InvariantCultureIgnoreCase
            }.AsReadOnly();

        /// <summary>
        /// Regex for validating an IPv4 address
        /// </summary>
        public static readonly Regex REGEX_IPV4 = new Regex(@"((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));

        /// <summary>
        /// Regex for validating an email address
        /// </summary>
        public static readonly Regex REGEX_EMAIL = new Regex(@"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(2));

        /// <summary>
        /// UTF8 encoding WITHOUT the Byte Order Marker
        /// </summary>
        public static readonly Encoding ENCODING_UTF8_WITHOUT_BOM = new UTF8Encoding(false); // Threadsafe according to https://stackoverflow.com/a/3024405

        /// <summary>
        /// UTF8 encoding WITH the Byte Order Marker
        /// </summary>
        public static readonly Encoding ENCODING_UTF8_WITH_BOM = new UTF8Encoding(true); // Threadsafe according to https://stackoverflow.com/a/3024405

        /// <summary>
        /// Are we running on a Windows platform?
        /// </summary>
        public static readonly bool OS_WINDOWS = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);

        /// <summary>
        /// Are we running on a UNIX/LINUX platform?
        /// </summary>
        public static readonly bool OS_UNIX = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);

        /// <summary>
        /// Are we running on a Mac/Apple platform?
        /// </summary>
        public static readonly bool OS_MAC = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);

        /// <summary>
        /// Map of StringComparer to StringComparison
        /// </summary>
        public static readonly IReadOnlyDictionary<StringComparer, StringComparison> MAP_StringComparer_StringComparison = MAP_StringComparer_StringComparison_Create();
        private static IReadOnlyDictionary<StringComparer, StringComparison> MAP_StringComparer_StringComparison_Create()
        {
            var d = new Dictionary<StringComparer, StringComparison>();
            d.TryAdd(StringComparer.CurrentCulture, StringComparison.CurrentCulture);
            d.TryAdd(StringComparer.CurrentCultureIgnoreCase, StringComparison.CurrentCultureIgnoreCase);
            d.TryAdd(StringComparer.InvariantCulture, StringComparison.InvariantCulture);
            d.TryAdd(StringComparer.InvariantCultureIgnoreCase, StringComparison.InvariantCultureIgnoreCase);
            d.TryAdd(StringComparer.Ordinal, StringComparison.Ordinal);
            d.TryAdd(StringComparer.OrdinalIgnoreCase, StringComparison.OrdinalIgnoreCase);
            return d;
        }

        /// <summary>
        /// Map of StringComparison to StringComparer
        /// </summary>
        public static readonly IReadOnlyDictionary<StringComparison, StringComparer> MAP_StringComparison_StringComparer = MAP_StringComparison_StringComparer_Create();
        private static IReadOnlyDictionary<StringComparison, StringComparer> MAP_StringComparison_StringComparer_Create()
        {
            var d = new Dictionary<StringComparison, StringComparer>();
            d.TryAdd(StringComparison.CurrentCulture, StringComparer.CurrentCulture);
            d.TryAdd(StringComparison.CurrentCultureIgnoreCase, StringComparer.CurrentCultureIgnoreCase);
            d.TryAdd(StringComparison.InvariantCulture, StringComparer.InvariantCulture);
            d.TryAdd(StringComparison.InvariantCultureIgnoreCase, StringComparer.InvariantCultureIgnoreCase);
            d.TryAdd(StringComparison.Ordinal, StringComparer.Ordinal);
            d.TryAdd(StringComparison.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase);
            return d;
        }

        /// <summary>
        /// Map of DotNet types to DbType
        /// </summary>
        public static readonly IReadOnlyDictionary<Type, DbType> MAP_Type_DbType = new Dictionary<Type, DbType>
            {
                { typeof(bool), DbType.Boolean },
                { typeof(bool?), DbType.Boolean },

                { typeof(byte), DbType.Byte },
                { typeof(byte?), DbType.Byte },
                { typeof(sbyte), DbType.SByte },
                { typeof(sbyte?), DbType.SByte },

                { typeof(short), DbType.Int16 },
                { typeof(short?), DbType.Int16 },
                { typeof(ushort), DbType.UInt16 },
                { typeof(ushort?), DbType.UInt16 },

                { typeof(char), DbType.StringFixedLength },
                { typeof(char?), DbType.StringFixedLength },
                { typeof(char[]), DbType.StringFixedLength },

                { typeof(int), DbType.Int32 },
                { typeof(int?), DbType.Int32 },
                { typeof(uint), DbType.UInt32 },
                { typeof(uint?), DbType.UInt32 },

                { typeof(long), DbType.Int64 },
                { typeof(long?), DbType.Int64 },
                { typeof(ulong), DbType.UInt64 },
                { typeof(ulong?), DbType.UInt64 },

                { typeof(float), DbType.Single },
                { typeof(float?), DbType.Single },
                { typeof(double), DbType.Double },
                { typeof(double?), DbType.Double },
                { typeof(decimal), DbType.Decimal },
                { typeof(decimal?), DbType.Decimal },

                { typeof(byte[]), DbType.Binary },

                { typeof(Guid), DbType.Guid },
                { typeof(Guid?), DbType.Guid },

                { typeof(string), DbType.String },

                { typeof(System.Net.IPAddress), DbType.String },
                { typeof(Uri), DbType.String },

                { typeof(System.Numerics.BigInteger), DbType.VarNumeric },
                { typeof(System.Numerics.BigInteger?), DbType.VarNumeric },

                { typeof(DateTime), DbType.DateTime },
                { typeof(DateTime?), DbType.DateTime },
                { typeof(DateTimeOffset), DbType.DateTimeOffset },
                { typeof(DateTimeOffset?), DbType.DateTimeOffset },

                { typeof(object), DbType.Object },
            };

        /// <summary>
        /// Map of DbType to DotNet types
        /// </summary>
        public static readonly IReadOnlyDictionary<DbType, Type> MAP_DbType_Type = new Dictionary<DbType, Type> {
                { DbType.AnsiString, typeof(string) },
                { DbType.AnsiStringFixedLength, typeof(string) },
                { DbType.Binary, typeof(byte[]) },
                { DbType.Boolean, typeof(bool) },
                { DbType.Byte, typeof(byte) },
                { DbType.Currency, typeof(decimal) },
                { DbType.Date, typeof(DateTime) },
                { DbType.DateTime, typeof(DateTime) },
                { DbType.DateTime2, typeof(DateTime) },
                { DbType.DateTimeOffset, typeof(DateTimeOffset) },
                { DbType.Decimal, typeof(decimal) },
                { DbType.Double, typeof(double) },
                { DbType.Guid, typeof(Guid) },
                { DbType.Int16, typeof(short) },
                { DbType.Int32, typeof(int) },
                { DbType.Int64, typeof(long) },
                { DbType.Object, typeof(object) },
                { DbType.SByte, typeof(sbyte) },
                { DbType.Single, typeof(float) },
                { DbType.String, typeof(string) },
                { DbType.StringFixedLength, typeof(char[]) },
                { DbType.Time, typeof(DateTime) },
                { DbType.UInt16, typeof(ushort) },
                { DbType.UInt32, typeof(uint) },
                { DbType.UInt64, typeof(ulong) },
                { DbType.VarNumeric, typeof(decimal) },
                { DbType.Xml, typeof(string) },
        };

        /// <summary>
        /// Case-Sensitive map of Color names to Colors
        /// </summary>
        public static IReadOnlyDictionary<string, Color> COLORS = COLORS_get();
        private static IReadOnlyDictionary<string, Color> COLORS_get()
        {
            // https://stackoverflow.com/a/3821197

            var d = new Dictionary<string, Color>();
            Type colorType = typeof(System.Drawing.Color);
            // We take only static property to avoid properties like Name, IsSystemColor ...
            PropertyInfo[] propInfos = colorType.GetProperties(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public);
            foreach (PropertyInfo propInfo in propInfos)
            {
                var colorGetMethod = propInfo.GetGetMethod();
                if (colorGetMethod == null) continue;
                var colorObject = colorGetMethod.Invoke(null, null);
                if (colorObject == null) continue;
                var colorObjectType = colorObject.GetType();
                if (!colorObjectType.Equals(typeof(Color))) continue;
                var color = (Color)colorObject;
                var colorName = propInfo.Name;
                d[colorName] = color;
            }
            return d.AsReadOnly();
        }

        /// <summary>
        /// Case-Insensitive hashset of boolean true values 
        /// </summary>
        public static readonly IReadOnlyCollection<string> SET_Bool_True = new HashSet<string>(BOOL_TRUE.Split(' '), StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Case-Insensitive hashset of boolean false values
        /// </summary>
        public static readonly IReadOnlyCollection<string> SET_Bool_False = new HashSet<string>(BOOL_FALSE.Split(' '), StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Case-Insensitive map of boolean string values to boolean values
        /// </summary>
        public static readonly IReadOnlyDictionary<string, bool> MAP_String_Bool = MAP_String_Bool_get();

        private static IReadOnlyDictionary<string, bool> MAP_String_Bool_get()
        {
            var d = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in BOOL_TRUE.Split(' ')) d.Add(item, true);
            foreach (var item in BOOL_FALSE.Split(' ')) d.Add(item, false);
            return (new Dictionary<string, bool>(d, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// The current EXE file name. Could be a full file path, or a partial file path, or null
        /// </summary>
        public static readonly string CURRENT_EXE = CURRENT_EXE_get();

        private static string CURRENT_EXE_get()
        {
            // https://stackoverflow.com/questions/616584/how-do-i-get-the-name-of-the-current-executable-in-c
            string name = null;
            try
            {
                name = Process.GetCurrentProcess()?.MainModule?.FileName;
                if (name != null)
                {
                    name = name.Trim();
                    if (name.Length == 0) name = null;
                }
            }
            catch (Exception) { }
            if (name != null) return name;

            try
            {
                name = AppDomain.CurrentDomain?.FriendlyName;
                if (name != null)
                {
                    name = name.Trim();
                    if (name.Length == 0) name = null;
                }
            }
            catch (Exception) { }
            if (name != null) return name;

            try
            {
                name = Process.GetCurrentProcess()?.ProcessName;
                if (name != null)
                {
                    name = name.Trim();
                    if (name.Length == 0) name = null;
                }
            }
            catch (Exception) { }
            if (name != null) return name;

            return null;
        }

        /// <summary>
        /// Are we executing via a batchfile or script or running the command directly from the console window?
        /// </summary>
        public static readonly bool IS_BATCHFILE_EXECUTED = IS_BATCHFILE_EXECUTED_get();
        private static bool IS_BATCHFILE_EXECUTED_get()
        {
            try
            {
                // http://stackoverflow.com/questions/3453220/how-to-detect-if-console-in-stdin-has-been-redirected?lq=1
                //return (0 == (System.Console.WindowHeight + System.Console.WindowWidth)) && System.Console.KeyAvailable;
                if (System.Console.WindowHeight != 0) return false;
                if (System.Console.WindowWidth != 0) return false;
                if (!System.Console.KeyAvailable) return false;
                return true;
            }
            catch (Exception e)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("Constant.IS_BATCHFILE_EXECUTED_get() failed. " + e?.ToString());
                }
                catch (Exception)
                {
                    try
                    {
                        Console.Error.WriteLine("Constant.IS_BATCHFILE_EXECUTED_get() failed. " + e?.ToString());
                    }
                    catch (Exception)
                    {
                        try
                        {
                            Console.WriteLine("Constant.IS_BATCHFILE_EXECUTED_get() failed. " + e?.ToString());
                        }
                        catch (Exception)
                        {
                            ; // Just swallow it I guess
                        }
                    }
                }
            }
            return false;
        }
    }
}
