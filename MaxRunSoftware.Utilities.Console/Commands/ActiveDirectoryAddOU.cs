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

using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class ActiveDirectoryAddOU : ActiveDirectoryBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Adds a new OU to ActiveDirectory");
            help.AddParameter(nameof(parentOU), "pou", "The parent OU or leave empty to place the new OU at the top level");
            help.AddValue("<new_OU_Name> <optional_description>");
            help.AddExample(HelpExamplePrefix + " MyNewOU");
            help.AddExample(HelpExamplePrefix + " -pou=Users MyNewOU \"This is my new OU\"");
        }

        private string parentOU;

        protected override void ExecuteInternal(ActiveDirectory ad)
        {
            parentOU = GetArgParameterOrConfig(nameof(parentOU), "pou").TrimOrNull();

            var newOUName = GetArgValueTrimmed(0);
            newOUName.CheckValueNotNull(nameof(newOUName), log);

            var newOUDescription = GetArgValueTrimmed(1);
            log.DebugParameter(nameof(newOUDescription), newOUDescription);

            log.Debug("Adding OU: " + newOUName);
            ad.AddOU(
                newOUName,
                parentOUName: parentOU,
                description: newOUDescription
                );
            log.Info("Successfully added OU " + newOUName);
        }
    }
}
