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

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class FTPDelete : FTPBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Deletes a file or files on a FTP/FTPS/SFTP server");
            help.AddValue("<fileToDelete1> <fileToDelete2> <etc>");
            help.AddExample(HelpExamplePrefix + " remotefile.txt");
            help.AddExample(HelpExamplePrefix + " -e=explicit remotefile.txt");
            help.AddExample(HelpExamplePrefix + " -e=implicit remotefile.txt");
            help.AddExample(HelpExamplePrefix + " -e=ssh remotefile.txt");
        }

        protected override void ExecuteInternal()
        {
            base.ExecuteInternal();

            var filesToDelete = GetArgValuesTrimmed();
            if (filesToDelete.IsEmpty()) throw ArgsException.ValueNotSpecified(nameof(filesToDelete));
            log.Debug(filesToDelete, nameof(filesToDelete));

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
