﻿/*
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
using System.Linq;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class FTPDelete : FTPBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Deletes a file or files on a FTP/FTPS/SFTP server");
            help.AddValue("<fileToDelete1> <fileToDelete2> <etc>");
            help.AddExample("-h=192.168.1.5 -u=testuser -p=testpass remotefile.txt");
            help.AddExample("-e=explicit -h=192.168.1.5 -u=testuser -p=testpass remotefile.txt");
            help.AddExample("-e=implicit -h=192.168.1.5 -u=testuser -p=testpass remotefile.txt");
            help.AddExample("-e=ssh -h=192.168.1.5 -u=testuser -p=testpass remotefile.txt");
        }

        protected override void Execute()
        {
            base.Execute();

            var filesToDelete = GetArgValues().OrEmpty().TrimOrNull().WhereNotNull().ToList();
            if (filesToDelete.IsEmpty()) throw new ArgsException("fileToDelete", "No files to delete provided");
            for (var i = 0; i < filesToDelete.Count; i++) log.Debug($"filesToDelete[{i}]: {filesToDelete[i]}");

            using (var c = OpenClient())
            {
                foreach (var fileToDelete in filesToDelete)
                {
                    try
                    {
                        c.DeleteFile(fileToDelete);
                        log.Info("Successfully deleted remote file " + fileToDelete);
                    }
                    catch (Exception e)
                    {
                        log.Error("Error attempting to delete remote file " + fileToDelete, e);
                    }
                }
            }
        }
    }
}
