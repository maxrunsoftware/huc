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
using System.Linq;
using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public abstract class ActiveDirectoryBase : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddParameter(nameof(host), "h", "ActiveDirectory server host name or IP");
            help.AddParameter(nameof(port), "o", "ActiveDirectory server port (" + Ldap.LDAP_PORT + ")");
            help.AddParameter(nameof(username), "u", "ActiveDirectory server username");
            help.AddParameter(nameof(password), "p", "ActiveDirectory server password");
            help.AddParameter(nameof(domainName), "d", "ActiveDirectory domain name");
        }

        private string host;
        private ushort port;
        private string username;
        private string password;
        private string domainName;

        protected override void ExecuteInternal()
        {
            if (!Constant.OS_WINDOWS) throw new Exception("This function is only supported on Windows clients");
            host = GetArgParameterOrConfigRequired(nameof(host), "h");
            port = GetArgParameterOrConfigInt(nameof(port), "o", Ldap.LDAP_PORT).ToString().ToUShort();
            username = GetArgParameterOrConfig(nameof(username), "u").TrimOrNull();
            if (username != null) password = GetArgParameterOrConfigRequired(nameof(password), "p");
            else GetArgParameterOrConfig(nameof(password), "p");

            domainName = GetArgParameterOrConfig(nameof(domainName), "d");

            using (var ad = new ActiveDirectory(
                server: host,
                userName: username,
                password: password,
                ldapPort: port,
                domainName: domainName
                )
            )
            {
                ExecuteInternal(ad);
            }
        }

        protected abstract void ExecuteInternal(ActiveDirectory ad);
        protected string HelpExamplePrefix => "-h=192.168.1.5 -u=administrator -p=testpass";

        protected ActiveDirectoryObject FindUser(ActiveDirectory ad, string samAccountName, string ou)
        {
            var users = ad.GetUsers();
            var ado = users
                .Where(o => samAccountName.EqualsCaseInsensitive(o.SAMAccountName))
                .Where(o => ActiveDirectory.MatchesDN(o.OrganizationalUnit, ou))
                .FirstOrDefault();

            if (ado == null) ado = users
                .Where(o => samAccountName.EqualsCaseInsensitive(o.UID))
                .Where(o => ActiveDirectory.MatchesDN(o.OrganizationalUnit, ou))
                .FirstOrDefault();

            if (ado == null) ado = users
                .Where(o => samAccountName.EqualsCaseInsensitive(o.SAMAccountName))
                .FirstOrDefault();

            if (ado == null) ado = users
                .Where(o => samAccountName.EqualsCaseInsensitive(o.UID))
                .FirstOrDefault();

            return ado;
        }
    }
}
