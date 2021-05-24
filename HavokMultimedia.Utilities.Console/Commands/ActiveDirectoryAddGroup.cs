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
    public class ActiveDirectoryAddGroup : ActiveDirectoryBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Adds a new group to ActiveDirectory");
            help.AddParameter("groupType", "gt", "The group type of the new group (" + ActiveDirectoryGroupType.GlobalSecurityGroup + ")  " + DisplayEnumOptions<ActiveDirectoryGroupType>());
            help.AddValue("<SAMAccountName>");
            help.AddExample("-h=192.168.1.5 -u=administrator -p=testpass testgroup");
            help.AddExample("-h=192.168.1.5 -u=administrator -p=testpass -gt=" + ActiveDirectoryGroupType.GlobalSecurityGroup + " testgroup");
        }

        protected override void ExecuteInternal()
        {
            base.ExecuteInternal();

            var groupType = GetArgParameterOrConfigEnum("groupType", "gt", ActiveDirectoryGroupType.GlobalSecurityGroup);

            var samAccountName = GetArgValueTrimmed(0);
            log.Debug(nameof(samAccountName) + ": " + samAccountName);
            if (samAccountName == null) throw new ArgsException(nameof(samAccountName), $"No {nameof(samAccountName)} specified");

            using (var ad = GetActiveDirectory())
            {
                log.Debug("Adding group: " + samAccountName);
                ad.AddGroup(
                    samAccountName,
                    groupType
                    );
                log.Info("Successfully added group " + samAccountName);
            }


        }
    }
}
