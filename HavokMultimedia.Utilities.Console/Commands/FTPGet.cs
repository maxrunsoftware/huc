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
using System.IO;
using System.Linq;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class FTPGet : FTPBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Gets a file from a FTP/FTPS/SFTP server");
            help.AddParameter("localPath", "Local directory to download files to (" + Path.GetFullPath(Environment.CurrentDirectory) + ")");
            help.AddParameter("ignoreMissingFiles", "Do not error on missing remote files (false)");
            help.AddParameter("search", "Recursively search for the file (false)");
            help.AddValue("<remote file 1> <remote file 2> <etc>");
        }

        private FtpClientFile FindFile(IFtpClient ftp, string fileName)
        {
            var dirs = new Queue<FtpClientFile>();
            var root = new FtpClientFile("", "", FtpClientFileType.Directory);
            dirs.Enqueue(root);

            while (dirs.Count > 0)
            {
                var dir = dirs.Dequeue();
                var objects = ftp.ListFiles(dir.FullName).ToList();
                foreach (var d in objects.Where(o => o.Type == FtpClientFileType.Directory)) dirs.Enqueue(d);
                foreach (var f in objects.Where(o => o.Type == FtpClientFileType.File)) if (string.Equals(f.Name, fileName)) return f;
                foreach (var f in objects.Where(o => o.Type == FtpClientFileType.File)) if (string.Equals(f.Name, fileName, StringComparison.OrdinalIgnoreCase)) return f;
            }

            return null;
        }

        protected override void Execute()
        {
            base.Execute();

            var remoteFiles = GetArgValues().OrEmpty().TrimOrNull().WhereNotNull().ToList();
            if (remoteFiles.IsEmpty()) throw new ArgsException("remoteFiles", "No remote files provided");
            for (var i = 0; i < remoteFiles.Count; i++) log.Debug($"remoteFile[{i}]: {remoteFiles[i]}");


            var localPath = Path.GetFullPath(GetArgParameterOrConfig("localPath", null, Environment.CurrentDirectory));
            var ignoreMissingFiles = GetArgParameterOrConfigBool("ignoreMissingFiles", null, false);
            var search = GetArgParameterOrConfigBool("search", null, false);

            using (var c = OpenClient())
            {
                foreach (var rfp in remoteFiles)
                {
                    var remoteFilePath = rfp;
                    var remoteFileName = ParseFileNameFromPath(remoteFilePath);
                    var localFileName = remoteFileName;
                    var localFilePath = Path.GetFullPath(Path.Combine(localPath, localFileName));

                    if (search)
                    {
                        var file = FindFile(c, remoteFileName);
                        if (file != null)
                        {
                            remoteFilePath = file.FullName;
                            remoteFileName = file.Name;
                        }
                    }

                    log.Debug($"Downloading file {remoteFilePath} to {localFilePath}");
                    DeleteExistingFile(localFilePath);
                    if (ignoreMissingFiles)
                    {
                        try
                        {
                            c.GetFile(remoteFilePath, localFilePath);
                            log.Info(remoteFilePath + "  -->  " + localFilePath);
                        }
                        catch (Exception e)
                        {
                            log.Warn($"ERROR downloading file {remoteFilePath} to {localFilePath}", e);
                        }
                    }
                    else
                    {
                        c.GetFile(remoteFilePath, localFilePath);
                        log.Info(remoteFilePath + "  -->  " + localFilePath);
                    }
                }
            }
        }
    }
}
