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
using System.Text;

namespace HavokMultimedia.Utilities.Console.Commands
{

    public class Table : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Manipulates delimited data");
            help.AddParameter("headerDelimiter", "hd", "Delimiter for the header row, comma/tab/pipe/etc (comma)");
            help.AddParameter("headerQuoting", "hq", "Quoting character(s) for the header row, single/double/none/etc (double)");
            help.AddParameter("headerExclude", "he", "Exclude the header row from the output file (false)");
            help.AddParameter("dataDelimiter", "dd", "Delimiter for each data row, comma/tab/pipe/etc (comma)");
            help.AddParameter("dataQuoting", "dq", "Quoting character(s) for each data row, single/double/none/etc (double)");
            help.AddParameter("dataExclude", "de", "Exclude the data rows from the output file (false)");
            help.AddParameter("newline", "nl", "The type of newline character to use, win/unix/mac (win)");
            help.AddParameter("encoding", "en", "Encoding of the output file, ASCII/BIGENDIANUNICODE/DEFAULT/UNICODE/UTF32/UTF8/UTF8BOM (UTF8)");
            help.AddValue("<tab delimited input file 1> <tab delimited input file 2> <etc>");
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

            { "\\r\\n", Utilities.Constant.NEWLINE_WINDOWS },
            { "WIN", Utilities.Constant.NEWLINE_WINDOWS },
            { "WINDOWS", Utilities.Constant.NEWLINE_WINDOWS },
            { "\\n", Utilities.Constant.NEWLINE_UNIX },
            { "UNIX", Utilities.Constant.NEWLINE_UNIX },
            { "NIX", Utilities.Constant.NEWLINE_UNIX },
            { "LINUX", Utilities.Constant.NEWLINE_UNIX },
            { "\\r", Utilities.Constant.NEWLINE_MAC },
            { "MAC", Utilities.Constant.NEWLINE_MAC },
            { "APPLE", Utilities.Constant.NEWLINE_MAC },
        }.AsReadOnly();

        public static string ParseOption(string input)
        {
            if (input == null) return string.Empty;
            if (input.Length == 0) return string.Empty;

            if (OptionKeywordMap.TryGetValue(input, out var val)) return val;

            return input;
        }

        private string hd;
        private string hq;
        private bool he;
        private string dd;
        private string dq;
        private bool de;
        private string nl;
        private Encoding en;

        private void Execute(Utilities.Table table, string outputFile)
        {
            log.Debug(nameof(outputFile) + ": " + outputFile);

            DeleteExistingFile(outputFile);

            log.Debug("Writing file with " + table.Columns.Count + " columns and " + table.Count + " rows to file " + outputFile);
            using (var stream = File.OpenWrite(outputFile))
            using (var streamWriter = new StreamWriter(stream, en))
            {
                table.ToDelimited(
                    o => streamWriter.Write(o),
                    headerDelimiter: hd,
                    headerQuoting: hq,
                    includeHeader: !he,
                    dataDelimiter: dd,
                    dataQuoting: dq,
                    includeRows: !de,
                    newLine: nl
                    );
                streamWriter.Flush();
                stream.Flush(true);
            }

            log.Info("Delimited file with " + table.Columns.Count + " columns, " + table.Count + " rows created: " + outputFile);
        }

        protected override void Execute()
        {
            var includedItems = Util.ParseInputFiles(GetArgValues().TrimOrNull().WhereNotNull());
            if (includedItems.Count < 1) throw new ArgsException("inputFiles", "No input files supplied");
            for (int i = 0; i < includedItems.Count; i++) log.Debug($"inputFile[{i}]: {includedItems[i]}");

            foreach (var includedItem in includedItems)
            {
                if (!File.Exists(includedItem)) throw new FileNotFoundException("Input file " + includedItem + " does not exist", includedItem);
            }

            hd = ParseOption(GetArgParameterOrConfig("headerDelimiter", "hd", "comma"));

            hq = ParseOption(GetArgParameterOrConfig("headerQuoting", "hq", "double"));

            he = GetArgParameterOrConfigBool("headerExclude", "he", false);

            dd = ParseOption(GetArgParameterOrConfig("dataDelimiter", "dd", "comma"));

            dq = ParseOption(GetArgParameterOrConfig("dataQuoting", "dq", "double"));

            de = GetArgParameterOrConfigBool("dataExclude", "de", false);

            nl = ParseOption(GetArgParameterOrConfig("newline", "nl", "WIN"));

            en = GetArgParameterOrConfigEncoding("encoding", "en");

            for (var i = 0; i < includedItems.Count; i++) log.Debug($"inputFile[{i}]: {includedItems[i]}");
            foreach (var includedItem in includedItems)
            {
                log.Debug($"Reading table file: {includedItem}");
                var table = ReadTableTab(includedItem);
                Execute(table, includedItem);
            }
        }




    }
}
