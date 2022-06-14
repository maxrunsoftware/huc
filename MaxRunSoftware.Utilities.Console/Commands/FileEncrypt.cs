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

using System.IO;
using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class FileEncrypt : Command
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        help.AddSummary("Encrypts a file");
        help.AddDetail("Either password or publicKey can be specified but not both");
        help.AddParameter(nameof(password), "p", "The password to encrypt the file with");
        help.AddParameter(nameof(publicKey), "pk", "The public key file used to encrypt the data");
        help.AddValue("<file to encrypt> <optional new encrypted file>");
        help.AddExample("-p=password data.txt data.encrypted");
        help.AddExample("-pk=MyPublicKey.txt data.txt data.encrypted");
    }

    private string password;
    private string publicKey;

    protected override void ExecuteInternal()
    {
        password = GetArgParameterOrConfig(nameof(password), "p").TrimOrNull();
        publicKey = GetArgParameterOrConfig(nameof(publicKey), "pk").TrimOrNull();
        if (password == null && publicKey == null) throw new ArgsException(nameof(password), "Either password or publicKey must be specified");

        if (password != null && publicKey != null) throw new ArgsException(nameof(password), "Both password and publicKey can not be specified at the same time");

        var fileToEncrypt = GetArgValueTrimmed(0);
        fileToEncrypt.CheckValueNotNull(nameof(fileToEncrypt), log);
        fileToEncrypt = Path.GetFullPath(fileToEncrypt);
        log.DebugParameter(nameof(fileToEncrypt), fileToEncrypt);
        CheckFileExists(fileToEncrypt);

        var encryptedFile = GetArgValueTrimmed(1) ?? fileToEncrypt;
        log.DebugParameter(nameof(encryptedFile), encryptedFile);

        var fileToEncryptData = ReadFileBinary(fileToEncrypt);
        var encryptedData = password != null
            ? Encryption.Encrypt(Constant.ENCODING_UTF8.GetBytes(password), fileToEncryptData)
            : Encryption.Encrypt(ReadFile(publicKey), fileToEncryptData);

        WriteFileBinary(encryptedFile, encryptedData);
        log.Info("Created encrypted file " + encryptedFile);
    }
}
