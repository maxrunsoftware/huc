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
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace HavokMultimedia.Utilities.Console.External
{
    public class VMware : IDisposable
    {
        private static readonly ILogger log = Program.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string hostname;
        private readonly string username;
        private readonly string password;
        private readonly HttpClient client;
        private string authToken;

        public IEnumerable<VMwareDatacenter> Datacenters => VMwareDatacenter.Query(this);
        public IEnumerable<VMwareDatastore> Datastores => VMwareDatastore.Query(this);

        public VMware(string hostname, string username, string password)
        {
            this.hostname = hostname.CheckNotNullTrimmed(nameof(hostname));
            this.username = username.CheckNotNullTrimmed(nameof(username));
            this.password = password.CheckNotNullTrimmed(nameof(password));

            var clientHandler = new HttpClientHandler();
            clientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
            clientHandler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, certChain, policyErrors) => { return true; };
            client = new HttpClient(clientHandler);
            Login();
        }

        private Uri Uri(string path) => new Uri("https://" + hostname + (path.StartsWith("/") ? path : ("/" + path)));

        public dynamic Query(string path, IDictionary<string, string> parameters = null)
        {
            var builder = new UriBuilder(Uri(path));
            builder.Port = -1;
            var query = HttpUtility.ParseQueryString(builder.Query);
            foreach (var kvp in parameters.OrEmpty())
            {
                query[kvp.Key] = kvp.Value;
            }
            builder.Query = query.ToString();
            string url = builder.ToString();

            var message = new HttpRequestMessage(HttpMethod.Get, url);
            message.Headers.Add(HttpRequestHeader.Authorization.ToString(), "Bearer " + authToken);
            message.Headers.Add(HttpRequestHeader.Accept.ToString(), "application/json");

            var result = Send(message);
            return result;
        }

        public IEnumerable<dynamic> QueryEnumerable(string path, IDictionary<string, string> parameters = null)
        {
            foreach (var obj in Query(path, parameters).value)
            {
                yield return obj;
            }
        }

        private void Login()
        {
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
            var message = new HttpRequestMessage(HttpMethod.Post, Uri("rest/com/vmware/cis/session"));
            message.Headers.Add(HttpRequestHeader.Authorization.ToString(), "Basic " + auth);
            message.Headers.Add(HttpRequestHeader.Accept.ToString(), "application/json");

            var result = Send(message);
            if (VMwareObject.HasValue(result, "type"))
            {
                string type = result.type;
                if (type.EndsWith("unauthenticated", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Invalid host, username, or password");
                }
            }
            authToken = result.value;
            log.Debug("Login: " + authToken);
        }

        private void Logout()
        {
            if (authToken == null) return;
            var message = new HttpRequestMessage(HttpMethod.Delete, Uri("rest/com/vmware/cis/session"));
            message.Headers.Add(HttpRequestHeader.Authorization.ToString(), "Bearer " + authToken);
            message.Headers.Add(HttpRequestHeader.Accept.ToString(), "application/json");

            var result = Send(message);
            var val = result;
            log.Debug("Logout: " + ToJson(val));
            authToken = null;
        }

        public static string ToJson(dynamic obj, bool formatted = true) => JsonConvert.SerializeObject(obj, formatted ? Formatting.Indented : Formatting.None);

        private dynamic Send(HttpRequestMessage message)
        {

            var response = client.SendAsync(message)?.Result?.Content?.ReadAsStringAsync()?.Result;
            if (response == null) return null;

            var obj = JsonConvert.DeserializeObject(response);
            log.Debug(message.RequestUri.ToString());
            log.Debug(ToJson(obj));
            return obj;
        }

        public void Dispose()
        {
            try
            {
                Logout();
            }
            catch (Exception e)
            {
                log.Warn("Failed to logout", e);
            }


            if (client != null)
            {
                try
                {
                    client.Dispose();
                }
                catch (Exception e)
                {
                    log.Warn("Failed to dispose of " + client.GetType().FullNameFormatted(), e);
                }
            }
        }
    }
}
