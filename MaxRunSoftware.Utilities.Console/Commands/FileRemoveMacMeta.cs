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

namespace MaxRunSoftware.Utilities.Console.Commands;

public class FileRemoveMacMeta : Command
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        help.AddSummary("Removes Apple/Mac files created by the system including .DS_Store and ._ files");
        help.AddParameter(nameof(recursive), "r", "Recursively searches for Mac files (false)");
        help.AddParameter(nameof(test), "t", "Don't actually delete files, just list the files that would be deleted (false)");
        help.AddValue("<target directory>");
        help.AddExample("MyDirectory");
        help.AddExample("-r -t SomeDir");
        help.AddDetail("WARNING: Running on an Apple/Mac system may cause unpredictable results and is not recommended");
    }

    private bool recursive;
    private bool test;

    protected override void ExecuteInternal()
    {
        recursive = GetArgParameterOrConfigBool(nameof(recursive), "r", false);
        test = GetArgParameterOrConfigBool(nameof(test), "t", false);

        var targetDirectory = GetArgValueDirectory(0);
        var filesToDelete = Util.FileListFiles(targetDirectory, recursive).Where(IsMacTrash).ToList();
        log.Info("Found " + filesToDelete.Count + " Apple/Mac files that will be deleted");
        foreach (var file in filesToDelete)
        {
            var msg = Util.FileGetSize(file).ToString().PadLeft(Constant.Bytes_Mebi.ToString().Length, ' ') + "  " + file;

            if (test) { log.Info(msg); }
            else
            {
                log.Debug(msg);
                try { File.Delete(file); }
                catch (Exception e)
                {
                    log.Warn("Failed to delete file: " + file);
                    log.Debug(e);
                }
            }
        }
    }

    private bool IsMacTrash(string file)
    {
        if (file == null) return false;

        var filename = Path.GetFileName(file);

        if (filename.EqualsCaseSensitive(".DS_Store"))
        {
            if (Util.FileGetSize(file) <= Constant.Bytes_Mebi) return true;
        }

        if (filename.StartsWith("._"))
        {
            if (Util.FileGetSize(file) == 4096L) return true;
        }

        return false;
    }
}
