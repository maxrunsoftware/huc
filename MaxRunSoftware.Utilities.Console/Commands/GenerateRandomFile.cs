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
using System.IO;
using System.Security.Cryptography;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class GenerateRandomFile : Command
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        help.AddSummary("Generates a random data file");
        help.AddParameter(nameof(bufferSizeMegabytes), "b", "Buffer size in megabytes (10)");
        help.AddParameter(nameof(length), "l", "Number of characters to include (1000)");
        help.AddParameter(nameof(width), "w", "Size of the column of data to write for each line (80)");
        help.AddParameter(nameof(secureRandom), "s", "Use the secure random algorithm (false)");
        help.AddParameter(nameof(characters), "c", "Character pool to use for generation (0-9a-z)");
        help.AddValue("<output file 1> <output file 2> <etc>");
        help.AddExample("testdata.txt");
        help.AddExample("-l=1000000 testdata1.txt testdata2.txt testdata3.txt");
    }

    private int bufferSizeMegabytes;
    private int length;
    private int width;
    private bool secureRandom;
    private string characters;

    protected override void ExecuteInternal()
    {
        bufferSizeMegabytes = GetArgParameterOrConfigInt(nameof(bufferSizeMegabytes), "b", 10);
        bufferSizeMegabytes = bufferSizeMegabytes * (int)Constant.BYTES_MEGA;

        length = GetArgParameterOrConfigInt(nameof(length), "l", 1000);
        width = GetArgParameterOrConfigInt(nameof(width), "w", 80);
        if (width < 1) width = int.MaxValue;

        secureRandom = GetArgParameterOrConfigBool(nameof(secureRandom), "s", false);
        characters = GetArgParameterOrConfig(nameof(characters), "c").TrimOrNull() ?? Constant.CHARS_A_Z_LOWER + Constant.CHARS_0_9;

        var outputFiles = GetArgValuesTrimmed();
        log.Debug(outputFiles, nameof(outputFiles));
        if (outputFiles.IsEmpty()) throw ArgsException.ValueNotSpecified(nameof(outputFiles));

        var chars = characters.ToCharArray();
        var random = new Random();
        var randomSecure = RandomNumberGenerator.Create();
        foreach (var outputFile in outputFiles)
        {
            var ll = 0;
            var ww = 0;

            DeleteExistingFile(outputFile);
            using (var fs = Util.FileOpenWrite(outputFile))
            {
                using (var sw = new StreamWriter(fs, Constant.ENCODING_UTF8, bufferSizeMegabytes))
                {
                    while (ll < length)
                    {
                        if (ww >= width)
                        {
                            ww = 0;
                            sw.WriteLine();
                        }

                        var ch = chars[secureRandom ? randomSecure.Next(chars.Length) : random.Next(chars.Length)];
                        sw.Write(ch);
                        ll++;
                        ww++;
                    }

                    sw.WriteLine();
                    sw.Flush();
                    fs.Flush();
                }
            }

            log.Info("Generated random file -> " + outputFile);
        }

        log.Debug("Completed generation of " + outputFiles.Count + " random files");
    }
}
