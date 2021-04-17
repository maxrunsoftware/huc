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

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class ShowProperties : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Shows property file definitions");
            help.AddParameter("showUndefined", "a", "Shows undefined properties too");
            help.AddValue("<propertiesFile> | CURRENT");
            help.AddExample("");
            help.AddExample("-a");
        }

        protected override void Execute()
        {
            var showUndefined = GetArgParameterOrConfigBool("showUndefined", "s", false);
            var fileLocation = GetArgValues().FirstOrDefault();
            ConfigFile c;
            if (fileLocation == null)
            {
                c = new ConfigFile();
            }
            else
            {
                c = new ConfigFile(fileLocation);
            }

            var keys = new List<string>();

            if (showUndefined) keys.AddRange(ConfigFile.GetAllKeys());
            var set = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
            foreach (var key in c.Keys)
            {
                if (set.Add(key)) keys.Add(key);
            }

            foreach (var prop in keys)
            {
                log.Info(prop + "=" + c[prop]);
            }
        }
    }
}
