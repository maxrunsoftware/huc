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

using System.IO;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public abstract class TableBase : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddValue("<tab delimited input file 1> <tab delimited input file 2> <etc>");
        }

        protected string CurrentOutputFile { get; private set; }

        protected override void ExecuteInternal()
        {
            var includedItems = Util.ParseInputFiles(GetArgValuesTrimmed());
            if (includedItems.Count < 1) throw new ArgsException("inputFiles", "No input files supplied");
            for (int i = 0; i < includedItems.Count; i++) log.Debug($"inputFile[{i}]: {includedItems[i]}");
            foreach (var includedItem in includedItems)
            {
                if (!File.Exists(includedItem)) throw new FileNotFoundException("Input file " + includedItem + " does not exist", includedItem);
            }
            for (var i = 0; i < includedItems.Count; i++) log.Debug($"inputFile[{i}]: {includedItems[i]}");

            foreach (var includedItem in includedItems)
            {
                log.Debug($"Reading table file: {includedItem}");
                var table = ReadTableTab(includedItem);
                var outputFile = includedItem;
                log.Debug(nameof(outputFile) + ": " + outputFile);
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
}
