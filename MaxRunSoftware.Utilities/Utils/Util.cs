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

using System.Dynamic;
using System.Security.Cryptography;

namespace MaxRunSoftware.Utilities;

public static partial class Util
{
    public static bool DynamicHasProperty(dynamic obj, string propertyName)
    {
        return obj is ExpandoObject
            ? ((IDictionary<string, object>)obj).ContainsKey(propertyName)
            : (bool)(obj.GetType().GetProperty(propertyName) != null);
    }

    /// <summary>
    /// Gets a 001/100 format for a running count
    /// </summary>
    /// <param name="index">The zero based index, +1 will be added automatically</param>
    /// <param name="total">The total number of items</param>
    /// <returns>001/100 formatted string</returns>
    public static string FormatRunningCount(int index, int total)
    {
        return (index + 1).ToStringPadded().Right(total.ToString().Length) + "/" + total;
    }

    public static string FormatRunningCountPercent(int index, int total, int decimalPlaces)
    {
        var len = 3;
        if (decimalPlaces > 0)
        {
            len += 1; // decimal
        }

        len += decimalPlaces;

        decimal indexDecimal = index + 1;
        var totalDecimal = total;
        var multiplierDecimal = 100;

        return (indexDecimal / totalDecimal * multiplierDecimal).ToString(MidpointRounding.AwayFromZero, decimalPlaces).PadLeft(len) + " %";
    }

    public static string RandomString(int size, char[] characterPool)
    {
        // https://stackoverflow.com/a/1344255

        var data = new byte[4 * size];

        using (var crypto = RandomNumberGenerator.Create())
        {
            crypto.GetBytes(data);
        }

        var result = new StringBuilder(size);
        for (var i = 0; i < size; i++)
        {
            var rnd = BitConverter.ToUInt32(data, i * 4);
            var idx = rnd % characterPool.Length;

            result.Append(characterPool[idx]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Parses an encoding name string to an Encoding. Values allowed are...
    /// ASCII
    /// BIGENDIANUNICODE
    /// DEFAULT
    /// UNICODE
    /// UTF32
    /// UTF8
    /// UTF8BOM
    /// If null is provided then UTF8 encoding is returned.
    /// </summary>
    /// <param name="encoding">The encoding name string</param>
    /// <returns>The Encoding or UTF8 Encoding if null is provided</returns>
    public static Encoding ParseEncoding(string encoding)
    {
        return (encoding.TrimOrNull() ?? "UTF8") switch
        {
            "ASCII" => Encoding.ASCII,
            "BIGENDIANUNICODE" => Encoding.BigEndianUnicode,
            "DEFAULT" => Encoding.Default,
            "UNICODE" => Encoding.Unicode,
            "UTF32" => Encoding.UTF32,
            "UTF8" => Constant.ENCODING_UTF8,
            "UTF8BOM" => Constant.ENCODING_UTF8_BOM,
            _ => throw new Exception("Unknown encoding type specified: " + encoding)
        };
    }
}
