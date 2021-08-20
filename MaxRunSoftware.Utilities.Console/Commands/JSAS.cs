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
using System.Collections.Generic;
using System.IO;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class JSAS : Command
    {
        private static readonly string[] MIME_TEXT = new string[] {
        "text/plain",
        "text/css",
        "text/csv",
        "text/html",
        "text/calendar",
        "text/javascript",
        "application/xhtml+xml",
        "application/xml",
        "text/xml"
        };

        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Calls JSAS web service");
            help.AddValue("<source URL> <password> <resource name>");
            help.AddValue("<source URL> <password> <resource name> <output file>");
            help.AddExample("https://192.168.0.10 MyPassword Somefile");
            help.AddExample("https://192.168.0.10 MyPassword Somefile MyDownloadedFile.txt");
        }

        private string resourceName;
        private string password;
        private string sourceUrl;
        private string outputFile;

        protected override void ExecuteInternal()
        {
            sourceUrl = GetArgValueTrimmed(0);
            log.DebugParameter(nameof(sourceUrl), sourceUrl);
            sourceUrl.CheckValueNotNull(nameof(sourceUrl), log);

            password = GetArgValueTrimmed(1);
            log.DebugParameter(nameof(password), password);
            password.CheckValueNotNull(nameof(password), log);

            resourceName = GetArgValueTrimmed(2);
            log.DebugParameter(nameof(resourceName), resourceName);
            resourceName.CheckValueNotNull(nameof(resourceName), log);

            outputFile = GetArgValueTrimmed(3);
            log.DebugParameter(nameof(outputFile), outputFile);
            if (outputFile != null)
            {
                outputFile = Path.GetFullPath(outputFile);
                log.DebugParameter(nameof(outputFile), outputFile);
                DeleteExistingFile(outputFile);
            }

            if (outputFile == null)
            {
                var response = Util.WebDownload(sourceUrl, username: resourceName, password: password);
                var ct = response.ContentType.ToLower();
                if (ct != null)
                {
                    var isText = false;
                    foreach (var mime in MIME_TEXT)
                    {
                        if (mime.EqualsCaseInsensitive(ct)) isText = true;
                    }

                    if (isText)
                    {
                        log.Info(Constant.ENCODING_UTF8_WITHOUT_BOM.GetString(response.Data));
                    }
                    else
                    {
                        log.Info("WebResponse.Data[" + response.Data.Length + "]");
                    }
                }
            }
            else
            {
                Util.WebDownload(sourceUrl, outputFile, username: resourceName, password: password);
                log.Info("File downloaded: " + outputFile);
            }

        }
    }
}
