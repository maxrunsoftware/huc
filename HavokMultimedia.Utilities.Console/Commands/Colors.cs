﻿/*
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
using HavokMultimedia.Utilities;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class Colors : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Shows all of the colors available");
            help.AddValue("<optional color name to list details for>");
        }

        private class Color
        {
            public string ColorName { get; init; }
            public System.Drawing.Color ColorValue { get; init; }

            private Color(string colorName, System.Drawing.Color colorValue)
            {
                ColorName = colorName;
                ColorValue = colorValue;
            }
            public static List<Color> Colors => Constant.COLORS
                .Select(o => new Color(o.Key, o.Value))
                .OrderBy(o => o.ColorName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            public static string[] ColorNames => Colors.Select(o => o.ColorName).ToArray();

            public static Color GetColor(string colorName)
            {
                return Colors.Where(o => string.Equals(o.ColorName, colorName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            }

            public static string ToPercent(byte colorByte)
            {
                return ((colorByte / 255.0F) * 100.0F).ToString(MidpointRounding.AwayFromZero, 0);
            }
        }


        protected override void Execute()
        {
            var values = GetArgValues().TrimOrNull().WhereNotNull();
            var colorRequested = values.FirstOrDefault();
            log.Debug(nameof(colorRequested) + ": " + colorRequested);
            if (colorRequested == null)
            {
                var width = 0;
                var colorNames = Color.ColorNames;
                foreach (var colorName in colorNames) width = Math.Max(width, colorName.Length);
                width = width + 3;

                var colorParts = colorNames.SplitIntoPartSizes(3);
                foreach (var colorPart in colorParts)
                {
                    var part1 = colorPart.GetAtIndexOrDefault(0) ?? string.Empty;
                    var part2 = colorPart.GetAtIndexOrDefault(1) ?? string.Empty;
                    var part3 = colorPart.GetAtIndexOrDefault(2) ?? string.Empty;

                    log.Info(part1.PadRight(width) + part2.PadRight(width) + part3.PadRight(width));
                }
            }
            else
            {
                var color = Color.GetColor(colorRequested);
                if (color == null)
                {
                    log.Info("Color not found: " + colorRequested);
                }
                else
                {
                    string formatColor(byte colorValue)
                    {
                        return colorValue.ToString().PadRight(3)
                            + "  "
                            + Color.ToPercent(colorValue).PadLeft(3)
                            + " %";
                    }
                    log.Info("Color: " + color.ColorName);
                    log.Info("    R: " + formatColor(color.ColorValue.R));
                    log.Info("    G: " + formatColor(color.ColorValue.G));
                    log.Info("    B: " + formatColor(color.ColorValue.B));
                    log.Info("    A: " + formatColor(color.ColorValue.A));
                }
            }


        }
    }
}