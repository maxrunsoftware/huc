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

public static partial class Util
{
    #region File

    public readonly struct FileListResult
    {
        public string Path { get; }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public bool IsDirectory { get; }

        public Exception Exception { get; }

        public FileListResult(string path, bool isDirectory, Exception exception) : this()
        {
            Path = path;
            IsDirectory = isDirectory;
            Exception = exception;
        }

        public override string ToString()
        {
            return Path;
        }

        public FileSystemInfo GetFileSystemInfo()
        {
            return IsDirectory ? new DirectoryInfo(Path) : new FileInfo(Path);
        }
    }

    internal static DirectoryNotFoundException CreateExceptionDirectoryNotFound(string directoryPath)
    {
        return new DirectoryNotFoundException("Directory does not exist " + directoryPath);
    }

    internal static FileNotFoundException CreateExceptionFileNotFound(string filePath)
    {
        return new FileNotFoundException("File does not exist " + filePath, filePath);
    }

    internal static DirectoryNotFoundException CreateExceptionIsNotDirectory(string directoryPath)
    {
        return new DirectoryNotFoundException("Path is a file not a directory " + directoryPath);
    }

    public static (string directoryName, string fileName, string extension) SplitFileName(string file)
    {
        file = Path.GetFullPath(file);
        var dn = Path.GetDirectoryName(file); 
        var d = dn == null ? null : Path.GetFullPath(dn);
        var f = Path.GetFileNameWithoutExtension(file);
        var e = Path.GetExtension(file).TrimOrNull();
        if (!string.IsNullOrEmpty(e) && e[0] == '.')
        {
            e = e.Remove(0, 1).TrimOrNull();
        }

        return (d.TrimOrNull() ?? string.Empty, f, e.TrimOrNull() ?? string.Empty);
    }

    public static string FileGetMD5(string file)
    {
        using (var stream = FileOpenRead(file))
        {
            return GenerateHashMD5(stream);
        }
    }

    public static string FileGetSHA1(string file)
    {
        using (var stream = FileOpenRead(file))
        {
            return GenerateHashSHA1(stream);
        }
    }

    public static string FileGetSHA256(string file)
    {
        using (var stream = FileOpenRead(file))
        {
            return GenerateHashSHA256(stream);
        }
    }

    public static string FileGetSHA384(string file)
    {
        using (var stream = FileOpenRead(file))
        {
            return GenerateHashSHA384(stream);
        }
    }

    public static string FileGetSHA512(string file)
    {
        using (var stream = FileOpenRead(file))
        {
            return GenerateHashSHA512(stream);
        }
    }

    public static long FileGetSize(string file)
    {
        return new FileInfo(file).Length;
    }

    public static string FileChangeName(string file, string newFileName)
    {
        file = Path.GetFullPath(file);
        var dir = Path.GetDirectoryName(file);
        var name = newFileName;
        var ext = string.Empty;
        if (Path.HasExtension(file))
        {
            ext = Path.GetExtension(file);
        }

        if (!ext.StartsWith("."))
        {
            ext = "." + ext;
        }

        var f = dir == null ? Path.Combine(name + ext) : Path.Combine(dir, name + ext);
        f = Path.GetFullPath(f);
        return f;
    }

    public static string FileChangeNameRandomized(string file)
    {
        return FileChangeNameAppendRight(file, "_" + Guid.NewGuid().ToString().Replace("-", ""));
    }

    public static string FileChangeNameAppendRight(string file, string stringToAppend)
    {
        return FileChangeName(file, Path.GetFileNameWithoutExtension(Path.GetFullPath(file)) + stringToAppend);
    }

    public static string FileChangeNameAppendLeft(string file, string stringToAppend)
    {
        return FileChangeName(file, stringToAppend + Path.GetFileNameWithoutExtension(Path.GetFullPath(file)));
    }

