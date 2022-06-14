/*
Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)

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
using System.Text;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class Table : TableBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Manipulates delimited data");
            help.AddParameter(nameof(headerDelimiter), "hd", "Delimiter for the header row, comma/tab/pipe/etc (comma)");
            help.AddParameter(nameof(headerQuoting), "hq", "Quoting character(s) for the header row, single/double/none/etc (double)");
            help.AddParameter(nameof(headerExclude), "he", "Exclude the header row from the output file (false)");
            help.AddParameter(nameof(dataDelimiter), "dd", "Delimiter for each data row, comma/tab/pipe/etc (comma)");
            help.AddParameter(nameof(dataQuoting), "dq", "Quoting character(s) for each data row, single/double/none/etc (double)");
            help.AddParameter(nameof(dataExclude), "de", "Exclude the data rows from the output file (false)");
            help.AddParameter(nameof(newline), "nl", "The type of newline character to use, win/unix/mac (win)");
            help.AddParameter(nameof(bracketGuids), "bg", "Add { and } brackets to GUID columns");
            help.AddExample("Orders.csv");
            help.AddExample("-hd=pipe -hq=single -he=true -dd=pipe -dq=single -de=false Orders.csv");
        }

        private static readonly IReadOnlyDictionary<string, string> OptionKeywordMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "COMMA", "," },
            { "TAB", "\t" },
            { "\\T", "\t" },
            { "PIPE", "|" },
            { "SPACE", " " },
            { "\\S", " " },

            { "NONE", "" },
            { "EMPTY", "" },

            { "Q", "'" },
            { "QUOTE", "'" },
            { "SINGLEQUOTE", "'" },
            { "SQUOTE", "'" },
            { "SINGLE", "'" },

            { "D", "\"" },
            { "DOUBLE", "\"" },
            { "DOUBLEQUOTE", "\"" },
            { "DQ", "\"" },
            { "\\\"", "\"" },

            { "\\r\\n", Constant.NEWLINE_WINDOWS },
            { "WIN", Constant.NEWLINE_WINDOWS },
            { "WINDOWS", Constant.NEWLINE_WINDOWS },
            { "\\n", Constant.NEWLINE_UNIX },
            { "UNIX", Constant.NEWLINE_UNIX },
            { "NIX", Constant.NEWLINE_UNIX },
            { "LINUX", Constant.NEWLINE_UNIX },
            { "\\r", Constant.NEWLINE_MAC },
            { "MAC", Constant.NEWLINE_MAC },
            { "APPLE", Constant.NEWLINE_MAC },
        }.AsReadOnly();

        public static string ParseOption(string input)
        {
            if (input == null) return string.Empty;
            if (input.Length == 0) return string.Empty;

            if (OptionKeywordMap.TryGetValue(input, out var val)) return val;

            return input;
        }

        private string headerDelimiter;
        private string headerQuoting;
        private bool headerExclude;
        private string dataDelimiter;
        private string dataQuoting;
        private bool dataExclude;
        private string newline;
        private bool bracketGuids;

        protected override void ExecuteInternal()
        {
            headerDelimiter = ParseOption(GetArgParameterOrConfig(nameof(headerDelimiter), "hd", "comma"));
            headerQuoting = ParseOption(GetArgParameterOrConfig(nameof(headerQuoting), "hq", "double"));
            headerExclude = GetArgParameterOrConfigBool(nameof(headerExclude), "he", false);
            dataDelimiter = ParseOption(GetArgParameterOrConfig(nameof(dataDelimiter), "dd", "comma"));
            dataQuoting = ParseOption(GetArgParameterOrConfig(nameof(dataQuoting), "dq", "double"));
            dataExclude = GetArgParameterOrConfigBool(nameof(dataExclude), "de", false);
            newline = ParseOption(GetArgParameterOrConfig(nameof(newline), "nl", "WIN"));
            bracketGuids = GetArgParameterOrConfigBool(nameof(bracketGuids), "bg", false);

            base.ExecuteInternal();
        }

        protected override string Convert(Utilities.Table table)
        {
            if (bracketGuids)
            {
                var isColumnGuid = table.Columns.Select(column => column.Type.In(typeof(System.Guid), typeof(System.Guid?))).ToArray();
                if (isColumnGuid.Any(o => o))
                {
                    string handler(Utilities.Table table, TableColumn column, TableRow row, int rowIndex, string value) => isColumnGuid[column.Index] ? ("{" + value + "}") : value;
                    table = table.Modify(handler);
                }
            }

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                table.ToDelimited(
                    o => writer.Write(o),
                    headerDelimiter: headerDelimiter,
                    headerQuoting: headerQuoting,
                    includeHeader: !headerExclude,
                    dataDelimiter: dataDelimiter,
                    dataQuoting: dataQuoting,
                    includeRows: !dataExclude,
                    newLine: newline
                    );
                writer.Flush();
                return sb.ToString();
            }
        }



    }
}
