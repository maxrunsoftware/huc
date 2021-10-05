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
using System.Linq;
using MaxRunSoftware.Utilities;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    [SuppressBanner]
    public class EncodePassword : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Encodes a password to be used in a HUC properties file");
            help.AddValue("<password>");
            help.AddExample("");
            help.AddExample("mySecretPassword");
        }

        protected override void ExecuteInternal()
        {
            var password = GetArgValueTrimmed(0);
            log.DebugParameter(nameof(password), password);
            password.CheckValueNotNull(nameof(password), log);

            var encodedPassword = PasswordEncode(password);
            log.Info(encodedPassword);

        }

    }
}
