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

using System.IO;
using System.Linq;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class FileEncrypt : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Encrypts a file");
            help.AddParameter("publicKey", "pk", "The public key file used to encrypt the data");
            help.AddValue("<file to encrypt> <optional new encrypted file>");
        }

        protected override void Execute()
        {
            var values = GetArgValues().TrimOrNull().WhereNotNull().ToList();
            var publicKeyFile = GetArgParameterOrConfigRequired("publicKey", "pk");
            publicKeyFile = Path.GetFullPath(publicKeyFile);
            CheckFileExists(publicKeyFile);
            var publicKey = ReadFile(publicKeyFile);

            var fileToEncrypt = values.GetAtIndexOrDefault(0);
            log.Debug($"{nameof(fileToEncrypt)}: {fileToEncrypt}");
            if (fileToEncrypt == null) throw new ArgsException(nameof(fileToEncrypt), $"No {nameof(fileToEncrypt)} specified to encrypt");
            fileToEncrypt = Path.GetFullPath(fileToEncrypt);
            CheckFileExists(fileToEncrypt);
            log.Debug($"{nameof(fileToEncrypt)}: {fileToEncrypt}");

            var encryptedFile = values.GetAtIndexOrDefault(1);
            if (encryptedFile == null) encryptedFile = fileToEncrypt;
            log.Debug($"{nameof(encryptedFile)}: {encryptedFile}");

            var fileToEncryptData = Util.FileRead(fileToEncrypt);
            var encryptedData = Encryption.Encrypt(publicKey, fileToEncryptData);
            Util.FileWrite(encryptedFile, encryptedData);
        }
    }
}