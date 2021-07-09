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
    public class ActiveDirectoryAddUserToGroup : ActiveDirectoryBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Adds a user to the specified group in ActiveDirectory");
            help.AddValue("<SAMAccountName> <group1> <group2> <etc>");
            help.AddExample(HelpExamplePrefix + " testuser MyGroup1 SomeOtherGroup");
        }

        protected override void ExecuteInternal(ActiveDirectory ad)
        {
            var values = GetArgValuesTrimmed1N();
            var samAccountName = values.firstValue;
            samAccountName.CheckValueNotNull(nameof(samAccountName), log);

            var groups = values.otherValues;
            log.Debug(groups, nameof(groups));
            if (groups.IsEmpty()) throw ArgsException.ValueNotSpecified(nameof(groups));

            log.Debug("Adding user " + samAccountName + " to groups " + groups.ToStringDelimited(", "));

            foreach (var group in groups)
            {
                log.Debug("Adding user " + samAccountName + " to group " + group);
                ad.AddUserToGroup(samAccountName, group);
            }

            log.Info("Successfully added user " + samAccountName + " to groups " + groups.ToStringDelimited(", "));
        }
    }
}
