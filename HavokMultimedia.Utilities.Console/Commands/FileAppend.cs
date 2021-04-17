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
using System.Linq;

namespace HavokMultimedia.Utilities.Console.Commands
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

        protected override void Execute()
        {
            //var encoding = GetArgParameterOrConfigEncoding("encoding", "en");
            var values = GetArgValues().WhereNotNull().ToList();
            if (values.Count < 1) throw new ArgsException("targetFile", "No target file specified");
            if (values.Count < 2) throw new ArgsException("sourceFile", "No source file(s) specified");

            var targetFile = values.PopHead();
            log.Debug($"{nameof(targetFile)}: {targetFile}");
            targetFile = Path.GetFullPath(targetFile);
            log.Debug($"{nameof(targetFile)}: {targetFile}");

            var sourceFiles = Util.ParseInputFiles(values).Select(o => Path.GetFullPath(o)).ToList();
            foreach (var sourceFile in sourceFiles) CheckFileExists(sourceFile);
            for (int i = 0; i < sourceFiles.Count; i++) log.Debug(nameof(sourceFiles) + "[" + i + "]: " + sourceFiles[i]);
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
