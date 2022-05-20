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
using System.Text;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class SystemInfo : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Displays various system information");
            help.AddParameter(nameof(showOS), "os", "Shows Operating System information (false)");
            help.AddExample("");
            help.AddExample("-os");
            help.AddDetail("If no arguments are specified then all information is displayed");
        }

        private bool showOS;

        private enum InfoType { OperatingSystem }

        private void AddOS(IDictionary<string, object> d)
        {
            d["Type"] = Constant.OS;
            d["Architecture"] = (Constant.OS_X32 ? "32" : "64") + "-bit";

            var os = Environment.OSVersion;

            d[nameof(os.Platform)] = os.Platform;
            d[nameof(os.VersionString)] = os.VersionString;
            d[nameof(os.Version) + nameof(os.Version.Major)] = os.Version.Major;
            d[nameof(os.Version) + nameof(os.Version.Minor)] = os.Version.Minor;
            d[nameof(os.ServicePack)] = os.ServicePack;
        }

        protected override void ExecuteInternal()
        {
            showOS = GetArgParameterOrConfigBool(nameof(showOS), "os", false);

            if (!showOS)
            {
                showOS = true;
            }

            var d = new IndexedDictionary<InfoType, IDictionary<string, object>>();

            if (showOS) AddOS(d[InfoType.OperatingSystem] = new IndexedDictionary<string, object>());

            var sb = new StringBuilder();
            foreach (var kvp in d)
            {
                var name = kvp.Key.ToString().SplitOnCamelCase().ToStringDelimited(" ");
                sb.AppendLine(name);
                var padlength = kvp.Value.Select(o => o.Key.Length + 2).Max();
                foreach (var kvp2 in kvp.Value)
                {
                    var itemName = kvp2.Key + ":";
                    itemName = itemName.TrimOrNull();
                    itemName = itemName.PadRight(padlength, ' ');
                    var itemValue = kvp2.Value.ToStringGuessFormat() ?? "";
                    sb.AppendLine("\t" + itemName + itemValue);
                }
                sb.AppendLine();
            }
            sb.TrimOrNull().AppendLine();

            log.Info(sb.ToString());
        }
    }
}
