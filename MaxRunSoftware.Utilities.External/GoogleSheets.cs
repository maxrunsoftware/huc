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

namespace MaxRunSoftware.Utilities.External
{
    public class GoogleSheets : IDisposable
    {
        private static readonly ILogger log = Logging.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
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
        public static int DefaultSheetWidth => 26;
        public static int DefaultSheetHeight => 100;

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

        #region Helpers

        private (Sheet sheet, string sheetName) GetSheet(string sheetName, bool createSheet)
        {
            var sheet = sheetName == null ? service.GetSpreadsheetSheetFirst(spreadsheetId) : service.GetSpreadsheetSheet(spreadsheetId, sheetName);
            if (sheet == null && createSheet)
            {
                CreateSheet(sheetName);
                sheet = service.GetSpreadsheetSheet(spreadsheetId, sheetName);
            }
            if (sheet == null) throw new Exception("Sheet " + sheetName + " not found");
            sheetName = sheet.Properties.Title;
            return (sheet, sheetName);
        }

        private T IssueRequest<T>(SheetsBaseServiceRequest<T> request, string sheetName, string beforeMessage, string afterMessage, bool logResponse = true) where T : Google.Apis.Requests.IDirectResponseSchema
        {
            log.Debug(beforeMessage + " [" + sheetName + "]");
            log.Debug("Issuing: " + request.GetType().NameFormatted());
            var response = request.Execute();
            log.Debug("Received response: " + response.GetType().NameFormatted());
            log.Debug(nameof(response.ETag) + ": " + response.ETag);
            if (logResponse) log.Debug(Environment.NewLine + JsonConvert.SerializeObject(response, Formatting.Indented));
            log.Debug(afterMessage + " [" + sheetName + "]");
            return response;
        }

        #endregion

        public void ClearSheet(string sheetName)
        {
            sheetName = GetSheet(sheetName, false).sheetName;

            ResizeSheet(sheetName, DefaultSheetWidth, DefaultSheetHeight);
            FormatCellsDefault(sheetName, 0, 0, width: DefaultSheetWidth, height: DefaultSheetHeight);

            string range = sheetName + "!A1:ZZ";
            var requestBody = new ClearValuesRequest();
            SpreadsheetsResource.ValuesResource.ClearRequest request = service.Spreadsheets.Values.Clear(requestBody, spreadsheetId, range);
            IssueRequest(request, sheetName, "Clearing sheet", "Cleared sheet");
        }

        public void ResizeSheet(string sheetName, int columns, int rows)
        {
            sheetName = GetSheet(sheetName, false).sheetName;

            var updateSheetPropertiesRequest = new UpdateSheetPropertiesRequest();
            if (updateSheetPropertiesRequest.Properties == null) updateSheetPropertiesRequest.Properties = new SheetProperties();
            if (updateSheetPropertiesRequest.Properties.GridProperties == null) updateSheetPropertiesRequest.Properties.GridProperties = new GridProperties();
            updateSheetPropertiesRequest.Properties.GridProperties.ColumnCount = columns;
            updateSheetPropertiesRequest.Properties.GridProperties.RowCount = rows;
            updateSheetPropertiesRequest.Fields = "gridProperties";

            var bussr = new BatchUpdateSpreadsheetRequest();
            bussr.Requests = new List<Request>();
            bussr.Requests.Add(new Request() { UpdateSheetProperties = updateSheetPropertiesRequest });
            var request = service.Spreadsheets.BatchUpdate(bussr, spreadsheetId);
            IssueRequest(request, sheetName, "Resizing sheet", "Resized sheet");
        }

        public void FormatCells(string sheetName, CellFormat cellFormat, GridRange range)
        {
            sheetName = GetSheet(sheetName, false).sheetName;

            var updateCellsRequest = new Request();
            updateCellsRequest.RepeatCell = new RepeatCellRequest();
            updateCellsRequest.RepeatCell.Range = range;
            updateCellsRequest.RepeatCell.Cell = new CellData() { UserEnteredFormat = cellFormat };
            updateCellsRequest.RepeatCell.Fields = "UserEnteredFormat(BackgroundColor,TextFormat)";

            var bussr = new BatchUpdateSpreadsheetRequest();
            bussr.Requests = new List<Request> { updateCellsRequest };

            var request = service.Spreadsheets.BatchUpdate(bussr, spreadsheetId);

            IssueRequest(request, sheetName, "Updating cell format", "Updated cell format");
        }

        public void FormatCells(
            string sheetName,
            int indexX,
            int indexY,
            int width = 1,
            int height = 1,
            System.Drawing.Color? backgroundColor = null,
            System.Drawing.Color? foregroundColor = null,
            bool? bold = null,
            bool? italic = null,
            bool? underline = null,
            bool? strikethrough = null,
            string fontFamily = null
            )
        {
            var sheetAndName = GetSheet(sheetName, false);
            var sheet = sheetAndName.sheet;
            sheetName = sheetAndName.sheetName;

            var cellFormat = new CellFormat();
            if (backgroundColor != null) cellFormat.BackgroundColor = backgroundColor.Value.ToGoogleColor();
            if (cellFormat.TextFormat == null) cellFormat.TextFormat = new TextFormat();
            if (foregroundColor != null) cellFormat.TextFormat.ForegroundColor = foregroundColor.Value.ToGoogleColor();
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

            FormatCells(sheetName, cellFormat, range);
        }

