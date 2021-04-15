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
            help.AddParameter("base64", "b64", "Was the file base 64 encoded (false)");
            help.AddValue("<private key file> <file to decrypt> <optional new decrypted file>");
        }

        protected override void Execute()
        {
            var values = GetArgValues().TrimOrNull().WhereNotNull().ToList();
            var base64 = GetArgParameterOrConfigBool("base64", "b64", false);

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

            var decryptedFile = values.GetAtIndexOrDefault(2);
            log.Debug($"{nameof(decryptedFile)}: {decryptedFile}");
            if (decryptedFile == null) decryptedFile = fileToDecrypt;
            log.Debug($"{nameof(decryptedFile)}: {decryptedFile}");

            var fileToDecryptData = Util.FileRead(fileToDecrypt);
            if (base64)
            {
                var base64DataString = Constant.ENCODING_UTF8_WITHOUT_BOM.GetString(fileToDecryptData);
                fileToDecryptData = Util.Base64(base64DataString);
            }

            var decryptedData = Encryption.Decrypt(privateKey, fileToDecryptData);
            Util.FileWrite(decryptedFile, decryptedData);
        }
    }
}
