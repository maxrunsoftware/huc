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
    public abstract class VMwareObject
    {

    }

    public class VMwareVM : VMwareObject
    {
        public string VM { get; }
        public string Name { get; }
        public string MemorySizeMB { get; }
        public string CpuCount { get; }
        public string PowerState { get; }
        public VMwareVM(VMware vmware, dynamic obj)
        {
            VM = obj.vm;
            Name = obj.name;
            MemorySizeMB = obj.memory_size_MiB;
            CpuCount = obj.cpu_count;
            PowerState = obj.power_state;
        }

        public static IEnumerable<VMwareVM> Query(VMware vmware)
        {
            foreach (var obj in vmware.QueryEnumerable("/rest/vcenter/vm"))
            {
                yield return new VMwareVM(vmware, obj);
            }
        }
    }

    public class VMwareDatacenter : VMwareObject
    {
        public string Name { get; }
        public string Datacenter { get; }

        public string DatastoreFolder { get; }
        public string HostFolder { get; }
        public string NetworkFolder { get; }
        public string VMFolder { get; }

        public VMwareDatacenter(VMware vmware, dynamic obj)
        {
            Name = obj.name;
            Datacenter = obj.datacenter;

            obj = vmware.Query("/rest/vcenter/datacenter/" + Datacenter).value;
            DatastoreFolder = obj.datastore_folder;
            HostFolder = obj.host_folder;
            NetworkFolder = obj.network_folder;
            VMFolder = obj.vm_folder;
        }

        public static IEnumerable<VMwareDatacenter> Query(VMware vmware)
        {
            foreach (var obj in vmware.QueryEnumerable("/rest/vcenter/datacenter"))
            {
                yield return new VMwareDatacenter(vmware, obj);
            }
        }
    }

    public class VMwareDatastore : VMwareObject
    {
        public string Name { get; }
        public string Datastore { get; }
        public string Type { get; }
        public string FreeSpace { get; }
        public string Capacity { get; }

        public string Accessible { get; }
        public string MultipleHostAccess { get; }
        public string ThinProvisioningSupported { get; }


        public VMwareDatastore(VMware vmware, dynamic obj)
        {
            Name = obj.name;
            Datastore = obj.datastore;
            Type = obj.type;
            FreeSpace = obj.free_space;
            Capacity = obj.capacity;


            obj = vmware.Query("/rest/vcenter/datastore/" + Datastore).value;
            Accessible = obj.accessible;
            MultipleHostAccess = obj.multiple_host_access;
            ThinProvisioningSupported = obj.thin_provisioning_supported;
        }

        public static IEnumerable<VMwareDatastore> Query(VMware vmware)
        {
            foreach (var obj in vmware.QueryEnumerable("/rest/vcenter/datastore"))
            {
                yield return new VMwareDatastore(vmware, obj);
            }
        }
    }

    public class VMwareFolder : VMwareObject
    {
        public string Name { get; }
        public string Folder { get; }
        public string Type { get; }

        public VMwareFolder(VMware vmware, dynamic obj)
        {
            Name = obj.name;
            Folder = obj.folder;
            Type = obj.type;
        }

        public static IEnumerable<VMwareFolder> Query(VMware vmware)
        {
            foreach (var obj in vmware.QueryEnumerable("/rest/vcenter/folder"))
            {
                yield return new VMwareFolder(vmware, obj);
            }
        }
    }

    public class VMwareHost : VMwareObject
    {
        public string Name { get; }
        public string Host { get; }
        public string ConnectionState { get; }
        public string PowerState { get; }

        public VMwareHost(VMware vmware, dynamic obj)
        {
            Name = obj.name;
            Host = obj.host;
            ConnectionState = obj.connection_state;
            PowerState = obj.power_state;
        }

        public static IEnumerable<VMwareHost> Query(VMware vmware)
        {
            foreach (var obj in vmware.QueryEnumerable("/rest/vcenter/host"))
            {
                yield return new VMwareHost(vmware, obj);
            }
        }
    }

    public class VMwareNetwork : VMwareObject
    {
        public string Name { get; }
        public string Network { get; }
        public string Type { get; }

        public VMwareNetwork(VMware vmware, dynamic obj)
        {
            Name = obj.name;
            Network = obj.network;
            Type = obj.type;
        }

        public static IEnumerable<VMwareNetwork> Query(VMware vmware)
        {
            foreach (var obj in vmware.QueryEnumerable("/rest/vcenter/network"))
            {
                yield return new VMwareNetwork(vmware, obj);
            }
        }
    }

    public class VMwareResourcePool : VMwareObject
    {
        public string Name { get; }
        public string ResourcePool { get; }

        public VMwareResourcePool(VMware vmware, dynamic obj)
        {
            Name = obj.name;
            ResourcePool = obj.resource_pool;
        }

        public static IEnumerable<VMwareResourcePool> Query(VMware vmware)
        {
            foreach (var obj in vmware.QueryEnumerable("/rest/vcenter/resource-pool"))
            {
                yield return new VMwareResourcePool(vmware, obj);
            }
        }
    }

    public class VMware : IDisposable
    {
        private static readonly ILogger log = Program.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string hostname;
        private readonly string username;
        private readonly string password;
        private readonly HttpClient client;
        private string authToken;

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

        public IEnumerable<VMwareVM> VMs => QueryEnumerable("/rest/vcenter/vm").Select(o => new VMwareVM(o));
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
            if (Extensions.IsPropertyExist(result, "type"))
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
