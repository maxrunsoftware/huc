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

namespace MaxRunSoftware.Utilities.Console.Commands;

public class FileChecksum : Command
{
    private enum ChecksumType
    {
        MD5,
        SHA1,
        SHA256,
        SHA512
    }

    // ReSharper disable once ParameterHidesMember
    private Func<string, string> GetHashMethod(ChecksumType checksumType)
    {
        switch (checksumType)
        {
            case ChecksumType.MD5: return Util.GenerateHashMD5;
            case ChecksumType.SHA1: return Util.GenerateHashSHA1;
            case ChecksumType.SHA256: return Util.GenerateHashSHA256;
            case ChecksumType.SHA512: return Util.GenerateHashSHA512;
            default: throw new NotImplementedException(nameof(checksumType) + " " + checksumType + " has not been implemented yet");
        }
    }

    protected override void CreateHelp(CommandHelpBuilder help)
    {
        help.AddSummary("Generates a checksum of a file(s)");
        help.AddParameter(nameof(checksumType), "t", "Checksum type to generate " + DisplayEnumOptions(ChecksumType.MD5));
        help.AddParameter(nameof(recursive), "r", "If a directory is specified, recursively search that directory and all subdirectories for files (false)");
        help.AddValue("<source file 1> <source file 2> <etc>");
        help.AddExample("MyFile.zip");
        help.AddExample("-t=SHA512 *.txt");
    }

    private ChecksumType checksumType;
    private bool recursive;

    protected override void ExecuteInternal()
    {
        checksumType = GetArgParameterOrConfigEnum(nameof(checksumType), "t", ChecksumType.MD5);
        recursive = GetArgParameterOrConfigBool(nameof(recursive), "r", false);

        var sourceFiles = ParseInputFiles(GetArgValuesTrimmed());
        if (sourceFiles.IsEmpty()) throw ArgsException.ValueNotSpecified(nameof(sourceFiles));

        log.Debug(sourceFiles, nameof(sourceFiles));

        foreach (var sourceFile in sourceFiles)
        {
            var checksumFunction = GetHashMethod(checksumType);
            var checksum = checksumFunction(sourceFile);

            if (sourceFiles.Count == 1) { log.Info(checksum); }
            else { log.Info(checksum + "  <--  " + sourceFile); }
        }
    }
}
