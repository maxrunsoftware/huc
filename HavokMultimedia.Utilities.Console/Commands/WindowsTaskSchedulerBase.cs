// /*
// Copyright (c) 2021 Steven Foster (steven.d.foster@gmail.com)
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
// */
using System;
using System.Collections.Generic;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{

    public abstract class WindowsTaskSchedulerBase : Command
    {


        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddParameter("host", "h", "Server hostname or IP");
            help.AddParameter("username", "u", "Server username");
            help.AddParameter("password", "p", "Server password");
            help.AddParameter("forceV1", "v1", "Server force version 1 task scheduler implementation (false)");
        }

        protected WindowsTaskScheduler GetTaskScheduler() => new WindowsTaskScheduler(host, username, password, forceV1: forceV1);

        private string host;
        private string username;
        private string password;
        private bool forceV1;


        protected override void Execute()
        {
            host = GetArgParameterOrConfigRequired("host", "h").TrimOrNull();

            username = GetArgParameterOrConfigRequired("username", "u").TrimOrNull();

            password = GetArgParameterOrConfigRequired("password", "p").TrimOrNull();

            forceV1 = GetArgParameterOrConfigBool("forceV1", "v1", false);
        }




    }
}
