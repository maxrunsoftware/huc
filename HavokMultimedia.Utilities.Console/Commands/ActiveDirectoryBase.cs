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
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public abstract class ActiveDirectoryBase : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddParameter("host", "h", "ActiveDirectory server host name or IP");
            help.AddParameter("port", "o", "ActiveDirectory server port (" + Ldap.LDAP_PORT + ")");
            help.AddParameter("username", "u", "ActiveDirectory server username");
            help.AddParameter("password", "p", "ActiveDirectory server password");
        }

        private string host;
        private ushort port;
        private string username;
        private string password;

        protected override void Execute()
        {
            host = GetArgParameterOrConfigRequired("host", "h");
            port = GetArgParameterOrConfigInt("port", "o", Ldap.LDAP_PORT).ToString().ToUShort();
            username = GetArgParameterOrConfig("username", "u");
            password = GetArgParameterOrConfig("password", "p");
        }

        protected ActiveDirectory GetActiveDirectory()
        {
            if (host == null) throw new Exception("base.Execute() never called for class " + GetType().FullNameFormatted());
            return new ActiveDirectory(server: host, userName: username, password: password, ldapPort: port);
        }
    }
}
