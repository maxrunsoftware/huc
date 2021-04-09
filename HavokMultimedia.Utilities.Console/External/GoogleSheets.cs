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
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;

namespace HavokMultimedia.Utilities.Console.External
{
    public class GoogleSheets : IDisposable
    {
        private static readonly ILogger log = Program.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly IReadOnlyList<string> GOOGLE_COLUMNS = CreateGoogleColumns();
        private static IReadOnlyList<string> CreateGoogleColumns()
        {
            var list = new List<string>();
            foreach (var c in Constant.CHARS_A_Z_UPPER_ARRAY)
            {
                list.Add(c.ToString());
            }

            foreach (var c1 in Constant.CHARS_A_Z_UPPER_ARRAY)
            {
                foreach (var c2 in Constant.CHARS_A_Z_UPPER_ARRAY)
                {
                    list.Add(c1.ToString() + c2.ToString());
                }
            }

            return list.AsReadOnly();
        }
        public static string GetGoogleColumnName(int index) => GOOGLE_COLUMNS.GetAtIndexOrDefault(index);

        private readonly SheetsService service;
        private readonly string spreadsheetId;

        public GoogleSheets(string jsonFileData, string applicationName, string spreadsheetId)
        {
            var credential = GoogleCredential.FromJson(jsonFileData).CreateScoped(new[] { SheetsService.Scope.Spreadsheets });
            var initializer = new BaseClientService.Initializer();
            initializer.HttpClientInitializer = credential;
            if (applicationName != null) initializer.ApplicationName = applicationName;
            service = new SheetsService(initializer);
            this.spreadsheetId = spreadsheetId;
        }

        public void ClearSheet(string sheetName)
        {
            var sheet = sheetName == null ? service.GetSpreadsheetSheetFirst(spreadsheetId) : service.GetSpreadsheetSheet(spreadsheetId, sheetName);
            if (sheet == null) throw new Exception("Sheet " + sheetName + " not found");
            sheetName = sheet.Properties.Title;

            string range = sheetName + "!A1:ZZ";
            ClearValuesRequest requestBody = new ClearValuesRequest();
            SpreadsheetsResource.ValuesResource.ClearRequest request = service.Spreadsheets.Values.Clear(requestBody, spreadsheetId, range);
            log.Debug("Clearing sheet " + sheetName);
            ClearValuesResponse response = request.Execute();
            log.Debug("Cleared sheet " + sheetName);
            log.Debug(JsonConvert.SerializeObject(response));
        }


        public void SetData(string sheetName, List<string[]> data)
        {
            var sheet = sheetName == null ? service.GetSpreadsheetSheetFirst(spreadsheetId) : service.GetSpreadsheetSheet(spreadsheetId, sheetName);
            if (sheet == null) throw new Exception("Sheet " + sheetName + " not found");
            sheetName = sheet.Properties.Title;

            int numberOfColumns = 0;
            foreach (var row in data) numberOfColumns = Math.Max(numberOfColumns, row.Length);

            var range = sheetName + "!A1:" + GetGoogleColumnName(numberOfColumns - 1);
            log.Debug("Setting data in range: " + range);

            log.Trace("Converting data to Google format");
            var googleData = new List<IList<object>>();
            foreach (var row in data)
            {
                var list2 = new List<object>();
                for (int i = 0; i < numberOfColumns; i++)
                {
                    var val = row.GetAtIndexOrDefault(i) ?? string.Empty;
                    list2.Add(val);
                }
                googleData.Add(list2);
            }

            var dataValueRange = new ValueRange { Range = range, Values = googleData };
            var updateData = new List<ValueRange> { dataValueRange };
            var requestBody = new BatchUpdateValuesRequest { ValueInputOption = "USER_ENTERED", Data = updateData };


            var request = service.Spreadsheets.Values.BatchUpdate(requestBody, spreadsheetId);
            log.Debug("Setting sheet values " + sheetName);
            BatchUpdateValuesResponse response = request.Execute();
            log.Debug("Set sheet values " + sheetName);
            log.Debug(JsonConvert.SerializeObject(response));
        }

        public void SetData(string sheetName, Table table)
        {
            List<string[]> list = new List<string[]>();
            list.Add(table.Columns.Select(o => o.Name).ToArray());
            foreach (var row in table)
            {
                var list2 = new List<string>();
                foreach (var item in row)
                {
                    list2.Add(item);
                }
                list.Add(list2.ToArray());
            }
            SetData(sheetName, list);
        }

        public void AddRow(string sheetName, params string[] rowValues)
        {
            var sheet = sheetName == null ? service.GetSpreadsheetSheetFirst(spreadsheetId) : service.GetSpreadsheetSheet(spreadsheetId, sheetName);
            if (sheet == null) throw new Exception("Sheet " + sheetName + " not found");
            sheetName = sheet.Properties.Title;

            // TODO: Assign values to desired properties of `requestBody`:
            var requestBody = new ValueRange();
            var list = new List<IList<object>>();
            var list2 = new List<object>();
            foreach (var rowValue in rowValues) list2.Add(rowValue);
            list.Add(list2);
            requestBody.Values = list;
            string range = sheetName + "!A1:ZZ";
            SpreadsheetsResource.ValuesResource.AppendRequest request = service.Spreadsheets.Values.Append(requestBody, spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

            log.Debug("Adding row to sheet " + sheetName);
            AppendValuesResponse response = request.Execute();
            log.Debug("Added row to sheet " + sheetName);
            log.Debug(JsonConvert.SerializeObject(response));

        }

        public void Dispose()
        {
            if (service != null) service.Dispose();
        }
    }

    public static class GoogleSheetsExtensions
    {
        public static Sheet GetSpreadsheetSheet(this SheetsService service, string spreadsheetId, int index)
        {
            var d = new SortedDictionary<int, Sheet>();
            var spreadsheet = service.GetSpreadsheet(spreadsheetId);
            foreach (Sheet sheet in spreadsheet.Sheets)
            {
                var indexNullable = sheet.Properties.Index;
                if (indexNullable == null) continue;
                var indexNotNullable = indexNullable.Value;
                d[indexNotNullable] = sheet;

            }

            var list = new List<Sheet>();
            foreach (var kvp in d)
            {
                list.Add(kvp.Value);
            }

            return list.GetAtIndexOrDefault(index);

        }

        public static Sheet GetSpreadsheetSheetFirst(this SheetsService service, string spreadsheetId) => service.GetSpreadsheetSheet(spreadsheetId, 0);

        public static Sheet GetSpreadsheetSheet(this SheetsService service, string spreadsheetId, string name)
        {
            var d = new SortedDictionary<string, Sheet>();
            var spreadsheet = service.GetSpreadsheet(spreadsheetId);
            foreach (Sheet sheet in spreadsheet.Sheets)
            {
                d[sheet.Properties.Title] = sheet;
            }
            foreach (var comp in Constant.LIST_StringComparison)
            {
                foreach (var kvp in d)
                {
                    if (string.Equals(name, kvp.Key, comp)) return kvp.Value;
                }
            }

            return null;
        }

        public static Spreadsheet GetSpreadsheet(this SheetsService service, string spreadsheetId) => service.Spreadsheets.Get(spreadsheetId).Execute();
    }
}
