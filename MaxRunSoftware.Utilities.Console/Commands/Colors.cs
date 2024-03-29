﻿// Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)
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
using System.Collections.Generic;
using System.Linq;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class Colors : Command
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        help.AddSummary("Shows all of the colors available");
        help.AddValue("<optional color name to list details for>");
        help.AddExample("");
        help.AddExample("red");
    }

    private class Color
    {
        public string ColorName { get; }
        public System.Drawing.Color ColorValue { get; }

        private Color(string colorName, System.Drawing.Color colorValue)
        {
            ColorName = colorName;
            ColorValue = colorValue;
        }

        private static List<Color> Colors => Constant.COLORS
            .Select(o => new Color(o.Key, o.Value))
            .OrderBy(o => o.ColorName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        public static string[] ColorNames => Colors.Select(o => o.ColorName).ToArray();

        public static Color GetColor(string colorName) => Colors.FirstOrDefault(o => string.Equals(o.ColorName, colorName, StringComparison.OrdinalIgnoreCase));

        public static string ToPercent(byte colorByte) => (colorByte / 255.0F * 100.0F).ToString(MidpointRounding.AwayFromZero, 0);
    }


    protected override void ExecuteInternal()
    {
        var colorRequested = GetArgValueTrimmed(0);
        log.DebugParameter(nameof(colorRequested), colorRequested);
        if (colorRequested == null)
        {
            var lines = Color.ColorNames.ToStringsColumns(4);
            foreach (var line in lines) log.Info(line);
        }
        else
        {
            var color = Color.GetColor(colorRequested);
            if (color == null) { log.Info("Color not found: " + colorRequested); }
            else
            {
                string FormatColor(byte colorValue) =>
                    colorValue.ToString().PadRight(3)
                    + "  "
                    + Color.ToPercent(colorValue).PadLeft(3)
                    + " %";

                log.Info("Color: " + color.ColorName);
                log.Info("    R: " + FormatColor(color.ColorValue.R));
                log.Info("    G: " + FormatColor(color.ColorValue.G));
                log.Info("    B: " + FormatColor(color.ColorValue.B));
                log.Info("    A: " + FormatColor(color.ColorValue.A));
            }
        }
    }
}
