// /*
// Copyright (c) 2021 Steven Foster (steven.d.foster@gmail.com)
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
// */
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class WGet : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Same as WGET command for getting web resources");
            help.AddValue("<source URL> <output file>");
        }

        protected override void Execute()
        {
            var sourceURL = GetArgValues().GetAtIndexOrDefault(0).TrimOrNull();
            log.Debug($"{nameof(sourceURL)}: {sourceURL}");
            if (sourceURL == null) throw new ArgsException(nameof(sourceURL), $"{nameof(sourceURL)} not provided");

            var outputFile = GetArgValues().GetAtIndexOrDefault(1).TrimOrNull();
            log.Debug($"{nameof(outputFile)}: {outputFile}");
            if (outputFile == null)
            {
                outputFile = sourceURL.Split('/').TrimOrNull().WhereNotNull().LastOrDefault();

                outputFile = Util.FilenameSanitize(outputFile, "_");
                outputFile = Path.Combine(Environment.CurrentDirectory, outputFile);
            }
            outputFile = Path.GetFullPath(outputFile);
            log.Debug($"{nameof(outputFile)}: {outputFile}");
            DeleteExistingFile(outputFile);

            using (var cli = new WebClient())
            {
                cli.DownloadFile(sourceURL, outputFile);

                log.Info(sourceURL + "  ->  " + outputFile);

            }

        }
    }
}
