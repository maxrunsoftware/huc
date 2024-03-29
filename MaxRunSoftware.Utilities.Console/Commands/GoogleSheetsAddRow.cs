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

using System;
using System.Linq;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class GoogleSheetsAddRow : GoogleSheetsBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Adds a row of data to a Google Sheet");
        help.AddDetail("For empty values use the keyword NULL instead of a blank");
        help.AddParameter(nameof(sheetName), "s", "The spreadsheet sheet name/tab to upload to (default first sheet)");
        help.AddValue("<column A value> <column B value> <column C value> <etc>");
        // ReSharper disable StringLiteralTypo
        help.AddExample("-k=`MyGoogleAppKey.json` -a=`MyApplicationName` -id=`dkjfsd328sdfuhscbjcds8hfjndsfdsfdsfe` AA null CC");
        // ReSharper restore StringLiteralTypo
    }

    private string sheetName;

    protected override void ExecuteInternal()
    {
        base.ExecuteInternal();
        sheetName = GetArgParameterOrConfig(nameof(sheetName), "s");

        var values = GetArgValues().TrimOrNull().ToList();
        log.Debug(values, nameof(values));

        for (var i = 0; i < values.Count; i++)
        {
            if (string.Equals(values[i], "null", StringComparison.OrdinalIgnoreCase)) { values[i] = null; }
        }

        using (var c = CreateConnection())
        {
            log.Debug("Adding row of data");
            c.AddRow(sheetName, values.ToArray());
            log.Info("Added row: " + string.Join(", ", values));
        }
    }
}
