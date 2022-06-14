// Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)
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

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace MaxRunSoftware.Utilities.External;

public class VMwareStoragePolicy : VMwareObject
{
    public class Disk : VMwareObject
    {
        public string Key { get; }
        public string VmHome { get; }
        public IReadOnlyList<string> Disks { get; }

        public Disk(JToken obj)
        {
            Key = obj.ToString("key");
            VmHome = obj.ToString("value", "vm_home");
            Disks = obj["value"]?["disks"].OrEmpty().Select(o => o?.ToString()).WhereNotNull().ToList();
        }
    }

    public string Name { get; }
    public string Description { get; }
    public string Policy { get; }
    public IReadOnlyList<Disk> Disks { get; }

    public VMwareStoragePolicy(VMwareClient vmware, JToken obj)
    {
        Name = obj.ToString("name");
        Description = obj.ToString("description");
        Policy = obj.ToString("policy");

        Disks = vmware.GetValueArray($"/rest/vcenter/storage/policies/{Policy}/vm").OrEmpty().Select(o => new Disk(o)).ToList();
    }

    public static IEnumerable<VMwareStoragePolicy> Query(VMwareClient vmware)
    {
        foreach (var obj in vmware.GetValueArray("/rest/vcenter/storage/policies")) yield return new VMwareStoragePolicy(vmware, obj);
    }
}
