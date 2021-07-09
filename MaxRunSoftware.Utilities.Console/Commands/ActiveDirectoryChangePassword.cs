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
    public class ActiveDirectoryChangePassword : ActiveDirectoryBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Changes the password of a user in ActiveDirectory");
            help.AddDetail("Requires LDAPS configured on the server or running on the AD server itself");
            help.AddValue("<SAMAccountName> <new password>");
            help.AddExample(HelpExamplePrefix + " testuser newpassword");
        }

        protected override void ExecuteInternal(ActiveDirectory ad)
        {
            var samAccountName = GetArgValueTrimmed(0);
            samAccountName.CheckValueNotNull(nameof(samAccountName), log);

            var newPassword = GetArgValueTrimmed(1);
            newPassword.CheckValueNotNull(nameof(newPassword), log);

            log.Debug($"Changing password for user {samAccountName} to {newPassword}");
            ad.ChangePassword(samAccountName, newPassword);
            log.Info($"Password changed successfully");
        }
    }
}
