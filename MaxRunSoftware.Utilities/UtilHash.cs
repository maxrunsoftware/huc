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

using System.Security.Cryptography;

namespace MaxRunSoftware.Utilities;

public static partial class Util
{
    private static string GenerateHashInternal(HashAlgorithm hashAlgorithm, Stream stream)
    {
        using (hashAlgorithm)
        {
            var hash = hashAlgorithm.ComputeHash(stream);
            var str = BitConverter.ToString(hash);
            return str.Replace("-", "").ToLower();
        }
    }

    private static string GenerateHashInternal(HashAlgorithm hashAlgorithm, byte[] bytes)
    {
        using (hashAlgorithm)
        {
            var hash = hashAlgorithm.ComputeHash(bytes);
            var str = BitConverter.ToString(hash);
            return str.Replace("-", "").ToLower();
        }
    }

    private static string GenerateHashInternal(HashAlgorithm hashAlgorithm, string file)
    {
        using (var fs = FileOpenRead(file))
        {
            return GenerateHashInternal(hashAlgorithm, fs);
        }
    }

    public static string GenerateHashMD5(Stream stream) => GenerateHashInternal(MD5.Create(), stream);

    public static string GenerateHashMD5(byte[] bytes) => GenerateHashInternal(MD5.Create(), bytes);

    public static string GenerateHashMD5(string file) => GenerateHashInternal(MD5.Create(), file);

    public static string GenerateHashSHA1(Stream stream) => GenerateHashInternal(SHA1.Create(), stream);

    public static string GenerateHashSHA1(byte[] bytes) => GenerateHashInternal(SHA1.Create(), bytes);

    public static string GenerateHashSHA1(string file) => GenerateHashInternal(SHA1.Create(), file);

    public static string GenerateHashSHA256(Stream stream) => GenerateHashInternal(SHA256.Create(), stream);

    public static string GenerateHashSHA256(byte[] bytes) => GenerateHashInternal(SHA256.Create(), bytes);

    public static string GenerateHashSHA256(string file) => GenerateHashInternal(SHA256.Create(), file);

    public static string GenerateHashSHA384(Stream stream) => GenerateHashInternal(SHA384.Create(), stream);

    public static string GenerateHashSHA384(byte[] bytes) => GenerateHashInternal(SHA384.Create(), bytes);

    public static string GenerateHashSHA384(string file) => GenerateHashInternal(SHA384.Create(), file);

    public static string GenerateHashSHA512(Stream stream) => GenerateHashInternal(SHA512.Create(), stream);

    public static string GenerateHashSHA512(byte[] bytes) => GenerateHashInternal(SHA512.Create(), bytes);

    public static string GenerateHashSHA512(string file) => GenerateHashInternal(SHA512.Create(), file);

}
