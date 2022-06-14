// Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Linq;
using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class ActiveDirectoryListUsersOfGroup : ActiveDirectoryListBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        help.AddSummary("Lists all user names that are members of the specified group in an ActiveDirectory");
        help.AddValue("<group or group pattern>");
        help.AddExample(HelpExamplePrefix + " M?Group*");
        base.CreateHelp(help);
    }

    protected override bool IsValidObject(ActiveDirectoryObject obj) => obj.IsUser && obj.MemberOfNames.Any(o => o.EqualsWildcard(groupPattern, true));

    private string groupPattern;
    public override string[] DefaultColumnsToInclude => base.DefaultColumnsToInclude.Add(nameof(ActiveDirectoryObject.MemberOfNamesString));

    protected override void ExecuteInternal(ActiveDirectory ad)
    {
        groupPattern = GetArgValueTrimmed(0);
        groupPattern.CheckValueNotNull(nameof(groupPattern), log);

        base.ExecuteInternal(ad);
    }
}
