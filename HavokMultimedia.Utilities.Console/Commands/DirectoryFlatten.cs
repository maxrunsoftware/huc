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

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class DirectoryFlatten : Command
    {
        private enum ConflictResolution { LeaveAsIs, KeepNewest }
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Scans all subfolders in target directory and moves the files in those subdirectories up to the target directory");
            help.AddParameter("conflictResolution", "c", "The conflict resolution to use when multiple files with the same name exist (" + nameof(ConflictResolution.LeaveAsIs) + ") " + DisplayEnumOptions<ConflictResolution>());
            help.AddValue("<target directory>");
            help.AddExample("MyDirectory");
        }

        protected override void ExecuteInternal()
        {
            //var encoding = GetArgParameterOrConfigEncoding("encoding", "en");
            var targetDirectory = GetArgValueTrimmed(0);
            targetDirectory.CheckValueNotNull(nameof(targetDirectory), log);
            targetDirectory = Path.GetFullPath(targetDirectory);
            log.DebugParameter(nameof(targetDirectory), targetDirectory);
            if (!Directory.Exists(targetDirectory)) throw new ArgsException(nameof(targetDirectory), $"<{nameof(targetDirectory)}> does not exist {targetDirectory}");

            var conflictResolution = GetArgParameterOrConfigEnum("conflictResolution", "c", ConflictResolution.LeaveAsIs);
            var subdirs = Util.FileListDirectories(targetDirectory, recursive: true)
                .Select(o => Path.GetFullPath(o))
                .Where(o => !o.EqualsCaseInsensitive(targetDirectory))
                .OrderBy(o => o, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var subfiles = new List<string>();
            foreach (var subdir in subdirs)
            {
                foreach (var subfile in Util.FileListFiles(subdir, recursive: false))
                {
                    subfiles.Add(subfile);
                }
            }

            foreach (var sourceFile in subfiles)
            {
                var sourceFileName = Path.GetFileName(sourceFile);
                var targetFile = Path.GetFullPath(Path.Combine(targetDirectory, sourceFileName));
                if (File.Exists(targetFile))
                {
                    log.Debug("Conflict: " + sourceFile);
                    if (conflictResolution == ConflictResolution.LeaveAsIs)
                    {
                        log.Info("Leaving file " + sourceFile + " as is");
                    }
                    else if (conflictResolution == ConflictResolution.KeepNewest)
                    {
                        var sourceFileTimestamp = File.GetLastWriteTimeUtc(sourceFile);
                        var targetFileTimestamp = File.GetLastWriteTimeUtc(targetFile);
                        if (sourceFileTimestamp >= targetFileTimestamp)
                        {
                            log.Info("Moving: " + sourceFile + "  -->  " + targetFile);
                            File.Move(sourceFile, targetFile, true);
                        }
                        else
                        {
                            log.Info("Deleting: " + sourceFile);
                            File.Delete(sourceFile);
                        }
                    }
                    else
                    {
                        throw new NotImplementedException(nameof(conflictResolution) + " [" + conflictResolution + "] has not been implemented yet");
                    }
                }
                else
                {
                    log.Info("Moving: " + sourceFile + "  -->  " + targetFile);
                    File.Move(sourceFile, targetFile, false);
                }
            }



        }
    }
}
