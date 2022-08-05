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

using System.IO.Compression;

namespace MaxRunSoftware.Utilities;

public static class ExtensionsByte
{
    #region IsValidUTF8

    public static bool IsValidUTF8(this byte[] buffer) => IsValidUTF8(buffer, buffer.Length);

    /// <summary></summary>
    /// <param name="buffer"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static bool IsValidUTF8(this byte[] buffer, int length)
    {
        var position = 0;
        var bytes = 0;
        while (position < length)
        {
            if (!IsValidUTF8(buffer, position, length, ref bytes)) return false;

            position += bytes;
        }

        return true;
    }

    /// <summary></summary>
    /// <param name="buffer"></param>
    /// <param name="position"></param>
    /// <param name="length"></param>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static bool IsValidUTF8(this byte[] buffer, int position, int length, ref int bytes)
    {
        if (length > buffer.Length) throw new ArgumentException("Invalid length");

        if (position > length - 1)
        {
            bytes = 0;
            return true;
        }

        var ch = buffer[position];

        if (ch <= 0x7F)
        {
            bytes = 1;
            return true;
        }

        if (ch >= 0xc2 && ch <= 0xdf)
        {
            if (position >= length - 2)
            {
                bytes = 0;
                return false;
            }

            if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf)
            {
                bytes = 0;
                return false;
            }

            bytes = 2;
            return true;
        }

        if (ch == 0xe0)
        {
            if (position >= length - 3)
            {
                bytes = 0;
                return false;
            }

            if (buffer[position + 1] < 0xa0 || buffer[position + 1] > 0xbf ||
                buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf)
            {
                bytes = 0;
                return false;
            }

            bytes = 3;
            return true;
        }

        if (ch >= 0xe1 && ch <= 0xef)
        {
            if (position >= length - 3)
            {
                bytes = 0;
                return false;
            }

            if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf ||
                buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf)
            {
                bytes = 0;
                return false;
            }

            bytes = 3;
            return true;
        }

        if (ch == 0xf0)
        {
            if (position >= length - 4)
            {
                bytes = 0;
                return false;
            }

            if (buffer[position + 1] < 0x90 || buffer[position + 1] > 0xbf ||
                buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
                buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
            {
                bytes = 0;
                return false;
            }

            bytes = 4;
            return true;
        }

        if (ch == 0xf4)
        {
            if (position >= length - 4)
            {
                bytes = 0;
                return false;
            }

            if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0x8f ||
                buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
                buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
            {
                bytes = 0;
                return false;
            }

            bytes = 4;
            return true;
        }

        if (ch >= 0xf1 && ch <= 0xf3)
        {
            if (position >= length - 4)
            {
                bytes = 0;
                return false;
            }

            if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf ||
                buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
                buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
            {
                bytes = 0;
                return false;
            }

            bytes = 4;
            return true;
        }

        return false;
    }

    #endregion IsValidUTF8

    #region Compression

    /// <summary>
    /// Compresses binary data
    /// </summary>
    /// <param name="data">Data to compress</param>
    /// <param name="compressionLevel">The level of compression</param>
    /// <returns>The compressed data</returns>
    public static byte[] CompressGZip(this byte[] data, CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        using (var stream = new MemoryStream())
        {
            using (var gZipStream = new GZipStream(stream, compressionLevel))
            {
                gZipStream.Write(data, 0, data.Length);

                gZipStream.Flush();
                stream.Flush();

                gZipStream.Close();
                stream.Close();

                return stream.ToArray();
            }
        }
    }

    /// <summary>
    /// Decompresses binary data
    /// </summary>
    /// <param name="data">The data to decompress</param>
    /// <returns>The decompressed data</returns>
    public static byte[] DecompressGZip(this byte[] data)
    {
        using (var stream = new MemoryStream(data))
        {
            using (var gZipStream = new GZipStream(stream, CompressionMode.Decompress))
            {
                using (var stream2 = new MemoryStream())
                {
                    gZipStream.CopyTo(stream2);
                    stream2.Flush();
                    stream2.Close();
                    return stream2.ToArray();
                }
            }
        }
    }

    #endregion Compression

    public static bool EqualsBytes(this Span<byte> b1, Span<byte> b2) => b1.SequenceEqual(b2);

    // https://stackoverflow.com/a/48599119
    public static bool EqualsBytes(this byte[] b1, byte[] b2) =>
        // alternative unsafe option https://stackoverflow.com/a/8808245
        EqualsBytes(b1, b2, false);

    public static bool EqualsBytes(this byte[] b1, byte[] b2, bool reverse)
    {
        if (b1 == b2) return true; //reference equality check

        if (b1 == null || b2 == null) return false;

        if (b1.Length != b2.Length) return false;

        var len = b1.Length;
        if (len == 0) return true;

        if (b1[0] != b2[0]) return false; // compare first byte

        if (b1[len - 1] != b2[len - 1]) return false; // compare last byte

        if (b1[len / 2] != b2[len / 2]) return false; // compare middle byte

        if (reverse)
        {
            for (var i = len - 1; i >= 0; i--)
            {
                if (b1[i] != b2[i]) return false;
            }
        }
        else
        {
            for (var i = 0; i < len; i++)
            {
                if (b1[i] != b2[i]) return false;
            }
        }

        return true;
    }
}
