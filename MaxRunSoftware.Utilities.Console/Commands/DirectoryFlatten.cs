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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class DirectoryFlatten : Command
{
    private enum ConflictResolution
    {
        LeaveAsIs,
        KeepNewest
    }

    protected override void CreateHelp(CommandHelpBuilder help)
    {
        help.AddSummary("Scans all subfolders in target directory and moves the files in those subdirectories up to the target directory");
        help.AddParameter(nameof(conflictResolution), "c", "The conflict resolution to use when multiple files with the same name exist (" + nameof(ConflictResolution.LeaveAsIs) + ") " + DisplayEnumOptions<ConflictResolution>());
        help.AddValue("<target directory>");
        help.AddExample("MyDirectory");
    }

    private ConflictResolution conflictResolution;

    protected override void ExecuteInternal()
    {
        //var encoding = GetArgParameterOrConfigEncoding("encoding", "en");
        var targetDirectory = GetArgValueDirectory(0);

        conflictResolution = GetArgParameterOrConfigEnum(nameof(conflictResolution), "c", ConflictResolution.LeaveAsIs);
        var subDirectories = Util.FileListDirectories(targetDirectory, true)
            .Select(Path.GetFullPath)
            .Where(o => !o.EqualsCaseInsensitive(targetDirectory))
            .OrderBy(o => o, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var subFiles = new List<string>();
        foreach (var subDirectory in subDirectories)
        foreach (var subFile in Util.FileListFiles(subDirectory)) { subFiles.Add(subFile); }

        foreach (var sourceFile in subFiles)
        {
            var sourceFileName = Path.GetFileName(sourceFile);
            var targetFile = Path.GetFullPath(Path.Combine(targetDirectory, sourceFileName));
            if (File.Exists(targetFile))
            {
                log.Debug("Conflict: " + sourceFile);
                if (conflictResolution == ConflictResolution.LeaveAsIs) { log.Info("Leaving file " + sourceFile + " as is"); }
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
                else { throw new NotImplementedException(nameof(conflictResolution) + " [" + conflictResolution + "] has not been implemented yet"); }
            }
            else
            {
                log.Info("Moving: " + sourceFile + "  -->  " + targetFile);
                File.Move(sourceFile, targetFile, false);
            }
        }
    }
}
