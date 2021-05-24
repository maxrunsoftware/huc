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
    public class ActiveDirectoryRemoveUserFromGroup : ActiveDirectoryBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Removes a user from the specified group in ActiveDirectory");
            help.AddValue("<SAMAccountName> <group1> <group2> <etc>");
            help.AddExample(HelpExamplePrefix + " testuser MyGroup1 SomeOtherGroup");
        }

        protected override void ExecuteInternal(ActiveDirectory ad)
        {
            var values = GetArgValuesTrimmed1N();
            var samAccountName = values.firstValue;
            log.Debug(nameof(samAccountName) + ": " + samAccountName);
            if (samAccountName == null) throw new ArgsException(nameof(samAccountName), $"No {nameof(samAccountName)} specified");

            var groups = values.otherValues;
            log.Debug(groups, nameof(groups));
            if (groups.IsEmpty()) throw new ArgsException(nameof(groups), $"No {nameof(groups)} specified");

            log.Debug("Removing user " + samAccountName + " from groups " + groups.ToStringDelimited(", "));
            foreach (var group in groups)
            {
                log.Debug("Removing user " + samAccountName + " from group " + group);
                ad.RemoveUserFromGroup(samAccountName, group);
            }
            log.Info("Successfully removed user " + samAccountName + " from groups " + groups.ToStringDelimited(", "));
        }
    }
}
