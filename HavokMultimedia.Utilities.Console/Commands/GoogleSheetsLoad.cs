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
        }

        protected override void Execute()
        {
            base.Execute();
            var sheetName = GetArgParameterOrConfig("sheetName", "s");
            var columns = GetArgParameterOrConfig("columns", "c").TrimOrNull();
            var characterThreshold = GetArgParameterOrConfigInt("characterThreshold", "ct", 1000000);

            var values = GetArgValues().TrimOrNull().WhereNotNull().ToList();
            var dataFileName = values.GetAtIndexOrDefault(0);
            log.Debug(nameof(dataFileName) + ": " + dataFileName);
            if (dataFileName == null) throw new ArgsException("dataFileName", "No dataFile specified");

            var table = ReadTableTab(dataFileName);

            if (columns != null)
            {
                var columnNames = columns.Split(new string[] { "," }, System.StringSplitOptions.None).TrimOrNull().WhereNotNull().ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var columnName in columnNames)
                {
                    if (!table.Columns.ContainsColumn(columnName))
                    {
                        throw new ArgsException(nameof(columns), "Table does not contain column [" + columnName + "]. Valid columns are " + table.Columns);
                    }
                }
                var columnsToRemove = new List<string>();
                foreach (var column in table.Columns)
                {
                    if (!columnNames.Contains(column.Name)) columnsToRemove.Add(column.Name);
                }
                log.Info("Reformatting table");
                table = table.RemoveColumns(columnsToRemove.ToArray());
                log.Info("Reformatted table");
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
