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

using System.Collections.Generic;
using System.Linq;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class ActiveDirectoryListUsersOfGroup : ActiveDirectoryListBase
    {
        protected override string Summary => "Lists all user names that are members of the specified group in an ActiveDirectory";
        protected override string Example => base.Example + " M?Group*";
        protected override bool IsValidObject(ActiveDirectoryObject obj) => obj.IsUser && obj.MemberOfNames.Any(o => o.EqualsWildcard(group, true));
        private string group;

        protected override void ParseParameters()
        {
            base.ParseParameters();
            group = GetArgValueTrimmed(0);
            log.Debug($"{nameof(group)}: {group}");
            if (group == null) throw new ArgsException(nameof(group), $"No {nameof(group)} specified");
        }

        protected override string Display(ActiveDirectoryObject obj, Format format)
        {
            var matchedGroups = new List<string>();
            foreach (var m in obj.MemberOfNames)
            {
                if (m.EqualsWildcard(group, true)) matchedGroups.Add(m);
            }
            if (format == Format.Display) return base.Display(obj, format) + " (" + matchedGroups.ToStringDelimited(",") + ")";
            if (format == Format.TAB) return base.Display(obj, format) + "\t" + matchedGroups.ToStringDelimited(",");
            return base.Display(obj, format);
        }

    }
}
