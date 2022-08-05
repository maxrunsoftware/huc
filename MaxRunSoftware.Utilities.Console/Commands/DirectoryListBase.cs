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

public abstract class DirectoryListBase : Command
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        help.AddParameter(nameof(listTime), "lt", "List time (false)");
        help.AddParameter(nameof(listTimeUtc), "ltu", "List time UTC (false)");
        help.AddParameter(nameof(creationTime), "ct", "Creation time (false)");
        help.AddParameter(nameof(creationTimeUtc), "ctu", "Creation time UTC (false)");
        help.AddParameter(nameof(lastAccessTime), "lat", "Last access time (false)");
        help.AddParameter(nameof(lastAccessTimeUtc), "latu", "Last access time UTC (false)");
        help.AddParameter(nameof(lastWriteTime), "lwt", "Last write time (false)");
        help.AddParameter(nameof(lastWriteTimeUtc), "lwtu", "Last write time UTC (false)");

        help.AddParameter(nameof(name), "n", "Name (false)");
        help.AddParameter(nameof(nameFull), "nf", "Full path and name (false)");
        help.AddParameter(nameof(nameRelative), "nr", "Full path and name minus the <directory> path (false)");

        help.AddParameter(nameof(parentDirectoryName), "pdn", "Full path (false)");
        help.AddParameter(nameof(parentDirectoryNameFull), "pr", "Full path minus the <directory> path (false)");

        help.AddParameter(nameof(baseDirectory), "bd", "Supplied target <directory> path (false)");

        help.AddParameter(nameof(type), "t", "Whether this is a File or Directory (false)");
        help.AddParameter(nameof(size), "s", "Size in bytes (false)");
        help.AddParameter(nameof(depthFromBase), "dfb", "The depth from the base (false)");


        help.AddParameter(nameof(filePattern), "fpat", "The name pattern to match on seperated by patternSeparator (*)");
        help.AddParameter(nameof(filePatternSeparator), "fpats", "The string to split the pattern field on (,)");

        help.AddParameter(nameof(depthToSearch), "dts", "The depth to recursively scan subdirectories (0)");
    }

    protected bool listTime;
    protected bool listTimeUtc;
    protected bool creationTime;
    protected bool creationTimeUtc;
    protected bool lastAccessTime;
    protected bool lastAccessTimeUtc;
    protected bool lastWriteTime;
    protected bool lastWriteTimeUtc;

    protected bool name;
    protected bool nameFull;
    protected bool nameRelative;

    protected bool parentDirectoryName;
    protected bool parentDirectoryNameFull;

    protected bool baseDirectory;

    protected bool type;
    protected bool size;
    protected bool depthFromBase;

    protected string filePattern;
    protected string filePatternSeparator;
    protected int depthToSearch;

    protected DateTime scanTime;

    protected FileSystemDirectory TargetDirectory { get; private set; }
    protected IReadOnlyList<string> FilePatterns { get; private set; }

    protected int GetDepth(FileSystemObject o)
    {
        var lenShort = TargetDirectory.PathParts.Count;
        var lenLong = o.PathParts.Count;
        if (o.IsFile) lenLong--;

        return lenLong - lenShort;
    }

    protected virtual bool ShouldInclude(FileSystemObject o, IEnumerable<string> filePatterns, bool includeTargetDirectory, bool includeFiles, bool includeDirectories)
    {
        if (!includeTargetDirectory && o.Equals(TargetDirectory)) return false; // same path, exclude target dir

        if (o.IsFile && o.IsReparsePoint) return false;
        if (o.IsFile && !includeFiles) return false;
        if (o.IsDirectory && !includeDirectories) return false;

        if (GetDepth(o) > depthToSearch) return false;

        if (o.Name.TrimOrNull() == null) return false;

        if (o.IsFile)
        {
            foreach (var fp in filePatterns)
            {
                if (o.NameMatchesWildcard(fp)) return true;
            }

            return false;
        }

        return true;
    }

    protected override void ExecuteInternal()
    {
        listTime = GetArgParameterOrConfigBool(nameof(listTime), "lt", false);
        listTimeUtc = GetArgParameterOrConfigBool(nameof(listTimeUtc), "ltu", false);
        creationTime = GetArgParameterOrConfigBool(nameof(creationTime), "ct", false);
        creationTimeUtc = GetArgParameterOrConfigBool(nameof(creationTimeUtc), "ctu", false);
        lastAccessTime = GetArgParameterOrConfigBool(nameof(lastAccessTime), "lat", false);
        lastAccessTimeUtc = GetArgParameterOrConfigBool(nameof(lastAccessTimeUtc), "latu", false);
        lastWriteTime = GetArgParameterOrConfigBool(nameof(lastWriteTime), "lwt", false);
        lastWriteTimeUtc = GetArgParameterOrConfigBool(nameof(lastWriteTimeUtc), "lwtu", false);

        name = GetArgParameterOrConfigBool(nameof(name), "n", false);
        nameFull = GetArgParameterOrConfigBool(nameof(nameFull), "nf", false);
        nameRelative = GetArgParameterOrConfigBool(nameof(nameRelative), "nr", false);

        parentDirectoryName = GetArgParameterOrConfigBool(nameof(parentDirectoryName), "pdn", false);
        parentDirectoryNameFull = GetArgParameterOrConfigBool(nameof(parentDirectoryNameFull), "pdnf", false);

        baseDirectory = GetArgParameterOrConfigBool(nameof(baseDirectory), "bd", false);

        type = GetArgParameterOrConfigBool(nameof(type), "t", false);
        size = GetArgParameterOrConfigBool(nameof(size), "s", false);
        depthFromBase = GetArgParameterOrConfigBool(nameof(depthFromBase), "dfb", false);

        filePattern = GetArgParameterOrConfig(nameof(filePattern), "fpat", "*") ?? "*";
        filePatternSeparator = GetArgParameterOrConfig(nameof(filePatternSeparator), "fpats", ",").TrimOrNull() ?? ",";
        depthToSearch = GetArgParameterOrConfigInt(nameof(depthToSearch), "dts", 0);

        var filePatterns = filePattern.Split(filePatternSeparator).TrimOrNull().WhereNotNull().ToArray();
        if (filePatterns.Length == 0) filePatterns = new[] { "*" };
        FilePatterns = filePatterns;

        var targetDirectory = GetArgValueTrimmed(0);
        log.DebugParameter(nameof(targetDirectory), targetDirectory);
        if (targetDirectory == null) throw ArgsException.ValueNotSpecified(nameof(targetDirectory));

        targetDirectory = Path.GetFullPath(targetDirectory);
        log.DebugParameter(nameof(targetDirectory), targetDirectory);
        CheckDirectoryExists(targetDirectory);

        TargetDirectory = FileSystemObject.GetDirectory(targetDirectory);
        scanTime = DateTime.Now;

        Parsers = CreateParsers();
    }

    private List<Parser> CreateParsers()
    {
        var timeConverter = (DateTime o) => o.ToString(DateTimeToStringFormat.YYYY_MM_DD_HH_MM_SS);
        var parsers = new List<Parser>
        {
            new(listTime, nameof(listTime), _ => timeConverter(scanTime)),
            new(listTimeUtc, nameof(listTimeUtc), _ => timeConverter(scanTime.ToUniversalTime())),
            new(creationTime, nameof(creationTime), o => timeConverter(o.CreationTime)),
            new(creationTimeUtc, nameof(creationTimeUtc), o => timeConverter(o.CreationTimeUtc)),
            new(lastAccessTime, nameof(lastAccessTime), o => timeConverter(o.LastAccessTime)),
            new(lastAccessTimeUtc, nameof(lastAccessTimeUtc), o => timeConverter(o.LastAccessTimeUtc)),
            new(lastWriteTime, nameof(lastWriteTime), o => timeConverter(o.LastWriteTime)),
            new(lastWriteTimeUtc, nameof(lastWriteTimeUtc), o => timeConverter(o.LastWriteTimeUtc)),
            new(name, nameof(name), o => o.Name),
            new(nameFull, nameof(nameFull), o => o.Path),
            new(nameRelative, nameof(nameRelative), o => Path.GetRelativePath(TargetDirectory.Path, o.Path)),
            new(parentDirectoryName, nameof(parentDirectoryName), o => o.Parent?.Name),
            new(parentDirectoryNameFull, nameof(parentDirectoryNameFull), o => o.Parent?.Path),
            new(baseDirectory, nameof(baseDirectory), _ => TargetDirectory.Path),
            new(type, nameof(type), o => o.IsDirectory ? "Directory" : "File"),
            new(size, nameof(size), o => o.Size.ToString()),
            new(depthFromBase, nameof(depthFromBase), o => GetDepth(o).ToString())
        };
        return parsers;
    }

    protected IReadOnlyList<Parser> Parsers { get; private set; }


    protected class Parser
    {
        public string Name { get; }
        public bool IsEnabled { get; set; }
        private readonly Func<FileSystemObject, string> func;

        public Parser(bool isEnabled, string name, Func<FileSystemObject, string> func)
        {
            IsEnabled = isEnabled;
            Name = name;
            this.func = func;
        }

        public string Parse(FileSystemObject o) => func(o);
    }
}
