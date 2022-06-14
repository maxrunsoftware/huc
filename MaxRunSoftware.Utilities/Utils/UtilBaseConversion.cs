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
    #region Base16

    private static readonly uint[] lookupBase16 = Base16();

    private static uint[] Base16()
    {
        var result = new uint[256];
        for (var i = 0; i < 256; i++)
        {
            var s = i.ToString("X2");
            result[i] = s[0] + ((uint)s[1] << 16);
        }

        return result;
    }

    public static string Base16(byte[] bytes)
    {
        // https://stackoverflow.com/a/24343727/48700 https://stackoverflow.com/a/624379

        var lookup32 = lookupBase16;
        var result = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            var val = lookup32[bytes[i]];
            result[2 * i] = (char)val;
            result[2 * i + 1] = (char)(val >> 16);
        }

        return new string(result);
    }

    public static byte[] Base16(string base16String)
    {
        var numberChars = base16String.Length;
        var bytes = new byte[numberChars / 2];
        for (var i = 0; i < numberChars; i += 2) bytes[i / 2] = Convert.ToByte(base16String.Substring(i, 2), 16);

        return bytes;
    }

    #endregion Base16

    #region Base64

    public static string Base64(byte[] bytes) => Convert.ToBase64String(bytes);

    public static byte[] Base64(string base64String) => Convert.FromBase64String(base64String);

    #endregion Base64
}
