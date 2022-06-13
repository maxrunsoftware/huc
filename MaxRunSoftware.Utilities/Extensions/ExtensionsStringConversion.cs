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
using System.Net.Mail;
using System.Security;

namespace MaxRunSoftware.Utilities;

public static class ExtensionsStringConversion
{
    #region bool

    public static bool ToBool(this string str)
    {
        return str.ToBoolTry(out var output) ? output : bool.Parse(str);
    }

    public static bool ToBoolTry(this string str, out bool output)
    {
        str = str.TrimOrNull();
        if (str == null)
        {
            output = default;
            return false;
        }

        if (str.Length < 6)
        {
            switch (str.ToUpperInvariant())
            {
                case "1":
                case "T":
                case "TRUE":
                case "Y":
                case "YES":
                    output = true;
                    return true;

                case "0":
                case "F":
                case "FALSE":
                case "N":
                case "NO":
                    output = false;
                    return true;
            }
        }

        var returnValue = bool.TryParse(str, out var r);
        output = r;
        return returnValue;
    }

    public static bool? ToBoolNullable(this string str)
    {
        return str?.ToBool();
    }

    public static bool ToBoolNullableTry(this string str, out bool? output)
    {
        if (str == null)
        {
            output = null;
            return true;
        }

        var r = str.ToBoolTry(out var o);
        output = r ? o : null;
        return r;
    }

    #endregion bool

    #region byte

    public static byte ToByte(this string str)
    {
        return byte.Parse(str);
    }

    public static bool ToByteTry(this string str, out byte output)
    {
        return byte.TryParse(str, out output);
    }

    public static byte? ToByteNullable(this string str)
    {
        return str?.ToByte();
    }

    public static bool ToByteNullableTry(this string str, out byte? output)
    {
        if (str == null)
        {
            output = null;
            return true;
        }

        var r = str.ToByteTry(out var o);
        output = r ? o : null;
        return r;
    }

    #endregion byte

    #region sbyte

    public static sbyte ToSByte(this string str)
    {
        return sbyte.Parse(str);
    }

    public static bool ToSByteTry(this string str, out sbyte output)
    {
        return sbyte.TryParse(str, out output);
    }

    public static sbyte? ToSByteNullable(this string str)
    {
        return str?.ToSByte();
    }

    public static bool ToSByteNullableTry(this string str, out sbyte? output)
    {
        if (str == null)
        {
            output = null;
            return true;
        }

        var r = str.ToSByteTry(out var o);
        output = r ? o : null;
        return r;
    }

    #endregion sbyte

    #region char

    public static char ToChar(this string str)
    {
        return char.Parse(str);
    }

    public static bool ToCharTry(this string str, out char output)
    {
        return char.TryParse(str, out output);
    }

    public static char? ToCharNullable(this string str)
    {
        return str?.ToChar();
    }

    public static bool ToCharNullableTry(this string str, out char? output)
    {
        if (str == null)
        {
            output = null;
            return true;
        }

        var r = str.ToCharTry(out var o);
        output = r ? o : null;
        return r;
    }

    #endregion char

    #region short

    public static short ToShort(this string str)
    {
        return short.Parse(str);
    }

    public static bool ToShortTry(this string str, out short output)
    {
        return short.TryParse(str, out output);
    }

    public static short? ToShortNullable(this string str)
    {
        return str?.ToShort();
    }

    public static bool ToShortNullableTry(this string str, out short? output)
    {
        if (str == null)
        {
            output = null;
            return true;
        }

        var r = str.ToShortTry(out var o);
        output = r ? o : null;
        return r;
    }

    #endregion short

    #region ushort

    public static ushort ToUShort(this string str)
    {
        return ushort.Parse(str);
    }

    public static bool ToUShortTry(this string str, out ushort output)
    {
        return ushort.TryParse(str, out output);
    }

    public static ushort? ToUShortNullable(this string str)
    {
        return str?.ToUShort();
    }

    public static bool ToUShortNullableTry(this string str, out ushort? output)
    {
        if (str == null)
        {
            output = null;
            return true;
        }

        var r = str.ToUShortTry(out var o);
        output = r ? o : null;
        return r;
    }

    #endregion ushort

    #region int

    public static int ToInt(this string str)
    {
        return int.Parse(str);
    }

    public static bool ToIntTry(this string str, out int output)
    {
        return int.TryParse(str, out output);
    }

    public static int? ToIntNullable(this string str)
    {
        return str?.ToInt();
    }

    public static bool ToIntNullableTry(this string str, out int? output)
    {
        if (str == null)
        {
            output = null;
            return true;
        }

        var r = str.ToIntTry(out var o);
        output = r ? o : null;
        return r;
    }

    #endregion int

    #region uint

    public static uint ToUInt(this string str)
    {
        return uint.Parse(str);
    }

    public static bool ToUIntTry(this string str, out uint output)
    {
        return uint.TryParse(str, out output);
    }

    public static uint? ToUIntNullable(this string str)
    {
        return str?.ToUInt();
    }

    public static bool ToUIntNullableTry(this string str, out uint? output)
    {
        if (str == null)
        {
            output = null;
            return true;
        }

        var r = str.ToUIntTry(out var o);
        output = r ? o : null;
        return r;
    }

    #endregion uint

    #region long

