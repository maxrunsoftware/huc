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
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class FTPList : FTPBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Lists files on a FTP/FTPS/SFTP server");
            help.AddParameter("recursive", "r", "Recursively search for the file (false)");
        }

        protected override void Execute()
        {
            base.Execute();



            var r = GetArgParameterOrConfigBool("recursive", "r", false);

            using (var c = OpenClient())
            {
                var dirs = new Queue<string>();
                dirs.Enqueue(null);
                while (dirs.Count > 0)
                {
                    var dir = dirs.Dequeue();
                    log.Info("D " + (dir ?? c.WorkingDirectory));
                    foreach (var f in c.ListFiles(dir))
                    {
                        if (r && f.Type == FtpClientFileType.Directory) dirs.Enqueue(f.FullName);
                        if (f.Type == FtpClientFileType.File) log.Info("  " + f.FullName);
                    }
                }
            }
        }
    }
}
