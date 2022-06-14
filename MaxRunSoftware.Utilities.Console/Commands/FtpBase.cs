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
using System.Security.Authentication;
using System.Text;
using System.Threading;
using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public abstract class FtpBase : Command
    {
        public enum FTPEncryptionMode { None, SSH, Explicit, Implicit }

        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddParameter(nameof(host), "h", "Hostname");
            help.AddParameter(nameof(port), "o", "Port (21/22/990 based on encryptionMode)");
            help.AddParameter(nameof(username), "u", "Username");
            help.AddParameter(nameof(password), "p", "Password");
            help.AddParameter(nameof(encryptionMode), "e", "Encryption Mode. NONE is for standard FTP. SSH is for SFTP. EXPLICIT and IMPLICIT are for FTPS [ NONE | SSH | Explicit | Implicit ] (NONE)");
            help.AddParameter(nameof(encryptionProtocol), "s", "FTPS encryption protocol [ None | Ssl2 | Ssl3 | Tls | Default | Tls11 | Tls12 ] (None)");
            help.AddParameter(nameof(bufferSizeMegabytes), "b", "SFTP buffer size in megabytes (10)");
            help.AddParameter(nameof(retryCount), "rc", "Number of times to retry connecting on failure (0)");
            help.AddParameter(nameof(retryDelay), "rd", "Seconds to wait before retrying connection on failure (5)");
            help.AddParameter(nameof(privateKey1File), "pk1", "SFTP private key 1 filename");
            help.AddParameter(nameof(privateKey1Password), "pk1pass", "SFTP private key 1 password");
            help.AddParameter(nameof(privateKey2File), "pk2", "SFTP private key 2 filename");
            help.AddParameter(nameof(privateKey2Password), "pk2pass", "SFTP private key 2 password");
            help.AddParameter(nameof(privateKey3File), "pk3", "SFTP private key 3 filename");
            help.AddParameter(nameof(privateKey3Password), "pk3pass", "SFTP private key 3 password");
        }

        protected string HelpExamplePrefix => "-h=192.168.1.5 -u=testuser -p=testpass";

        #region Helpers

        private bool ShouldExclude(string filename, string filePattern)
        {
            if (filePattern == null) return false;
            var f = Path.GetFileName(filename);

            if (f.EqualsWildcard(filePattern, true)) return false;

            return true;
        }

        protected static string ParseFileNameFromPath(string path) => PathSplit(path).LastOrDefault();

        protected static string[] PathSplit(string path)
        {
            if (path.TrimOrNull() == null) return Array.Empty<string>();
            return path.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).Where(o => o.TrimOrNull() != null).ToArray();
        }

        protected static string[] ParsePathWithoutFileName(string path)
        {
            var p = PathSplit(path).ToList();
            if (p.Count > 0) p.PopTail();
            return p.ToArray();
        }

        protected static string DetermineLocalFileName(string localFile, string remoteFile)
        {
            if (localFile != null) return localFile;

            var fileName = ParseFileNameFromPath(remoteFile);
            if (fileName == null) throw new ArgumentException($"Could not determine LocalFile from RemoteFile {remoteFile}", "RemoteFile");
            localFile = Path.Combine(Environment.CurrentDirectory, fileName);
            return localFile;
        }

        protected static string PathCombine(string[] pathParts, string fileName)
        {
            pathParts = pathParts ?? Array.Empty<string>();
            var sb = new StringBuilder();
            sb.Append("/");
            foreach (var p in pathParts)
            {
                sb.Append(p);
                sb.Append("/");
            }
            sb.Append(fileName);
            return sb.ToString();
        }

        #endregion Helpers

        private string host;
        private ushort port;
        private string username;
        private string password;
        private FTPEncryptionMode encryptionMode;
        private SslProtocols encryptionProtocol;
        private uint bufferSizeMegabytes;
        private uint retryCount;
        private uint retryDelay;
        private string privateKey1File;
        private string privateKey1Password;
        private string privateKey2File;
        private string privateKey2Password;
        private string privateKey3File;
        private string privateKey3Password;

        protected override void ExecuteInternal()
        {
            host = GetArgParameterOrConfigRequired(nameof(host), "h");

            encryptionMode = GetArgParameterOrConfigEnum(nameof(encryptionMode), "e", FTPEncryptionMode.None);

            var defaultPort = 21;
            if (encryptionMode == FTPEncryptionMode.None) defaultPort = 21;
            else if (encryptionMode == FTPEncryptionMode.SSH) defaultPort = 22;
            else if (encryptionMode == FTPEncryptionMode.Implicit) defaultPort = 990;
            else if (encryptionMode == FTPEncryptionMode.Explicit) defaultPort = 21;
            port = GetArgParameterOrConfigInt(nameof(port), "o", defaultPort).ToString().ToUShort();

            username = GetArgParameterOrConfig(nameof(username), "u").TrimOrNull();

            password = GetArgParameterOrConfig(nameof(password), "p").TrimOrNull();

            encryptionProtocol = GetArgParameterOrConfigEnum(nameof(encryptionProtocol), "s", SslProtocols.None);

            bufferSizeMegabytes = GetArgParameterOrConfigInt(nameof(bufferSizeMegabytes), "b", 10).ToString().ToUInt();

            retryCount = GetArgParameterOrConfigInt(nameof(retryCount), "rc", 0).ToString().ToUInt();
            retryDelay = GetArgParameterOrConfigInt(nameof(retryDelay), "rd", 5).ToString().ToUInt();

            privateKey1File = GetArgParameterOrConfig(nameof(privateKey1File), "pk1").TrimOrNull();
            if (privateKey1File != null)
            {
                privateKey1File = Path.GetFullPath(privateKey1File);
                CheckFileExists(privateKey1File);
            }
            privateKey1Password = GetArgParameterOrConfig(nameof(privateKey1Password), "pk1pass").TrimOrNull();

            privateKey2File = GetArgParameterOrConfig(nameof(privateKey2File), "pk2").TrimOrNull();
            if (privateKey2File != null)
            {
                privateKey2File = Path.GetFullPath(privateKey2File);
                CheckFileExists(privateKey2File);
            }
            privateKey2Password = GetArgParameterOrConfig(nameof(privateKey2Password), "pk2pass").TrimOrNull();

            privateKey3File = GetArgParameterOrConfig(nameof(privateKey3File), "pk3").TrimOrNull();
            if (privateKey3File != null)
            {
                privateKey3File = Path.GetFullPath(privateKey3File);
                CheckFileExists(privateKey3File);
            }
            privateKey3Password = GetArgParameterOrConfig(nameof(privateKey3Password), "pk3pass").TrimOrNull();
        }

        #region OpenClient

        private IFtpClient OpenClientFTP()
        {
            log.Debug($"Connecting to FTP server {host}:{port} with username {username}");
            var c = new FtpClientFtp(host, port, username, password);
            log.Debug("Connection successful");
            return c;
        }

        private IFtpClient OpenClientFTPS()
        {
            var em = Util.GetEnumItem<FtpClientFtpSEncryptionMode>(encryptionMode.ToString());
            log.Debug($"Connecting to {em} FTPS server {host}:{port} with username {username} with EncryptionProtocol={encryptionProtocol}");
            var c = new FtpClientFtp(host, port, username, password, em, encryptionProtocol);
            log.Debug("Connection successful");
            return c;
        }

        private IFtpClient OpenClientSFTP()
        {
            var pkfs = new List<SshKeyFile>();
            if (privateKey1File.TrimOrNull() != null) pkfs.Add(new SshKeyFile(privateKey1File, privateKey1Password));
            if (privateKey2File.TrimOrNull() != null) pkfs.Add(new SshKeyFile(privateKey2File, privateKey2Password));
            if (privateKey3File.TrimOrNull() != null) pkfs.Add(new SshKeyFile(privateKey3File, privateKey3Password));

            FtpClientSFtp c;
            if (pkfs.Count == 0)
            {
                log.Debug($"Connecting to SFTP server {host}:{port} with username {username}");
                c = new FtpClientSFtp(host, port, username, password);
            }
            else
            {
                log.Debug($"Connecting to SFTP server {host}:{port} with username {username} with {pkfs.Count} private keys");
                c = new FtpClientSFtp(host, port, username, pkfs);
            }
            log.Debug("Connection successful");
            var bs = bufferSizeMegabytes * (uint)Constant.BYTES_MEGA;
            log.Debug($"Setting Buffer Size: {bs}");
            c.SetBufferSize(bs);
            return c;
        }

        protected IFtpClient OpenClient()
        {
            if (host == null) throw new Exception("base.Execute() never called for class " + GetType().FullNameFormatted());

            for (int i = 0; i <= retryCount; i++)
            {
                try
                {
                    return encryptionMode switch
                    {
                        FTPEncryptionMode.None => OpenClientFTP(),
                        FTPEncryptionMode.SSH => OpenClientSFTP(),
                        FTPEncryptionMode.Explicit => OpenClientFTPS(),
                        FTPEncryptionMode.Implicit => OpenClientFTPS(),
                        _ => throw new NotImplementedException($"No handler created for FTPEncryptionMode '{encryptionMode}'"),
                    };
                }
                catch (Exception e)
                {
                    if (i == retryCount) throw;
                    else log.Warn("Error connecting to server", e);
                }

                if (retryDelay > 0)
                {
                    log.Info($"Waiting {retryDelay} seconds before retry");
                    Thread.Sleep(1000 * (int)retryDelay);
                    log.Debug("Retrying now");
                }
            }

            throw new NotImplementedException("Should never get here");
        }

        #endregion OpenClient
    }
}
