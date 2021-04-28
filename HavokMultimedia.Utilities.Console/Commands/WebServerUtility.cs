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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using EmbedIO;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class WebServerUtility : WebServerBase
    {
        public class Handler
        {
            public string Name => HandlerMethod.Method.Name;
            public string NameWithSpaces => Name.SplitOnCamelCase().Select(o => Upper(o)).ToStringDelimited(" ");
            public bool IsEnabled { get; set; } = false;
            public Func<IHttpContext, object> HandlerMethod { get; }
            public HttpVerbs HttpVerbs { get; }
            public string URL => "/" + Upper(Name);
            public string HelpP1 => Lower(Name) + "Disable";
            public string HelpP2 => Name.SplitOnCamelCase().Select(o => Lower(o[0].ToString())).ToStringDelimited("") + "d";
            public string HelpDescription => $"Disables the {URL} utility";

            private static string Upper(string str) => str.First().ToString().ToUpper() + str.Substring(1);
            private static string Lower(string str) => str.First().ToString().ToLower() + str.Substring(1);
            public Handler(Func<IHttpContext, object> handlerMethod, HttpVerbs httpVerbs)
            {
                HandlerMethod = handlerMethod;
                HttpVerbs = httpVerbs;
            }
        }

        private IList<Handler> GetHandlers() => new List<Handler>
            {
                new Handler(GenerateRandom, HttpVerbs.Get),
                new Handler(GenerateRandomFile, HttpVerbs.Get)
            };

        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);

            help.AddSummary("Creates a web server that provides various utilities");
            foreach (var handler in GetHandlers()) help.AddParameter(handler.HelpP1, handler.HelpP2, handler.HelpDescription);
            help.AddExample("");
        }

        private IList<Handler> handlers;
        protected override void Execute()
        {
            base.Execute();
            var config = GetConfig();
            handlers = GetHandlers();

            foreach (var handler in handlers)
            {
                handler.IsEnabled = !GetArgParameterOrConfigBool(handler.HelpP1, handler.HelpP2, false);
            }

            foreach (var handler in handlers)
            {
                if (handler.IsEnabled) config.AddPathHandler(handler.URL, handler.HttpVerbs, handler.HandlerMethod);
            }

            config.AddPathHandler("/", HttpVerbs.Get, Index);


            using (var server = GetWebServer(config))
            {
                foreach (var ipa in config.UrlPrefixes) log.Info("  " + ipa);
                log.Info("WebServer running, press ESC or Q to quit");
                while (true)
                {
                    Thread.Sleep(50);
                    var cki = System.Console.ReadKey(true);

                    if (cki.Key.In(ConsoleKey.Escape, ConsoleKey.Q)) break;
                }
            }

            log.Info("WebServer shutdown");
        }

        private object Index(IHttpContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<br />");
            foreach (var handler in handlers)
            {
                if (handler.IsEnabled) sb.AppendLine($"<p><a href=\"" + handler.URL + "\">" + handler.NameWithSpaces + "</a></p>");
            }

            return External.WebServer.HtmlMessage("Utilities", sb.ToString());
        }

        private object GenerateRandom(IHttpContext context)
        {
            var sb = new StringBuilder();
            using (var srandom = RandomNumberGenerator.Create())
            {
                var length = context.GetParameterInt("length", 100);
                var chars = context.GetParameterString("chars", Constant.CHARS_A_Z_LOWER + Constant.CHARS_0_9);

                for (int i = 0; i < length; i++)
                {
                    var c = chars[srandom.Next(chars.Length)];
                    sb.Append(c);
                }

            }
            return sb.ToString();
        }

        private object GenerateRandomFile(IHttpContext context)
        {
            var randomString = (string)GenerateRandom(context);
            context.AddHeader("Content-Disposition", "attachment", "filename=\"random.txt\"");
            var bytes = Constant.ENCODING_UTF8_WITHOUT_BOM.GetBytes(randomString);
            using (var stream = context.OpenResponseStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }
            return "Generated Random File";
        }
    }
}
