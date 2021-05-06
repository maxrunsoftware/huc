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

namespace HavokMultimedia.Utilities.Console.Commands
{

    public class FileReplaceString : Command
    {
        private static readonly IReadOnlyDictionary<string, string> OptionKeywordMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "\\t", "\t" },
            { "\\s", " " },
            //{ "None", "" },
            //{ "Empty", "" },
            { "SingleQuote", "'" },
            { "\\'", "'" },
            { "DoubleQuote", "\"" },
            { "\\\"", "\"" },

            { "\\r\\n", Utilities.Constant.NEWLINE_WINDOWS },
            { "LinefeedWindows", Utilities.Constant.NEWLINE_WINDOWS },

            { "\\n", Utilities.Constant.NEWLINE_UNIX },
            { "NewLine", Utilities.Constant.NEWLINE_UNIX },
            { "LinefeedUNIX", Utilities.Constant.NEWLINE_UNIX },

            { "\\r", Utilities.Constant.NEWLINE_MAC },
            { "CarriageReturn", Utilities.Constant.NEWLINE_MAC },
            { "LinefeedMAC", Utilities.Constant.NEWLINE_MAC },
            { "LinefeedApple", Utilities.Constant.NEWLINE_MAC },

            { "BackSlash", "\\" },
            { "\\\\", "\\" },
        }.AsReadOnly();

        private string ParseOption(string input)
        {
            if (input == null) return string.Empty;
            if (input.Length == 0) return string.Empty;

            foreach (var kw in OptionKeywordMap)
            {
                if (string.Equals(kw.Key, input, StringComparison.OrdinalIgnoreCase)) return kw.Value;
            }

            foreach (var kw in OptionKeywordMap)
            {
                if (kw.Key.StartsWith("\\"))
                {
                    input = input.Replace(kw.Key, kw.Value);
                }
            }

            return input;
        }

        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Replaces a string with a different string in a file");
            help.AddParameter("encoding", "en", "Encoding of the input file, ASCII/BIGENDIANUNICODE/DEFAULT/UNICODE/UTF32/UTF8/UTF8BOM (UTF8)");
            help.AddParameter("caseInsensitive", "i", "Ignore case when searching for the string to replace (false)");
            help.AddValue("<old string> <new string> <file to replace in>");
            help.AddExample("`Person` `Steve` mydoc.txt");
        }

        protected override void ExecuteInternal()
        {
            var encoding = GetArgParameterOrConfigEncoding("encoding", "en");
            var caseInsensitive = GetArgParameterOrConfigBool("caseInsensitive", "i", false);
            var values = GetArgValues().WhereNotNull().ToList();
            var oldString = values.GetAtIndexOrDefault(0);
            log.Debug($"{nameof(oldString)}: {oldString}");
            if (oldString == null) throw new ArgsException(nameof(oldString), $"No {nameof(oldString)} specified");
            var oldStringParsed = ParseOption(oldString);
            log.Debug($"{nameof(oldStringParsed)}: {oldStringParsed}");

            var newString = values.GetAtIndexOrDefault(1);
            log.Debug($"{nameof(newString)}: {newString}");
            if (newString == null) throw new ArgsException(nameof(newString), $"No {nameof(newString)} specified");
            var newStringParsed = ParseOption(newString);
            log.Debug($"{nameof(newStringParsed)}: {newStringParsed}");

            var file = values.GetAtIndexOrDefault(2);
            log.Debug($"file: {file}");
            if (file == null) throw new ArgsException(nameof(file), "No file specified");

            file = Util.ParseInputFile(file);
            if (file == null) throw new FileNotFoundException($"Could not find file {file}", file);
            if (!File.Exists(file)) throw new FileNotFoundException($"Could not find file {file}", file);

            log.Debug($"Processing file: {file}");
            var fileText = ReadFile(file, encoding);
            var occurances = caseInsensitive ? fileText.CountOccurances(oldStringParsed, StringComparison.OrdinalIgnoreCase) : fileText.CountOccurances(oldStringParsed);
            log.Debug($"Found {occurances} occurances of {oldString} in file {file}");
            fileText = caseInsensitive ? fileText.Replace(oldStringParsed, newStringParsed, StringComparison.OrdinalIgnoreCase) : fileText.Replace(oldStringParsed, newStringParsed);
            WriteFile(file, fileText, encoding);
            log.Info($"Replaced {occurances} occurances of {oldString} to {newString} in file {file}");
        }
    }
}
