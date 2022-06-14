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
using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public abstract class WindowsTaskSchedulerBase : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddParameter(nameof(host), "h", "Server hostname or IP");
            help.AddParameter(nameof(username), "u", "Server username");
            help.AddParameter(nameof(password), "p", "Server password");
            help.AddParameter(nameof(forceV1), "v1", "Server force version 1 task scheduler implementation (false)");
        }

        protected string HelpExamplePrefix => "-h=192.168.1.5 -u=administrator -p=testpass";

        protected WindowsTaskScheduler GetTaskScheduler()
        {
            if (host == null) throw new Exception("base.Execute() never called for class " + GetType().FullNameFormatted());

            return new WindowsTaskScheduler(host, username, password, forceV1: forceV1);
        }

        private string host;
        private string username;
        private string password;
        private bool forceV1;

        protected override void ExecuteInternal()
        {
            host = GetArgParameterOrConfigRequired(nameof(host), "h").TrimOrNull();
            username = GetArgParameterOrConfigRequired(nameof(username), "u").TrimOrNull();
            password = GetArgParameterOrConfigRequired(nameof(password), "p").TrimOrNull();
            forceV1 = GetArgParameterOrConfigBool(nameof(forceV1), "v1", false);
        }

        protected bool RemapUsername(ref string username)
        {
            if (username == null)
            {
                return false;
            }

            if (username.EqualsCaseInsensitive("SYSTEM"))
            {
                username = WindowsTaskScheduler.USER_SYSTEM;
                return true;
            }
            if (username.EqualsCaseInsensitive("LOCALSERVICE"))
            {
                username = WindowsTaskScheduler.USER_LOCALSERVICE;
                return true;
            }
            if (username.EqualsCaseInsensitive("NETWORKSERVICE"))
            {
                username = WindowsTaskScheduler.USER_NETWORKSERVICE;
                return true;
            }

            return false;
        }

    }
}
