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
using System.IO;
using System.Linq;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class FTPMGet : FTPBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Gets multiple files from a FTP/FTPS/SFTP server");
            help.AddParameter("remotePath", "Remote directory to get files from");
            help.AddParameter("localPath", "Local directory to download files to (" + Path.GetFullPath(Environment.CurrentDirectory) + ")");
            help.AddParameter("filePattern", "Pattern to match files using * and ? as wildcards");
        }

        protected override void Execute()
        {
            base.Execute();

            var remotePath = GetArgParameterOrConfig("remotePath", null);
            if (remotePath != null && remotePath.Last() != '/') remotePath = remotePath + "/";
            log.Debug($"remotePath: {remotePath}");

            var localPath = Path.GetFullPath(GetArgParameterOrConfig("localPath", null, Environment.CurrentDirectory));
            if (!Directory.Exists(localPath)) throw new DirectoryNotFoundException("Could not find " + nameof(localPath) + " directory " + localPath);

            var filePattern = GetArgParameterOrConfig("filePattern", null);
            if (filePattern.TrimOrNull() == null) filePattern = null;
            log.Debug($"filePattern: {filePattern}");

            using (var c = OpenClient())
            {
                foreach (var remoteFile in c.ListFiles(remotePath))
                {
                    if (remoteFile.Type != FtpClientFileType.File) continue;

                    if (filePattern != null && filePattern != "*" && !remoteFile.Name.EqualsWildcard(filePattern, true)) continue;

                    var localFilePath = Path.GetFullPath(Path.Combine(localPath, remoteFile.Name));
                    DeleteExistingFile(localFilePath);
                    log.Debug("Downloading file " + remoteFile.FullName + " to " + localFilePath);
                    c.GetFile(remoteFile.FullName, localFilePath);
                    log.Info(remoteFile.FullName + "  -->  " + localFilePath);
                }
            }
        }
    }
}
