/*
Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)

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

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MaxRunSoftware.Utilities.External;

public class VMwareDatacenter : VMwareObject
{
    public string Name { get; }
    public string Datacenter { get; }
    public string DatastoreFolder { get; }
    public string HostFolder { get; }
    public string NetworkFolder { get; }
    public string VMFolder { get; }

    public VMwareDatacenter(VMwareClient vmware, JToken obj)
    {
        Name = obj.ToString("name");
        Datacenter = obj.ToString("datacenter");

        obj = QueryValueObjectSafe(vmware, "/rest/vcenter/datacenter/" + Datacenter);
        if (obj != null)
        {
            DatastoreFolder = obj.ToString("datastore_folder");
            HostFolder = obj.ToString("host_folder");
            NetworkFolder = obj.ToString("network_folder");
            VMFolder = obj.ToString("vm_folder");
        }
    }

    public static IEnumerable<VMwareDatacenter> Query(VMwareClient vmware)
    {
        foreach (var obj in vmware.GetValueArray("/rest/vcenter/datacenter"))
        {
            yield return new VMwareDatacenter(vmware, obj);
        }
    }
}