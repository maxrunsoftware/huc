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

using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class ActiveDirectoryAddGroup : ActiveDirectoryBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Adds a new group to ActiveDirectory");
            help.AddParameter(nameof(groupType), "gt", "The group type of the new group (" + ActiveDirectoryGroupType.GlobalSecurityGroup + ")  " + DisplayEnumOptions<ActiveDirectoryGroupType>());
            help.AddValue("<SAMAccountName>");
            help.AddExample(HelpExamplePrefix + " testgroup");
            help.AddExample(HelpExamplePrefix + " -gt=" + ActiveDirectoryGroupType.GlobalSecurityGroup + " testgroup");
        }

        private ActiveDirectoryGroupType groupType;

        protected override void ExecuteInternal(ActiveDirectory ad)
        {
            groupType = GetArgParameterOrConfigEnum(nameof(groupType), "gt", ActiveDirectoryGroupType.GlobalSecurityGroup);

            var samAccountName = GetArgValueTrimmed(0);
            samAccountName.CheckValueNotNull(nameof(samAccountName), log);

            log.Debug("Adding group: " + samAccountName);
            ad.AddGroup(
                samAccountName,
                groupType
                );
            log.Info("Successfully added group " + samAccountName);



        }
    }
}
