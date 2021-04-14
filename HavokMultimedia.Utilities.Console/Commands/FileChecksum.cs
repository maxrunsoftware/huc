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
using System.IO;
using System.Linq;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class FileChecksum : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Generates a checksum of a file(s)");
            help.AddParameter("checksumType", "t", "Checksum type to generate [ MD5 | SHA1 | SHA256 | SHA512 ] (MD5)");
            help.AddParameter("recursive", "r", "If a directory is specified, recursively search that directory and all sudirectories for files (false)");
            help.AddValue("<source file 1> <source file 2> <etc>");
        }

        protected override void Execute()
        {
            var checksumType = GetArgParameterOrConfig("checksumType", "t", "MD5");
            if (checksumType.NotIn(StringComparer.OrdinalIgnoreCase, "MD5", "SHA1", "SHA256", "SHA512")) throw new ArgsException(nameof(checksumType), $"Invalid {nameof(checksumType)}");
            var recursive = GetArgParameterOrConfigBool("recursive", "r", false);

            var values = GetArgValues().TrimOrNull().WhereNotNull().ToList();
            if (values.IsEmpty()) throw new ArgsException("sourceFiles", "No source files specified");
            for (int i = 0; i < values.Count; i++) log.Debug("sourceFiles[" + i + "]: " + values[i]);

            var inputFiles = Util.ParseInputFiles(values, recursive: recursive)
                .OrderBy(o => o, Constant.OS_WINDOWS ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                .ToList();
            if (inputFiles.IsEmpty()) throw new ArgsException("sourceFiles", "No source files specified");

            for (int i = 0; i < inputFiles.Count; i++) log.Debug("inputFiles[" + i + "]: " + inputFiles[i]);

            foreach (var inputFile in inputFiles)
            {
                string checksum = null;

                if (checksumType.EqualsCaseInsensitive("MD5")) checksum = Util.GenerateHashMD5(inputFile);
                else if (checksumType.EqualsCaseInsensitive("SHA1")) checksum = Util.GenerateHashSHA1(inputFile);
                else if (checksumType.EqualsCaseInsensitive("SHA256")) checksum = Util.GenerateHashSHA256(inputFile);
                else if (checksumType.EqualsCaseInsensitive("SHA512")) checksum = Util.GenerateHashSHA512(inputFile);
                else throw new NotImplementedException(nameof(checksumType) + " " + checksumType + " has not been implemented yet");

                if (inputFiles.Count == 1) log.Info(checksum);
                else log.Info(checksum + "  <--  " + inputFile);
            }

        }
    }
}
