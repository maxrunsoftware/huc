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
using System.Net;
using System.Net.Mail;
using System.Security;

namespace MaxRunSoftware.Utilities
{
    public static class ExtensionsStringConversion
    {
        #region bool

        public static bool ToBool(this string str) => str.ToBoolTry(out var output) ? output : bool.Parse(str);

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

        public static bool? ToBoolNullable(this string str) => str == null ? null : (bool?)str.ToBool();

        public static bool ToBoolNullableTry(this string str, out bool? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToBoolTry(out var o);
            output = r ? o : (bool?)null;
            return r;
        }

        #endregion bool

        #region byte

        public static byte ToByte(this string str) => byte.Parse(str);

        public static bool ToByteTry(this string str, out byte output) => byte.TryParse(str, out output);

        public static byte? ToByteNullable(this string str) => str == null ? null : (byte?)str.ToByte();

        public static bool ToByteNullableTry(this string str, out byte? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToByteTry(out var o);
            output = r ? o : (byte?)null;
            return r;
        }

        #endregion byte

        #region sbyte

        public static sbyte ToSByte(this string str) => sbyte.Parse(str);

        public static bool ToSByteTry(this string str, out sbyte output) => sbyte.TryParse(str, out output);

        public static sbyte? ToSByteNullable(this string str) => str == null ? null : (sbyte?)str.ToSByte();

        public static bool ToSByteNullableTry(this string str, out sbyte? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToSByteTry(out var o);
            output = r ? o : (sbyte?)null;
            return r;
        }

        #endregion sbyte

        #region char

        public static char ToChar(this string str) => char.Parse(str);

        public static bool ToCharTry(this string str, out char output) => char.TryParse(str, out output);

        public static char? ToCharNullable(this string str) => str == null ? null : (char?)str.ToChar();

        public static bool ToCharNullableTry(this string str, out char? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToCharTry(out var o);
            output = r ? o : (char?)null;
            return r;
        }

        #endregion char

        #region short

        public static short ToShort(this string str) => short.Parse(str);

        public static bool ToShortTry(this string str, out short output) => short.TryParse(str, out output);

        public static short? ToShortNullable(this string str) => str == null ? null : (short?)str.ToShort();

        public static bool ToShortNullableTry(this string str, out short? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToShortTry(out var o);
            output = r ? o : (short?)null;
            return r;
        }

        #endregion short

        #region ushort

        public static ushort ToUShort(this string str) => ushort.Parse(str);

        public static bool ToUShortTry(this string str, out ushort output) => ushort.TryParse(str, out output);

        public static ushort? ToUShortNullable(this string str) => str == null ? null : (ushort?)str.ToUShort();

        public static bool ToUShortNullableTry(this string str, out ushort? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToUShortTry(out var o);
            output = r ? o : (ushort?)null;
            return r;
        }

        #endregion ushort

        #region int

        public static int ToInt(this string str) => int.Parse(str);

        public static bool ToIntTry(this string str, out int output) => int.TryParse(str, out output);

        public static int? ToIntNullable(this string str) => str == null ? null : (int?)str.ToInt();

        public static bool ToIntNullableTry(this string str, out int? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToIntTry(out var o);
            output = r ? o : (int?)null;
            return r;
        }

        #endregion int

        #region uint

        public static uint ToUInt(this string str) => uint.Parse(str);

        public static bool ToUIntTry(this string str, out uint output) => uint.TryParse(str, out output);

        public static uint? ToUIntNullable(this string str) => str == null ? null : (uint?)str.ToUInt();

        public static bool ToUIntNullableTry(this string str, out uint? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToUIntTry(out var o);
            output = r ? o : (uint?)null;
            return r;
        }

        #endregion uint

        #region long

        public static long ToLong(this string str) => long.Parse(str);

        public static bool ToLongTry(this string str, out long output) => long.TryParse(str, out output);

        public static long? ToLongNullable(this string str) => str == null ? null : (long?)str.ToLong();

        public static bool ToLongNullableTry(this string str, out long? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToLongTry(out var o);
            output = r ? o : (long?)null;
            return r;
        }

        #endregion long

        #region ulong

        public static ulong ToULong(this string str) => ulong.Parse(str);

        public static bool ToULongTry(this string str, out ulong output) => ulong.TryParse(str, out output);

        public static ulong? ToULongNullable(this string str) => str == null ? null : (ulong?)str.ToULong();

        public static bool ToULongNullableTry(this string str, out ulong? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToULongTry(out var o);
            output = r ? o : (ulong?)null;
            return r;
        }

        #endregion ulong

        #region float

        public static float ToFloat(this string str) => float.Parse(str);

        public static bool ToFloatTry(this string str, out float output) => float.TryParse(str, out output);

        public static float? ToFloatNullable(this string str) => str == null ? null : (float?)str.ToFloat();

        public static bool ToFloatNullableTry(this string str, out float? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToFloatTry(out var o);
            output = r ? o : (float?)null;
            return r;
        }

        #endregion float

        #region double

        public static double ToDouble(this string str) => double.Parse(str);

        public static bool ToDoubleTry(this string str, out double output) => double.TryParse(str, out output);

        public static double? ToDoubleNullable(this string str) => str == null ? null : (double?)str.ToDouble();

        public static bool ToDoubleNullableTry(this string str, out double? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToDoubleTry(out var o);
            output = r ? o : (double?)null;
            return r;
        }

        #endregion double

        #region decimal

        public static decimal ToDecimal(this string str) => decimal.Parse(str);

        public static bool ToDecimalTry(this string str, out decimal output) => decimal.TryParse(str, out output);

        public static decimal? ToDecimalNullable(this string str) => str == null ? null : (decimal?)str.ToDecimal();

        public static bool ToDecimalNullableTry(this string str, out decimal? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToDecimalTry(out var o);
            output = r ? o : (decimal?)null;
            return r;
        }

        #endregion decimal

        #region DateTime

        public static DateTime ToDateTime(this string str) => DateTime.Parse(str);

        public static bool ToDateTimeTry(this string str, out DateTime output) => DateTime.TryParse(str, out output);

        public static DateTime? ToDateTimeNullable(this string str) => str == null ? null : (DateTime?)str.ToDateTime();

        public static bool ToDateTimeNullableTry(this string str, out DateTime? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToDateTimeTry(out var o);
            output = r ? o : (DateTime?)null;
            return r;
        }

        #endregion DateTime

        #region Guid

        public static Guid ToGuid(this string str) => Guid.Parse(str);

        public static bool ToGuidTry(this string str, out Guid output) => Guid.TryParse(str, out output);

        public static Guid? ToGuidNullable(this string str) => str == null ? null : (Guid?)str.ToGuid();

        public static bool ToGuidNullableTry(this string str, out Guid? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToGuidTry(out var o);
            output = r ? o : (Guid?)null;
            return r;
        }

        #endregion Guid

        #region IPAddress

        public static IPAddress ToIPAddress(this string str) => str == null ? null : IPAddress.Parse(str);

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

        public static Uri ToUri(this string str) => str == null ? null : new Uri(str);

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

        public static MailAddress ToMailAddress(this string str) => str == null ? null : new MailAddress(str);

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

        public static SecureString ToSecureString(this string str) => new NetworkCredential("", str).SecurePassword;

        #endregion SecureString
    }
}

