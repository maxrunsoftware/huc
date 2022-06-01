// /*
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
// */

using System;
using System.IO;
using System.Security.Cryptography;

namespace MaxRunSoftware.Utilities
{
    public static partial class Util
    {
        public static byte[] EncryptionEncryptAsymetric(string pemPublicKey, byte[] data, RSAEncryptionPadding encryptionPadding)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportFromPem(pemPublicKey.ToCharArray());
                return rsa.Encrypt(data, encryptionPadding);
            }
        }

        public static byte[] EncryptionDecryptAsymetric(string pemPrivateKey, byte[] data, RSAEncryptionPadding encryptionPadding)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportFromPem(pemPrivateKey.ToCharArray());
                return rsa.Decrypt(data, encryptionPadding);
            }
        }

        public static (string publicKey, string privateKey) EncryptionGeneratePublicPrivateKeys(int length)
        {
            using (var rsa = RSA.Create(length))
            {
                //rsa.ExportParameters(true);
                string publicKey = EncryptionGeneratePublicPrivateKeysHelper.ExportPublicKey(rsa);
                string privateKey = EncryptionGeneratePublicPrivateKeysHelper.ExportPrivateKey(rsa);
                return (publicKey, privateKey);
            }
        }

        private static class EncryptionGeneratePublicPrivateKeysHelper
        {
            public static string ExportPrivateKey(RSA rsa)
            {
                var sb = new StringWriter();
                ExportPrivateKey(rsa, sb);
                return sb.ToString();
            }

            public static string ExportPublicKey(RSA rsa)
            {
                var sb = new StringWriter();
                ExportPublicKey(rsa, sb);
                return sb.ToString();
            }

            private static void ExportPrivateKey(RSA csp, TextWriter outputStream)
            {
                // https://stackoverflow.com/a/23739932
                var parameters = csp.ExportParameters(true);
                using (var stream = new MemoryStream())
                {
                    var writer = new BinaryWriter(stream);
                    writer.Write((byte)0x30); // SEQUENCE
                    using (var innerStream = new MemoryStream())
                    {
                        var innerWriter = new BinaryWriter(innerStream);
                        EncodeIntegerBigEndian(innerWriter, new byte[] { 0x00 }); // Version
                        EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
                        EncodeIntegerBigEndian(innerWriter, parameters.Exponent);
                        EncodeIntegerBigEndian(innerWriter, parameters.D);
                        EncodeIntegerBigEndian(innerWriter, parameters.P);
                        EncodeIntegerBigEndian(innerWriter, parameters.Q);
                        EncodeIntegerBigEndian(innerWriter, parameters.DP);
                        EncodeIntegerBigEndian(innerWriter, parameters.DQ);
                        EncodeIntegerBigEndian(innerWriter, parameters.InverseQ);
                        var length = (int)innerStream.Length;
                        EncodeLength(writer, length);
                        writer.Write(innerStream.GetBuffer(), 0, length);
                    }

                    var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
                    outputStream.WriteLine("-----BEGIN RSA PRIVATE KEY-----");
                    // Output as Base64 with lines chopped at 64 characters
                    for (var i = 0; i < base64.Length; i += 64)
                    {
                        outputStream.WriteLine(base64, i, Math.Min(64, base64.Length - i));
                    }
                    outputStream.WriteLine("-----END RSA PRIVATE KEY-----");
                }
            }

            private static void ExportPublicKey(RSA csp, TextWriter outputStream)
            {
                // https://stackoverflow.com/a/28407693
                var parameters = csp.ExportParameters(false);
                using (var stream = new MemoryStream())
                {
                    var writer = new BinaryWriter(stream);
                    writer.Write((byte)0x30); // SEQUENCE
                    using (var innerStream = new MemoryStream())
                    {
                        var innerWriter = new BinaryWriter(innerStream);
                        innerWriter.Write((byte)0x30); // SEQUENCE
                        EncodeLength(innerWriter, 13);
                        innerWriter.Write((byte)0x06); // OBJECT IDENTIFIER
                        var rsaEncryptionOid = new byte[] { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x01 };
                        EncodeLength(innerWriter, rsaEncryptionOid.Length);
                        innerWriter.Write(rsaEncryptionOid);
                        innerWriter.Write((byte)0x05); // NULL
                        EncodeLength(innerWriter, 0);
                        innerWriter.Write((byte)0x03); // BIT STRING
                        using (var bitStringStream = new MemoryStream())
                        {
                            var bitStringWriter = new BinaryWriter(bitStringStream);
                            bitStringWriter.Write((byte)0x00); // # of unused bits
                            bitStringWriter.Write((byte)0x30); // SEQUENCE
                            using (var paramsStream = new MemoryStream())
                            {
                                var paramsWriter = new BinaryWriter(paramsStream);
                                EncodeIntegerBigEndian(paramsWriter, parameters.Modulus); // Modulus
                                EncodeIntegerBigEndian(paramsWriter, parameters.Exponent); // Exponent
                                var paramsLength = (int)paramsStream.Length;
                                EncodeLength(bitStringWriter, paramsLength);
                                bitStringWriter.Write(paramsStream.GetBuffer(), 0, paramsLength);
                            }
                            var bitStringLength = (int)bitStringStream.Length;
                            EncodeLength(innerWriter, bitStringLength);
                            innerWriter.Write(bitStringStream.GetBuffer(), 0, bitStringLength);
                        }
                        var length = (int)innerStream.Length;
                        EncodeLength(writer, length);
                        writer.Write(innerStream.GetBuffer(), 0, length);
                    }

                    var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
                    outputStream.WriteLine("-----BEGIN PUBLIC KEY-----");
                    for (var i = 0; i < base64.Length; i += 64)
                    {
                        outputStream.WriteLine(base64, i, Math.Min(64, base64.Length - i));
                    }
                    outputStream.WriteLine("-----END PUBLIC KEY-----");
                }
            }

            private static void EncodeLength(BinaryWriter stream, int length)
            {
                if (length < 0) throw new ArgumentOutOfRangeException("length", "Length must be non-negative");
                if (length < 0x80)
                {
                    // Short form
                    stream.Write((byte)length);
                }
                else
                {
                    // Long form
                    var temp = length;
                    var bytesRequired = 0;
                    while (temp > 0)
                    {
                        temp >>= 8;
                        bytesRequired++;
                    }
                    stream.Write((byte)(bytesRequired | 0x80));
                    for (var i = bytesRequired - 1; i >= 0; i--)
                    {
                        stream.Write((byte)(length >> (8 * i) & 0xff));
                    }
                }
            }

            private static void EncodeIntegerBigEndian(BinaryWriter stream, byte[] value, bool forceUnsigned = true)
            {
                stream.Write((byte)0x02); // INTEGER
                var prefixZeros = 0;
                for (var i = 0; i < value.Length; i++)
                {
                    if (value[i] != 0) break;
                    prefixZeros++;
                }
                if (value.Length - prefixZeros == 0)
                {
                    EncodeLength(stream, 1);
                    stream.Write((byte)0);
                }
                else
                {
                    if (forceUnsigned && value[prefixZeros] > 0x7f)
                    {
                        // Add a prefix zero to force unsigned if the MSB is 1
                        EncodeLength(stream, value.Length - prefixZeros + 1);
                        stream.Write((byte)0);
                    }
                    else
                    {
                        EncodeLength(stream, value.Length - prefixZeros);
                    }
                    for (var i = prefixZeros; i < value.Length; i++)
                    {
                        stream.Write(value[i]);
                    }
                }
            }

        }

    }
}
