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
    public class ActiveDirectoryListObjects : ActiveDirectoryListBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Lists all or pattern matched objects in an ActiveDirectory");
            help.AddExample(HelpExamplePrefix);
            help.AddExample(HelpExamplePrefix + " ?teve*");
            help.AddValue("<Optional object name or pattern to match on>");
            base.CreateHelp(help);
        }

        protected override bool IsValidObject(ActiveDirectoryObject obj)
        {
            if (objectPattern == null) return true;
            return obj.ObjectName.EqualsWildcard(objectPattern, true);
        }

        private string objectPattern;

        protected override void ExecuteInternal(ActiveDirectory ad)
        {
            objectPattern = GetArgValueTrimmed(0);
            log.Debug($"{nameof(objectPattern)}: {objectPattern}");
            base.ExecuteInternal(ad);
        }
    }
}
