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
using System.Net;
using System.Text;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class WGet : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Same as WGET command for getting web resources");
            help.AddParameter("username", "u", "Basic authentication username");
            help.AddParameter("password", "p", "Basic authentication password");
            help.AddValue("<source URL> <output file>");
            help.AddExample("https://github.com/Steven-D-Foster/huc/releases/download/v1.3.0/huc-linux.zip");
            help.AddExample("https://github.com github.txt");
        }

        protected override void Execute()
        {
            var username = GetArgParameterOrConfig("username", "u").TrimOrNull();
            var password = GetArgParameterOrConfig("password", "p").TrimOrNull();

            var sourceURL = GetArgValueTrimmed(0);
            log.Debug($"{nameof(sourceURL)}: {sourceURL}");
            if (sourceURL == null) throw new ArgsException(nameof(sourceURL), $"{nameof(sourceURL)} not provided");

            var outputFile = GetArgValueTrimmed(1);
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
                bool unauthorized = false;
                try
                {
                    cli.DownloadFile(sourceURL, outputFile);
                }
                catch (WebException we)
                {
                    if (username != null && password != null)
                    {
                        if (we.Message.Contains("(401)"))
                        {
                            unauthorized = true;
                        }
                    }
                }
                if (unauthorized)
                {
                    // https://stackoverflow.com/a/26016919
                    string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                    cli.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", credentials);
                    cli.DownloadFile(sourceURL, outputFile);
                }
                log.Info(sourceURL + "  ->  " + outputFile);

            }

        }
    }
}
