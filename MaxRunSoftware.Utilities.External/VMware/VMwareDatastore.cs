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

namespace MaxRunSoftware.Utilities.External
{
    public class VMwareDatastore : VMwareObject
    {
        public string Name { get; }
        public string Datastore { get; }
        public string Type { get; }
        public long? FreeSpace { get; }
        public long? Capacity { get; }
        public long? Used => FreeSpace == null || Capacity == null ? null : Capacity.Value - FreeSpace.Value;
        public byte? PercentFree => FreeSpace == null || Capacity == null ? null : (byte)((double)FreeSpace.Value / (double)Capacity.Value * (double)100);
        public byte? PercentUsed => PercentFree == null ? null : (byte)((double)100 - (double)PercentFree.Value);

        public bool? Accessible { get; }
        public bool? MultipleHostAccess { get; }
        public bool? ThinProvisioningSupported { get; }

        public VMwareDatastore(VMware vmware, JToken obj)
        {
            Name = obj.ToString("name");
            Datastore = obj.ToString("datastore");
            Type = obj.ToString("type");
            FreeSpace = obj.ToLong("free_space");
            Capacity = obj.ToLong("capacity");

            obj = QueryValueObjectSafe(vmware, "/rest/vcenter/datastore/" + Datastore);
            if (obj != null)
            {
                Accessible = obj.ToBool("accessible");
                MultipleHostAccess = obj.ToBool("multiple_host_access");
                ThinProvisioningSupported = obj.ToBool("thin_provisioning_supported");
            }
        }

        public static IEnumerable<VMwareDatastore> Query(VMware vmware)
        {
            foreach (var obj in vmware.GetValueArray("/rest/vcenter/datastore"))
            {
                yield return new VMwareDatastore(vmware, obj);
            }
        }
    }

}
