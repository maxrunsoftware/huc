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
using System.IO;
using System.Security.Cryptography;
using SecurityDriven.Inferno;

namespace MaxRunSoftware.Utilities.External;

public class Encryption
{
    private static byte[] RandomBytes(int length)
    {
        var array = new byte[length];
        using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(array);
        return array;
    }

    public static byte[] EncryptAsymetric(string pemPublicKey, byte[] data) => Util.EncryptionEncryptAsymetric(pemPublicKey, data, RSAEncryptionPadding.OaepSHA512);

    public static byte[] DecryptAsymetric(string pemPrivateKey, byte[] data) => Util.EncryptionDecryptAsymetric(pemPrivateKey, data, RSAEncryptionPadding.OaepSHA512);

    public static byte[] EncryptSymetric(byte[] password, byte[] data, byte[] salt = null) => SuiteB.Encrypt(password, data, salt);

    public static byte[] DecryptSymetric(byte[] password, byte[] data, byte[] salt = null) => SuiteB.Decrypt(password, data, salt);

    public static (string publicKey, string privateKey) GenerateKeyPair(int length) => Util.EncryptionGeneratePublicPrivateKeys(length);

    public static byte[] Encrypt(string publicKey, byte[] data)
    {
        byte[] password = RandomBytes(256);

        var encryptedData = EncryptSymetric(password, data);
        var encryptedPass = EncryptAsymetric(publicKey, password);
        if (encryptedPass.Length != 512) throw new Exception("Expecting encrypted password length of 512 but was " + encryptedPass.Length);
        var result = encryptedPass.Append(encryptedData);

        return result;
    }

    public static byte[] Decrypt(string privateKey, byte[] data)
    {
        var (encryptedPass, encryptedData) = data.Split(512);

        var decryptedPass = DecryptAsymetric(privateKey, encryptedPass);
        var decryptedData = DecryptSymetric(decryptedPass, encryptedData);

        return decryptedData;
    }

    public static byte[] Encrypt(byte[] password, byte[] data)
    {
        byte[] salt = new byte[256];

        var encryptedData = EncryptSymetric(password, data, salt);

        var result = salt.Append(encryptedData);

        return result;
    }

    public static byte[] Decrypt(byte[] password, byte[] data)
    {
        var (salt, encryptedData) = data.Split(256);

        var decryptedData = DecryptSymetric(password, encryptedData, salt);

        return decryptedData;
    }

}