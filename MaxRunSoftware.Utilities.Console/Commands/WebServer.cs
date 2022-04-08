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
using System.Threading;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class WebServer : WebServerBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);

            help.AddSummary("Creates a web server to host static html files");
            help.AddDetail("You can use an index.html file in the root of the directory to display a page rather then a directory listing");
            help.AddValue("<directory to serve>");
            help.AddExample(".");
            help.AddExample("-o=80 c:\\www");
        }

        protected override void ExecuteInternal()
        {
            base.ExecuteInternal();

            var directoryToServe = GetArgValueDirectory(0, valueName: "directoryToServe", useCurrentDirectoryAsDefault: true);

            var config = GetConfig();
            config.DirectoryToServe = directoryToServe;
            config.DirectoryToServeUrlPath = "/";

            log.Info("Serving file from " + directoryToServe);
            LoopUntilKey(config);
        }
    }
}
