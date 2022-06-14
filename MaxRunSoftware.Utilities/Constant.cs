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

using System.Collections.Immutable;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using MaxRunSoftware.Utilities;

[assembly: Guid(Constant.ID)]

namespace MaxRunSoftware.Utilities;

public static class Constant
{
    public const string ID = "461985d6-d681-4a0f-b110-547f3beaf967";

    #region Chars

    /// <summary>
    /// Characters A-Z
    /// </summary>
    public const string CHARS_A_Z_UPPER = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static readonly ImmutableArray<char> CHARS_A_Z_UPPER_ARRAY = ImmutableArray.Create(CHARS_A_Z_UPPER.ToCharArray());

    /// <summary>
    /// Characters a-z
    /// </summary>
    public const string CHARS_A_Z_LOWER = "abcdefghijklmnopqrstuvwxyz";

    public static readonly ImmutableArray<char> CHARS_A_Z_LOWER_ARRAY = ImmutableArray.Create(CHARS_A_Z_LOWER.ToCharArray());

    /// <summary>
    /// Numbers 0-9
    /// </summary>
    public const string CHARS_0_9 = "0123456789";

    public static readonly ImmutableArray<char> CHARS_0_9_ARRAY = ImmutableArray.Create(CHARS_0_9.ToCharArray());

    /// <summary>
    /// A-Z a-z 0-9
    /// </summary>
    public const string CHARS_ALPHANUMERIC = CHARS_A_Z_UPPER + CHARS_A_Z_LOWER + CHARS_0_9;

    public static readonly ImmutableArray<char> CHARS_ALPHANUMERIC_ARRAY = ImmutableArray.Create(CHARS_ALPHANUMERIC.ToCharArray());

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

    public static readonly ImmutableArray<char> CHARS_00000_00128_ARRAY = ImmutableArray.Create(CHARS_00000_00128.ToCharArray());

    #endregion Chars

    #region NewLine

    public static readonly string NEWLINE = Environment.NewLine;
    public const string NEWLINE_WINDOWS = "\r\n";
    public const string NEWLINE_UNIX = "\n";
    public const string NEWLINE_MAC = "\r";

    #endregion NewLine

    #region Bytes

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

    #endregion Bytes

    #region Zero

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

    #endregion Zero

    #region Regex

    public static readonly string REGEX_IPV4_STRING = @"((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)";

