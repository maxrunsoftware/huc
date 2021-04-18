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

using System;
using System.Linq;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class ActiveDirectoryListUserDetails : ActiveDirectoryBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Lists user details of a user account in an ActiveDirectory");
            help.AddValue("<username>");
        }

        protected override void Execute()
        {
            base.Execute();

            var values = GetArgValues().TrimOrNull().WhereNotNull();
            var username = values.GetAtIndexOrDefault(0);
            log.Debug(nameof(username) + ": " + username);
            if (username == null) throw new ArgsException(nameof(username), "No <" + nameof(username) + "> specified to search for");

            using (var ad = GetActiveDirectory())
            {
                var user = ad.GetAll().OrEmpty()
                    .Where(o => o.IsUser)
                    .Where(o => username.EqualsCaseInsensitive(new string[] { o.SAMAccountName, o.LogonName, o.LogonNamePreWindows2000 }))
                    .FirstOrDefault();

                if (user == null)
                {
                    log.Info("User " + username + " not found");
                }
                else
                {
                    foreach (var kvp in user.GetAllProperties().OrderBy(o => o.Key, StringComparer.OrdinalIgnoreCase))
                    {

                        log.Info(kvp.Key + ": " + kvp.Value);
                    }
                }

            }
        }
    }
}
