﻿// Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)
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

public class ActiveDirectoryRemoveOU : ActiveDirectoryBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Removes an OU from ActiveDirectory");
        //help.AddDetail("Requires LDAPS configured on the server");
        help.AddValue("<OU name or DN of OU>");
        help.AddExample(HelpExamplePrefix + " MyOU");
    }

    protected override void ExecuteInternal(ActiveDirectory ad)
    {
        var ouName = GetArgValueTrimmed(0);
        ouName.CheckValueNotNull(nameof(ouName), log);

        log.Debug("Removing OU: " + ouName);
        ad.RemoveOU(ouName);
        log.Info("Successfully removed OU " + ouName);
    }
}
