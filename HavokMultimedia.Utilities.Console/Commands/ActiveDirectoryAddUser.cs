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
    public class ActiveDirectoryAddUser : ActiveDirectoryBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Adds a new user to ActiveDirectory");
            help.AddParameter("firstname", "fn", "The firstname of the new user");
            help.AddParameter("lastname", "ln", "The lastname of the new user");
            help.AddParameter("displayname", "dn", "The displayname of the new user");
            help.AddParameter("emailaddress", "ea", "The email address of the new user");
            help.AddValue("<SAMAccountName>");
            help.AddExample("-h=192.168.1.5 -u=administrator -p=testpass testuser");
            help.AddExample("-h=192.168.1.5 -u=administrator -p=testpass -fn=First -fn=Last -dn=MyUser testuser");
        }

        protected override void ExecuteInternal()
        {
            base.ExecuteInternal();

            var firstname = GetArgParameterOrConfig("firstname", "fn").TrimOrNull();
            var lastname = GetArgParameterOrConfig("lastname", "ln").TrimOrNull();
            var displayname = GetArgParameterOrConfig("displayname", "dn").TrimOrNull();
            var emailaddress = GetArgParameterOrConfig("emailaddress", "ea").TrimOrNull();

            var samAccountName = GetArgValueTrimmed(0);
            log.Debug(nameof(samAccountName) + ": " + samAccountName);
            if (samAccountName == null) throw new ArgsException(nameof(samAccountName), $"No {nameof(samAccountName)} specified");

            using (var ad = GetActiveDirectory())
            {
                log.Debug("Adding user: " + samAccountName);
                ad.AddUser(
                    samAccountName,
                    displayName: displayname,
                    firstName: firstname,
                    lastName: lastname,
                    emailAddress: emailaddress
                    );
                log.Info("Successfully added user " + samAccountName);

                //log.Debug("Changing password for user " + samAccountName + "   " + ou);
                //ado.Password = userpassword;
                //log.Info("Successfully set password for user " + samAccountName + "   " + ou);

            }


        }
    }
}