        public void FormatCellsDefault(string sheetName, int indexX, int indexY, int width = 1, int height = 1)
        {
            FormatCells(
                sheetName,
                indexX,
                indexY,
                width: width,
                height: height,
                backgroundColor: System.Drawing.Color.White,
                foregroundColor: System.Drawing.Color.Black,
                bold: false,
                italic: false,
                underline: false,
                strikethrough: false,
                fontFamily: "Arial"
                );
        }

        public void SetData(string sheetName, List<string[]> data)
        {
            sheetName = GetSheet(sheetName, true).sheetName;

            ClearSheet(sheetName);
            ResizeSheet(sheetName, data.MaxLength(), data.Count);

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
            IssueRequest(request, sheetName, "Setting sheet values", "Set sheet values");
        }

        public void SetData(string sheetName, Table table, int characterThreshold)
        {
            if (table.GetNumberOfCells() > 5000000) throw new Exception("Cannot load table with " + table.GetNumberOfCells().ToStringCommas() + " cells, Google's limit is 5,000,000");
            int nullSize = 6;

            log.Info("Adding columns [" + table.Columns.Select(o => o.Name).ToStringDelimited("], [") + "]");
            var list = new List<string[]> { table.Columns.Select(o => o.Name).ToArray() };
            SetData(sheetName, list);
            int rowsTotal = table.Count;
            int rowsCurrent = 0;
            log.Info($"Adding {table.Count} rows");
            foreach (var rowChunk in table.GetRowsChunkedByNumberOfCharacters(characterThreshold, nullSize))
            {
                list = new();
                foreach (var row in rowChunk) list.Add(row.ToArray());
                AddRows(sheetName, list);
                rowsCurrent += list.Count;
                log.Info("Added rows " + Util.FormatRunningCount(rowsCurrent - 1, rowsTotal) + " " + Util.FormatRunningCountPercent(rowsCurrent - 1, rowsTotal, 2) + "   + " + list.Count + " rows");
            }

            FormatCellsDefault(sheetName, 0, 0, width: table.Columns.Count, height: table.Count + 1);
            ResizeSheet(sheetName, table.Columns.Count, table.Count + 1);
        }

        public List<string[]> Query(string sheetName, string range = "A1:ZZ")
        {
            sheetName = GetSheet(sheetName, false).sheetName;

            string rangeString = sheetName + "!" + (range ?? "A1:ZZ");
            var request = service.Spreadsheets.Values.Get(spreadsheetId, rangeString);

            var response = IssueRequest(request, sheetName, $"Querying {rangeString}", $"Queried {rangeString}", logResponse: false);
            IList<IList<object>> responseValues = response.Values;

            var list = new List<string[]>();
            foreach (var sublist in responseValues.OrEmpty())
            {
                list.Add(sublist.OrEmpty().ToStringsGuessFormat().ToArray());
            }
            list.ResizeAll(list.MaxLength());

            log.Debug($"Query returned {list.Count} rows");

            return list;
        }

        public void AddRow(string sheetName, params string[] rowValues) => AddRows(sheetName, new List<string[]> { rowValues });

        public void AddRows(string sheetName, IList<string[]> rows)
        {
            sheetName = GetSheet(sheetName, true).sheetName;

            // TODO: Assign values to desired properties of `requestBody`:
            var requestBody = new ValueRange();
            var listOuter = new List<IList<object>>();
            foreach (var array in rows)
            {
                var listInner = new List<object>();
                foreach (var str in array)
                {
                    listInner.Add(str);
                }
                listOuter.Add(listInner);
            }
            requestBody.Values = listOuter;
            string range = sheetName + "!A1:A1";
            SpreadsheetsResource.ValuesResource.AppendRequest request = service.Spreadsheets.Values.Append(requestBody, spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

            IssueRequest(request, sheetName, $"Adding {listOuter.Count} rows to sheet", $"Added {listOuter.Count} rows to sheet");
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

            var request = service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, spreadsheetId);
            IssueRequest(request, sheetName, "Adding new sheet", "Added new sheet");
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

        public static Sheet GetSpreadsheetSheetFirst(this SheetsService service, string spreadsheetId) => service.GetSpreadsheetSheet(spreadsheetId, 0);

        public static Spreadsheet GetSpreadsheet(this SheetsService service, string spreadsheetId) => service.Spreadsheets.Get(spreadsheetId).Execute();

        public static int GetId(this Sheet sheet) => sheet.Properties.SheetId ?? 0;
    }
}
