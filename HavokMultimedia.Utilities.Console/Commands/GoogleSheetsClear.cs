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

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class GoogleSheetsClear : GoogleSheetsBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Clears all data in a Google Sheet");
            help.AddParameter(nameof(sheetName), "s", "The spreadsheet sheet name/tab to clear (default first sheet)");
            help.AddExample("-k=`MyGoogleAppKey.json` -a=`MyApplicationName` -id=`dkjfsd328sdfuhscbjcds8hfjndsfdsfdsfe` -s=`Sheet1`");
            help.AddExample("-k=`MyGoogleAppKey.json` -a=`MyApplicationName` -id=`dkjfsd328sdfuhscbjcds8hfjndsfdsfdsfe`");
        }

        private string sheetName;

        protected override void ExecuteInternal()
        {
            base.ExecuteInternal();
            sheetName = GetArgParameterOrConfig(nameof(sheetName), "s");

            using (var c = CreateConnection())
            {
                log.Debug("Clearing sheet");
                c.ClearSheet(sheetName);
                log.Info("Cleared sheet");
            }
        }
    }
}
