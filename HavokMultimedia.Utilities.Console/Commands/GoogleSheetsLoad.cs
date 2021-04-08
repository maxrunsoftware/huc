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
using System.IO;
using System.Linq;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using HavokMultimedia.Utilities.Console.External;
using Newtonsoft.Json;

namespace HavokMultimedia.Utilities.Console.Commands
{

    public class GoogleSheetsLoad : Command
    {



        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Loads a tab delimited data file into a Google Sheet");
            help.AddParameter("securityKeyFile", "k", "The JSON formatted security key file");
            help.AddParameter("applicationName", "a", "The Google Developer application name");
            help.AddParameter("spreadSheetId", "id", "The ID of the spreadsheet to upload to");
            help.AddParameter("sheetName", "s", "The spreadsheet sheet name/tab to upload to (default first sheet)");
            help.AddValue("<tab delimited data file>");
        }

        protected override void Execute()
        {

            var securityKeyFile = GetArgParameterOrConfigRequired("securityKeyFile", "k");
            var securityKeyFileData = ReadFile(securityKeyFile);
            log.Debug(nameof(securityKeyFileData) + ": " + securityKeyFileData);

            var applicationName = GetArgParameterOrConfigRequired("applicationName", "a");
            var spreadSheetId = GetArgParameterOrConfigRequired("spreadSheetId", "id");
            var sheetName = GetArgParameterOrConfig("sheetName", "s");

            var values = GetArgValues().TrimOrNull().WhereNotNull().ToList();
            var dataFileName = values.GetAtIndexOrDefault(0);
            log.Debug(nameof(dataFileName) + ": " + dataFileName);
            if (dataFileName == null) throw new ArgsException("dataFileName", "No dataFile specified");

            var table = ReadTableTab(dataFileName);


            using (var c = new GoogleSheets(securityKeyFileData, applicationName, spreadSheetId))
            {
                c.ClearSheet(sheetName);
                c.SetData(sheetName, table);


            }
        }
    }
}
