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

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class DirectorySize : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Scans all fills and subfolders of a directory to obtain the size of the directory");
            help.AddValue("<target directory>");
            help.AddExample("MyDirectory");
        }

        protected override void ExecuteInternal()
        {
            var targetDirectory = GetArgValueDirectory(0);

            log.Debug("Scanning for files: " + targetDirectory);
            var files = Util.FileListFiles(targetDirectory, recursive: true).ToList();
            log.Debug($"Found {files.Count} files");

            long totalSize = 0;
            foreach (var file in files)
            {
                var size = Util.FileGetSize(file);
                log.Trace(size.ToString().PadLeft(long.MaxValue.ToString().Length + 1) + " " + file);
                totalSize += size;
            }
            var sizes = new List<(long size, string suffix)>
            {
                (Constant.BYTES_TERA, "TB"),
                (Constant.BYTES_GIGA, "GB"),
                (Constant.BYTES_MEGA, "MB"),
                (Constant.BYTES_KILO, "KB"),
                (-1, "B"),
            };


            foreach (var sz in sizes)
            {
                if (totalSize < 1)
                {
                    log.Info("0");
                    break;
                }
                if (totalSize >= sz.size)
                {
                    var newsize = (double)totalSize / (double)sz.size;
                    newsize = newsize.Round(MidpointRounding.AwayFromZero, 2);
                    log.Info(newsize + " " + sz.suffix);
                    break;
                }
            }

        }
    }
}
