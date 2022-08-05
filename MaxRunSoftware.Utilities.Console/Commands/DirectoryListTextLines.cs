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

// ReSharper disable StringLiteralTypo

namespace MaxRunSoftware.Utilities.Console.Commands;

public class DirectoryListTextLines : DirectoryListBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Scans all files and directories in target directory and exports that information to a tab delimited file");
        help.AddParameter(nameof(numLinesToRead), "nltr", "Number of lines to read 0=All  (0)");

        help.AddValue("<directory> <tab delimited filename>");
        help.AddExample("MyDirectory mydata.txt");
        help.AddExample("C:\\windows mywindata.txt");
    }

    protected int numLinesToRead;


    protected Utilities.Table ToTable()
    {
        numLinesToRead = GetArgParameterOrConfigInt(nameof(numLinesToRead), "nltr", 0);

        var parsers = Parsers.Where(o => o.IsEnabled).ToList();
        var list = new List<List<string>>();
        var header = parsers.Select(o => o.Name.Capitalize()).ToList();
        header.Add("LineNumber");
        header.Add("LineText");

        list.Add(header);

        foreach (var o in TargetDirectory.FilesRecursive)
        {
            if (!o.IsExist) continue;
            if (!ShouldInclude(o, FilePatterns, false, true, false)) continue;

            var rowTemplate = new List<string>(header.Count);
            foreach (var parser in parsers) rowTemplate.Add(parser.Parse(o));

            log.Info("Reading file: " + o.Path);

            var fileText = ReadFile(o.Path);
            var fileLines = fileText.SplitOnNewline();
            if (fileLines == null || fileLines.Length == 0) fileLines = new[] { "" };
            for (var i = 0; i < fileLines.Length; i++)
            {
                var lineNumber = i + 1;
                if (numLinesToRead > 0 && lineNumber > numLinesToRead) continue;
                var row = new List<string>(rowTemplate);
                row.Add(lineNumber.ToString());
                row.Add(fileLines[i]);
                list.Add(row);
            }
        }

        return Utilities.Table.Create(list, true);
    }

    protected override void ExecuteInternal()
    {
        base.ExecuteInternal();

        if (Parsers.All(o => !o.IsEnabled))
        {
            log.Info("No arguments specified so enabling all except readers");
            foreach (var p in Parsers) p.IsEnabled = true;
        }


        var targetFile = GetArgValueTrimmed(1);
        log.Debug(nameof(targetFile), targetFile);
        if (targetFile == null) throw new ArgsException(nameof(targetFile), $"No {nameof(targetFile)} specified to save to");
        targetFile = Path.GetFullPath(targetFile);
        log.Debug(nameof(targetFile), targetFile);

        DeleteExistingFile(targetFile);

        var t = ToTable();
        log.Info("Successfully created " + t);
        WriteTableTab(targetFile, t);
        log.Info("Successfully wrote file with " + (t.Count + 1) + " lines");
    }

    /*
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        help.AddSummary("Scans all files and directories in target directory and exports that information to a tab delimited file");
        help.AddValue("<directory> <tab delimited filename>");
        help.AddExample("MyDirectory mydata.txt");
        help.AddExample("C:\\windows mywindata.txt");
    }

  
    protected override void ExecuteInternal()
    {
        var targetTabFile = GetArgValueTrimmed(1);
        log.DebugParameter(nameof(targetTabFile), targetTabFile);
        if (targetTabFile == null) throw ArgsException.ValueNotSpecified(nameof(targetTabFile));

        targetTabFile = Path.GetFullPath(targetTabFile);
        log.DebugParameter(nameof(targetTabFile), targetTabFile);

        var header = new List<string>();
        
        if (listTime) header.Add(nameof(listTime));
        if (listTimeUtc) header.Add(nameof(listTimeUtc));
        if (creationTime) header.Add(nameof(creationTime));
        if (creationTimeUtc) header.Add(nameof(creationTimeUtc));
        if (lastAccessTime) header.Add(nameof(lastAccessTime));
        if (lastAccessTimeUtc) header.Add(nameof(lastAccessTimeUtc));
        if (lastWriteTime) header.Add(nameof(lastWriteTime));
        if (lastWriteTimeUtc) header.Add(nameof(lastWriteTimeUtc));
        
        if (name) header.Add(nameof(name));
        if (nameFull) header.Add(nameof(nameFull));
        if (nameRelative) header.Add(nameof(nameRelative));
        
        if (parentDirectoryName) header.Add(nameof(parentDirectoryName));
        if (parentDirectoryNameFull) header.Add(nameof(parentDirectoryNameFull));
        
        if (parentDirectoryNameFull) header.Add(nameof(parentDirectoryNameFull));
        
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

            var fullPath = listingEntry.PathFull;
            var basePath = targetDirectory;
            if (fullPath.Length == basePath.Length) continue; // same path, exclude target dir

            var typeString = Util.IsFile(fullPath) ? "File" : Util.IsDirectory(fullPath) ? "Directory" : "Unknown";
            // ReSharper disable ConvertIfToOrExpression
            // ReSharper disable ReplaceWithSingleAssignment.False
            var include = false;
            if (typeString == "File" && includeFiles) include = true;

            if (typeString == "Directory" && includeDirectories) include = true;
            // ReSharper restore ReplaceWithSingleAssignment.False
            // ReSharper restore ConvertIfToOrExpression

            if (!include) continue;

            var depth = Util.PathParts(fullPath).Length - Util.PathParts(basePath).Length - 1;
            if (depth > recursiveDepth) continue;

            if (pattern != null && pattern != "*")
            {
                var fileName = Path.GetFileName(fullPath);
                if (!fileName.EqualsWildcard(pattern)) continue;
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

            if (path) row.Add(Directory.GetParent(fullPath)?.FullName);

            if (pathRelative)
            {
                var parentFullName = Directory.GetParent(fullPath)?.FullName;
                row.Add(parentFullName == null ? null : Path.GetRelativePath(basePath, parentFullName));
            }

            if (parentName) row.Add(Directory.GetParent(fullPath)?.Name);

            if (type) row.Add(typeString);

            if (size) row.Add(Util.FileGetSize(fullPath).ToString());

            tableList.Add(row);
        }

        var table = Utilities.Table.Create(tableList, true);
        DeleteExistingFile(targetTabFile);
        WriteTableTab(targetTabFile, table);
    }
    */
}
