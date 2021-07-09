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
using Renci.SshNet;

namespace MaxRunSoftware.Utilities.External
{
    public sealed class SshKeyFile
    {
        public string FileName { get; }
        public string Password { get; }

        public SshKeyFile(string fileName, string password = null)
        {
            FileName = Path.GetFullPath(fileName.CheckNotNullTrimmed(nameof(fileName)));
            Password = password.TrimOrNull();
        }
    }

    public class Ssh : IDisposable
    {
        private static readonly ILogger log = Logging.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private SshClient _client;

        private SshClient Client
        {
            get
            {
                var c = _client;
                if (c == null) throw new ObjectDisposedException(GetType().FullNameFormatted());
                return c;
            }
        }

        public string ClientVersion => Client.ConnectionInfo.ClientVersion;

        public Ssh(string host, ushort port, string username, string password) => _client = (SshClient)CreateClient(host, port, username, password, null, typeof(SshClient));

        public Ssh(string host, ushort port, string username, IEnumerable<SshKeyFile> privateKeys) => _client = (SshClient)CreateClient(host, port, username, null, privateKeys, typeof(SshClient));

        public string RunCommand(string command)
        {
            using (var cmd = Client.CreateCommand(command))
            {
                return cmd.Execute();
            }
        }

        #region Create Clients

        private static object CreateClient(string host, ushort port, string username, string password, IEnumerable<SshKeyFile> keyFiles, Type clientType)
        {
            clientType.CheckNotNull(nameof(clientType));
            host = host.CheckNotNullTrimmed(nameof(host));
            port = port.CheckNotZero(nameof(port));
            username = username.CheckNotNullTrimmed(nameof(username));
            password = password.TrimOrNull();
            var pks = (keyFiles ?? Enumerable.Empty<SshKeyFile>()).WhereNotNull().ToList();
            if (password == null && pks.Count == 0) throw new ArgumentException($"No password provided and no keyFiles provided.", nameof(password));
            if (password != null && pks.Count > 0) throw new ArgumentException($"Keyfiles are not supported when a Password is supplied.", nameof(password));

            var pkfs = new List<PrivateKeyFile>();
            foreach (var pk in pks)
            {
                pk.FileName.CheckFileExists();
                PrivateKeyFile pkf;
                if (pk.Password == null)
                {
                    pkf = new PrivateKeyFile(pk.FileName);
                    log.Debug("Using private key file " + pk.FileName);
                }
                else
                {
                    pkf = new PrivateKeyFile(pk.FileName, pk.Password);
                    log.Debug("Using (password protected) private key file " + pk.FileName);
                }
                pkfs.Add(pkf);
            }

            BaseClient client = null;
            if (clientType.Equals(typeof(SshClient))) client = password == null ? new SshClient(host, port, username, pkfs.ToArray()) : new SshClient(host, port, username, password);
            if (clientType.Equals(typeof(SftpClient))) client = password == null ? new SftpClient(host, port, username, pkfs.ToArray()) : new SftpClient(host, port, username, password);
            if (client == null) throw new NotImplementedException("Cannot create SSH Client for type " + clientType.FullNameFormatted());
            try
            {
                log.Debug("Connecting " + clientType.Name + " to server " + host + ":" + port + " with username " + username);
                client.Connect();
                log.Debug("Connection successful");
            }
            catch (Exception)
            {
                try
                {
                    if (client.IsConnected) client.Disconnect();
                }
                catch (Exception ee)
                {
                    log.Warn("Error disconnecting", ee);
                }
                try
                {
                    client.Dispose();
                }
                catch (Exception ee)
                {
                    log.Warn("Error disposing", ee);
                }
                throw;
            }
            return client;
        }

        public static SftpClient CreateSFtpClient(string host, ushort port, string username, string password, IEnumerable<SshKeyFile> keyFiles) => (SftpClient)CreateClient(host, port, username, password, keyFiles, typeof(SftpClient));

        public static SshClient CreateSshClient(string host, ushort port, string username, string password, IEnumerable<SshKeyFile> keyFiles) => (SshClient)CreateClient(host, port, username, password, keyFiles, typeof(SshClient));

        #endregion Create Clients

        public void Dispose()
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
    }
}
