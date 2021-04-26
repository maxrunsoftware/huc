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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.Authentication;
using EmbedIO.Files;
using Swan.Logging;

namespace HavokMultimedia.Utilities.Console.External
{
    public class WebServerConfig
    {
        public IList<string> Hostnames { get; } = new List<string>();
        public ushort Port { get; set; } = 8080;
        public string DirectoryToServe { get; set; }
        public string DirectoryToServeUrlPath { get; set; } = "/";
        public IDictionary<string, (HttpVerbs verbs, Func<IHttpContext, object> handler)> PathHandlers { get; } = new Dictionary<string, (HttpVerbs, Func<IHttpContext, object>)>(StringComparer.OrdinalIgnoreCase);
        public IList<(string username, string password)> Users { get; } = new List<(string username, string password)>();
        public IReadOnlyList<string> UrlPrefixes => Hostnames.OrderBy(o => o, StringComparer.OrdinalIgnoreCase).Select(o => $"http://{o}:{Port}").ToList().AsReadOnly();

        public WebServerConfig()
        {
            foreach (var ip in Util.NetGetIPAddresses().Where(o => o.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
            {
                Hostnames.Add(ip.ToString());
            }
            if (!Hostnames.Contains("localhost")) Hostnames.Add("localhost");
            if (!Hostnames.Contains("127.0.0.1")) Hostnames.Add("127.0.0.1");
        }
        private WebServerConfig(WebServerConfig config)
        {
            foreach (var hostname in config.Hostnames) this.Hostnames.Add(hostname);
            this.Port = config.Port;
            this.DirectoryToServe = config.DirectoryToServe;
            this.DirectoryToServeUrlPath = config.DirectoryToServeUrlPath;
            foreach (var kvp in config.PathHandlers) this.PathHandlers.Add(kvp.Key, (kvp.Value.verbs, kvp.Value.handler));
            foreach (var item in config.Users) this.Users.Add((item.username, item.password));
        }
        public WebServerConfig Copy()
        {
            return new WebServerConfig(this);
        }
    }
    public class WebServer : IDisposable
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

        private static readonly ILogger log = Program.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly SingleUse started = new SingleUse();
        private readonly SingleUse disposable = new SingleUse();
        private static readonly SingleUse registerLoggers = new SingleUse();
        private static void RegisterLoggers()
        {
            if (!registerLoggers.TryUse()) return;
            Swan.Logging.Logger.NoLogging();
            foreach (var logger in SwanLogger.CreateLoggers(log)) Swan.Logging.Logger.RegisterLogger(logger);


        }
        private EmbedIO.WebServer server;
        public WebServer()
        {
            RegisterLoggers();
        }



        public void Start(WebServerConfig config)
        {

            if (!started.TryUse()) throw new Exception("Start() already called");
            config = config.Copy();

            for (int i = 0; i < config.UrlPrefixes.Count; i++) log.Debug(nameof(config.UrlPrefixes) + "[" + i + "]: " + config.UrlPrefixes[i]);
            server = new EmbedIO.WebServer(o => o.WithUrlPrefixes(config.UrlPrefixes).WithMode(HttpListenerMode.EmbedIO));

            if (config.Users.Count > 0)
            {
                BasicAuthenticationModule b = new BasicAuthenticationModule("/");
                foreach (var account in config.Users)
                {
                    b.Accounts[account.username] = account.password;
                }
                server = server.WithModule(b);
            }

            server = server.WithLocalSessionManager();

            foreach (var pathHandler in config.PathHandlers)
            {
                var path = pathHandler.Key;
                if (!path.StartsWith("/")) path = "/" + path;
                log.Debug(nameof(pathHandler) + "[" + path + "]: " + pathHandler.Value.Item1);
                server = server.WithModule(new ActionModule(path, pathHandler.Value.Item1, ctx => ctx.SendDataAsync(pathHandler.Value.Item2(ctx))));
            }
            if (config.DirectoryToServe != null && config.DirectoryToServeUrlPath != null)
            {
                var directoryToServeUrlPath = config.DirectoryToServeUrlPath;
                if (!directoryToServeUrlPath.StartsWith("/")) directoryToServeUrlPath = "/" + directoryToServeUrlPath;
                log.Debug(nameof(config.DirectoryToServeUrlPath) + ": " + directoryToServeUrlPath);
                log.Debug(nameof(config.DirectoryToServe) + ": " + config.DirectoryToServe);
                server = server.WithStaticFolder(directoryToServeUrlPath, config.DirectoryToServe, false, (o) => o.DirectoryLister = DirectoryLister.Html);
            }


            server.StateChanged += (s, e) => log.Debug($"WebServer New State - {e.NewState}");
            server.HandleHttpException(async (context, exception) =>
            {
                context.Response.StatusCode = exception.StatusCode;
                log.Debug($"HTTP Exception for {context.RequestedPath}  {exception}");
                Thread.Sleep(ResponseDelayMilliseconds);
                switch (exception.StatusCode)
                {
                    case 404:
                        await context.SendStringAsync(HtmlMessage("404 - Not Found", $"Path {context.RequestedPath} not found"), "text/html", Encoding.UTF8);
                        break;
                    case 401:
                        context.Response.Headers.Add("WWW-Authenticate: Basic");
                        await context.SendStringAsync(HtmlMessage("401 - Unauthorized", $"Please login to continue"), "text/html", Encoding.UTF8);
                        break;
                    default:
                        await HttpExceptionHandler.Default(context, exception);
                        break;
                }
            });

            log.Debug("WebServer starting...");
            server.RunAsync();
            log.Debug("WebServer started");
        }

        public int ResponseDelayMilliseconds { get; set; } = 100;
        public void Dispose()
        {
            if (!disposable.TryUse()) return;
            log.Debug("Shutting down web server");
            var s = server;
            server = null;
            if (s == null) return;
            try
            {
                s.Dispose();
            }
            catch (Exception e)
            {
                log.Warn("Error disposing of " + s.GetType().FullNameFormatted(), e);
            }
            log.Debug("Web server shut down");
        }

        public static string HtmlMessage(string title, string msg)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<html>");
            sb.AppendLine($"  <head>");
            sb.AppendLine($"    <meta charset=\"utf - 8\">");
            sb.AppendLine($"    <title>{title}</title>");
            sb.AppendLine($"  </head>");
            sb.AppendLine($"  <body>");
            sb.AppendLine($"    <h1>{title}</h1>");
            sb.AppendLine($"    <p>{msg}</p>");
            sb.AppendLine($"  </body>");
            sb.AppendLine($"</html>");
            return sb.ToString();
        }
    }

    public static class WebServerExtensions
    {
        public static SortedDictionary<string, string> GetParameters(this IHttpContext context)
        {
            var d = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var parameters = context.GetRequestQueryData();
            foreach (var key in parameters.AllKeys)
            {
                var k = key.TrimOrNull();
                if (k == null) continue;
                var v = parameters[key].TrimOrNull();
                if (v == null) continue;
                d[k] = v;
            }
            return d;
        }
    }
}