    public static long ToLong(this string str)
    {
        return long.Parse(str);
    }

    public static bool ToLongTry(this string str, out long output)
    {
        return long.TryParse(str, out output);
    }

    public static long? ToLongNullable(this string str)
    {
        return str?.ToLong();
    }

    public static bool ToLongNullableTry(this string str, out long? output)
    {
        if (str == null)
        {
            output = null;
            return true;
        }

        var r = str.ToLongTry(out var o);
        output = r ? o : null;
        return r;
    }

    #endregion long

    #region ulong

    public static ulong ToULong(this string str)
    {
        return ulong.Parse(str);
    }

    public static bool ToULongTry(this string str, out ulong output)
    {
        return ulong.TryParse(str, out output);
    }

    public static ulong? ToULongNullable(this string str)
    {
        return str?.ToULong();
    }

    public static bool ToULongNullableTry(this string str, out ulong? output)
    {
        if (str == null)
        {
            output = null;
            return true;
        }

        var r = str.ToULongTry(out var o);
        output = r ? o : null;
        return r;
    }

    #endregion ulong

    #region float

    public static float ToFloat(this string str)
    {
        return float.Parse(str);
    }

    public static bool ToFloatTry(this string str, out float output)
    {
        return float.TryParse(str, out output);
    }

    public static float? ToFloatNullable(this string str)
    {
        return str?.ToFloat();
    }

    public static bool ToFloatNullableTry(this string str, out float? output)
    {
        if (str == null)
        {
            output = null;
            return true;
        }

        var r = str.ToFloatTry(out var o);
        output = r ? o : null;
        return r;
    }

    #endregion float

    #region double

    public static double ToDouble(this string str)
    {
        return double.Parse(str);
    }

    public static bool ToDoubleTry(this string str, out double output)
    {
        return double.TryParse(str, out output);
    }

    public static double? ToDoubleNullable(this string str)
    {
        return str?.ToDouble();
    }

    public static bool ToDoubleNullableTry(this string str, out double? output)
    {
        if (str == null)
        {
            output = null;
            return true;
        }

        var r = str.ToDoubleTry(out var o);
        output = r ? o : null;
        return r;
    }

    #endregion double

    #region decimal

    public static decimal ToDecimal(this string str)
    {
        return decimal.Parse(str);
    }

    public static bool ToDecimalTry(this string str, out decimal output)
    {
        return decimal.TryParse(str, out output);
    }

    public static decimal? ToDecimalNullable(this string str)
    {
        return str?.ToDecimal();
    }

    public static bool ToDecimalNullableTry(this string str, out decimal? output)
    {
        if (str == null)
        {
            output = null;
            return true;
        }

        var r = str.ToDecimalTry(out var o);
        output = r ? o : null;
        return r;
    }

    #endregion decimal

    #region DateTime

    public static DateTime ToDateTime(this string str)
    {
        return DateTime.Parse(str);
    }

    public static bool ToDateTimeTry(this string str, out DateTime output)
    {
        return DateTime.TryParse(str, out output);
    }

    public static DateTime? ToDateTimeNullable(this string str)
    {
        return str?.ToDateTime();
    }

    public static bool ToDateTimeNullableTry(this string str, out DateTime? output)
    {
        if (str == null)
        {
            output = null;
            return true;
        }

        var r = str.ToDateTimeTry(out var o);
        output = r ? o : null;
        return r;
    }

    #endregion DateTime

    #region Guid

    public static Guid ToGuid(this string str)
    {
        return Guid.Parse(str);
    }

    public static bool ToGuidTry(this string str, out Guid output)
    {
        return Guid.TryParse(str, out output);
    }

    public static Guid? ToGuidNullable(this string str)
    {
        return str?.ToGuid();
    }

    public static bool ToGuidNullableTry(this string str, out Guid? output)
    {
        if (str == null)
        {
            output = null;
            return true;
        }

        var r = str.ToGuidTry(out var o);
        output = r ? o : null;
        return r;
    }

    #endregion Guid

    #region IPAddress

    public static IPAddress ToIPAddress(this string str)
    {
        return str == null ? null : IPAddress.Parse(str);
    }

    public static bool ToIPAddressTry(this string str, out IPAddress output)
    {
        if (str == null)
        {
            output = null;
            return false;
        }

        return IPAddress.TryParse(str, out output);
    }

    #endregion IPAddress

    #region Uri

    public static Uri ToUri(this string str)
    {
        return str == null ? null : new Uri(str);
    }

    public static bool ToUriTry(this string str, out Uri output)
    {
        if (str == null)
        {
            output = null;
            return false;
        }

        return Uri.TryCreate(str, UriKind.Absolute, out output);
    }

    #endregion Uri

    #region MailAddress

    public static MailAddress ToMailAddress(this string str)
    {
        return str == null ? null : new MailAddress(str);
    }

    public static bool ToMailAddressTry(this string str, out MailAddress output)
    {
        if (str == null)
        {
            output = null;
            return false;
        }

        try
        {
            output = new MailAddress(str);
            return true;
        }
        catch (Exception)
        {
            output = null;
            return false;
        }
    }

    #endregion MailAddress

    #region SecureString

    public static SecureString ToSecureString(this string str)
    {
        return new NetworkCredential("", str).SecurePassword;
    }

    #endregion SecureString
}
