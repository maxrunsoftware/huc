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

using System.Linq;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class ActiveDirectoryDeleteUser : ActiveDirectoryBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Deletes a user from ActiveDirectory");
            //help.AddDetail("Requires LDAPS configured on the server");
            help.AddValue("<SAMAccountName> <Optional OU>");
            help.AddExample("-h=192.168.1.5 -u=administrator -p=testpass testuser");
            help.AddExample("-h=192.168.1.5 -u=administrator -p=testpass testuser CN=Users,DC=testdomain,DC=test,DC=org");
        }

        protected override void Execute()
        {
            base.Execute();
            var values = GetArgValues().TrimOrNull().WhereNotNull();

            var samAccountName = values.GetAtIndexOrDefault(0);
            log.Debug(nameof(samAccountName) + ": " + samAccountName);
            if (samAccountName == null) throw new ArgsException(nameof(samAccountName), $"No {nameof(samAccountName)} specified");

            var ou = values.GetAtIndexOrDefault(1);
            log.Debug(nameof(ou) + ": " + ou);

            using (var ad = GetActiveDirectory())
            {
                if (ou == null)
                {
                    ou = ad.DomainUsersGroupDN;
                    log.Debug(nameof(ou) + ": " + ou);
                }

                var ado = FindUser(ad, samAccountName, ou);
                if (ado == null)
                {
                    log.Warn("User does not exist " + samAccountName + "   " + ou);
                }

                log.Debug("Deleting user " + samAccountName + "   " + ou);
                ad.DeleteObject(ado);
                log.Info("Successfully deleted user " + samAccountName + "   " + ou);

            }


        }
    }
}
