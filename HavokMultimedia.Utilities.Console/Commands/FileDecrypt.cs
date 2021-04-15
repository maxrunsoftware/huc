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
    public class FileDecrypt : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Decrypts a file");
            help.AddValue("<private key file> <file to decrypt> <decrypted file>");
        }

        protected override void Execute()
        {
            var values = GetArgValues().TrimOrNull().WhereNotNull().ToList();

            var privateKeyFile = values.GetAtIndexOrDefault(0);
            log.Debug($"{nameof(privateKeyFile)}: {privateKeyFile}");
            if (privateKeyFile == null) throw new ArgsException(nameof(privateKeyFile), $"No {nameof(privateKeyFile)} specified");
            privateKeyFile = Path.GetFullPath(privateKeyFile);
            CheckFileExists(privateKeyFile);
            log.Debug($"{nameof(privateKeyFile)}: {privateKeyFile}");
            var privateKey = ReadFile(privateKeyFile, Constant.ENCODING_UTF8_WITHOUT_BOM);

            var fileToDecrypt = values.GetAtIndexOrDefault(1);
            log.Debug($"{nameof(fileToDecrypt)}: {fileToDecrypt}");
            if (fileToDecrypt == null) throw new ArgsException(nameof(fileToDecrypt), $"No {nameof(fileToDecrypt)} specified to decrypt");
            fileToDecrypt = Path.GetFullPath(fileToDecrypt);
            CheckFileExists(fileToDecrypt);
            log.Debug($"{nameof(fileToDecrypt)}: {fileToDecrypt}");
            var fileToDecryptData = Util.FileRead(fileToDecrypt, Constant.ENCODING_UTF8_WITHOUT_BOM);

            var decryptedFile = values.GetAtIndexOrDefault(2);
            log.Debug($"{nameof(decryptedFile)}: {decryptedFile}");
            if (decryptedFile == null) throw new ArgsException(nameof(decryptedFile), $"No {nameof(decryptedFile)} specified to output to");
            decryptedFile = Path.GetFullPath(decryptedFile);
            log.Debug($"{nameof(decryptedFile)}: {decryptedFile}");

            var fileToDecryptDataBinary = Util.Base64(fileToDecryptData);
            var decryptedData = Encryption.Decrypt(privateKey, fileToDecryptDataBinary);
            Util.FileWrite(decryptedFile, decryptedData);
        }
    }
}
