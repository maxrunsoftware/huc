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

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class ActiveDirectoryMoveUser : ActiveDirectoryBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Moves a user from one OU to another OU in ActiveDirectory");
            help.AddValue("<SAMAccountName> <new OU samAccountName>");
            help.AddExample("-h=192.168.1.5 -u=administrator -p=testpass testuser MyNewOU");
        }

        protected override void ExecuteInternal()
        {
            base.ExecuteInternal();

            var samAccountName = GetArgValueTrimmed(0);
            log.Debug(nameof(samAccountName) + ": " + samAccountName);
            if (samAccountName == null) throw new ArgsException(nameof(samAccountName), $"No {nameof(samAccountName)} specified");

            var newOUSAMAccountName = GetArgValueTrimmed(1);
            log.Debug(nameof(newOUSAMAccountName) + ": " + newOUSAMAccountName);
            if (newOUSAMAccountName == null) throw new ArgsException(nameof(newOUSAMAccountName), $"No {nameof(newOUSAMAccountName)} specified");

            using (var ad = GetActiveDirectory())
            {
                log.Debug($"Changing OU of user {samAccountName} to {newOUSAMAccountName}");
                ad.MoveUser(samAccountName, newOUSAMAccountName);
                log.Info($"User {samAccountName} successfully moved to {newOUSAMAccountName}");
            }


        }
    }
}
