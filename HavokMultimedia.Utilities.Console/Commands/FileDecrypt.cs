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
    public class FileDecrypt : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Decrypts a file");
            help.AddParameter("privateKey", "pk", "The private key used to decrypt the data");
            help.AddValue("<file to decrypt> <optional new decrypted file>");
        }

        protected override void Execute()
        {
            var values = GetArgValues().TrimOrNull().WhereNotNull().ToList();
            var privateKeyFile = GetArgParameterOrConfigRequired("privateKey", "pk");
            privateKeyFile = Path.GetFullPath(privateKeyFile);
            CheckFileExists(privateKeyFile);
            var privateKey = ReadFile(privateKeyFile);

            var fileToDecrypt = values.GetAtIndexOrDefault(0);
            log.Debug($"{nameof(fileToDecrypt)}: {fileToDecrypt}");
            if (fileToDecrypt == null) throw new ArgsException(nameof(fileToDecrypt), $"No {nameof(fileToDecrypt)} specified to decrypt");
            fileToDecrypt = Path.GetFullPath(fileToDecrypt);
            CheckFileExists(fileToDecrypt);
            log.Debug($"{nameof(fileToDecrypt)}: {fileToDecrypt}");

            var decryptedFile = values.GetAtIndexOrDefault(1);
            if (decryptedFile == null) decryptedFile = fileToDecrypt;
            log.Debug($"{nameof(decryptedFile)}: {decryptedFile}");

            var fileToDecryptData = Util.FileRead(fileToDecrypt);
            var decryptedData = Encryption.Decrypt(privateKey, fileToDecryptData);
            Util.FileWrite(decryptedFile, decryptedData);
        }
    }
}
