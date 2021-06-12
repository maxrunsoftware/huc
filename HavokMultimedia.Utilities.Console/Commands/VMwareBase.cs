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
    public abstract class VMwareBase : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddParameter(nameof(host), "h", "VMware server host name or IP");
            help.AddParameter(nameof(username), "u", "VMware server username");
            help.AddParameter(nameof(password), "p", "VMware server password");
        }

        private string host;
        private string username;
        private string password;

        protected override void ExecuteInternal()
        {
            host = GetArgParameterOrConfigRequired(nameof(host), "h");
            username = GetArgParameterOrConfig(nameof(username), "u");
            password = GetArgParameterOrConfig(nameof(password), "p");

            using (var vmware = GetVMware())
            {
                ExecuteInternal(vmware);
            }
        }

        protected abstract void ExecuteInternal(VMware vmware);
        protected string HelpExamplePrefix => "-h=192.168.1.5 -u=administrator -p=testpass";
        protected VMware GetVMware()
        {
            if (host == null) throw new Exception("base.Execute() never called for class " + GetType().FullNameFormatted());
            return new VMware(host, username, password);
        }




    }
}
