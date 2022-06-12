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

using System.Linq;
using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class GenerateKeyPair : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Generates a public/private key pair");
            help.AddParameter(nameof(length), "l", "The RSA key length (1024)");
            help.AddValue("<public key file> <private key file>");
            help.AddExample("MyPublicKey.txt MyPrivateKey.txt");
            help.AddExample("-l=4096 MyPublicKey.txt MyPrivateKey.txt");
        }

        private int length;

        protected override void ExecuteInternal()
        {
            length = GetArgParameterOrConfigInt(nameof(length), "l", 1024);

            var publicKeyFile = GetArgValueTrimmed(0);
            log.Debug(nameof(publicKeyFile), publicKeyFile);
            if (publicKeyFile == null) throw new ArgsException(nameof(publicKeyFile), $"No {nameof(publicKeyFile)} specified to save to");

            var privateKeyFile = GetArgValueTrimmed(1);
            log.Debug(nameof(privateKeyFile), privateKeyFile);
            if (privateKeyFile == null) throw new ArgsException(nameof(privateKeyFile), $"No {nameof(privateKeyFile)} specified to save to");

            var keyPair = Encryption.GenerateKeyPair(length: length);

            WriteFile(publicKeyFile, keyPair.publicKey, Constant.ENCODING_UTF8);
            WriteFile(privateKeyFile, keyPair.privateKey, Constant.ENCODING_UTF8);
        }
    }
}
