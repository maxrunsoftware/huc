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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MaxRunSoftware.Utilities.External;

public abstract class FtpClientBase : IFtpClient
{
    private readonly Lazy<string> serverInfo;
    protected readonly ILogger log;
    public string ServerInfo => serverInfo.Value;

    public abstract string WorkingDirectory { get; }

    protected FtpClientBase()
    {
        log = Logging.LogFactory.GetLogger(GetType());
        serverInfo = new Lazy<string>(GetServerInfo);
    }

    protected abstract string GetServerInfo();

    /*
    protected static string CombinePath(string[] remotePathParts, string remoteFile = null)
    {
        if (remoteFile.TrimOrNull() == null) remoteFile = null;

        if (remotePathParts.Length == 0)
        {
            return remoteFile == null ? string.Empty : "/" + remoteFile;
        }

        var sb = new StringBuilder();
        sb.Append("/");
        foreach (var remotePathPart in remotePathParts)
        {
            sb.Append(remotePathPart);
            sb.Append("/");
        }
        if (remoteFile == null)
        {
            sb.Remove(sb.Length - 1, 1);
        }
        else
        {
            sb.Append(remoteFile);
        }

        return sb.ToString();
    }
    */

    protected abstract void GetFile(string remoteFile, Stream localStream);

    protected abstract void PutFile(string remoteFile, Stream localStream);

    protected abstract bool ExistsFile(string remoteFile);
    protected abstract bool ExistsDirectory(string remoteDirectory);

    protected abstract void ListFiles(string remotePath, List<FtpClientFile> fileList);

    protected abstract void DeleteFileSingle(string remoteFile);

    protected virtual string ToAbsolutePath(string path)
    {
        if (!path.StartsWith("/"))
        {
            var wd = WorkingDirectory;
            if (!wd.EndsWith("/")) path = "/" + path;
            path = wd + path;
        }

        var pathParts = path.Split('/').Where(part => part.TrimOrNull() != null).ToArray();
        var stack = new Stack<string>();
        foreach (var pathPart in pathParts)
        {
            if (pathPart.TrimOrNull() == ".")
            { }
            else if (pathPart.TrimOrNull() == "..")
            {
                if (stack.Count > 0) stack.Pop();
            }
            else
            {
                stack.Push(pathPart);
            }
        }
        path = "/" + string.Join("/", stack);
        return path;
    }

    public abstract void Dispose();

    public void GetFile(string remoteFile, string localFile)
    {
        remoteFile.CheckNotNullTrimmed(nameof(remoteFile));
        localFile.CheckNotNullTrimmed(nameof(localFile));
        localFile = Path.GetFullPath(localFile);

        if (File.Exists(localFile))
        {
            log.Debug($"Deleting existing local file: {localFile}");
            File.Delete(localFile);
        }

        //remoteFile = ToAbsolutePath(remoteFile);
        if (!ExistsFile(remoteFile)) throw new FileNotFoundException($"Remote file {remoteFile} does not exist", remoteFile);
        log.Debug($"Downloading file {remoteFile} --> {localFile}");

        using (var localFileStream = Util.FileOpenWrite(localFile))
        {
            GetFile(remoteFile, localFileStream);
            localFileStream.Flush();
        }
        var fl = (new FileInfo(localFile)).Length.ToStringCommas();
        log.Debug($"Downloaded {fl} byte file {remoteFile} --> {localFile}");
    }

    public void PutFile(string remoteFile, string localFile)
    {
        remoteFile.CheckNotNullTrimmed(nameof(remoteFile));
        localFile.CheckNotNullTrimmed(nameof(localFile));
        localFile = Path.GetFullPath(localFile);
        localFile.CheckFileExists();

        //remoteFile = ToAbsolutePath(remoteFile);

        log.Debug($"Uploading file {localFile} --> {remoteFile}");
        using (var localFileStream = Util.FileOpenRead(localFile))
        {
            PutFile(remoteFile, localFileStream);
            localFileStream.Flush();
        }
        var fl = (new FileInfo(localFile)).Length.ToStringCommas();
        log.Debug($"Uploaded {fl} byte file {localFile} --> {remoteFile}");
    }

    public IEnumerable<FtpClientFile> ListFiles(string remotePath)
    {
        remotePath = remotePath.TrimOrNull();
        var remotePathPrintable = remotePath ?? WorkingDirectory;

        log.Debug($"Getting remote file listing of {remotePathPrintable}");
        var list = new List<FtpClientFile>();
        ListFiles(remotePath, list);
        log.Debug($"Found {list.Count} files at {remotePathPrintable}");
        return list;
    }

    public IEnumerable<FtpClientFile> ListFilesRecursive(string remotePath)
    {
        remotePath = remotePath.TrimOrNull() ?? ".";
        remotePath = ToAbsolutePath(remotePath);

        var list = new List<FtpClientFile>
        {
            new FtpClientFile(remotePath.Split('/').LastOrDefault(o => o.TrimOrNull() != null), remotePath, FtpClientFileType.Directory)
        };
        var checkedDirectories = new HashSet<string>();
        foreach (var dir in list.Where(o => o.Type == FtpClientFileType.Directory).ToList())
        {
            if (checkedDirectories.Add(dir.FullName))
            {
                log.Debug($"Getting remote file listing of {dir.FullName}");
                ListFiles(dir.FullName, list);
            }
        }

        log.Debug($"Found {list.Count} recursive files at {remotePath}");
        return list;
    }

    public void DeleteFile(string remoteFile)
    {
        remoteFile = ToAbsolutePath(remoteFile);
        if (remoteFile.ContainsAny("*", "?")) // wildcard
        {
            var pathParts = remoteFile.Split("/").TrimOrNull().WhereNotNull().ToList();
            var remoteFileName = pathParts.PopTail();
            string path = string.Empty;
            if (pathParts.Count > 0) path = "/" + string.Join("/", pathParts);
            foreach (var file in ListFiles(path))
            {
                if (file.Name.EqualsWildcard(remoteFileName))
                {
                    log.Debug("Attempting delete of remote file " + file.FullName);
                    DeleteFileSingle(file.FullName);
                    log.Info("Successfully deleted remote file " + file.FullName);
                }
            }
        }
        else
        {
            log.Debug("Attempting delete of remote file " + remoteFile);
            DeleteFileSingle(remoteFile);
            log.Info("Successfully deleted remote file " + remoteFile);
        }
    }
}