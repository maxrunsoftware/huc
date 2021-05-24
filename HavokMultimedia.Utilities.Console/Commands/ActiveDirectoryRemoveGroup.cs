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
    public class ActiveDirectoryRemoveGroup : ActiveDirectoryBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Removes a group from ActiveDirectory");
            //help.AddDetail("Requires LDAPS configured on the server");
            help.AddValue("<SAMAccountName>");
            help.AddExample("-h=192.168.1.5 -u=administrator -p=testpass testgroup");
        }

        protected override void ExecuteInternal()
        {
            base.ExecuteInternal();
            var values = GetArgValues().TrimOrNull().WhereNotNull();

            var samAccountName = values.GetAtIndexOrDefault(0).TrimOrNull();
            log.Debug(nameof(samAccountName) + ": " + samAccountName);
            if (samAccountName == null) throw new ArgsException(nameof(samAccountName), $"No {nameof(samAccountName)} specified");

            using (var ad = GetActiveDirectory())
            {
                log.Debug("Removing group " + samAccountName);
                ad.RemoveGroup(samAccountName);
                log.Info("Successfully removed group " + samAccountName);

            }


        }
    }
}
