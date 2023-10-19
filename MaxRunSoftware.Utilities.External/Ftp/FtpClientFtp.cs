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
using System.Net;
using System.Security.Authentication;
using FluentFTP;

namespace MaxRunSoftware.Utilities.External;

public class FtpClientFtp : FtpClientBase
{
    private FtpClient client;

    private FtpClient Client
    {
        get
        {
            var c = client;
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
        username ??= password = "anonymous";
        client = new FtpClient(host: host, port: port, user: username, pass: password, logger: new FtpLogger(log));
        client.Connect();
        log.Debug("Connecting to FTP server " + host + ":" + port + " with username " + username);
        //FtpTrace.LogPassword = true;
        //FtpTrace.LogPrefix = true;
        client.Connect();
        log.Debug("Connection successful");
    }

    public FtpClientFtp(string host, ushort port, string username, string password, FtpClientFtpSEncryptionMode encryptionMode, SslProtocols sslProtocols = SslProtocols.None)
    {
        host = host.CheckNotNullTrimmed(nameof(host));
        port = port.CheckNotZero(nameof(port));
        username = username.TrimOrNull();
        password = password.TrimOrNull();
        username ??= password = "anonymous";

        client = new FtpClient(host: host, port: port, user: username, pass: password, logger: new FtpLogger(log));
        client.ValidateCertificate += (_, e) =>
        {
            log.Debug("Cert: " + e.Certificate.GetRawCertDataString());
            e.Accept = true;
        };

        client.Config.EncryptionMode = (FtpEncryptionMode)typeof(FtpEncryptionMode).GetEnumValue(encryptionMode.ToString());
        client.Config.SslProtocols = sslProtocols;

        log.Debug("Connecting to FTPS server " + host + ":" + port + " with username " + username);
        //FtpTrace.LogPassword = true;
        //FtpTrace.LogPrefix = true;
        client.Connect();
        log.Debug("Connection successful");
    }

    private class FtpLogger : IFtpLogger
    {
        private readonly ILogger log;
        public FtpLogger(ILogger log) => this.log = log.CheckNotNull(nameof(log));

        public void Log(FtpLogEntry entry)
        {
            var msg = "FTP: ";
            if (entry.Message != null) msg += entry.Message;
            if (entry.Exception != null) msg += entry.Exception.ToString();

            var ftpTraceLevel = entry.Severity;
            if (ftpTraceLevel == FtpTraceLevel.Verbose) { log.Trace(msg); }
            else if (ftpTraceLevel == FtpTraceLevel.Info) { log.Debug(msg); }
            else if (ftpTraceLevel == FtpTraceLevel.Warn) { log.Warn(msg); }
            else if (ftpTraceLevel == FtpTraceLevel.Error) log.Error(msg);
        }
    }

    protected override void GetFile(string remoteFile, Stream localStream) => Client.DownloadStream(localStream, remoteFile);

    protected override void PutFile(string remoteFile, Stream localStream)
    {
        var success = false;
        try
        {
            Client.UploadStream(localStream, remoteFile);
            success = true;
        }
        catch (Exception e) { log.Warn("Error putting file using security protocol, retrying with all known security protocols", e); }

        if (!success)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                Client.UploadStream(localStream, remoteFile);
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
            if (file.Type == FtpObjectType.Directory) { type = FtpClientFileType.Directory; }
            else if (file.Type == FtpObjectType.File) { type = FtpClientFileType.File; }
            else if (file.Type == FtpObjectType.Link) type = FtpClientFileType.Link;

            fileList.Add(new FtpClientFile(name, fullName, type));
        }
    }

    protected override string GetServerInfo() => Client.ServerOS + " : " + Client.ServerType;

    protected override void DeleteFileSingle(string remoteFile)
    {
        log.Debug("Deleting remote file: " + remoteFile);
        Client.DeleteFile(remoteFile);
    }

    public override void Dispose()
    {
        var c = client;
        client = null;

        if (c == null) return;

        try { c.Disconnect(); }
        catch (Exception e) { log.Warn("Error disconnecting from server", e); }

        try { c.Dispose(); }
        catch (Exception e) { log.Warn($"Error disposing of {c.GetType().FullNameFormatted()}", e); }
    }

    protected override bool ExistsFile(string remoteFile) => Client.FileExists(remoteFile);

    protected override bool ExistsDirectory(string remoteDirectory) => Client.DirectoryExists(remoteDirectory);
}
