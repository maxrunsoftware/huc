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
using System.Net;
using System.Security.Authentication;
using FluentFTP;
using Renci.SshNet;

namespace MaxRunSoftware.Utilities.External
{
    public enum FtpClientFtpSEncryptionMode { Explicit, Implicit }

    public enum FtpClientFileType { Unknown, Directory, File, Link }

    public interface IFtpClient : IDisposable
    {
        string ServerInfo { get; }

        string WorkingDirectory { get; }

        void GetFile(string remoteFile, string localFile);

        void PutFile(string remoteFile, string localFile);

        void DeleteFile(string remoteFile);

        IEnumerable<FtpClientFile> ListFiles(string remotePath);
    }

    public class FtpClientFile
    {
        public string Name { get; }

        public string FullName { get; }

        public FtpClientFileType Type { get; }

        public FtpClientFile(string name, string fullName, FtpClientFileType type)
        {
            Name = name;
            FullName = fullName;
            Type = type;
        }

        /// <summary>
        /// Checks to see if our FullName matches a pathOrPattern value.
        /// if FullName=/dir1/file1.txt  pathOrPattern=/*/file?.txt  isCaseSensitive=true  IsMatch=true
        /// if FullName=/dir1/file1.txt  pathOrPattern=/*/FILE?.TXT  isCaseSensitive=true  IsMatch=false
        /// </summary>
        /// <param name="pathOrPattern"></param>
        /// <param name="isCaseSensitive"></param>
        /// <returns></returns>
        public bool IsMatch(string pathOrPattern, bool isCaseSensitive)
        {
            pathOrPattern = pathOrPattern.CheckNotNullTrimmed(nameof(pathOrPattern));
            var source = pathOrPattern.StartsWith("/") ? FullName : Name;
            return source.EqualsWildcard(pathOrPattern, !isCaseSensitive);
        }
    }

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
                {
                    ; // noop
                }
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
            remotePath = remotePath.TrimOrNull();
            if (remotePath == null) remotePath = ".";
            remotePath = ToAbsolutePath(remotePath);

