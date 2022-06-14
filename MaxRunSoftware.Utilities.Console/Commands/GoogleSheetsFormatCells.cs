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

using System;
using System.Drawing;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class GoogleSheetsFormatCells : GoogleSheetsBase
{
    //private static IEnumerable<string> ColorNames => Constant.COLORS.Keys.OrderBy(o => o, StringComparer.OrdinalIgnoreCase);
    private static Color? ParseColor(string colorName)
    {
        foreach (var kvp in Constant.COLORS)
        {
            if (string.Equals(kvp.Key, colorName, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }

        return null;
    }

    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Applies formatting to a block of Google Sheets cells");
        help.AddParameter(nameof(sheetName), "s", "The spreadsheet sheet name/tab to clear (default first sheet)");
        help.AddParameter(nameof(xPosition), "x", "The zero based column index to start at (0)");
        help.AddParameter(nameof(yPosition), "y", "The zero based row index to start at (0)");
        help.AddParameter(nameof(width), "w", "The number of column to include (1)");
        help.AddParameter(nameof(height), "h", "The number of rows to include (1)");
        help.AddParameter(nameof(foregroundColor), "fc", "The foreground color (Black) [ use \"huc colors\" to show colors ]");
        help.AddParameter(nameof(backgroundColor), "bc", "The background color (White) [ use \"huc colors\" to show colors ]");
        help.AddParameter(nameof(bold), "b", "Bold text (false)");
        help.AddParameter(nameof(italic), "i", "Italic text (false)");
        help.AddParameter(nameof(underline), "u", "Underline text (false)");
        help.AddParameter(nameof(strikethrough), "st", "Strikethrough text (false)");
        // ReSharper disable StringLiteralTypo
        help.AddExample("-k=`MyGoogleAppKey.json` -a=`MyApplicationName` -id=`dkjfsd328sdfuhscbjcds8hfjndsfdsfdsfe` -width=100 -b -fc=Red -bc=Blue");
        // ReSharper restore StringLiteralTypo
    }

    private string sheetName;
    private int xPosition;
    private int yPosition;
    private int width;
    private int height;
    private string foregroundColor;
    private string backgroundColor;
    private bool bold;
    private bool italic;
    private bool underline;
    private bool strikethrough;

    protected override void ExecuteInternal()
    {
        base.ExecuteInternal();
        sheetName = GetArgParameterOrConfig(nameof(sheetName), "s");
        xPosition = GetArgParameterOrConfigInt(nameof(xPosition), "x", 0);
        yPosition = GetArgParameterOrConfigInt(nameof(yPosition), "y", 0);
        width = GetArgParameterOrConfigInt(nameof(width), "w", 1);
        height = GetArgParameterOrConfigInt(nameof(height), "h", 1);
        foregroundColor = GetArgParameterOrConfig(nameof(foregroundColor), "fc", "Black");
        backgroundColor = GetArgParameterOrConfig(nameof(backgroundColor), "bc", "White");
        bold = GetArgParameterOrConfigBool(nameof(bold), "b", false);
        italic = GetArgParameterOrConfigBool(nameof(italic), "i", false);
        underline = GetArgParameterOrConfigBool(nameof(underline), "u", false);
        strikethrough = GetArgParameterOrConfigBool(nameof(strikethrough), "st", false);

        var fColor = ParseColor(foregroundColor);
        if (fColor == null)
        {
            throw new ArgsException(nameof(foregroundColor), nameof(foregroundColor) + " " + foregroundColor + " does not exist");
        }

        var bColor = ParseColor(backgroundColor);
        if (bColor == null)
        {
            throw new ArgsException(nameof(backgroundColor), nameof(backgroundColor) + " " + backgroundColor + " does not exist");
        }

        using (var c = CreateConnection())
        {
            log.Debug("Formatting cells in sheet");
            c.FormatCells(
                sheetName,
                xPosition,
                yPosition,
                width,
                height,
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
