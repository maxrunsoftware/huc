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
using System.Linq;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class ShowProperties : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Shows property file definitions");
            help.AddParameter(nameof(showUndefined), "a", "Shows undefined properties too");
            help.AddValue("<propertiesFile> | CURRENT");
            help.AddExample("");
            help.AddExample("-a");
        }

        private bool showUndefined;

        protected override void ExecuteInternal()
        {
            showUndefined = GetArgParameterOrConfigBool(nameof(showUndefined), "s", false);
            var propertiesFile = GetArgValueTrimmed(0);
            log.DebugParameter(nameof(propertiesFile), propertiesFile);

            var configFile = propertiesFile == null ? new ConfigFile() : new ConfigFile(propertiesFile);

            var keys = new List<string>();

            if (showUndefined) keys.AddRange(ConfigFile.GetAllKeys());
            var set = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
            foreach (var key in configFile.Keys)
            {
                if (set.Add(key)) keys.Add(key);
            }

            foreach (var prop in keys)
            {
                log.Info(prop + "=" + configFile[prop]);
            }
        }
    }
}
