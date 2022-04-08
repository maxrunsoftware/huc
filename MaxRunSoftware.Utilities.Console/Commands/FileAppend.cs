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

using System.IO;
using System.Linq;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class FileAppend : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Appends one or more source files to a target file");
            //help.AddParameter("encoding", "en", "Encoding of the input file, ASCII/BIGENDIANUNICODE/DEFAULT/UNICODE/UTF32/UTF8/UTF8BOM (UTF8)");
            help.AddValue("<target file> <source file 1> <source file 2> <etc>");
            help.AddExample("mainfile.txt file1.txt file2.txt");
        }

        protected override void ExecuteInternal()
        {
            //var encoding = GetArgParameterOrConfigEncoding("encoding", "en");
            var values = GetArgValuesTrimmed1N();
            var targetFile = values.firstValue;
            targetFile.CheckValueNotNull(nameof(targetFile), log);
            targetFile = Path.GetFullPath(targetFile);
            log.DebugParameter(nameof(targetFile), targetFile);

            var sourceFiles = values.otherValues;
            log.Debug(sourceFiles, nameof(sourceFiles));
            if (values.otherValues.IsEmpty()) throw ArgsException.ValueNotSpecified(nameof(sourceFiles));
            sourceFiles = ParseInputFiles(values.otherValues);
            log.Debug(sourceFiles, nameof(sourceFiles));
            CheckFileExists(sourceFiles);

            using (var targetFileStream = Util.FileOpenWrite(targetFile))
            {
                targetFileStream.Seek(targetFileStream.Length, SeekOrigin.Begin); // Set the stream position to the end of the file. 
                foreach (var sourceFile in sourceFiles)
                {
                    using (var sourceFileStream = Util.FileOpenRead(sourceFile))
                    {
                        log.Debug("Writing file " + sourceFile + " to " + targetFile);
                        sourceFileStream.CopyTo(targetFileStream, 10 * (int)Constant.BYTES_MEGA);
                        log.Info(targetFile + "  <--  " + sourceFile);
                    }
                }
                targetFileStream.Flush();
                targetFileStream.CloseSafe();
            }
        }
    }
}
