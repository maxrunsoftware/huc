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
using System.Linq;
using System.Text;
using EmbedIO;

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

        public void AddPathHandler(string path, HttpVerbs httpVerbs, Func<IHttpContext, object> handler) => PathHandlers.Add(path, (httpVerbs, handler));

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetType().NameFormatted());

            if (Hostnames.IsEmpty()) sb.AppendLine("  " + nameof(Hostnames) + ": <empty>");
            for (int i = 0; i < Hostnames.Count; i++) sb.AppendLine("  " + nameof(Hostnames) + "[" + i + "]: " + Hostnames[i]);

            sb.AppendLine("  " + nameof(Port) + ": " + Port);
            sb.AppendLine("  " + nameof(DirectoryToServe) + ": " + DirectoryToServe);
            sb.AppendLine("  " + nameof(DirectoryToServeUrlPath) + ": " + DirectoryToServeUrlPath);

            var pathHandlers = PathHandlers.ToList();
            if (pathHandlers.IsEmpty()) sb.AppendLine("  " + nameof(PathHandlers) + ": <empty>");
            for (int i = 0; i < pathHandlers.Count; i++) sb.AppendLine("  " + nameof(PathHandlers) + "[" + i + "]: " + pathHandlers[i].Key + "  (" + pathHandlers[i].Value.verbs + ")");

            var users = Users.OrderBy(o => o.username, StringComparer.OrdinalIgnoreCase).ToList();
            if (users.IsEmpty()) sb.AppendLine("  " + nameof(Users) + ": <empty>");
            for (int i = 0; i < users.Count; i++) sb.AppendLine("  " + nameof(Users) + "[" + users[i].username + "]: " + users[i].password);

            var urlPrefixes = UrlPrefixes.ToList();
            if (urlPrefixes.IsEmpty()) sb.AppendLine("  " + nameof(UrlPrefixes) + ": <empty>");
            for (int i = 0; i < urlPrefixes.Count; i++) sb.AppendLine("  " + nameof(UrlPrefixes) + "[" + i + "]: " + urlPrefixes[i]);

            return sb.ToString();
        }
    }
}
