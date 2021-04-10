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
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text;
using HavokMultimedia.Utilities;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class Colors : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Shows all of the colors available");
        }


        protected override void Execute()
        {
            var colorNames = Constant.COLORS.Keys.OrderBy(o => o, StringComparer.OrdinalIgnoreCase).ToArray();
            var width = 0;
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
    }
}
