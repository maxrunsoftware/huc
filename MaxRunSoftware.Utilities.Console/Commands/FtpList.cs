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

using System.Collections.Generic;
using MaxRunSoftware.Utilities.External;
using Renci.SshNet.Common;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class FtpList : FtpBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Lists files on a FTP/FTPS/SFTP server");
        help.AddParameter(nameof(recursive), "r", "Recursively search for the file (false)");
        help.AddValue("<path>");
        help.AddExample(HelpExamplePrefix);
        help.AddExample(HelpExamplePrefix + " -e=explicit");
        help.AddExample(HelpExamplePrefix + " -e=implicit");
        help.AddExample(HelpExamplePrefix + " -e=ssh");
        help.AddExample(HelpExamplePrefix + " -r `/home/user`");
        help.AddExample(HelpExamplePrefix + " -e=explicit -r `/home/user`");
        help.AddExample(HelpExamplePrefix + " -e=implicit -r `/home/user`");
        help.AddExample(HelpExamplePrefix + " -e=ssh -r `/home/user`");
    }

    private bool recursive;

    protected override void ExecuteInternal()
    {
        base.ExecuteInternal();

        recursive = GetArgParameterOrConfigBool(nameof(recursive), "r", false);
        var path = GetArgValueTrimmed(0);
        log.DebugParameter(nameof(path), path);

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
                        if (recursive && f.Type == FtpClientFileType.Directory)
                        {
                            if (f.FullName.EndsWith("/..")) continue;

                            if (f.FullName.EndsWith("/.")) continue;

                            dirs.Enqueue(f.FullName);
                        }

                        if (f.Type == FtpClientFileType.File) log.Info("  " + f.FullName);
                    }
                }
                catch (SftpPermissionDeniedException permissionDeniedException) { log.Warn(msg + " - " + permissionDeniedException.Message); }
                catch (SftpPathNotFoundException pathNotFoundException) { log.Warn(msg + " - " + pathNotFoundException.Message); }
            }
        }
    }
}
