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
    public class GoogleSheetsAddRow : GoogleSheetsBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Adds a row of data to a Google Sheet");
            help.AddDetail("For empty values use the keyword NULL instead of a blank");
            help.AddParameter("sheetName", "s", "The spreadsheet sheet name/tab to upload to (default first sheet)");
            help.AddValue("<column A value> <column B value> <column C value> <etc>");
        }

        protected override void Execute()
        {
            base.Execute();
            var sheetName = GetArgParameterOrConfig("sheetName", "s");

            var values = GetArgValues().TrimOrNull().ToList();
            for (int i = 0; i < values.Count; i++) log.Debug("values[" + i + "]: " + values[i]);

            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], "null", System.StringComparison.OrdinalIgnoreCase))
                {
                    values[i] = null;
                }
            }

            using (var c = CreateConnection())
            {
                log.Debug("Adding row of data");
                c.AddRow(sheetName, values.ToArray());
                log.Info("Added row: " + string.Join(", ", values));
            }
        }
    }
}
