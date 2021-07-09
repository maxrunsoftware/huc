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
using Newtonsoft.Json.Linq;

namespace MaxRunSoftware.Utilities.External
{
    public class VMware : IDisposable
    {
        // https://developer.vmware.com/docs/vsphere-automation/v7.0U1/

        private static readonly ILogger log = Logging.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string hostname;
        private readonly string username;
        private readonly string password;
        private readonly HttpClient client;
        private string authToken;

        public IEnumerable<VMwareDatacenter> Datacenters => VMwareDatacenter.Query(this);
        public IEnumerable<VMwareDatastore> Datastores => VMwareDatastore.Query(this);
        public IEnumerable<VMwareVM> VMs => VMwareVM.Query(this);
        public IEnumerable<VMwareFolder> Folders => VMwareFolder.Query(this);
        public IEnumerable<VMwareHost> Hosts => VMwareHost.Query(this);
        public IEnumerable<VMwareNetwork> Network => VMwareNetwork.Query(this);
        public IEnumerable<VMwareResourcePool> ResourcePools => VMwareResourcePool.Query(this);
        public IEnumerable<VMwareStoragePolicy> StoragePolicies => VMwareStoragePolicy.Query(this);
        [JsonIgnore] public IEnumerable<VMwareVMSlim> VMsSlim => VMwareVMSlim.Query(this);

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

        private string Action(string path, HttpMethod method, IDictionary<string, string> parameters = null, IDictionary<string, string> contentParameters = null, string contentJson = null)
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

            var message = new HttpRequestMessage(method, url);
            message.Headers.Add(HttpRequestHeader.Authorization.ToString(), "Bearer " + authToken);
            message.Headers.Add(HttpRequestHeader.Accept.ToString(), "application/json");
            if (contentParameters != null && contentParameters.Count > 0) message.Content = new FormUrlEncodedContent(contentParameters);
            if (contentJson != null) message.Content = new StringContent(contentJson, Encoding.UTF8, "application/json");
            var result = Send(message);
            return result;
        }

        public string Patch(string path, IDictionary<string, string> parameters = null, IDictionary<string, string> contentParameters = null, string contentJson = null) => Action(path, HttpMethod.Patch, parameters, contentParameters, contentJson);
        public string Post(string path, IDictionary<string, string> parameters = null, IDictionary<string, string> contentParameters = null, string contentJson = null) => Action(path, HttpMethod.Post, parameters, contentParameters, contentJson);
        public string Post(string path, string parameterName, string parameterValue) => Action(path, HttpMethod.Post, new Dictionary<string, string> { { parameterName, parameterValue } });
        public string Get(string path, IDictionary<string, string> parameters = null) => Action(path, HttpMethod.Get, parameters);

        public JToken GetValue(string path, IDictionary<string, string> parameters = null)
        {
            var json = Get(path, parameters);
            var obj = JObject.Parse(json);
            var type = obj["type"]?.ToString();
            if (type != null)
            {
                if (type.EndsWith("service_unavailable", StringComparison.OrdinalIgnoreCase))
                {
                    log.Debug("Service Unavailable: " + path);

                    try
                    {
                        foreach (var message in obj["value"]?["messages"].OrEmpty())
                        {
                            var defaultMessage = message["default_message"]?.ToString().TrimOrNull();
                            var id = message["id"]?.ToString().TrimOrNull();
                            if (id != null && message != null) log.Debug(id + "  -->  " + defaultMessage);
                            else if (id != null) log.Debug(id);
                            else if (defaultMessage != null) log.Debug(defaultMessage);
                        }
                    }
                    catch (Exception e)
                    {
                        log.Debug("Error trying to parse error message", e);
                    }
                    return null;
                }
            }
            if (!obj.ContainsKey("value")) return null;
            return obj["value"];
        }

        public IEnumerable<JToken> GetValueArray(string path, IDictionary<string, string> parameters = null)
        {
            var obj = GetValue(path, parameters);
            foreach (var o in obj.OrEmpty())
            {
                yield return o;
            }
        }

        private void Login()
        {
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
            var message = new HttpRequestMessage(HttpMethod.Post, Uri("rest/com/vmware/cis/session"));
            message.Headers.Add(HttpRequestHeader.Authorization.ToString(), "Basic " + auth);
            message.Headers.Add(HttpRequestHeader.Accept.ToString(), "application/json");

            var result = Send(message);
            var obj = JObject.Parse(result);
            if (obj.ContainsKey("type"))
            {
                string type = obj["type"].ToString();
                if (type.EndsWith("unauthenticated", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Invalid host, username, or password");
                }
            }
            authToken = obj["value"].ToString();
            log.Debug("Login: " + authToken);
        }

        private void Logout()
        {
            if (authToken == null) return;
            var message = new HttpRequestMessage(HttpMethod.Delete, Uri("rest/com/vmware/cis/session"));
            message.Headers.Add(HttpRequestHeader.Authorization.ToString(), "Bearer " + authToken);
            message.Headers.Add(HttpRequestHeader.Accept.ToString(), "application/json");

            var result = Send(message);
            log.Debug("Logout: " + result);
            authToken = null;
        }

        public static string FormatJson(string json, bool formatted = true) => JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json), formatted ? Formatting.Indented : Formatting.None);

        private string Send(HttpRequestMessage message)
        {
            var response = client.SendAsync(message)?.Result?.Content?.ReadAsStringAsync()?.Result;
            if (response == null) return null;

            log.Debug(message.RequestUri.ToString());
            log.Debug(FormatJson(response));
            return response;
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
