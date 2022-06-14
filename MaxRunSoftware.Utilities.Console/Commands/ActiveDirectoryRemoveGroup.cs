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

using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class ActiveDirectoryRemoveGroup : ActiveDirectoryBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Removes a group from ActiveDirectory");
        //help.AddDetail("Requires LDAPS configured on the server");
        help.AddValue("<SAMAccountName>");
        help.AddExample(HelpExamplePrefix + " testgroup");
    }

    protected override void ExecuteInternal(ActiveDirectory ad)
    {
        var values = GetArgValues().TrimOrNull().WhereNotNull();

        var samAccountName = values.GetAtIndexOrDefault(0).TrimOrNull();
        samAccountName.CheckValueNotNull(nameof(samAccountName), log);

        log.Debug("Removing group " + samAccountName);
        ad.RemoveGroup(samAccountName);
        log.Info("Successfully removed group " + samAccountName);
    }
}
