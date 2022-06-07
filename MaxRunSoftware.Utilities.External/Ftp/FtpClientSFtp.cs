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
using Renci.SshNet;

namespace MaxRunSoftware.Utilities.External
{
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
}
