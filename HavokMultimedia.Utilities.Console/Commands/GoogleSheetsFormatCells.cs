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
    public class GoogleSheetsFormatCells : GoogleSheetsBase
    {
        private static IEnumerable<string> ColorNames => Constant.COLORS.Keys.OrderBy(o => o, StringComparer.OrdinalIgnoreCase);
        private static System.Drawing.Color? ParseColor(string colorName)
        {
            foreach (var kvp in Constant.COLORS)
            {
                if (string.Equals(kvp.Key, colorName, StringComparison.OrdinalIgnoreCase)) return kvp.Value;
            }
            return null;
        }
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Applies formatting to a block of Google Sheets cells");
            help.AddParameter("sheetName", "s", "The spreadsheet sheet name/tab to clear (default first sheet)");
            help.AddParameter("xPosition", "x", "The zero based column index to start at (0)");
            help.AddParameter("yPosition", "y", "The zero based row index to start at (0)");
            help.AddParameter("width", "w", "The number of column to include (1)");
            help.AddParameter("height", "h", "The number of rows to include (1)");
            help.AddParameter("foregroundColor", "fc", "The foreground color (Black) [ use \"huc colors\" to show colors ]");
            help.AddParameter("backgroundColor", "bc", "The background color (White) [ use \"huc colors\" to show colors ]");
            help.AddParameter("bold", "b", "Bold text (false)");
            help.AddParameter("italic", "i", "Italic text (false)");
            help.AddParameter("underline", "u", "Underline text (false)");
            help.AddParameter("strikethrough", "st", "Strikethrough text (false)");
            help.AddExample("-k=`MyGoogleAppKey.json` -a=`MyApplicationName` -id=`dkjfsd328sdfuhscbjcds8hfjndsfdsfdsfe` -width=100 -b -fc=Red -bc=Blue");
        }

        protected override void Execute()
        {
            base.Execute();
            var sheetName = GetArgParameterOrConfig("sheetName", "s");

            var xPosition = GetArgParameterOrConfigInt("xPosition", "x", 0);
            var yPosition = GetArgParameterOrConfigInt("yPosition", "y", 0);
            var width = GetArgParameterOrConfigInt("width", "w", 1);
            var height = GetArgParameterOrConfigInt("height", "h", 1);
            var foregroundColor = GetArgParameterOrConfig("foregroundColor", "fc", "Black");
            var backgroundColor = GetArgParameterOrConfig("backgroundColor", "bc", "White");
            var bold = GetArgParameterOrConfigBool("bold", "b", false);
            var italic = GetArgParameterOrConfigBool("italic", "i", false);
            var underline = GetArgParameterOrConfigBool("underline", "u", false);
            var strikethrough = GetArgParameterOrConfigBool("strikethrough", "st", false);

            var fColor = ParseColor(foregroundColor);
            if (fColor == null) throw new ArgsException(nameof(foregroundColor), nameof(foregroundColor) + " " + foregroundColor + " does not exist");

            var bColor = ParseColor(backgroundColor);
            if (bColor == null) throw new ArgsException(nameof(backgroundColor), nameof(backgroundColor) + " " + backgroundColor + " does not exist");

            using (var c = CreateConnection())
            {
                log.Debug("Formatting cells in sheet");
                c.FormatCells(
                    sheetName,
                    xPosition,
                    yPosition,
                    width: width,
                    height: height,
                    foregroundColor: fColor,
                    backgroundColor: bColor,
                    bold: bold,
                    italic: italic,
                    underline: underline,
                    strikethrough: strikethrough
                );
                log.Info("Formatted cells in sheet");
            }
        }
    }
}
