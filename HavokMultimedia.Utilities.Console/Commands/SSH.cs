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
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class SSH : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Execute a SSH statement and/or script remotely");
            help.AddParameter("host", "h", "Hostname");
            help.AddParameter("port", "o", "Port (22)");
            help.AddParameter("username", "u", "Username");
            help.AddParameter("password", "p", "Password");
            //help.AddParameter("bufferSizeMegabytes", "b", "SFTP buffer size in megabytes (10)");

            help.AddParameter("privateKey1File", "pk1", "SFTP private key 1 filename");
            help.AddParameter("privateKey1Password", "pk1pass", "SFTP private key 1 password");
            help.AddParameter("privateKey2File", "pk2", "SFTP private key 2 filename");
            help.AddParameter("privateKey2Password", "pk2pass", "SFTP private key 2 password");
            help.AddParameter("privateKey3File", "pk3", "SFTP private key 3 filename");
            help.AddParameter("privateKey3Password", "pk3pass", "SFTP private key 3 password");

            help.AddParameter("sshScriptFile", "f", "SSH script file to execute");
            help.AddValue("<ssh command>");
        }

        protected override void Execute()
        {
            var host = GetArgParameterOrConfigRequired("host", "h");
            var port = GetArgParameterOrConfigInt("port", "o", 22).ToString().ToUShort();
            var username = GetArgParameterOrConfig("username", "u").TrimOrNull();
            var password = GetArgParameterOrConfig("password", "p").TrimOrNull();
            //var bufferSizeMegabytes = GetArgParameterOrConfigInt("bufferSizeMegabytes", "b", 10).ToString().ToUInt();

            var sshkeys = new List<SshKeyFile>();

            var privateKey1File = GetArgParameterOrConfig("privateKey1File", "pk1").TrimOrNull();
            if (privateKey1File != null)
            {
                privateKey1File = Path.GetFullPath(privateKey1File);
                if (!File.Exists(privateKey1File)) throw new FileNotFoundException("privateKey1File not found", privateKey1File);
            }
            var privateKey1Password = GetArgParameterOrConfig("privateKey1Password", "pk1pass").TrimOrNull();
            if (privateKey1File != null) sshkeys.Add(new SshKeyFile(privateKey1File, privateKey1Password));

            var privateKey2File = GetArgParameterOrConfig("privateKey2File", "pk2").TrimOrNull();
            if (privateKey2File != null)
            {
                privateKey2File = Path.GetFullPath(privateKey2File);
                if (!File.Exists(privateKey2File)) throw new FileNotFoundException("privateKey2File not found", privateKey2File);
            }
            var privateKey2Password = GetArgParameterOrConfig("privateKey2Password", "pk2pass").TrimOrNull();
            if (privateKey2File != null) sshkeys.Add(new SshKeyFile(privateKey2File, privateKey2Password));

            var privateKey3File = GetArgParameterOrConfig("privateKey3File", "pk3").TrimOrNull();
            if (privateKey3File != null)
            {
                privateKey3File = Path.GetFullPath(privateKey3File);
                if (!File.Exists(privateKey3File)) throw new FileNotFoundException("privateKey3File not found", privateKey3File);
            }
            var privateKey3Password = GetArgParameterOrConfig("privateKey3Password", "pk3pass").TrimOrNull();
            if (privateKey3File != null) sshkeys.Add(new SshKeyFile(privateKey3File, privateKey3Password));


            var command = GetArgValues().TrimOrNull().WhereNotNull().FirstOrDefault();
            log.Debug("command: " + command);
            var f = GetArgParameterOrConfig("sshScriptFile", "f").TrimOrNull();

            string fData = null;
            if (f != null) fData = ReadFile(f);
            if (fData.TrimOrNull() != null) log.Debug($"sshScriptFileData: {fData.Length}");

            if (command.TrimOrNull() == null && fData.TrimOrNull() == null) throw new Exception($"No SSH command(s) to execute");
            var commands = (command ?? string.Empty) + Constant.NEWLINE_WINDOWS + (fData ?? string.Empty);
            commands = commands.TrimOrNull();
            if (commands == null) throw new ArgsException("command", "No SSH command(s) to execute");
            log.Debug($"commands: {commands}");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Utilities.Table[] tables = Array.Empty<Utilities.Table>();

            using (var ssh = External.Ssh.CreateSshClient(host, port, username, password, sshkeys))
            {
                ssh.ErrorOccurred += (sender, args) => log.Error("SSH Error", args.Exception);
                ssh.RunCommand(commands);
            }

            stopwatch.Stop();
            var stopwatchtime = stopwatch.Elapsed.TotalSeconds.ToString(MidpointRounding.AwayFromZero, 3);
            log.Info($"Completed SSH command(s) execution in {stopwatchtime} seconds");


            log.Debug("SSH completed");
        }



    }
}
