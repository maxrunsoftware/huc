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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class Ssh : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Execute a SSH statement and/or script remotely");
            help.AddParameter(nameof(host), "h", "Hostname");
            help.AddParameter(nameof(port), "o", "Port (22)");
            help.AddParameter(nameof(username), "u", "Username");
            help.AddParameter(nameof(password), "p", "Password");
            help.AddParameter(nameof(privateKey1File), "pk1", "SFTP private key 1 filename");
            help.AddParameter(nameof(privateKey1Password), "pk1pass", "SFTP private key 1 password");
            help.AddParameter(nameof(privateKey2File), "pk2", "SFTP private key 2 filename");
            help.AddParameter(nameof(privateKey2Password), "pk2pass", "SFTP private key 2 password");
            help.AddParameter(nameof(privateKey3File), "pk3", "SFTP private key 3 filename");
            help.AddParameter(nameof(privateKey3Password), "pk3pass", "SFTP private key 3 password");
            help.AddParameter(nameof(sshScriptFile), "f", "SSH script file to execute");
            help.AddValue("<ssh command>");
            help.AddExample("-h=192.168.1.5 -u=testuser -p=testpass `ls`");
            help.AddExample("-h=192.168.1.5 -u=testuser -p=testpass `cd someDirectory; ls -la;`");
        }

        private string host;
        private ushort port;
        private string username;
        private string password;
        private string privateKey1File;
        private string privateKey1Password;
        private string privateKey2File;
        private string privateKey2Password;
        private string privateKey3File;
        private string privateKey3Password;
        private string sshScriptFile;

        protected override void ExecuteInternal()
        {
            host = GetArgParameterOrConfigRequired(nameof(host), "h");
            port = GetArgParameterOrConfigInt(nameof(port), "o", 22).ToString().ToUShort();
            username = GetArgParameterOrConfig(nameof(username), "u").TrimOrNull();
            password = GetArgParameterOrConfig(nameof(password), "p").TrimOrNull();

            var sshkeys = new List<SshKeyFile>();

            privateKey1File = GetArgParameterOrConfig(nameof(privateKey1File), "pk1").TrimOrNull();
            if (privateKey1File != null)
            {
                privateKey1File = Path.GetFullPath(privateKey1File);
                if (!File.Exists(privateKey1File)) throw new FileNotFoundException(nameof(privateKey1File) + " not found", privateKey1File);
            }
            privateKey1Password = GetArgParameterOrConfig(nameof(privateKey1Password), "pk1pass").TrimOrNull();
            if (privateKey1File != null) sshkeys.Add(new SshKeyFile(privateKey1File, privateKey1Password));

            privateKey2File = GetArgParameterOrConfig(nameof(privateKey2File), "pk2").TrimOrNull();
            if (privateKey2File != null)
            {
                privateKey2File = Path.GetFullPath(privateKey2File);
                if (!File.Exists(privateKey2File)) throw new FileNotFoundException(nameof(privateKey2File) + " not found", privateKey2File);
            }
            privateKey2Password = GetArgParameterOrConfig(nameof(privateKey2Password), "pk2pass").TrimOrNull();
            if (privateKey2File != null) sshkeys.Add(new SshKeyFile(privateKey2File, privateKey2Password));

            privateKey3File = GetArgParameterOrConfig(nameof(privateKey3File), "pk3").TrimOrNull();
            if (privateKey3File != null)
            {
                privateKey3File = Path.GetFullPath(privateKey3File);
                if (!File.Exists(privateKey3File)) throw new FileNotFoundException(nameof(privateKey3File) + " not found", privateKey3File);
            }
            privateKey3Password = GetArgParameterOrConfig(nameof(privateKey3Password), "pk3pass").TrimOrNull();
            if (privateKey3File != null) sshkeys.Add(new SshKeyFile(privateKey3File, privateKey3Password));


            var command = GetArgValueTrimmed(0);
            log.DebugParameter(nameof(command), command);

            sshScriptFile = GetArgParameterOrConfig(nameof(sshScriptFile), "f").TrimOrNull();
            string sshScriptFileData = null;
            if (sshScriptFile != null) sshScriptFileData = ReadFile(sshScriptFile);
            if (sshScriptFileData.TrimOrNull() != null) log.DebugParameter(nameof(sshScriptFileData), sshScriptFileData.Length);

            if (command.TrimOrNull() == null && sshScriptFileData.TrimOrNull() == null) throw new ArgsException(nameof(command), $"No SSH command(s) to execute");
            var commands = (command ?? string.Empty) + Constant.NEWLINE_WINDOWS + (sshScriptFileData ?? string.Empty);
            commands = commands.TrimOrNull();
            if (commands == null) throw new ArgsException(nameof(command), "No SSH command(s) to execute");
            log.DebugParameter(nameof(commands), commands);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            using (var ssh = External.Ssh.CreateSshClient(host, port, username, password, sshkeys))
            {
                ssh.ErrorOccurred += (sender, args) => log.Error("SSH Error", args.Exception);
                using (var cmd = ssh.CreateCommand(commands))
                {
                    cmd.Execute();

                    var result = cmd.Result.TrimOrNull();
                    if (result != null) log.Info(result);

                    result = (new StreamReader(cmd.ExtendedOutputStream)).ReadToEnd().TrimOrNull();
                    if (result != null) log.Warn(result);
                }
            }

            stopwatch.Stop();
            var stopwatchtime = stopwatch.Elapsed.TotalSeconds.ToString(MidpointRounding.AwayFromZero, 3);
            log.Debug($"Completed SSH command(s) execution in {stopwatchtime} seconds");

            log.Debug("SSH completed");
        }
    }
}
