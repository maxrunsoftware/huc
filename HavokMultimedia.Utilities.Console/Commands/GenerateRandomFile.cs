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
using System.Security.Cryptography;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class GenerateRandomFile : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Generates a random data file");
            help.AddParameter("bufferSizeMegabytes", "b", "Buffer size in megabytes (10)");
            help.AddParameter("length", "l", "Number of characters to include (1000)");
            help.AddParameter("width", "w", "Size of the column of data to write for each line (80)");
            help.AddParameter("secureRandom", "s", "Use the secure random algorithim (false)");
            help.AddParameter("characters", "c", "Character pool to use for generation (0-9a-z)");
            help.AddValue("<output file 1> <output file 2> <etc>");
            help.AddExample("testdata.txt");
            help.AddExample("-l=1000000 testdata1.txt testdata2.txt testdata3.txt");
        }

        protected override void ExecuteInternal()
        {
            var b = GetArgParameterOrConfigInt("bufferSizeMegabytes", "b", 10);
            b = b * (int)Constant.BYTES_MEGA;

            var l = GetArgParameterOrConfigInt("length", "l", 1000);
            var w = GetArgParameterOrConfigInt("width", "w", 80);
            var secureRandom = GetArgParameterOrConfigBool("secureRandom", "s", false);
            if (w < 1) w = int.MaxValue;

            var c = GetArgParameterOrConfig("characters", "c").TrimOrNull() ?? (Constant.CHARS_A_Z_LOWER + Constant.CHARS_0_9);

            var outputFiles = GetArgValuesTrimmed();
            log.Debug(outputFiles, nameof(outputFiles));
            if (outputFiles.IsEmpty()) throw new ArgsException(nameof(outputFiles), $"No <{nameof(outputFiles)}> specified");

            var chars = c.ToCharArray();
            var random = new Random();
            var srandom = RandomNumberGenerator.Create();
            foreach (var outputFile in outputFiles)
            {
                int ll = 0;
                int ww = 0;

                DeleteExistingFile(outputFile);
                using (var fs = Util.FileOpenWrite(outputFile))
                {
                    using (var sw = new StreamWriter(fs, Constant.ENCODING_UTF8_WITHOUT_BOM, b))
                    {
                        while (ll < l)
                        {
                            if (ww >= w)
                            {
                                ww = 0;
                                sw.WriteLine();
                            }
                            var ch = chars[secureRandom ? srandom.Next(chars.Length) : random.Next(chars.Length)];
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
}
