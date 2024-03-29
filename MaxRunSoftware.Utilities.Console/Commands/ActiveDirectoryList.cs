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

// ReSharper disable StringLiteralTypo

namespace MaxRunSoftware.Utilities.Console.Commands;

public class ActiveDirectoryList : ActiveDirectoryBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Lists all objects and their attributes in an ActiveDirectory to the specified tab delimited file");
        help.AddParameter(nameof(includeExpensiveProperties), "e", "Whether to include expensive properties or not (false)");
        help.AddValue("<output tab delimited file>");
        help.AddExample(HelpExamplePrefix + " adlist.txt");
    }

    private bool includeExpensiveProperties;

    protected override void ExecuteInternal(ActiveDirectory ad)
    {
        includeExpensiveProperties = GetArgParameterOrConfigBool(nameof(includeExpensiveProperties), "e", false);
        var outputFile = GetArgValueTrimmed(0);
        outputFile.CheckValueNotNull(nameof(outputFile), log);

        var activeDirectoryObjects = ad.GetAll();
        var t = ActiveDirectoryObject.CreateTable(activeDirectoryObjects, includeExpensiveProperties);
        WriteTableTab(outputFile, t);
    }
}
