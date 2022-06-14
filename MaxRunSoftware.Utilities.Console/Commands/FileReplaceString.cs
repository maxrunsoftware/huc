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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// ReSharper disable StringLiteralTypo

namespace MaxRunSoftware.Utilities.Console.Commands;

public class FileReplaceString : Command
{
    private static readonly IReadOnlyDictionary<string, string> optionKeywordMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "\\t", "\t" },
        { "\\s", " " },
        { "None", "" },
        //{ "Empty", "" },
        { "SingleQuote", "'" },
        { "\\'", "'" },
        { "DoubleQuote", "\"" },
        { "\\\"", "\"" },

        { "\\r\\n", Constant.NEWLINE_WINDOWS },
        { "LinefeedWindows", Constant.NEWLINE_WINDOWS },

        { "\\n", Constant.NEWLINE_UNIX },
        { "NewLine", Constant.NEWLINE_UNIX },
        { "LinefeedUNIX", Constant.NEWLINE_UNIX },

        { "\\r", Constant.NEWLINE_MAC },
        { "CarriageReturn", Constant.NEWLINE_MAC },
        { "LinefeedMAC", Constant.NEWLINE_MAC },
        { "LinefeedApple", Constant.NEWLINE_MAC },

        { "BackSlash", "\\" },
        { "\\\\", "\\" }
    }.AsReadOnly();

    private string ParseOption(string input)
    {
        if (input == null) return string.Empty;

        if (input.Length == 0) return string.Empty;

        foreach (var kw in optionKeywordMap)
            if (string.Equals(kw.Key, input, StringComparison.OrdinalIgnoreCase))
                return kw.Value;

        foreach (var kw in optionKeywordMap)
            if (kw.Key.StartsWith("\\"))
                input = input.Replace(kw.Key, kw.Value);

        return input;
    }

    protected override void CreateHelp(CommandHelpBuilder help)
    {
        help.AddSummary("Replaces a string with a different string in a file");
        help.AddParameter(nameof(encoding), "en", "Encoding of the input file (" + nameof(FileEncoding.UTF8) + ")  " + DisplayEnumOptions<FileEncoding>());
        help.AddParameter(nameof(caseInsensitive), "i", "Ignore case when searching for the string to replace (false)");
        help.AddValue("<old string> <new string> <file to replace in>");
        help.AddExample("`Person` `Steve` mydoc.txt");
        help.AddDetail("Keywords...");
        foreach (var kw in optionKeywordMap.Keys.OrderBy(o => o.ToLower())) help.AddDetail("  " + kw);
    }

    private Encoding encoding;
    private bool caseInsensitive;

    protected override void ExecuteInternal()
    {
        encoding = GetArgParameterOrConfigEncoding(nameof(encoding), "en");
        caseInsensitive = GetArgParameterOrConfigBool(nameof(caseInsensitive), "i", false);

        var values = GetArgValues().WhereNotNull().ToList();
        var oldString = values.GetAtIndexOrDefault(0);
        oldString.CheckValueNotNull(nameof(oldString), log);
        var oldStringParsed = ParseOption(oldString);
        log.DebugParameter(nameof(oldStringParsed), oldStringParsed);

        var newString = values.GetAtIndexOrDefault(1);
        newString.CheckValueNotNull(nameof(newString), log);
        var newStringParsed = ParseOption(newString);
        log.DebugParameter(nameof(newStringParsed), newStringParsed);

        var file = values.GetAtIndexOrDefault(2);
        file.CheckValueNotNull(nameof(file), log);
        var actualFile = ParseInputFile(file);
        if (actualFile == null) throw new FileNotFoundException($"Could not find file {file}", file);

        if (!File.Exists(actualFile)) throw new FileNotFoundException($"Could not find file {file}", file);

        file = actualFile;

        log.Debug($"Processing file: {file}");
        var fileText = ReadFile(file, encoding);
        var occurrences = caseInsensitive ? fileText.CountOccurrences(oldStringParsed, StringComparison.OrdinalIgnoreCase) : fileText.CountOccurrences(oldStringParsed);
        log.Debug($"Found {occurrences} occurances of {oldString} in file {file}");
        fileText = caseInsensitive ? fileText.Replace(oldStringParsed, newStringParsed, StringComparison.OrdinalIgnoreCase) : fileText.Replace(oldStringParsed, newStringParsed);
        WriteFile(file, fileText, encoding);
        log.Info($"Replaced {occurrences} occurances of {oldString} to {newString} in file {file}");
    }
}
