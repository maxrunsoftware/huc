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

using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class ActiveDirectoryListComputers : ActiveDirectoryListBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Lists all or pattern matched computers in an ActiveDirectory");
            help.AddExample(HelpExamplePrefix);
            help.AddExample(HelpExamplePrefix + " ?yComputer*");
            help.AddValue("<Optional computer name or pattern to match on>");
            base.CreateHelp(help);
        }

        protected override bool IsValidObject(ActiveDirectoryObject obj)
        {
            if (!obj.IsComputer) return false;
            if (computerPattern == null) return true;
            return obj.ObjectName.EqualsWildcard(computerPattern, true);
        }

        private string computerPattern;

        protected override void ExecuteInternal(ActiveDirectory ad)
        {
            computerPattern = GetArgValueTrimmed(0);
            log.DebugParameter(nameof(computerPattern), computerPattern);
            base.ExecuteInternal(ad);
        }
    }
}