            var list = new List<FtpClientFile>
            {
                new FtpClientFile(remotePath.Split('/').Where(o => o.TrimOrNull() != null).LastOrDefault(), remotePath, FtpClientFileType.Directory)
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
                var pathparts = remoteFile.Split("/").TrimOrNull().WhereNotNull().ToList();
                var remoteFileName = pathparts.PopTail();
                string path = string.Empty;
                if (pathparts.Count > 0) path = "/" + string.Join("/", pathparts);
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

    public class FtpClientSFtp : FtpClientBase
    {
        private SftpClient _client;

        private SftpClient Client
        {
            get
            {
                var c = _client;
                if (c == null) throw new ObjectDisposedException(GetType().FullNameFormatted());
                return c;
            }
        }

        public override string WorkingDirectory => Client.WorkingDirectory;

        public FtpClientSFtp(string host, ushort port, string username, string password) => _client = Ssh.CreateSFtpClient(host, port, username, password, null);

        public FtpClientSFtp(string host, ushort port, string username, IEnumerable<SshKeyFile> privateKeys) => _client = Ssh.CreateSFtpClient(host, port, username, null, privateKeys);

        protected override void GetFile(string remoteFile, Stream localStream) => Client.DownloadFile(remoteFile, localStream);

        protected override void PutFile(string remoteFile, Stream localStream) => Client.UploadFile(localStream, remoteFile, true);

        protected override void ListFiles(string remotePath, List<FtpClientFile> fileList)
        {
            remotePath = remotePath.TrimOrNull() == null ? "." : remotePath;
            foreach (var file in Client.ListDirectory(remotePath))
            {
                var name = file.Name;
                var fullName = file.FullName;
                if (!fullName.StartsWith("/")) fullName = "/" + fullName;
                var type = FtpClientFileType.Unknown;
                if (file.IsDirectory) type = FtpClientFileType.Directory;
                else if (file.IsRegularFile) type = FtpClientFileType.File;
                else if (file.IsSymbolicLink) type = FtpClientFileType.Link;

                fileList.Add(new FtpClientFile(name, fullName, type));
            }
        }

        protected override string GetServerInfo() => Client.ConnectionInfo.ClientVersion;

        protected override void DeleteFileSingle(string remoteFile)
        {
            log.Debug("Deleting remote file: " + remoteFile);
            Client.DeleteFile(remoteFile);
        }

        public void SetBufferSize(uint bufferSize)
        {
            if (Client.BufferSize != bufferSize)
            {
                log.Debug("Setting buffer size to " + bufferSize);
                Client.BufferSize = bufferSize;
            }
        }

        public override void Dispose()
        {
            var c = _client;
            _client = null;

            if (c == null) return;
            try
            {
                c.Disconnect();
            }
            catch (Exception e)
            {
                log.Warn($"Error disconnecting from server", e);
            }

            try
            {
                c.Dispose();
            }
            catch (Exception e)
            {
                log.Warn($"Error disposing of {c.GetType().FullNameFormatted()}", e);
            }
        }

        protected override bool ExistsFile(string remoteFile) => Client.Exists(remoteFile);
        protected override bool ExistsDirectory(string remoteDirectory) => Client.Exists(remoteDirectory);
    }

    public class FtpClientFtp : FtpClientBase
    {
        private FtpClient _client;

        private FtpClient Client
        {
            get
            {
                var c = _client;
                if (c == null) throw new ObjectDisposedException(GetType().FullNameFormatted());
                return c;
            }
        }

        public override string WorkingDirectory => Client.GetWorkingDirectory();

        public FtpClientFtp(string host, ushort port, string username, string password)
        {
            host = host.CheckNotNullTrimmed(nameof(host));
            port = port.CheckNotZero(nameof(port));
            username = username.TrimOrNull();
            password = password.TrimOrNull();
            if (username == null) username = password = "anonymous";
            _client = new FtpClient(host, port, new System.Net.NetworkCredential(username, password));
            log.Debug("Connecting to FTP server " + host + ":" + port + " with username " + username);
            //FtpTrace.LogPassword = true;
            //FtpTrace.LogPrefix = true;
            _client.OnLogEvent = LogMessage;
            _client.Connect();
            log.Debug("Connection successful");
        }

        public FtpClientFtp(string host, ushort port, string username, string password, FtpClientFtpSEncryptionMode encryptionMode, SslProtocols sslProtocols = SslProtocols.None)
        {
            host = host.CheckNotNullTrimmed(nameof(host));
            port = port.CheckNotZero(nameof(port));
            username = username.TrimOrNull();
            password = password.TrimOrNull();
            if (username == null) username = password = "anonymous";

            _client = new FtpClient(host, port, new System.Net.NetworkCredential(username, password));
            _client.ValidateCertificate += new FtpSslValidation((control, e) => { log.Debug("Cert: " + e.Certificate.GetRawCertDataString()); e.Accept = true; });
            _client.EncryptionMode = Util.GetEnumItem<FtpEncryptionMode>(encryptionMode.ToString());

            _client.SslProtocols = sslProtocols;

            log.Debug("Connecting to FTPS server " + host + ":" + port + " with username " + username);
            //FtpTrace.LogPassword = true;
            //FtpTrace.LogPrefix = true;
            _client.OnLogEvent = LogMessage;
            _client.Connect();
            log.Debug("Connection successful");
        }

        private void LogMessage(FtpTraceLevel ftpTraceLevel, string message)
        {
            var msg = "FTP: " + message;
            if (ftpTraceLevel == FtpTraceLevel.Verbose) log.Trace(msg);
            else if (ftpTraceLevel == FtpTraceLevel.Info) log.Debug(msg);
            else if (ftpTraceLevel == FtpTraceLevel.Warn) log.Warn(msg);
            else if (ftpTraceLevel == FtpTraceLevel.Error) log.Error(msg);
        }

        protected override void GetFile(string remoteFile, Stream localStream) => Client.Download(localStream, remoteFile);

        protected override void PutFile(string remoteFile, Stream localStream)
        {
            bool success = false;
            try
            {
                Client.Upload(localStream, remoteFile);
                success = true;
            }
            catch (Exception e)
            {
                log.Warn("Error putting file using security protocol, retrying with all known security protocols", e);
            }

            if (!success)
            {
                try
                {
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    Client.Upload(localStream, remoteFile);
                }
                catch (Exception ee)
                {
                    log.Error("Error putting file (second time)", ee);
                    throw;
                }

            }
        }

        protected override void ListFiles(string remotePath, List<FtpClientFile> fileList)
        {
            foreach (var file in remotePath.TrimOrNull() == null ? Client.GetListing() : Client.GetListing(remotePath))
            {
                var name = file.Name;
                var fullName = file.FullName;
                if (!fullName.StartsWith("/")) fullName = "/" + fullName;

                var type = FtpClientFileType.Unknown;
                if (file.Type == FtpFileSystemObjectType.Directory) type = FtpClientFileType.Directory;
                else if (file.Type == FtpFileSystemObjectType.File) type = FtpClientFileType.File;
                else if (file.Type == FtpFileSystemObjectType.Link) type = FtpClientFileType.Link;

                fileList.Add(new FtpClientFile(name, fullName, type));
            }
        }

        protected override string GetServerInfo() => Client.ServerOS.ToString() + " : " + Client.ServerType.ToString();

        protected override void DeleteFileSingle(string remoteFile)
        {
            log.Debug("Deleting remote file: " + remoteFile);
            Client.DeleteFile(remoteFile);
        }

        public override void Dispose()
        {
            var c = _client;
            _client = null;

            if (c == null) return;
            try
            {
                c.Disconnect();
            }
            catch (Exception e)
            {
                log.Warn($"Error disconnecting from server", e);
            }

            try
            {
                c.Dispose();
            }
            catch (Exception e)
            {
                log.Warn($"Error disposing of {c.GetType().FullNameFormatted()}", e);
            }
        }

        protected override bool ExistsFile(string remoteFile) => Client.FileExists(remoteFile);
        protected override bool ExistsDirectory(string remoteDirectory) => Client.DirectoryExists(remoteDirectory);
    }
}
