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
            help.AddDetail("Either password or publicKey can be specified but not both");
            help.AddParameter("password", "p", "The password to encrypt the file with");
            help.AddParameter("publicKey", "pk", "The public key file used to encrypt the data");
            help.AddValue("<file to encrypt> <optional new encrypted file>");
        }

        protected override void Execute()
        {
            var values = GetArgValues().TrimOrNull().WhereNotNull().ToList();

            var password = GetArgParameterOrConfig("password", "p").TrimOrNull();
            var publicKeyFile = GetArgParameterOrConfig("publicKey", "pk").TrimOrNull();
            if (password == null && publicKeyFile == null) throw new ArgsException(nameof(password), $"Either password or publicKey must be specified");
            if (password != null && publicKeyFile != null) throw new ArgsException(nameof(password), $"Both password and publicKey can not be specified at the same time");

            var fileToEncrypt = values.GetAtIndexOrDefault(0);
            log.Debug($"{nameof(fileToEncrypt)}: {fileToEncrypt}");
            if (fileToEncrypt == null) throw new ArgsException(nameof(fileToEncrypt), $"No {nameof(fileToEncrypt)} specified to encrypt");
            fileToEncrypt = Path.GetFullPath(fileToEncrypt);
            CheckFileExists(fileToEncrypt);
            log.Debug($"{nameof(fileToEncrypt)}: {fileToEncrypt}");

            var encryptedFile = values.GetAtIndexOrDefault(1);
            if (encryptedFile == null) encryptedFile = fileToEncrypt;
            log.Debug($"{nameof(encryptedFile)}: {encryptedFile}");

            var fileToEncryptData = ReadFileBinary(fileToEncrypt);
            byte[] encryptedData;
            if (password != null)
            {
                var passwordBytes = Constant.ENCODING_UTF8_WITHOUT_BOM.GetBytes(password);
                encryptedData = Encryption.Encrypt(passwordBytes, fileToEncryptData);
            }
            else
            {
                var publicKey = ReadFile(publicKeyFile);
                encryptedData = Encryption.Encrypt(publicKey, fileToEncryptData);
            }
            WriteFileBinary(encryptedFile, encryptedData);
            log.Info("Created encrypted file " + encryptedFile);
        }
    }
}