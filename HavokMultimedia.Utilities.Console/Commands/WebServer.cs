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
using System.Threading;
using EmbedIO;
using Swan;
using Swan.Logging;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class WebServer : Command
    {
        private class SwanLogger : Swan.Logging.ILogger
        {
            public Swan.Logging.LogLevel LogLevel { get; init; }
            private readonly ILogger log;
            private SwanLogger(Swan.Logging.LogLevel logLevel, ILogger log)
            {
                this.LogLevel = logLevel;
                this.log = log;
            }

            public static IEnumerable<Swan.Logging.ILogger> CreateLoggers(ILogger log)
            {
                foreach (var type in Util.GetEnumItems<Swan.Logging.LogLevel>())
                {
                    yield return new SwanLogger(type, log);
                }
            }

            public void Dispose() { }

            public void Log(LogMessageReceivedEventArgs logEvent)
            {
                if (logEvent.MessageType != LogLevel) return;
                switch (LogLevel)
                {
                    case Swan.Logging.LogLevel.None:
                        break;
                    case Swan.Logging.LogLevel.Trace:
                        if (logEvent.Exception == null) log.Trace(logEvent.Message); else log.Trace(logEvent.Message, logEvent.Exception);
                        break;
                    case Swan.Logging.LogLevel.Debug:
                        if (logEvent.Exception == null) log.Debug(logEvent.Message); else log.Debug(logEvent.Message, logEvent.Exception);
                        break;
                    case Swan.Logging.LogLevel.Info:
                        if (logEvent.Exception == null) log.Info(logEvent.Message); else log.Info(logEvent.Message, logEvent.Exception);
                        break;
                    case Swan.Logging.LogLevel.Warning:
                        if (logEvent.Exception == null) log.Warn(logEvent.Message); else log.Warn(logEvent.Message, logEvent.Exception);
                        break;
                    case Swan.Logging.LogLevel.Error:
                        if (logEvent.Exception == null) log.Error(logEvent.Message); else log.Error(logEvent.Message, logEvent.Exception);
                        break;
                    case Swan.Logging.LogLevel.Fatal:
                        if (logEvent.Exception == null) log.Critical(logEvent.Message); else log.Critical(logEvent.Message, logEvent.Exception);
                        break;
                    default:
                        break;
                }
            }
        }
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Creates a web server to host static html files");
            help.AddParameter("ipaddress", "ip", "IP address to bind to (localhost)");
            help.AddParameter("port", "o", "Port to bind to (8080)");
            help.AddDetail("It is recommended to have an index.html file in the root of the directory");
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
            log.Debug($"{nameof(directoryToServe)}: {directoryToServe}");

            Logger.NoLogging();
            foreach (var logger in SwanLogger.CreateLoggers(log)) Logger.RegisterLogger(logger);

            var server = new EmbedIO.WebServer(o => o.WithUrlPrefix($"http://{ipaddress}:{port}").WithMode(HttpListenerMode.EmbedIO));
            server = server.WithLocalSessionManager();
            server = server.WithStaticFolder("/", directoryToServe, false);
            server.StateChanged += (s, e) => log.Debug($"WebServer New State - {e.NewState}");

            using (server)
            {
                log.Debug("WebServer starting...");
                server.RunAsync();
                log.Debug("WebServer started");
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
