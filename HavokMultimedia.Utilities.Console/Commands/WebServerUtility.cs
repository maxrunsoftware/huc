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

namespace HavokMultimedia.Utilities.Console.Commands
{
    [HideCommand]
    public class WebServerUtility : WebServerBase
    {
        public class Handler
        {
            public string Name => HandlerType.NameFormatted().Substring(nameof(WebServerUtility).Length);
            public string NameWithSpaces => Name.SplitOnCamelCase().Select(o => Upper(o)).ToStringDelimited(" ");
            public bool IsEnabled { get; set; } = false;
            public Func<IHttpContext, object> HandlerMethod { get; }
            public HttpVerbs HttpVerbs { get; }
            public string URL => "/" + Upper(Name);
            public string HelpP1 => Lower(Name) + "Disable";
            public string HelpP2 => Name.SplitOnCamelCase().Select(o => Lower(o[0].ToString())).ToStringDelimited("") + "d";
            public string HelpDescription => $"Disables the {URL} utility";
            private Type HandlerType { get; set; }
            private static string Upper(string str) => str.First().ToString().ToUpper() + str.Substring(1);
            private static string Lower(string str) => str.First().ToString().ToLower() + str.Substring(1);
            public Handler(Type type)
            {
                HandlerType = type;
                var o = (WebServerUtilityBase)Util.CreateInstance(type);
                HandlerMethod = o.Handle;
                HttpVerbs = o.Verbs;
            }

            public static List<Type> HandlerTypes => typeof(WebServerUtilityBase).Assembly
            .GetTypesOf<WebServerUtilityBase>(requireNoArgConstructor: true)
            .OrderBy(o => o.FullNameFormatted())
            .ToList();

            public static List<Handler> Handlers => HandlerTypes
            .Select(o => new Handler(o))
            .ToList();

        }

        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);

            help.AddSummary("Creates a web server that provides various utilities");
            foreach (var handler in Handler.Handlers) help.AddParameter(handler.HelpP1, handler.HelpP2, handler.HelpDescription);
            help.AddExample("");
        }

        private IList<Handler> handlers;
        protected override void ExecuteInternal()
        {
            base.ExecuteInternal();
            var config = GetConfig();

            foreach (var handlerType in Handler.HandlerTypes) log.Debug("Found Handler: " + handlerType.FullNameFormatted());

            handlers = Handler.Handlers;

            foreach (var handler in handlers)
            {
                handler.IsEnabled = !GetArgParameterOrConfigBool(handler.HelpP1, handler.HelpP2, false);
            }

            foreach (var handler in handlers)
            {
                if (handler.IsEnabled) config.AddPathHandler(handler.URL, handler.HttpVerbs, handler.HandlerMethod);
            }

            config.AddPathHandler("/", HttpVerbs.Get, Index);

            LoopUntilKey(config);
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




    }
}
