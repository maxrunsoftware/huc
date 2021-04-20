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

using System.Linq;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class GoogleSheetsLoad : GoogleSheetsBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Loads a tab delimited data file into a Google Sheet");
            help.AddParameter("sheetName", "s", "The spreadsheet sheet name/tab to upload to (default first sheet)");
            help.AddParameter("columns", "c", "The command delimited list of columns to load (all columns)");
            help.AddParameter("characterThreshold", "ct", "Batch size character limit, Google request must be less then 10MB, you should usually leave this as default (1000000)");
            help.AddValue("<tab delimited data file>");
            help.AddExample("-k=`MyGoogleAppKey.json` -a=`MyApplicationName` -id=`dkjfsd328sdfuhscbjcds8hfjndsfdsfdsfe` Orders.txt");
        }

        protected override void Execute()
        {
            base.Execute();
            var sheetName = GetArgParameterOrConfig("sheetName", "s");
            var columns = GetArgParameterOrConfig("columns", "c").TrimOrNull();
            var characterThreshold = GetArgParameterOrConfigInt("characterThreshold", "ct", 1000000);

            var dataFileName = GetArgValueTrimmed(0);
            log.Debug(nameof(dataFileName) + ": " + dataFileName);
            if (dataFileName == null) throw new ArgsException("dataFileName", "No dataFile specified");

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
}
