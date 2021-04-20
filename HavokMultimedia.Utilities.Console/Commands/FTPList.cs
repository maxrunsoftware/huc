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

using System.Collections.Generic;
using System.Linq;
using HavokMultimedia.Utilities.Console.External;
using Renci.SshNet.Common;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class FTPList : FTPBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Lists files on a FTP/FTPS/SFTP server");
            help.AddParameter("recursive", "r", "Recursively search for the file (false)");
            help.AddValue("<path>");
            help.AddExample("-h=192.168.1.5 -u=testuser -p=testpass");
            help.AddExample("-e=explicit -h=192.168.1.5 -u=testuser -p=testpass");
            help.AddExample("-e=implicit -h=192.168.1.5 -u=testuser -p=testpass");
            help.AddExample("-e=ssh -h=192.168.1.5 -u=testuser -p=testpass");
            help.AddExample("-h=192.168.1.5 -u=testuser -p=testpass -r `/home/user`");
            help.AddExample("-e=explicit -h=192.168.1.5 -u=testuser -p=testpass -r `/home/user`");
            help.AddExample("-e=implicit -h=192.168.1.5 -u=testuser -p=testpass -r `/home/user`");
            help.AddExample("-e=ssh -h=192.168.1.5 -u=testuser -p=testpass -r `/home/user`");

        }

        protected override void Execute()
        {
            base.Execute();

            var r = GetArgParameterOrConfigBool("recursive", "r", false);
            var path = GetArgValueTrimmed(0);

            using (var c = OpenClient())
            {
                var dirs = new Queue<string>();

                dirs.Enqueue(path);
                while (dirs.Count > 0)
                {
                    var dir = dirs.Dequeue();
                    var msg = "D " + (dir ?? c.WorkingDirectory);

                    try
                    {
                        var enumerator = c.ListFiles(dir);
                        log.Info(msg);
                        foreach (var f in enumerator)
                        {
                            if (r && f.Type == FtpClientFileType.Directory)
                            {
                                if (f.FullName.EndsWith("/..")) continue;
                                if (f.FullName.EndsWith("/.")) continue;
                                dirs.Enqueue(f.FullName);
                            }
                            if (f.Type == FtpClientFileType.File) log.Info("  " + f.FullName);
                        }
                    }
                    catch (SftpPermissionDeniedException pde)
                    {
                        log.Warn(msg + " - " + pde.Message);
                    }
                    catch (SftpPathNotFoundException pnfe)
                    {
                        log.Warn(msg + " - " + pnfe.Message);
                    }
                }
            }
        }
    }
}
