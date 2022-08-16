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

namespace MaxRunSoftware.Utilities;

public abstract class FileSystemObject : ImmutableObjectBase<FileSystemObject>
{
    //private static readonly ILogger log = LogFactory.LogFactoryImpl.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

    protected static readonly StringComparer STRING_COMPARER = Constant.Path_IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
    protected static readonly StringComparison STRING_COMPARISON = Constant.Path_IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    private static readonly char[] pathDelimiters = Constant.PathDelimiters.ToArray();

    public string Path { get; }

    private readonly Lzy<IReadOnlyList<string>> pathParts;
    public IReadOnlyList<string> PathParts => pathParts.Value;
    private IReadOnlyList<string> PathParts_Build() => Path.Split(pathDelimiters).Where(o => o.TrimOrNull() != null).ToList();

    private readonly Lzy<string> name;
    public string Name => name.Value;
    private string Name_Build()
    {
        var fn = System.IO.Path.GetFileName(Path);
        if (fn.TrimOrNull() != null) return fn;
        var dn = System.IO.Path.GetDirectoryName(Path);
        return dn.TrimOrNull() != null ? dn : fileSystemInfo.Value.Name;
    }

    private readonly Lzy<FileAttributes> fileAttributes;
    protected FileAttributes FileAttributes => fileAttributes.Value;
    private FileAttributes FileAttributes_Build() => File.GetAttributes(Path);

    public Exception Exception => null;

    public abstract bool IsDirectory { get; }
    public bool IsReparsePoint => (FileAttributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
    public bool IsFile => !IsDirectory;
    public bool IsHidden => (FileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
    public bool IsReadOnly => (FileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;

    public abstract bool IsExist { get; }
    public abstract long Size { get; }

    private readonly Lzy<FileSystemDirectory> parent;
    public FileSystemDirectory Parent => parent.Value;
    private FileSystemDirectory Parent_Build()
    {
        var di = IsDirectory ? DirectoryInfo.Parent : FileInfo.Directory;
        return di == null ? null : new FileSystemDirectory(di.FullName);
    }

    private readonly Lzy<FileSystemInfo> fileSystemInfo;
    public FileInfo FileInfo => fileSystemInfo.Value as FileInfo;
    public DirectoryInfo DirectoryInfo => fileSystemInfo.Value as DirectoryInfo;
    private FileSystemInfo FileSystemInfo_Build() => IsDirectory ? new DirectoryInfo(Path) : new FileInfo(Path);

    private readonly Lzy<DateTime> creationTime;
    public DateTime CreationTime => creationTime.Value;
    private DateTime CreationTime_Build() => File.GetCreationTime(Path);

    private readonly Lzy<DateTime> creationTimeUtc;
    public DateTime CreationTimeUtc => creationTimeUtc.Value;
    private DateTime CreationTimeUtc_Build() => File.GetCreationTimeUtc(Path);

    private readonly Lzy<DateTime> lastAccessTime;
    public DateTime LastAccessTime => lastAccessTime.Value;
    private DateTime LastAccessTime_Build() => File.GetLastAccessTime(Path);

    private readonly Lzy<DateTime> lastAccessTimeUtc;
    public DateTime LastAccessTimeUtc => lastAccessTimeUtc.Value;
    private DateTime LastAccessTimeUtc_Build() => File.GetLastAccessTimeUtc(Path);

    private readonly Lzy<DateTime> lastWriteTime;
    public DateTime LastWriteTime => lastWriteTime.Value;
    private DateTime LastWriteTime_Build() => File.GetLastWriteTime(Path);

    private readonly Lzy<DateTime> lastWriteTimeUtc;
    public DateTime LastWriteTimeUtc => lastWriteTimeUtc.Value;
    private DateTime LastWriteTimeUtc_Build() => File.GetLastWriteTimeUtc(Path);

    protected internal FileSystemObject(string path)
    {
        path.CheckNotNull(nameof(path));
        Path = System.IO.Path.GetFullPath(path);

        pathParts = CreateLazy(PathParts_Build);
        fileAttributes = CreateLazy(FileAttributes_Build);
        fileSystemInfo = CreateLazy(FileSystemInfo_Build);
        name = CreateLazy(Name_Build);

        parent = CreateLazy(Parent_Build);

        creationTime = CreateLazy(CreationTime_Build);
        creationTimeUtc = CreateLazy(CreationTimeUtc_Build);

        lastAccessTime = CreateLazy(LastAccessTime_Build);
        lastAccessTimeUtc = CreateLazy(LastAccessTimeUtc_Build);

        lastWriteTime = CreateLazy(LastWriteTime_Build);
        lastWriteTimeUtc = CreateLazy(LastWriteTimeUtc_Build);
    }

    public bool IsParentOf(FileSystemObject other)
    {
        if (ReferenceEquals(other, null)) return false;
        if (ReferenceEquals(other, this)) return true;

        if (PathParts.Count >= other.PathParts.Count) return false;

        for (var i = 0; i < PathParts.Count; i++)
        {
            if (!STRING_COMPARER.Equals(PathParts[i], other.PathParts[i])) return false;
        }

        return true;
    }

    protected override string ToString_Build() => Path;

    protected override int GetHashCode_Build() => STRING_COMPARER.GetHashCode(Path);

    protected override int CompareTo_Internal(FileSystemObject other) => STRING_COMPARER.Compare(Path, other.Path);

    protected override bool Equals_Internal(FileSystemObject other) => IsDirectory == other.IsDirectory && STRING_COMPARER.Equals(Path, other.Path);

    public bool NameMatchesWildcard(string wildcard)
    {
        if (string.IsNullOrEmpty(wildcard)) return false;
        if (wildcard == "*") return true;
        return Name.EqualsWildcard(wildcard);
    }

    public static FileSystemObject Get(string path) => (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory ? new FileSystemDirectory(path) : new FileSystemFile(path);

    public static FileSystemFile GetFile(string path) => Get(path) is not FileSystemFile o ? throw new FileNotFoundException($"Path provided is a directory not a file '{path}'.", path) : o;

    public static FileSystemDirectory GetDirectory(string path) => Get(path) is not FileSystemDirectory o ? throw new DirectoryNotFoundException($"Path provided is a file not a directory '{path}'.") : o;
}
