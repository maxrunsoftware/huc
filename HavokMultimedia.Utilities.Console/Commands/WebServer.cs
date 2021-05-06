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
using System.Threading;

namespace HavokMultimedia.Utilities.Console.Commands
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

            var directoryToServe = GetArgValueTrimmed(0) ?? Environment.CurrentDirectory;
            log.Debug($"{nameof(directoryToServe)}: {directoryToServe}");
            directoryToServe = Path.GetFullPath(directoryToServe);
            if (!Directory.Exists(directoryToServe)) throw new DirectoryNotFoundException("Directory was not found " + directoryToServe);
            log.Debug($"{nameof(directoryToServe)}: {directoryToServe}");

            var config = GetConfig();
            config.DirectoryToServe = directoryToServe;
            config.DirectoryToServeUrlPath = "/";

            using (var server = GetWebServer(config))
            {
                foreach (var ipa in config.UrlPrefixes) log.Info("  " + ipa);
                log.Info("WebServer running, press ESC or Q to quit, serving files from " + directoryToServe);
                while (true)
                {
                    Thread.Sleep(50);
                    var cki = System.Console.ReadKey(true);

                    if (cki.Key.In(ConsoleKey.Escape, ConsoleKey.Q)) break;
                }
            }

            log.Info("WebServer shutdown");
        }
    }
}
