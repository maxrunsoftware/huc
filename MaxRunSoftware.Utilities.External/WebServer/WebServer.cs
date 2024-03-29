﻿// Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)
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
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.Authentication;
using EmbedIO.Files;
using Swan.Logging;

namespace MaxRunSoftware.Utilities.External;

public class WebServer : IDisposable
{
    private class SwanLogger : Swan.Logging.ILogger
    {
        public Swan.Logging.LogLevel LogLevel { get; }
        private readonly ILogger logSwan;

        private SwanLogger(Swan.Logging.LogLevel logLevel, ILogger logSwan)
        {
            LogLevel = logLevel;
            this.logSwan = logSwan;
        }

        public static IEnumerable<Swan.Logging.ILogger> CreateLoggers(ILogger log)
        {
            foreach (var type in typeof(Swan.Logging.LogLevel).GetEnumValues()) yield return new SwanLogger((Swan.Logging.LogLevel)type, log);
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
                    if (logEvent.Exception == null) { logSwan.Trace(logEvent.Message); }
                    else { logSwan.Trace(logEvent.Message, logEvent.Exception); }

                    break;
                case Swan.Logging.LogLevel.Debug:
                    if (logEvent.Exception == null) { logSwan.Debug(logEvent.Message); }
                    else { logSwan.Debug(logEvent.Message, logEvent.Exception); }

                    break;
                case Swan.Logging.LogLevel.Info:
                    if (logEvent.Exception == null) { logSwan.Info(logEvent.Message); }
                    else { logSwan.Info(logEvent.Message, logEvent.Exception); }

                    break;
                case Swan.Logging.LogLevel.Warning:
                    if (logEvent.Exception == null) { logSwan.Warn(logEvent.Message); }
                    else { logSwan.Warn(logEvent.Message, logEvent.Exception); }

                    break;
                case Swan.Logging.LogLevel.Error:
                    if (logEvent.Exception == null) { logSwan.Error(logEvent.Message); }
                    else { logSwan.Error(logEvent.Message, logEvent.Exception); }

                    break;
                case Swan.Logging.LogLevel.Fatal:
                    if (logEvent.Exception == null) { logSwan.Critical(logEvent.Message); }
                    else { logSwan.Critical(logEvent.Message, logEvent.Exception); }

                    break;
            }
        }
    }

    private static readonly ILogger log = Logging.LogFactory.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

    private readonly SingleUse started = new();
    private readonly SingleUse disposable = new();
    private static readonly SingleUse registerLoggers = new();

    private static void RegisterLoggers()
    {
        if (!registerLoggers.TryUse()) return;

        Logger.NoLogging();
        foreach (var logger in SwanLogger.CreateLoggers(log)) Logger.RegisterLogger(logger);
    }

    private EmbedIO.WebServer server;

    public WebServer() { RegisterLoggers(); }

    private async Task ProcessAction(IHttpContext context, Func<IHttpContext, object> handler)
    {
        var o = handler(context) ?? string.Empty;
        if (o is string s) { await context.SendStringAsync(s, "text/html", Encoding.UTF8); }
        else { await context.SendDataAsync(o); }
    }


    public void Start(WebServerConfig config)
    {
        if (!started.TryUse()) throw new Exception("Start() already called");

        config = config.Copy();
        log.Debug(config.ToString());
        server = new EmbedIO.WebServer(o => o.WithUrlPrefixes(config.UrlPrefixes).WithMode(HttpListenerMode.EmbedIO));

        if (config.Users.Count > 0)
        {
            var b = new BasicAuthenticationModule("/");
            foreach (var account in config.Users) b.Accounts[account.username] = account.password;

            server = server.WithModule(b);
        }

        server = server.WithLocalSessionManager();

        foreach (var pathHandler in config.PathHandlers)
        {
            var path = pathHandler.Key;
            if (!path.StartsWith("/")) path = "/" + path;

            log.Debug(nameof(pathHandler) + "[" + path + "]: " + pathHandler.Value.Item1);
            var am = new ActionModule(path, pathHandler.Value.Item1, ctx => ProcessAction(ctx, pathHandler.Value.handler));
            server = server.WithModule(am);
        }

        if (config.DirectoryToServe != null && config.DirectoryToServeUrlPath != null)
        {
            var directoryToServeUrlPath = config.DirectoryToServeUrlPath;
            if (!directoryToServeUrlPath.StartsWith("/")) directoryToServeUrlPath = "/" + directoryToServeUrlPath;

            log.Debug(nameof(config.DirectoryToServeUrlPath) + ": " + directoryToServeUrlPath);
            log.Debug(nameof(config.DirectoryToServe) + ": " + config.DirectoryToServe);
            server = server.WithStaticFolder(directoryToServeUrlPath, config.DirectoryToServe, false, o => o.DirectoryLister = DirectoryLister.Html);
        }


        server.StateChanged += (_, e) => log.Debug($"WebServer New State - {e.NewState}");
        server.HandleHttpException(async (context, exception) =>
        {
            context.Response.StatusCode = exception.StatusCode;
            log.Debug($"HTTP Exception for {context.RequestedPath}  {exception}");
            Thread.Sleep(ResponseDelayMilliseconds);
            switch (exception.StatusCode)
            {
                case 404:
                    await context.SendStringAsync(HtmlMessage("404 - Not Found", $"<p>Path {context.RequestedPath} not found</p>"), "text/html", Encoding.UTF8);
                    break;
                case 401:
                    context.AddHeader("WWW-Authenticate", "Basic");
                    await context.SendStringAsync(HtmlMessage("401 - Unauthorized", "<p>Please login to continue</p>"), "text/html", Encoding.UTF8);
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

        try { s.Dispose(); }
        catch (Exception e) { log.Warn("Error disposing of " + s.GetType().FullNameFormatted(), e); }

        log.Debug("Web server shut down");
    }

    public static string HtmlMessage(string title, string msg, string css = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<html>");
        sb.AppendLine("  <head>");
        sb.AppendLine("    <meta charset=\"utf - 8\">");
        if (title != null) sb.AppendLine($"    <title>{title}</title>");

        if (css != null)
        {
            sb.AppendLine("    <style>");
            sb.AppendLine($"    {css}");
            sb.AppendLine("    </style>");
        }

        sb.AppendLine("  </head>");
        sb.AppendLine("  <body>");
        if (title != null) sb.AppendLine($"    <h1>{title}</h1>");

        if (msg != null) sb.AppendLine($"    {msg}");

        sb.AppendLine("  </body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }
}
