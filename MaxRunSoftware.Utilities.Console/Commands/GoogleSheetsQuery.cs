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

public class GoogleSheetsQuery : GoogleSheetsBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Query a Google Sheet for data and generate a tab delimited file of the data");
        help.AddParameter(nameof(sheetName), "s", "The spreadsheet sheet name/tab to query (default first sheet)");
        help.AddParameter(nameof(range), "r", "The range to query from (A1:ZZ)");
        help.AddValue("<tab delimited output file name>");
        // ReSharper disable StringLiteralTypo
        help.AddExample("-k=`MyGoogleAppKey.json` -a=`MyApplicationName` -id=`dkjfsd328sdfuhscbjcds8hfjndsfdsfdsfe` MyFile.txt");
        // ReSharper restore StringLiteralTypo
    }

    private string sheetName;
    private string range;

    protected override void ExecuteInternal()
    {
        base.ExecuteInternal();
        sheetName = GetArgParameterOrConfig(nameof(sheetName), "s");
        range = GetArgParameterOrConfig(nameof(range), "r", "A1:ZZ");

        var outputFile = GetArgValueTrimmed(0);
        outputFile.CheckValueNotNull(nameof(outputFile), log);

        using (var c = CreateConnection())
        {
            log.Debug("Querying sheet");
            var items = c.Query(sheetName, range);
            var table = Utilities.Table.Create(items, true);
            WriteTableTab(outputFile, table);
            log.Info("Sheet with " + items.Count + " rows written to " + outputFile);
        }
    }
}