    public static string FileChangeExtension(string file, string newExtension)
    {
        file = Path.GetFullPath(file);
        var dir = Path.GetDirectoryName(file);
        var name = Path.GetFileNameWithoutExtension(file);
        var ext = newExtension ?? string.Empty;
        if (!ext.StartsWith("."))
        {
            ext = "." + ext;
        }

        var f = dir == null ? Path.Combine(name + ext) : Path.Combine(dir, name + ext);
        f = Path.GetFullPath(f);
        return f;
    }

    public static bool IsDirectory(string path)
    {
        path = path.CheckNotNullTrimmed(path);
        if (File.Exists(path))
        {
            return false;
        }

        if (Directory.Exists(path))
        {
            return true;
        }

        return false;
    }

    public static bool IsEqualFile(string file1, string file2, bool buffered)
    {
        file1.CheckFileExists("file1");
        file2.CheckFileExists("file2");

        // TODO: Check if file system is case sensitive or not then do a case-insensitive comparison since Windows uses case-insensitive filesystem.
        if (string.Equals(file1, file2))
        {
            return true;
        }

        var file1Size = FileGetSize(file1);
        var file2Size = FileGetSize(file2);

        if (file1Size != file2Size)
        {
            return false;
        }

        if (buffered)
        {
            var file1Bytes = FileRead(file1);
            var file2Bytes = FileRead(file2);

            return file1Bytes.EqualsBytes(file2Bytes);
        }
        // No buffer needed http://stackoverflow.com/a/2069317 http://blogs.msdn.com/b/brada/archive/2004/04/15/114329.aspx

        // Compare method from http://stackoverflow.com/a/1359947
        const int bytesToRead = sizeof(long);

        var iterations = (int)Math.Ceiling((double)file1Size / bytesToRead);

        using (var fs1 = FileOpenRead(file1))
        using (var fs2 = FileOpenRead(file2))
        {
            var one = new byte[bytesToRead];
            var two = new byte[bytesToRead];

            for (var i = 0; i < iterations; i++)
            {
                // ReSharper disable once MustUseReturnValue
                fs1.Read(one, 0, bytesToRead);
                
                // ReSharper disable once MustUseReturnValue
                fs2.Read(two, 0, bytesToRead);

                if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static bool IsFile(string path)
    {
        path = path.CheckNotNullTrimmed(path);
        if (Directory.Exists(path))
        {
            return false;
        }

        if (File.Exists(path))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns a list of files and directories including the provided directory
    /// </summary>
    /// <param name="directoryPath">The directory path to list</param>
    /// <param name="recursive">Whether to be recursive or not</param>
    /// <returns>An IEnumerable of FileListResult</returns>
    public static IEnumerable<FileListResult> FileList(string directoryPath, bool recursive = false)
    {
        directoryPath = directoryPath.CheckNotNullTrimmed(nameof(directoryPath));
        directoryPath = Path.GetFullPath(directoryPath);
        if (IsFile(directoryPath))
        {
            throw CreateExceptionIsNotDirectory(directoryPath);
        }

        if (!IsDirectory(directoryPath))
        {
            throw CreateExceptionDirectoryNotFound(directoryPath);
        }

        var alreadyScannedDirectories = new HashSet<string>(Constant.OS_WINDOWS ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        var queue = new Queue<string>();
        queue.Enqueue(directoryPath);
        while (queue.Count > 0)
        {
            var currentDirectory = Path.GetFullPath(queue.Dequeue());

            // prevent loops
            if (!alreadyScannedDirectories.Add(currentDirectory))
            {
                continue;
            }

            // ignore links to other paths
            if (!currentDirectory.StartsWith(directoryPath, Constant.OS_WINDOWS ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                continue;
            }

            Exception exception = null;
            string[] files = null;
            try
            {
                var subDirectories = Directory.GetDirectories(currentDirectory).OrEmpty();
                for (var i = 0; i < subDirectories.Length; i++)
                {
                    queue.Enqueue(subDirectories[i]);
                }

                files = Directory.GetFiles(currentDirectory).OrEmpty();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            yield return new FileListResult(currentDirectory, true, exception);

            if (exception == null && files != null)
            {
                for (var i = 0; i < files.Length; i++)
                {
                    yield return new FileListResult(Path.GetFullPath(files[i]), false, null);
                }
            }

            if (!recursive)
            {
                queue.Clear();
            }
        }
    }

    public static IEnumerable<string> FileListFiles(string directoryPath, bool recursive = false)
    {
        foreach (var o in FileList(directoryPath, recursive))
        {
            if (o.Exception == null && !o.IsDirectory)
            {
                yield return o.Path;
            }
        }
    }

    public static IEnumerable<string> FileListDirectories(string directoryPath, bool recursive = false)
    {
        foreach (var o in FileList(directoryPath, recursive))
        {
            if (o.Exception == null && o.IsDirectory)
            {
                yield return o.Path;
            }
        }
    }

    public static bool FileIsAbsolutePath(string path)
    {
        if (path.TrimOrNull() == null)
        {
            return false;
        }

        if (path.IndexOfAny(Path.GetInvalidPathChars()) != -1)
        {
            return false;
        }

        if (!Path.IsPathRooted(path))
        {
            return false;
        }

        var pathRoot = Path.GetPathRoot(path);
        if (pathRoot != null && pathRoot.Equals(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    public static string FileGetParent(string path)
    {
        path = Path.GetFullPath(path);
        string reassemblyChar;
        if (path.Contains("\\"))
        {
            // windows path
            reassemblyChar = "\\";
        }
        else if (path.Contains("/"))
        {
            // linux / mac path
            reassemblyChar = "/";
        }
        else
        {
            reassemblyChar = Constant.OS_WINDOWS ? "\\" : "/";
        }

        var pathParts = path.Split(reassemblyChar).TrimOrNull().WhereNotNull().ToList();
        if (pathParts.Count == 1)
        {
            return null;
        }

        pathParts.PopTail();
        path = pathParts.ToStringDelimited(reassemblyChar);
        if (reassemblyChar.Equals("/"))
        {
            path = "/" + path;
        }

        return Path.GetFullPath(path);
    }

    public static FileStream FileOpenRead(string filename)
    {
        return File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    public static FileStream FileOpenWrite(string filename)
    {
        return File.Open(filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
    }

    public static byte[] FileRead(string path)
    {
        // https://github.com/mosa/Mono-Class-Libraries/blob/master/mcs/class/corlib/System.IO/File.cs

        using (var stream = FileOpenRead(path))
        {
            var size = stream.Length;
            // limited to 2GB according to http://msdn.microsoft.com/en-us/library/system.io.file.readallbytes.aspx
            if (size > int.MaxValue)
            {
                throw new IOException("Reading more than 2GB with this call is not supported");
            }

            var pos = 0;
            var count = (int)size;
            var result = new byte[size];
            while (count > 0)
            {
                var n = stream.Read(result, pos, count);
                if (n == 0)
                {
                    throw new IOException("Unexpected end of stream");
                }

                pos += n;
                count -= n;
            }

            return result;
        }
    }

    public static string FileRead(string path, Encoding encoding)
    {
        return encoding.GetString(FileRead(path));
    }

    public static void FileWrite(string path, byte[] data, bool append = false)
    {
        path.CheckNotNull(nameof(path));
        if (File.Exists(path) && !append)
        {
            File.Delete(path);
        }
        
        using (var stream = File.OpenWrite(path))
        {
            stream.Position = stream.Length;
            stream.Write(data, 0, data.Length);
            stream.Flush(true);
        }
    }

    public static void FileWrite(string path, string data, Encoding encoding, bool append = false)
    {
        FileWrite(path, encoding.GetBytes(data), append);
    }

    public static string FilenameSanitize(string path, string replacement)
    {
        if (path == null)
        {
            return null;
        }
        //if (replacement == null) throw new ArgumentNullException("replacement");

        var illegalChars = new HashSet<char>(Path.GetInvalidFileNameChars());

        var pathChars = path.ToCharArray();
        var sb = new StringBuilder();

        for (var i = 0; i < pathChars.Length; i++)
        {
            var c = pathChars[i];
            if (illegalChars.Contains(c))
            {
                if (replacement != null)
                {
                    sb.Append(replacement);
                }
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    #endregion File

    #region Temp

    private static readonly object lockTemp = new();

    private sealed class TempDirectory : IDisposable
    {
        private static readonly ILogger log = LogFactory.LogFactoryImpl.GetLogger<TempDirectory>();
        private readonly string path;

        public TempDirectory(string path)
        {
            log.Debug($"Creating temporary directory {path}");
            Directory.CreateDirectory(path);
            this.path = path;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(path))
                {
                    log.Debug($"Deleting temporary directory {path}");
                    Directory.Delete(path, true);
                    log.Debug($"Successfully deleted temporary directory {path}");
                }
            }
            catch (Exception e)
            {
                log.Warn($"Error deleting temporary directory {path}", e);
            }
        }
    }

    private sealed class TempFile : IDisposable
    {
        private static readonly ILogger log = LogFactory.LogFactoryImpl.GetLogger<TempFile>();
        private readonly string path;

        public TempFile(string path)
        {
            log.Debug($"Creating temporary file {path}");
            File.WriteAllBytes(path, Array.Empty<byte>());
            this.path = path;
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(path))
                {
                    log.Debug($"Deleting temporary file {path}");
                    File.Delete(path);
                    log.Debug($"Successfully deleted temporary file {path}");
                }
            }
            catch (Exception e)
            {
                log.Warn($"Error deleting temporary file {path}", e);
            }
        }
    }

    public static IDisposable CreateTempDirectory(out string path)
    {
        lock (lockTemp)
        {
            var parentDir = Path.GetTempPath();
            string p;
            do
            {
                p = Path.GetFullPath(Path.Combine(parentDir, Path.GetRandomFileName()));
            } while (Directory.Exists(p));

            var t = new TempDirectory(p);
            path = p;
            return t;
        }
    }

    public static IDisposable CreateTempFile(out string path)
    {
        lock (lockTemp)
        {
            var parentDir = Path.GetTempPath();
            string p;
            do
            {
                p = Path.GetFullPath(Path.Combine(parentDir, Path.GetRandomFileName()));
            } while (File.Exists(p));

            var t = new TempFile(p);
            path = p;
            return t;
        }
    }

    #endregion Temp

    #region Path

    public static string[] PathParse(string path)
    {
        return PathParse(path, Constant.PATH_DELIMITERS_STRINGS);
    }

    public static string[] PathParse(string path, params string[] pathDelimiters)
    {
        return PathParse(path.Yield(), pathDelimiters);
    }

    public static string[] PathParse(IEnumerable<string> pathParts)
    {
        return PathParse(pathParts, Constant.PATH_DELIMITERS_STRINGS);
    }

    public static string[] PathParse(string path, IEnumerable<string> pathDelimiters)
    {
        return PathParse(path.Yield(), pathDelimiters);
    }

    public static string[] PathParse(IEnumerable<string> pathParts, IEnumerable<string> pathDelimiters)
    {
        var list = (pathParts ?? Enumerable.Empty<string>()).TrimOrNull().WhereNotNull().ToList();

        var pathDelimitersArray = pathDelimiters.CheckNotNull(nameof(pathDelimiters)).ToArray();
        var list2 = new List<string>();
        foreach (var item in list)
        {
            var itemParts = item.Split(pathDelimitersArray, StringSplitOptions.RemoveEmptyEntries).TrimOrNull().WhereNotNull().ToArray();
            foreach (var itemPart in itemParts)
            {
                list2.Add(itemPart);
            }
        }

        return list2.ToArray();
    }

    public static string PathToString(IEnumerable<string> pathParts, string pathDelimiter = "/", bool trailingDelimiter = true)
    {
        var s = string.Join(pathDelimiter, pathParts).TrimOrNull() ?? string.Empty;
        s = pathDelimiter + s;
        if (s.Length > 1 && trailingDelimiter)
        {
            s = s + pathDelimiter;
        }

        return s;
    }

    #endregion Path
}
