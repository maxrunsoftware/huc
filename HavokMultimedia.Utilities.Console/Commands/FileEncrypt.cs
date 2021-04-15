/*
Copyright (c) 2021 Steven Foster (steven.d.foster@gmail.com)

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
using System.Linq;
using System.Security.Cryptography;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class FileEncrypt : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Encrypts a file");
            help.AddValue("<public key file> <file to encrypt> <encrypted file>");
        }

        protected override void Execute()
        {
            var values = GetArgValues().TrimOrNull().WhereNotNull().ToList();

            var publicKeyFile = values.GetAtIndexOrDefault(0);
            log.Debug($"{nameof(publicKeyFile)}: {publicKeyFile}");
            if (publicKeyFile == null) throw new ArgsException(nameof(publicKeyFile), $"No {nameof(publicKeyFile)} specified");
            publicKeyFile = Path.GetFullPath(publicKeyFile);
            CheckFileExists(publicKeyFile);
            log.Debug($"{nameof(publicKeyFile)}: {publicKeyFile}");
            var publicKey = ReadFile(publicKeyFile, Constant.ENCODING_UTF8_WITHOUT_BOM);

            var fileToEncrypt = values.GetAtIndexOrDefault(1);
            log.Debug($"{nameof(fileToEncrypt)}: {fileToEncrypt}");
            if (fileToEncrypt == null) throw new ArgsException(nameof(fileToEncrypt), $"No {nameof(fileToEncrypt)} specified to encrypt");
            fileToEncrypt = Path.GetFullPath(fileToEncrypt);
            CheckFileExists(fileToEncrypt);
            log.Debug($"{nameof(fileToEncrypt)}: {fileToEncrypt}");
            var fileToEncryptData = Util.FileRead(fileToEncrypt);

            var encryptedFile = values.GetAtIndexOrDefault(2);
            log.Debug($"{nameof(encryptedFile)}: {encryptedFile}");
            if (encryptedFile == null) throw new ArgsException(nameof(encryptedFile), $"No {nameof(encryptedFile)} specified to output to");
            encryptedFile = Path.GetFullPath(encryptedFile);
            log.Debug($"{nameof(encryptedFile)}: {encryptedFile}");

            var encryptedData = Encryption.Encrypt(publicKey, fileToEncryptData);
            var encryptedDataBase64 = Util.Base64(encryptedData);
            Util.FileWrite(encryptedFile, encryptedDataBase64, Constant.ENCODING_UTF8_WITHOUT_BOM);


        }
    }
}