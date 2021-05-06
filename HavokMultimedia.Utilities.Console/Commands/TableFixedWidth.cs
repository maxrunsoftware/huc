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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HavokMultimedia.Utilities.Console.Commands
{

    public class TableFixedWidth : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Manipulates delimited data into a fixed width format");
            help.AddDetail("By default, any columns with a 0 will be excluded from the result. All columns must have a width provided. The input file will be overwritten.");
            help.AddParameter("headerInclude", "h", "Include the header row in the output file (false)");
            help.AddParameter("newline", "n", "The type of newline character to use, win/unix/mac (win)");
            help.AddParameter("encoding", "e", "Encoding of the output file, ASCII/BIGENDIANUNICODE/DEFAULT/UNICODE/UTF32/UTF8/UTF8BOM (UTF8)");

            help.AddValue("<tab delimited input file> <column1width> <column2width> <etc>");
        }



        protected override void ExecuteInternal()
        {
            var colWidths = GetArgValuesTrimmed();
            var inputFile = colWidths.PopHead();
            log.Debug($"inputFile: {inputFile}");
            for (int i = 0; i < colWidths.Count; i++) log.Debug($"columnWidth[{i}]: {colWidths[i]}");
            var widths = colWidths.Select(o => o.ToUInt()).ToArray();
            for (int i = 0; i < widths.Length; i++) log.Debug($"widths[{i}]: {widths[i]}");


            inputFile = Util.ParseInputFiles(inputFile.Yield()).FirstOrDefault();
            log.Debug($"inputFile: {inputFile}");
            if (!File.Exists(inputFile)) throw new FileNotFoundException("Input file " + inputFile + " does not exist", inputFile);

            var headerInclude = GetArgParameterOrConfigBool("headerInclude", "h", false);
            var newline = Table.ParseOption(GetArgParameterOrConfig("newline", "n", "WIN"));
            var encoding = GetArgParameterOrConfigEncoding("encoding", "e");

            log.Debug($"Reading table file: {inputFile}");
            var table = ReadTableTab(inputFile, encoding);
            if (colWidths.Count > table.Columns.Count)
            {
                var msg = $"Too many columns specified, file only contains {table.Columns.Count} columns but {colWidths.Count} were provided";
                throw new ArgsException("inputFile", msg);
            }

            if (colWidths.Count < table.Columns.Count)
            {
                var msg = $"Not enough columns specified, file contains {table.Columns.Count} columns but only {colWidths.Count} were provided";
                throw new ArgsException("inputFile", msg);
            }



            var outputFile = inputFile;
            log.Debug($"outputFile: {outputFile}");
            DeleteExistingFile(outputFile);

            string FormatCell(string cellData, int width)
            {
                if (cellData == null) cellData = string.Empty;
                cellData = cellData.PadRight(width, ' ');
                cellData = cellData.Left(width);
                return cellData;
            }

            using (var stream = File.OpenWrite(outputFile))
            using (var streamWriter = new StreamWriter(stream, encoding))
            {
                var colCount = table.Columns.Count;
                if (headerInclude)
                {
                    for (int i = 0; i < colCount; i++)
                    {
                        if (widths[i] == 0) continue;
                        var cell = FormatCell(table.Columns[i].Name, (int)widths[i]);
                        streamWriter.Write(cell);
                    }
                    streamWriter.WriteLine();
                }

                foreach (var row in table)
                {
                    for (int i = 0; i < colCount; i++)
                    {
                        if (widths[i] == 0) continue;
                        var cell = FormatCell(row[i], (int)widths[i]);
                        streamWriter.Write(cell);
                    }
                    streamWriter.WriteLine();
                }

                streamWriter.Flush();
                stream.Flush(true);
            }

            log.Debug("Successfully converted table to fixed width file");
            log.Info("Fixed width file with " + table.Columns.Count + " columns, " + table.Count + " rows created: " + outputFile);



        }




    }
}
