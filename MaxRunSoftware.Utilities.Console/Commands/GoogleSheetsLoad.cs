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

namespace MaxRunSoftware.Utilities.Console.Commands;

public class GoogleSheetsLoad : GoogleSheetsBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Loads a tab delimited data file into a Google Sheet");
        help.AddParameter(nameof(sheetName), "s", "The spreadsheet sheet name/tab to upload to (default first sheet)");
        help.AddParameter(nameof(columns), "c", "The command delimited list of columns to load (all columns)");
        help.AddParameter(nameof(characterThreshold), "ct", "Batch size character limit, Google request must be less then 10MB, you should usually leave this as default (1000000)");
        help.AddValue("<tab delimited data file>");
        // ReSharper disable StringLiteralTypo
        help.AddExample("-k=`MyGoogleAppKey.json` -a=`MyApplicationName` -id=`dkjfsd328sdfuhscbjcds8hfjndsfdsfdsfe` Orders.txt");
        // ReSharper restore StringLiteralTypo
    }

    private string sheetName;
    private string columns;
    private int characterThreshold;

    protected override void ExecuteInternal()
    {
        base.ExecuteInternal();
        sheetName = GetArgParameterOrConfig(nameof(sheetName), "s");
        columns = GetArgParameterOrConfig(nameof(columns), "c").TrimOrNull();
        characterThreshold = GetArgParameterOrConfigInt(nameof(characterThreshold), "ct", 1000000);

        var dataFileName = GetArgValueTrimmed(0);
        dataFileName.CheckValueNotNull(nameof(dataFileName), log);

        var table = ReadTableTab(dataFileName);

        if (columns != null)
        {
            var columnNamesToKeep = columns.Split(",").TrimOrNull().WhereNotNull().ToArray();
            log.Info("Reformatting table to only keep columns [" + columnNamesToKeep.ToStringDelimited("], [") + "]");
            table = table.SetColumnsListTo(columnNamesToKeep);
        }

        using (var c = CreateConnection())
        {
            log.Info("Loading data");
            c.SetData(sheetName, table, characterThreshold);
            log.Info("Data loaded");
        }
    }
}
