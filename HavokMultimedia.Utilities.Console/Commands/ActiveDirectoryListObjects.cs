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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public abstract class ActiveDirectoryListBase : ActiveDirectoryBase
    {
        protected abstract string Summary { get; }
        protected virtual string Example => "-h=192.168.1.5 -u=administrator -p=testpass";
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary(Summary);
            help.AddExample(Example);
        }

        protected override void ExecuteInternal()
        {
            base.ExecuteInternal();

            using (var ad = GetActiveDirectory())
            {
                var objects = ad.GetAll().OrEmpty();
                foreach (var obj in objects.OrderBy(o => o.DistinguishedName, StringComparer.OrdinalIgnoreCase))
                {
                    if (IsValidObject(obj)) log.Info(Display(obj));
                }
            }

        }

        protected virtual string Display(ActiveDirectoryObject obj)
        {
            return (obj.LogonNamePreWindows2000 ?? obj.LogonName ?? obj.Name) + "  -->  " + obj.DistinguishedName;
        }

        protected abstract bool IsValidObject(ActiveDirectoryObject obj);
    }

    public class ActiveDirectoryListObjects : ActiveDirectoryListBase
    {
        protected override string Summary => "Lists all object names in an ActiveDirectory";
        protected override bool IsValidObject(ActiveDirectoryObject obj) => true;
    }

    public class ActiveDirectoryListUsers : ActiveDirectoryListBase
    {
        protected override string Summary => "Lists all user names in an ActiveDirectory";
        protected override bool IsValidObject(ActiveDirectoryObject obj) => obj.IsUser;
    }

    public class ActiveDirectoryListUsersOfGroup : ActiveDirectoryListBase
    {
        protected override string Summary => "Lists all user names that are members of the specified group in an ActiveDirectory";
        protected override string Example => base.Example + " M?Group*";
        protected override bool IsValidObject(ActiveDirectoryObject obj) => obj.IsUser && obj.MemberOfNames.Any(o => o.EqualsWildcard(group, true));
        private string group;
        protected override void ExecuteInternal()
        {
            group = GetArgValueTrimmed(0);
            log.Debug($"{nameof(group)}: {group}");
            if (group == null) throw new ArgsException(nameof(group), $"No {nameof(group)} specified");

            base.ExecuteInternal();
        }

        protected override string Display(ActiveDirectoryObject obj)
        {
            var matchedGroups = new List<string>();
            foreach (var m in obj.MemberOfNames)
            {
                if (m.EqualsWildcard(group, true)) matchedGroups.Add(m);
            }
            return (obj.LogonNamePreWindows2000 ?? obj.LogonName ?? obj.Name) + "," + obj.DistinguishedName + "," + matchedGroups.ToStringDelimited("|");
        }
    }

    public class ActiveDirectoryListGroups : ActiveDirectoryListBase
    {
        protected override string Summary => "Lists all group names in an ActiveDirectory";
        protected override bool IsValidObject(ActiveDirectoryObject obj) => obj.IsGroup;
    }

    public class ActiveDirectoryListComputers : ActiveDirectoryListBase
    {
        protected override string Summary => "Lists all computer names in an ActiveDirectory";
        protected override bool IsValidObject(ActiveDirectoryObject obj) => obj.IsComputer;
    }
}