    /// <summary>
    /// Regex for validating an IPv4 address
    /// </summary>
    public static readonly Regex REGEX_IPV4 = new(REGEX_IPV4_STRING, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));

    public static readonly string REGEX_EMAIL_STRING = @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$";

    /// <summary>
    /// Regex for validating an email address
    /// </summary>
    public static readonly Regex REGEX_EMAIL = new(REGEX_EMAIL_STRING, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(2));

    #endregion Regex

    #region Encoding

    /// <summary>
    /// UTF8 encoding WITHOUT the Byte Order Marker
    /// </summary>
    public static readonly Encoding ENCODING_UTF8 = new UTF8Encoding(false); // Thread safe according to https://stackoverflow.com/a/3024405

    /// <summary>
    /// UTF8 encoding WITH the Byte Order Marker
    /// </summary>
    public static readonly Encoding ENCODING_UTF8_BOM = new UTF8Encoding(true); // Thread safe according to https://stackoverflow.com/a/3024405

    #endregion Encoding

    #region OS

    /// <summary>
    /// Operating System are we currently running
    /// </summary>
    public static readonly OSPlatform OS = OS_get();

    private static OSPlatform OS_get()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OSPlatform.Windows;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OSPlatform.OSX;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OSPlatform.Linux;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            {
                return OSPlatform.FreeBSD;
            }
        }
        catch (Exception e)
        {
            LogError(e);
        }

        // Unknown OS
        return OSPlatform.Windows;
    }

    /// <summary>
    /// Are we running on a Windows platform?
    /// </summary>
    public static readonly bool OS_WINDOWS = OS == OSPlatform.Windows;

    /// <summary>
    /// Are we running on a UNIX/LINUX platform?
    /// </summary>
    public static readonly bool OS_UNIX = OS == OSPlatform.Linux || OS == OSPlatform.FreeBSD;

    /// <summary>
    /// Are we running on a Mac/Apple platform?
    /// </summary>
    public static readonly bool OS_MAC = OS == OSPlatform.OSX;

    /// <summary>
    /// Are we running on a 32-bit operating system?
    /// </summary>
    public static readonly bool OS_X32 = !Environment.Is64BitOperatingSystem;

    /// <summary>
    /// Are we running on a 64-bit operating system?
    /// </summary>
    public static readonly bool OS_X64 = Environment.Is64BitOperatingSystem;

    #endregion OS

    #region StringComparer

    /// <summary>
    /// List of String Comparisons from most restrictive to least
    /// </summary>
    public static readonly IReadOnlyList<StringComparer> LIST_StringComparer = new List<StringComparer>
    {
        StringComparer.Ordinal,
        StringComparer.CurrentCulture,
        StringComparer.InvariantCulture,
        StringComparer.OrdinalIgnoreCase,
        StringComparer.CurrentCultureIgnoreCase,
        StringComparer.InvariantCultureIgnoreCase
    }.AsReadOnly();

    /// <summary>
    /// Map of StringComparer to StringComparison
    /// </summary>
    public static readonly IReadOnlyDictionary<StringComparer, StringComparison> StringComparer_StringComparison = MAP_StringComparer_StringComparison_Create();

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

    #endregion StringComparer

    #region StringComparison

    /// <summary>
    /// List of String Comparisons from most restrictive to least
    /// </summary>
    public static readonly IReadOnlyList<StringComparison> LIST_StringComparison = new List<StringComparison>
    {
        StringComparison.Ordinal,
        StringComparison.CurrentCulture,
        StringComparison.InvariantCulture,
        StringComparison.OrdinalIgnoreCase,
        StringComparison.CurrentCultureIgnoreCase,
        StringComparison.InvariantCultureIgnoreCase
    }.AsReadOnly();

    /// <summary>
    /// Map of StringComparison to StringComparer
    /// </summary>
    public static readonly IReadOnlyDictionary<StringComparison, StringComparer> StringComparison_StringComparer = MAP_StringComparison_StringComparer_Create();

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

    #endregion StringComparison

    #region Type

    public static readonly IReadOnlySet<Type> TYPES_BASE_NUMERIC = new HashSet<Type>
    {
        typeof(sbyte), typeof(byte),
        typeof(short), typeof(ushort),
        typeof(int), typeof(uint),
        typeof(long), typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(decimal)
    };

    public static readonly IReadOnlySet<Type> TYPES_BASE_DECIMAL = new HashSet<Type>
    {
        typeof(float),
        typeof(double),
        typeof(decimal)
    };

    /// <summary>
    /// Map of DotNet types to DbType
    /// </summary>
    public static readonly IReadOnlyDictionary<Type, DbType> Type_DbType = new Dictionary<Type, DbType>
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

        { typeof(IPAddress), DbType.String },
        { typeof(Uri), DbType.String },

        { typeof(BigInteger), DbType.Decimal },
        { typeof(BigInteger?), DbType.Decimal },

        { typeof(DateTime), DbType.DateTime },
        { typeof(DateTime?), DbType.DateTime },
        { typeof(DateTimeOffset), DbType.DateTimeOffset },
        { typeof(DateTimeOffset?), DbType.DateTimeOffset },

        { typeof(object), DbType.Object }
    };

    #endregion Type

    #region DbType

    /// <summary>
    /// Map of DbType to DotNet types
    /// </summary>
    public static readonly IReadOnlyDictionary<DbType, Type> DbType_Type = new Dictionary<DbType, Type>
    {
        { DbType.AnsiString, typeof(string) },
        { DbType.AnsiStringFixedLength, typeof(char[]) },
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
        { DbType.Xml, typeof(string) }
    };

    public static readonly IReadOnlySet<DbType> DBTYPES_NUMERIC = new HashSet<DbType>
    {
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
    };


    public static readonly IReadOnlySet<DbType> DBTYPES_CHARACTERS = new HashSet<DbType>
    {
        DbType.AnsiString,
        DbType.AnsiStringFixedLength,
        DbType.String,
        DbType.StringFixedLength,
        DbType.Xml
    };

    public static readonly IReadOnlySet<DbType> DBTYPES_DATETIME = new HashSet<DbType>
    {
        DbType.Date,
        DbType.DateTime,
        DbType.DateTime2,
        DbType.DateTimeOffset,
        DbType.Time
    };

    #endregion DbType

    #region Colors

    /// <summary>
    /// Case-Sensitive map of Color names to Colors
    /// </summary>
    public static IReadOnlyDictionary<string, Color> COLORS = COLORS_get();

    private static IReadOnlyDictionary<string, Color> COLORS_get()
    {
        // https://stackoverflow.com/a/3821197

        var d = new Dictionary<string, Color>();

        try
        {
            var colorType = typeof(Color);
            // We take only static property to avoid properties like Name, IsSystemColor ...
            var propInfos = colorType.GetProperties(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public);
            foreach (var propInfo in propInfos)
            {
                var colorGetMethod = propInfo.GetGetMethod();
                if (colorGetMethod == null)
                {
                    continue;
                }

                var colorObject = colorGetMethod.Invoke(null, null);
                if (colorObject == null)
                {
                    continue;
                }

                var colorObjectType = colorObject.GetType();
                if (!colorObjectType.Equals(typeof(Color)))
                {
                    continue;
                }

                var color = (Color)colorObject;
                var colorName = propInfo.Name;
                d[colorName] = color;
            }
        }
        catch (Exception e)
        {
            LogError(e);
        }

        return d;
    }

    #endregion Colors

    #region IO

    public const int BUFFER_SIZE_MINIMUM = 1024 * 4;

    /// <summary>
    /// We pick a value that is the largest multiple of 4096 that is still smaller than the
    /// large object heap threshold (85K). The CopyTo/CopyToAsync buffer is short-lived and is
    /// likely to be collected at Gen0, and it offers a significant improvement in Copy performance.
    /// </summary>
    public const int BUFFER_SIZE_OPTIMAL = 81920;

    #endregion

    #region Bool

    private const string BOOL_TRUE = "1 T TRUE Y YES";
    private const string BOOL_FALSE = "0 F FALSE N NO";

    /// <summary>
    /// Case-Insensitive hashset of boolean true values
    /// </summary>
    public static readonly IReadOnlySet<string> BOOLS_TRUE = new HashSet<string>(BOOL_TRUE.Split(' '), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Case-Insensitive hashset of boolean false values
    /// </summary>
    public static readonly IReadOnlySet<string> BOOLS_FALSE = new HashSet<string>(BOOL_FALSE.Split(' '), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Case-Insensitive map of boolean string values to boolean values
    /// </summary>
    public static readonly IReadOnlyDictionary<string, bool> String_Bool = String_Bool_get();

    private static IReadOnlyDictionary<string, bool> String_Bool_get()
    {
        var d = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in BOOL_TRUE.Split(' '))
        {
            d.Add(item, true);
        }

        foreach (var item in BOOL_FALSE.Split(' '))
        {
            d.Add(item, false);
        }

        return new Dictionary<string, bool>(d, StringComparer.OrdinalIgnoreCase);
    }

    #endregion Bool

    #region Path

    public static readonly IReadOnlySet<char> PATH_DELIMITERS = new HashSet<char>(new[] { '/', '\\', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
    public static readonly ImmutableArray<char> PATH_DELIMITERS_ARRAY = PATH_DELIMITERS.OrderBy(o => o).ToImmutableArray();
    public static readonly IReadOnlySet<string> PATH_DELIMITERS_STRINGS = new HashSet<string>(PATH_DELIMITERS.Select(o => o.ToString()));
    public static readonly ImmutableArray<string> PATH_DELIMITERS_STRINGS_ARRAY = PATH_DELIMITERS_ARRAY.Select(o => o.ToString()).ToArray().ToImmutableArray();
    public static readonly bool PATH_CASE_SENSITIVE = !OS_WINDOWS;

    #endregion Path

    #region CurrentLocation

    public static readonly IReadOnlyList<string> CURRENT_EXE_DIRECTORIES = GetCurrentLocationsDirectory().AsReadOnly();

    public static readonly string CURRENT_EXE_DIRECTORY = CURRENT_EXE_DIRECTORIES.FirstOrDefault();

    public static readonly IReadOnlyList<string> CURRENT_EXES = GetCurrentLocationsFile().AsReadOnly();

    /// <summary>
    /// The current EXE file name. Could be a full file path, or a partial file path, or null
    /// </summary>
    public static readonly string CURRENT_EXE = CURRENT_EXES.FirstOrDefault();


    private static List<string> GetCurrentLocationsDirectory()
    {
        var list = new List<string>();
        var set = new HashSet<string>(PATH_CASE_SENSITIVE ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

        foreach (var location in GetCurrentLocations())
        {
            try
            {
                if (Directory.Exists(location))
                {
                    if (set.Add(location)) list.Add(location);
                }
                else if (File.Exists(location))
                {
                    var location2 = Path.GetDirectoryName(location);
                    if (location2 != null)
                    {
                        location2 = Path.GetFullPath(location2);
                        if (Directory.Exists(location2))
                        {
                            if (set.Add(location2)) list.Add(location2);
                        }
                    }

                }
            }
            catch { }
        }
        
        return list;
    }
    
    private static List<string> GetCurrentLocationsFile()
    {
        var list = new List<string>();
        var set = new HashSet<string>(PATH_CASE_SENSITIVE ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

        foreach (var location in GetCurrentLocations())
        {
            try
            {
                if (File.Exists(location))
                {
                    if (set.Add(location)) list.Add(location);
                }
            }
            catch { }
        }
        
        return list;
    }
    
    private static List<string> GetCurrentLocations()
    {
        // https://stackoverflow.com/questions/616584/how-do-i-get-the-name-of-the-current-executable-in-c

        var list = new List<string>();
        try
        {
            list.Add(Process.GetCurrentProcess().MainModule?.FileName);
        }
        catch { }

        try
        {
            list.Add(AppDomain.CurrentDomain.FriendlyName);
        }
        catch { }

        try
        {
            list.Add(Process.GetCurrentProcess().ProcessName);
        }
        catch { }

        try
        {
            list.Add(typeof(Constant).Assembly.Location);
        }
        catch { }

        try
        {
            list.Add(Path.GetFullPath("."));
        }
        catch { }

        try
        {
            list.Add(Environment.CurrentDirectory);
        }
        catch { }

        var set = new HashSet<string>(PATH_CASE_SENSITIVE ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

        var list2 = new List<string>();
        foreach (var item in list)
        {
            var item2 = TrimOrNull(item);
            if (item2 == null) continue;
            
            try
            {
                item2 = Path.GetFullPath(item2);
            }
            catch { }
            
            try
            {
                if (!File.Exists(item2) && !Directory.Exists(item2))
                {
                    if (File.Exists(item2 + ".exe"))
                    {
                        item2 += ".exe";
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            catch { }

            if (!set.Add(item2)) continue;

            list2.Add(item2);
        }

        return list2;
    }
    
    
    /// <summary>
    /// Are we executing via a batch file or script or running the command directly from the console window?
    /// </summary>
    public static readonly bool IS_BATCHFILE_EXECUTED = IS_BATCHFILE_EXECUTED_get();

    private static bool IS_BATCHFILE_EXECUTED_get()
    {
        try
        {
            // http://stackoverflow.com/questions/3453220/how-to-detect-if-console-in-stdin-has-been-redirected?lq=1
            //return (0 == (System.Console.WindowHeight + System.Console.WindowWidth)) && System.Console.KeyAvailable;
            if (Console.WindowHeight != 0)
            {
                return false;
            }

            if (Console.WindowWidth != 0)
            {
                return false;
            }

            if (!Console.KeyAvailable)
            {
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            LogError(e);
        }

        return false;
    }

    #endregion CurrentLocation

    #region Helpers

    private static void LogError(Exception exception, [CallerMemberName] string memberName = "")
    {
        var msg = nameof(Constant) + "." + memberName + "() failed.";
        if (exception != null)
        {
            try
            {
                var err = exception.ToString();
                msg = msg + " " + err;
            }
            catch (Exception)
            {
                try
                {
                    var err = exception.Message;
                    msg = msg + " " + err;
                }
                catch (Exception)
                {
                    try
                    {
                        var err = exception.GetType().FullName;
                        msg = msg + " " + err;
                    }
                    catch (Exception) { }
                }
            }
        }

        try
        {
            Debug.WriteLine(msg);
        }
        catch (Exception)
        {
            try
            {
                Console.Error.WriteLine(msg);
            }
            catch (Exception)
            {
                try
                {
                    Console.WriteLine(msg);
                }
                catch { }
            }
        }
    }

    private static string TrimOrNull(string str)
    {
        if (str == null)
        {
            return null;
        }

        str = str.Trim();
        return str.Length == 0 ? null : str;
    }

    #endregion Helpers
}
