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
            help.AddDetail("Either password or privateKey can be specified but not both");
            help.AddParameter("password", "p", "The password to encrypt the file with");
            help.AddParameter("privateKey", "pk", "The private key file used to decrypt the data");
            help.AddValue("<file to decrypt> <optional new decrypted file>");
            help.AddExample("-p=password data.encrypted dataDecrypted.txt");
            help.AddExample("-pk=MyPrivateKey.txt data.encrypted dataDecrypted.txt");
        }

        protected override void ExecuteInternal()
        {
            var password = GetArgParameterOrConfig("password", "p").TrimOrNull();
            var privateKeyFile = GetArgParameterOrConfig("privateKey", "pk");
            if (password == null && privateKeyFile == null) throw new ArgsException(nameof(password), $"Either password or privateKey must be specified");
            if (password != null && privateKeyFile != null) throw new ArgsException(nameof(password), $"Both password and privateKey can not be specified at the same time");

            var fileToDecrypt = GetArgValueTrimmed(0);
            log.Debug($"{nameof(fileToDecrypt)}: {fileToDecrypt}");
            if (fileToDecrypt == null) throw new ArgsException(nameof(fileToDecrypt), $"No <{nameof(fileToDecrypt)}> specified to decrypt");
            fileToDecrypt = Path.GetFullPath(fileToDecrypt);
            CheckFileExists(fileToDecrypt);
            log.Debug($"{nameof(fileToDecrypt)}: {fileToDecrypt}");

            var decryptedFile = GetArgValueTrimmed(1);
            if (decryptedFile == null) decryptedFile = fileToDecrypt;
            log.Debug($"{nameof(decryptedFile)}: {decryptedFile}");

            var fileToDecryptData = ReadFileBinary(fileToDecrypt);
            byte[] decryptedData;
            if (password != null)
            {
                var passwordBytes = Constant.ENCODING_UTF8_WITHOUT_BOM.GetBytes(password);
                decryptedData = Encryption.Decrypt(passwordBytes, fileToDecryptData);
            }
            else
            {
                var privateKey = ReadFile(privateKeyFile);
                decryptedData = Encryption.Decrypt(privateKey, fileToDecryptData);
            }
            WriteFileBinary(decryptedFile, decryptedData);
            log.Info("Created decrypted file " + decryptedFile);
        }
    }
}
