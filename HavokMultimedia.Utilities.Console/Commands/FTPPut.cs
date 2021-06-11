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
using System.IO;
using System.Linq;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class FTPPut : FTPBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Puts files on a FTP/FTPS/SFTP server");
            help.AddParameter(nameof(remotePath), "Remote directory to upload files to");
            help.AddParameter(nameof(ignoreMissingFiles), "Do not error on missing local files (false)");
            help.AddValue("<local file 1> <local file 2> <etc>");
            help.AddExample("-h=192.168.1.5 -u=testuser -p=testpass localfile.txt");
            help.AddExample("-e=explicit -h=192.168.1.5 -u=testuser -p=testpass localfile.txt");
            help.AddExample("-e=implicit -h=192.168.1.5 -u=testuser -p=testpass localfile.txt");
            help.AddExample("-e=ssh -h=192.168.1.5 -u=testuser -p=testpass localfile.txt");
        }

        private string remotePath;
        private bool ignoreMissingFiles;

        protected override void ExecuteInternal()
        {
            base.ExecuteInternal();

            var localFiles = GetArgValuesTrimmed();
            log.Debug(localFiles, nameof(localFiles));
            if (localFiles.IsEmpty()) throw ArgsException.ValueNotSpecified(nameof(localFiles));

            remotePath = GetArgParameterOrConfig(nameof(remotePath), null);
            if (remotePath != null && remotePath.Last() != '/') remotePath = remotePath + "/";
            log.DebugParameter(nameof(remotePath), remotePath);

            ignoreMissingFiles = GetArgParameterOrConfigBool(nameof(ignoreMissingFiles), null, false);

            localFiles = ParseInputFiles(localFiles);
            var localFiles2 = new List<string>();

            foreach (var localFile in localFiles)
            {
                var lf = Path.GetFullPath(localFile);
                if (File.Exists(lf))
                {
                    localFiles2.Add(lf);
                }
                else
                {
                    if (ignoreMissingFiles)
                    {
                        log.Warn($"File does not exist {lf}");
                    }
                    else
                    {
                        throw new FileNotFoundException($"File does not exist {lf}");
                    }
                }
            }
            if (localFiles2.Count < 1)
            {
                log.Warn($"No valid {nameof(localFiles)} provided.");
                return;
            }
            for (var i = 0; i < localFiles2.Count; i++)
            {
                log.Debug($"{nameof(localFiles)}[{i}]: {localFiles2[i]}");
            }


            using (var c = OpenClient())
            {
                foreach (var localFilePath in localFiles2)
                {
                    var localFile = Path.GetFileName(localFilePath);
                    var remoteFilePath = (remotePath ?? string.Empty) + localFile;
                    log.Debug($"Uploading file {localFilePath} to {remoteFilePath}");
                    c.PutFile(remoteFilePath, localFilePath);
                    log.Info(localFilePath + "  -->  " + remoteFilePath);
                }
            }
        }
    }
}
