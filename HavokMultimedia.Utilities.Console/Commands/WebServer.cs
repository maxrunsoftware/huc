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
using EmbedIO;
namespace HavokMultimedia.Utilities.Console.Commands
{
    public class WebServer : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Creates a web server");
            help.AddParameter("ipaddress", "ip", "IP address to bind to (localhost)");
            help.AddParameter("port", "o", "Port to bind to (8080)");
            help.AddValue("<directory to serve>");
        }

        protected override void Execute()
        {
            var ipaddress = GetArgParameterOrConfig("ipaddress", "ip").TrimOrNull() ?? "localhost";
            var port = GetArgParameterOrConfigInt("port", "o", 8080);
            var directoryToServe = GetArgValues().GetAtIndexOrDefault(0) ?? Environment.CurrentDirectory;
            log.Debug($"{nameof(directoryToServe)}: {directoryToServe}");
            directoryToServe = Path.GetFullPath(directoryToServe);
            if (!Directory.Exists(directoryToServe)) throw new DirectoryNotFoundException("Directory was not found " + directoryToServe);

            var server = new EmbedIO.WebServer(o => o.WithUrlPrefix($"http://{ipaddress}:{port}").WithMode(HttpListenerMode.EmbedIO));
            server = server.WithLocalSessionManager();
            server = server.WithStaticFolder("/", directoryToServe, false);
            server.StateChanged += (s, e) => log.Debug($"WebServer New State - {e.NewState}");

            using (server)
            {
                server.RunAsync();
                log.Info("Server running, press ESC or Q to quit, serving files from " + directoryToServe);
                while (true)
                {
                    Thread.Sleep(50);
                    var cki = System.Console.ReadKey(true);

                    if (cki.Key.In(ConsoleKey.Escape, ConsoleKey.Q)) break;
                }
            }

            log.Debug("WebServer shutdown");
        }

    }
}
