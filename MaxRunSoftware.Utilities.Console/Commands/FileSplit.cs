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
using System.IO;
using System.Linq;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class FileSplit : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Splits a source file into 2 or more target files based on line numbers");
            help.AddValue("<source file> <target file 1> <target file 2> <etc>");
            help.AddExample("file.txt file1.txt file2.txt file3.txt");
            help.AddDetail("Note that this function buffers the entire file into memory, so it may require a lot of RAM");
        }

        protected override void ExecuteInternal()
        {
            //var encoding = GetArgParameterOrConfigEncoding("encoding", "en");
            var values = GetArgValuesTrimmed1N();
            var sourceFile = values.firstValue;
            sourceFile.CheckValueNotNull(nameof(sourceFile), log);
            sourceFile = Path.GetFullPath(sourceFile);
            log.DebugParameter(nameof(sourceFile), sourceFile);
            CheckFileExists(sourceFile);

            var targetFiles = values.otherValues.Select(o => Path.GetFullPath(o)).ToList();
            if (targetFiles.IsEmpty()) throw ArgsException.ValueNotSpecified(nameof(targetFiles));
            log.Debug(targetFiles, nameof(targetFiles));

            var sourceFileData = Util.FileRead(sourceFile, Constant.ENCODING_UTF8);
            var sourceFileDataLines = sourceFileData.SplitOnNewline();
            sourceFileData = null;
            GC.Collect();

            var parts = sourceFileDataLines.SplitIntoParts(targetFiles.Count);
            sourceFileDataLines = null;
            GC.Collect();

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                var partData = part.ToStringDelimited(Environment.NewLine);
                Util.FileWrite(targetFiles[i], partData, Constant.ENCODING_UTF8);
            }
        }
    }
}
