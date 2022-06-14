/*
Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)

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

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class DirectoryList : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Scans all files and directories in target directory and exports that information to a tab delimited file");
            help.AddParameter(nameof(creationTime), "ct", "Creation time (false)");
            help.AddParameter(nameof(creationTimeUtc), "ctu", "Creation time UTC (false)");
            help.AddParameter(nameof(lastAccessTime), "lat", "Last access time (false)");
            help.AddParameter(nameof(lastAccessTimeUtc), "latu", "Last access time UTC (false)");
            help.AddParameter(nameof(lastWriteTime), "lwt", "Last write time (false)");
            help.AddParameter(nameof(lastWriteTimeUtc), "lwtu", "Last write time UTC (false)");
            help.AddParameter(nameof(name), "n", "Name (false)");
            help.AddParameter(nameof(nameFull), "nf", "Full path and name (false)");
            help.AddParameter(nameof(nameRelative), "nr", "Full path and name minus the <directory> path (false)");
            help.AddParameter(nameof(path), "p", "Full path (false)");
            help.AddParameter(nameof(pathRelative), "pr", "Full path minus the <directory> path (false)");
            help.AddParameter(nameof(parentName), "pn", "The singular name of the parent directory (false)");
            help.AddParameter(nameof(type), "t", "Whether this is a File or Directory or Unknown (false)");
            help.AddParameter(nameof(size), "s", "Size in bytes (false)");
            help.AddParameter(nameof(recursiveDepth), "rd", "The depth to recursively scan subdirectories (0)");
            help.AddParameter(nameof(pattern), "pat", "The name pattern to match on (*)");
            help.AddParameter(nameof(includeFiles), "if", "Whether to include files (true)");
            help.AddParameter(nameof(includeDirectories), "id", "Whether to include directories (true)");
            help.AddValue("<directory> <tab delimited filename>");
            help.AddExample("MyDirectory mydata.txt");
            help.AddExample("C:\\windows mywindata.txt");
        }

        private bool creationTime;
        private bool creationTimeUtc;
        private bool lastAccessTime;
        private bool lastAccessTimeUtc;
        private bool lastWriteTime;
        private bool lastWriteTimeUtc;
        private bool name;
        private bool nameFull;
        private bool nameRelative;
        private bool path;
        private bool pathRelative;
        private bool parentName;
        private bool type;
        private bool size;
        private int recursiveDepth;
        private string pattern;
        private bool includeFiles;
        private bool includeDirectories;

        protected override void ExecuteInternal()
        {
            creationTime = GetArgParameterOrConfigBool(nameof(creationTime), "ct", false);
            creationTimeUtc = GetArgParameterOrConfigBool(nameof(creationTimeUtc), "ctu", false);
            lastAccessTime = GetArgParameterOrConfigBool(nameof(lastAccessTime), "lat", false);
            lastAccessTimeUtc = GetArgParameterOrConfigBool(nameof(lastAccessTimeUtc), "latu", false);
            lastWriteTime = GetArgParameterOrConfigBool(nameof(lastWriteTime), "lwt", false);
            lastWriteTimeUtc = GetArgParameterOrConfigBool(nameof(lastWriteTimeUtc), "lwtu", false);
            name = GetArgParameterOrConfigBool(nameof(name), "n", false);
            nameFull = GetArgParameterOrConfigBool(nameof(nameFull), "nf", false);
            nameRelative = GetArgParameterOrConfigBool(nameof(nameRelative), "nr", false);
            path = GetArgParameterOrConfigBool(nameof(path), "p", false);
            pathRelative = GetArgParameterOrConfigBool(nameof(pathRelative), "pr", false);
            parentName = GetArgParameterOrConfigBool(nameof(parentName), "pn", false);
            type = GetArgParameterOrConfigBool(nameof(type), "t", false);
            size = GetArgParameterOrConfigBool(nameof(size), "s", false);
            recursiveDepth = GetArgParameterOrConfigInt(nameof(recursiveDepth), "rd", 0);
            pattern = GetArgParameterOrConfig(nameof(pattern), "pat", "*");
            includeFiles = GetArgParameterOrConfigBool(nameof(includeFiles), "if", true);
            includeDirectories = GetArgParameterOrConfigBool(nameof(includeDirectories), "id", true);

            var targetDirectory = GetArgValueTrimmed(0);
            log.DebugParameter(nameof(targetDirectory), targetDirectory);
            if (targetDirectory == null) throw ArgsException.ValueNotSpecified(nameof(targetDirectory));
            targetDirectory = Path.GetFullPath(targetDirectory);
            log.DebugParameter(nameof(targetDirectory), targetDirectory);

            var targetTabFile = GetArgValueTrimmed(1);
            log.DebugParameter(nameof(targetTabFile), targetTabFile);
            if (targetTabFile == null) throw ArgsException.ValueNotSpecified(nameof(targetTabFile));
            targetTabFile = Path.GetFullPath(targetTabFile);
            log.DebugParameter(nameof(targetTabFile), targetTabFile);

            var header = new List<string>();
            if (creationTime) header.Add(nameof(creationTime));
            if (creationTimeUtc) header.Add(nameof(creationTimeUtc));
            if (lastAccessTime) header.Add(nameof(lastAccessTime));
            if (lastAccessTimeUtc) header.Add(nameof(lastAccessTimeUtc));
            if (lastWriteTime) header.Add(nameof(lastWriteTime));
            if (lastWriteTimeUtc) header.Add(nameof(lastWriteTimeUtc));
            if (name) header.Add(nameof(name));
            if (nameFull) header.Add(nameof(nameFull));
            if (nameRelative) header.Add(nameof(nameRelative));
            if (path) header.Add(nameof(path));
            if (pathRelative) header.Add(nameof(pathRelative));
            if (parentName) header.Add(nameof(parentName));
            if (type) header.Add(nameof(type));
            if (size) header.Add(nameof(size));

            var tableList = new List<List<string>>();
            tableList.Add(header);

            var listing = Util.FileList(targetDirectory, recursiveDepth != 0); // TODO: Implement custom recursive depth searching
            foreach (var listingEntry in listing)
            {
                var row = new List<string>();

                var fullPath = listingEntry.Path;
                var basePath = targetDirectory;
                if (fullPath.Length == basePath.Length) continue; // same path, exclude target dir

                var typeString = Util.IsFile(fullPath) ? "File" : (Util.IsDirectory(fullPath) ? "Directory" : "Unknown");
                bool include = false;
                if (typeString == "File" && includeFiles) include = true;
                if (typeString == "Directory" && includeDirectories) include = true;
                if (!include) continue;

                var depth = Util.PathParse(fullPath).Length - Util.PathParse(basePath).Length - 1;
                if (depth > recursiveDepth) continue;

                if (pattern != null && pattern != "*")
                {
                    var fileName = Path.GetFileName(fullPath);
                    if (!fileName.EqualsWildcard(pattern))
                    {
                        continue;
                    }
                }

                if (creationTime) row.Add(File.GetCreationTime(fullPath).ToStringYYYYMMDDHHMMSS());
                if (creationTimeUtc) row.Add(File.GetCreationTimeUtc(fullPath).ToStringYYYYMMDDHHMMSS());
                if (lastAccessTime) row.Add(File.GetLastAccessTime(fullPath).ToStringYYYYMMDDHHMMSS());
                if (lastAccessTimeUtc) row.Add(File.GetLastAccessTimeUtc(fullPath).ToStringYYYYMMDDHHMMSS());
                if (lastWriteTime) row.Add(File.GetLastWriteTime(fullPath).ToStringYYYYMMDDHHMMSS());
                if (lastWriteTimeUtc) row.Add(File.GetLastWriteTimeUtc(fullPath).ToStringYYYYMMDDHHMMSS());
                if (name) row.Add(Path.GetFileName(fullPath));
                if (nameFull) row.Add(fullPath);
                if (nameRelative) row.Add(Path.GetRelativePath(basePath, fullPath));
                if (path) row.Add(Directory.GetParent(fullPath).FullName);
                if (pathRelative) row.Add(Path.GetRelativePath(basePath, Directory.GetParent(fullPath).FullName));
                if (parentName) row.Add(Directory.GetParent(fullPath).Name);
                if (type) row.Add(typeString);
                if (size) row.Add(Util.FileGetSize(fullPath).ToString());
                tableList.Add(row);
            }

            var table = Utilities.Table.Create(tableList, true);
            DeleteExistingFile(targetTabFile);
            WriteTableTab(targetTabFile, table);

        }
    }
}
