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

using System.Text;

namespace MaxRunSoftware.Utilities.Console.Commands;

public abstract class TableBase : Command
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        help.AddParameter(nameof(encoding), "en", "Encoding of the input table " + DisplayEnumOptions(FileEncoding.UTF8));
        help.AddValue("<tab delimited input file 1> <tab delimited input file 2> <etc>");
    }

    protected string CurrentOutputFile { get; private set; }

    private Encoding encoding;

    protected override void ExecuteInternal()
    {
        encoding = GetArgParameterOrConfigEncoding(nameof(encoding), "en");

        var inputFiles = ParseInputFiles(GetArgValuesTrimmed());
        if (inputFiles.Count < 1) throw new ArgsException(nameof(inputFiles), $"No <{nameof(inputFiles)}> supplied");

        log.Debug(inputFiles, nameof(inputFiles));
        CheckFileExists(inputFiles);

        foreach (var includedItem in inputFiles)
        {
            log.Debug($"Reading table file: {includedItem}");
            var table = ReadTableTab(includedItem, encoding);
            var outputFile = includedItem;
            log.DebugParameter(nameof(outputFile), outputFile);
            CurrentOutputFile = outputFile;
            DeleteExistingFile(outputFile);

            log.Debug("Writing file with " + table.Columns.Count + " columns and " + table.Count + " rows to file " + outputFile);

            var data = Convert(table);
            WriteFile(outputFile, data);

            log.Info("File with " + table.Columns.Count + " columns, " + table.Count + " rows created: " + outputFile);
        }
    }

    protected abstract string Convert(Utilities.Table table);
}
