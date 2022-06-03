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

using System.Net;
using System.Security;

namespace MaxRunSoftware.Utilities;

public static class ExtensionsToString
{
    #region ToStringPadded

    public static string ToStringPadded(this byte value) => value.ToString().PadLeft(byte.MaxValue.ToString().Length, '0');

    public static string ToStringPadded(this sbyte value) => value.ToString().PadLeft(sbyte.MaxValue.ToString().Length, '0');

    public static string ToStringPadded(this decimal value) => value.ToString().PadLeft(decimal.MaxValue.ToString().Length, '0');

    public static string ToStringPadded(this double value) => value.ToString().PadLeft(double.MaxValue.ToString().Length, '0');

    public static string ToStringPadded(this float value) => value.ToString().PadLeft(float.MaxValue.ToString().Length, '0');

    public static string ToStringPadded(this int value) => value.ToString().PadLeft(int.MaxValue.ToString().Length, '0');

    public static string ToStringPadded(this uint value) => value.ToString().PadLeft(uint.MaxValue.ToString().Length, '0');

    public static string ToStringPadded(this long value) => value.ToString().PadLeft(long.MaxValue.ToString().Length, '0');

    public static string ToStringPadded(this ulong value) => value.ToString().PadLeft(ulong.MaxValue.ToString().Length, '0');

    public static string ToStringPadded(this short value) => value.ToString().PadLeft(short.MaxValue.ToString().Length, '0');

    public static string ToStringPadded(this ushort value) => value.ToString().PadLeft(ushort.MaxValue.ToString().Length, '0');

    #endregion ToStringPadded

    #region ToStringCommas

    public static string ToStringCommas(this int value) => string.Format("{0:n0}", value);

    public static string ToStringCommas(this uint value) => string.Format("{0:n0}", value);

    public static string ToStringCommas(this long value) => string.Format("{0:n0}", value);

    public static string ToStringCommas(this ulong value) => string.Format("{0:n0}", value);

    public static string ToStringCommas(this short value) => string.Format("{0:n0}", value);

    public static string ToStringCommas(this ushort value) => string.Format("{0:n0}", value);

    #endregion ToStringCommas

    #region ToStringRoundAwayFromZero

    public static string ToString(this double value, MidpointRounding rounding, int decimalPlaces) => value.Round(rounding, decimalPlaces).ToString("N" + decimalPlaces).Replace(",", "");

    public static string ToString(this float value, MidpointRounding rounding, int decimalPlaces) => value.Round(rounding, decimalPlaces).ToString("N" + decimalPlaces).Replace(",", "");

    public static string ToString(this decimal value, MidpointRounding rounding, int decimalPlaces) => value.Round(rounding, decimalPlaces).ToString("N" + decimalPlaces).Replace(",", "");

    #endregion ToStringRoundAwayFromZero


    public static string ToStringItems(this IEnumerable enumerable)
    {
        var list = new List<string>();

        foreach (var item in enumerable)
        {
            list.Add(item.ToStringGuessFormat());
        }

        return "[" + string.Join(",", list) + "]";
    }

    public static string ToStringISO8601(this DateTime dateTime) => dateTime.ToString("o"); // ISO 8601
    public static string ToStringYYYYMMDD(this DateTime dateTime) => dateTime.ToString("yyyy-MM-dd");
    public static string ToStringYYYYMMDDHHMMSS(this DateTime dateTime) => dateTime.ToString("yyyy-MM-dd HH:mm:ss");

    public static string ToStringGuessFormat(this object obj)
    {
        if (obj == null) return null;
        var t = obj.GetType();
        if (t.IsNullable(out var underlyingType)) t = underlyingType;
        if (t == typeof(string)) return (string)obj;
        if (t == typeof(DateTime)) return ((DateTime)obj).ToStringYYYYMMDDHHMMSS();
        if (t == typeof(DateTime?)) return ((DateTime?)obj).Value.ToStringYYYYMMDDHHMMSS();
        if (t == typeof(byte[])) return "0x" + Util.Base16((byte[])obj);
        if (obj is IEnumerable enumerable) return enumerable.ToStringItems();
        return obj.ToString();
    }
    public static IEnumerable<string> ToStringsGuessFormat(this IEnumerable<object> enumerable)
    {
        foreach (var obj in enumerable.OrEmpty()) yield return obj.ToStringGuessFormat();
    }

    public static string ToStringGenerated(this object obj, BindingFlags flags)
    {
        // TODO: Can add performance improvements if needed

        obj.CheckNotNull(nameof(obj));

        var t = obj.GetType();

        var list = new List<string>();
        foreach (var prop in t.GetProperties(flags))
        {
            if (prop == null) continue;
            if (!prop.CanRead) continue;

            var name = prop.Name;
            var val = prop.GetValue(obj).ToStringGuessFormat() ?? "null";

            list.Add(name + "=" + val);
        }

        var sb = new StringBuilder();
        sb.Append(t.NameFormatted());
        sb.Append("(");
        sb.Append(list.ToStringDelimited(", "));
        sb.Append(")");

        return sb.ToString();
    }

    public static string ToStringGenerated(this object obj) => ToStringGenerated(obj, BindingFlags.Instance | BindingFlags.Public);

    public static string ToStringDelimited<T>(this IEnumerable<T> enumerable, string delimiter) => string.Join(delimiter, enumerable);
    public static string ToStringDelimited(this IEnumerable<object> enumerable, string delimiter) => enumerable.Select(o => o.ToStringGuessFormat()).ToStringDelimited(delimiter);

    public static string ToStringInsecure(this SecureString secureString) => new NetworkCredential("", secureString).Password;

    public static string ToStringTotalSeconds(this TimeSpan timeSpan, int numberOfDecimalDigits = 0) => timeSpan.TotalSeconds.ToString(MidpointRounding.AwayFromZero, Math.Max(0, numberOfDecimalDigits));

    private static readonly string[] ToStringBase16Cache = Enumerable.Range(0, 256).Select(o => BitConverter.ToString(new byte[] { (byte)o })).ToArray();
    public static string ToStringBase16(this byte b) => ToStringBase16Cache[b];

    private static readonly string[] ToStringBase64Cache = Enumerable.Range(0, 256).Select(o => Convert.ToBase64String(new byte[] { (byte)o }).Substring(0, 2)).ToArray();
    public static string ToStringBase64(this byte b) => ToStringBase64Cache[b];

}
