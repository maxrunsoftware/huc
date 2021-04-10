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

        public void FormatCell(string sheetName, CellFormat cellFormat, GridRange range)
        {
            var sheet = sheetName == null ? service.GetSpreadsheetSheetFirst(spreadsheetId) : service.GetSpreadsheetSheet(spreadsheetId, sheetName);
            if (sheet == null) throw new Exception("Sheet " + sheetName + " not found");
            sheetName = sheet.Properties.Title;
            int sheetId = sheet.GetId();

            //define cell color
            var userEnteredFormat = cellFormat;
            BatchUpdateSpreadsheetRequest bussr = new BatchUpdateSpreadsheetRequest();

            //create the update request for cells from the first row
            var updateCellsRequest = new Request()
            {
                RepeatCell = new RepeatCellRequest()
                {
                    Range = range,
                    Cell = new CellData() { UserEnteredFormat = cellFormat },
                    Fields = "UserEnteredFormat(BackgroundColor,TextFormat)"
                }
            };
            bussr.Requests = new List<Request>();
            bussr.Requests.Add(updateCellsRequest);
            var bur = service.Spreadsheets.BatchUpdate(bussr, spreadsheetId);


            log.Debug("Updating cell format for sheet " + sheetName);
            var response = bur.Execute();
            log.Debug("Updated cell format for sheet " + sheetName);
            log.Debug(JsonConvert.SerializeObject(response));

        }

        public void FormatCell(
            string sheetName,
            int indexX,
            int indexY,
            int width = 1,
            int height = 1,
            Color backgroundColor = null,
            Color foregroundColor = null,
            bool? bold = null,
            bool? italic = null,
            bool? underline = null,
            bool? strikethrough = null,
            string fontFamily = null
            )
        {
            var sheet = sheetName == null ? service.GetSpreadsheetSheetFirst(spreadsheetId) : service.GetSpreadsheetSheet(spreadsheetId, sheetName);
            if (sheet == null) throw new Exception("Sheet " + sheetName + " not found");

            var cellFormat = new CellFormat();
            if (backgroundColor != null) cellFormat.BackgroundColor = backgroundColor;
            if (cellFormat.TextFormat == null) cellFormat.TextFormat = new TextFormat();
            if (foregroundColor != null) cellFormat.TextFormat.ForegroundColor = foregroundColor;
            if (bold != null) cellFormat.TextFormat.Bold = bold.Value;
            if (italic != null) cellFormat.TextFormat.Italic = italic.Value;
            if (underline != null) cellFormat.TextFormat.Underline = underline.Value;
            if (strikethrough != null) cellFormat.TextFormat.Strikethrough = strikethrough.Value;
            if (fontFamily != null) cellFormat.TextFormat.FontFamily = fontFamily;

            var range = new GridRange()
            {
                SheetId = sheet.GetId(),
                StartColumnIndex = indexX,
                StartRowIndex = indexY,
                EndColumnIndex = indexX + width,
                EndRowIndex = indexY + height
            };

            FormatCell(sheetName, cellFormat, range);

        }

        public void FormatCell(string sheetName)
        {
            var sheet = sheetName == null ? service.GetSpreadsheetSheetFirst(spreadsheetId) : service.GetSpreadsheetSheet(spreadsheetId, sheetName);
            if (sheet == null) throw new Exception("Sheet " + sheetName + " not found");
            sheetName = sheet.Properties.Title;


            int sheetId = (int)sheet.Properties.SheetId;

            //define cell color
            var userEnteredFormat = new CellFormat()
            {
                BackgroundColor = new Color()
                {
                    Blue = 0,
                    Red = 1,
                    Green = (float)0.5,
                    Alpha = (float)0.1
                },
                TextFormat = new TextFormat()
                {
                    Bold = true
                }
            };
            BatchUpdateSpreadsheetRequest bussr = new BatchUpdateSpreadsheetRequest();

            //create the update request for cells from the first row
            var updateCellsRequest = new Request()
            {
                RepeatCell = new RepeatCellRequest()
                {
                    Range = new GridRange()
                    {
                        SheetId = sheetId,
                        StartColumnIndex = 0,
                        StartRowIndex = 0,
                        EndColumnIndex = 1,
                        EndRowIndex = 1
                    },
                    Cell = new CellData()
                    {
                        UserEnteredFormat = userEnteredFormat
                    },
                    Fields = "UserEnteredFormat(BackgroundColor,TextFormat)"
                }
            };
            bussr.Requests = new List<Request>();
            bussr.Requests.Add(updateCellsRequest);
            var bur = service.Spreadsheets.BatchUpdate(bussr, spreadsheetId);
            bur.Execute();
        }


        public void SetData(string sheetName, List<string[]> data)
        {
            var sheet = sheetName == null ? service.GetSpreadsheetSheetFirst(spreadsheetId) : service.GetSpreadsheetSheet(spreadsheetId, sheetName);
            if (sheet == null)
            {
                CreateSheet(sheetName);
                sheet = service.GetSpreadsheetSheet(spreadsheetId, sheetName);
            }
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

            FormatCell(
                sheetName,
                0, 0,
                width: numberOfColumns, height: googleData.Count,
                backgroundColor: System.Drawing.Color.White.ToGoogleColor(),
                foregroundColor: System.Drawing.Color.Black.ToGoogleColor(),
                bold: false,
                italic: false,
                underline: false,
                strikethrough: false
                );

            FormatCell(
                sheetName,
                0, 0,
                width: numberOfColumns, height: 1,
                backgroundColor: System.Drawing.Color.White.ToGoogleColor(),
                foregroundColor: System.Drawing.Color.Black.ToGoogleColor(),
                bold: true,
                italic: false,
                underline: false,
                strikethrough: false
                );

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

        public static IList<object> AsGoogleListInner(params string[] strs)
        {
            var list = new List<object>();
            foreach (var str in strs) list.Add(str);
            return list;
        }
        public static List<IList<object>> AsGoogleListOuter(params IList<object>[] lists)
        {
            var list = new List<IList<object>>();
            foreach (var l in lists) list.Add(l);
            return list;
        }

        public void AddRow(string sheetName, params string[] rowValues)
        {
            var sheet = sheetName == null ? service.GetSpreadsheetSheetFirst(spreadsheetId) : service.GetSpreadsheetSheet(spreadsheetId, sheetName);
            if (sheet == null)
            {
                CreateSheet(sheetName);
                sheet = service.GetSpreadsheetSheet(spreadsheetId, sheetName);
            }
            if (sheet == null) throw new Exception("Sheet " + sheetName + " not found");
            sheetName = sheet.Properties.Title;

            // TODO: Assign values to desired properties of `requestBody`:
            var requestBody = new ValueRange();
            requestBody.Values = AsGoogleListOuter(AsGoogleListInner(rowValues));
            string range = sheetName + "!A1:A1";
            SpreadsheetsResource.ValuesResource.AppendRequest request = service.Spreadsheets.Values.Append(requestBody, spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

            log.Debug("Adding row to sheet " + sheetName);
            AppendValuesResponse response = request.Execute();
            log.Debug("Added row to sheet " + sheetName);
            log.Debug(JsonConvert.SerializeObject(response));

        }

        public void CreateSheet(string sheetName)
        {
            var sheet = service.GetSpreadsheetSheet(spreadsheetId, sheetName);
            if (sheet != null)
            {
                log.Debug("Not creating already existing sheet " + sheetName);
                return;
            }
            log.Debug("Adding new sheet " + sheetName);

            var addSheetRequest = new AddSheetRequest();
            addSheetRequest.Properties = new SheetProperties();
            addSheetRequest.Properties.Title = sheetName;
            BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest();
            batchUpdateSpreadsheetRequest.Requests = new List<Request>();
            batchUpdateSpreadsheetRequest.Requests.Add(new Request { AddSheet = addSheetRequest });

            var batchUpdateRequest = service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, spreadsheetId);
            log.Debug("Adding new sheet " + sheetName);
            var response = batchUpdateRequest.Execute();
            log.Debug("Added new sheet " + sheetName);
            log.Debug(JsonConvert.SerializeObject(response));
        }

        public void Dispose()
        {
            if (service != null) service.Dispose();
        }
    }

    public static class GoogleSheetsExtensions
    {
        public static Color ToGoogleColor(this System.Drawing.Color color) => new Color
        {
            Red = (float)color.R / 255.0F,
            Green = (float)color.G / 255.0F,
            Blue = (float)color.B / 255.0F,
            Alpha = (float)color.A / 255.0F
        };

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

        public static int GetId(this Sheet sheet) => sheet.Properties.SheetId ?? 0;

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
