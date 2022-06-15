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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class VMwareList : VMwareBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Lists the objects in a VMware VCenter");
        help.AddValue("<object type 1> <object type 2> <etc>");
        help.AddDetail("ObjectTypes:");
        foreach (var name in ObjectTypeNames) help.AddDetail("  " + name);

        help.AddExample(HelpExamplePrefix + " " + nameof(VMwareDatacenter));
        help.AddExample(HelpExamplePrefix + " " + nameof(VMwareFolder) + " " + nameof(VMwareNetwork));
    }

    private IReadOnlyList<string> ObjectTypeNames => handlerFunctions.Keys
        .OrderBy(o => o, StringComparer.OrdinalIgnoreCase)
        .Select(o => o.Substring("VMware".Length))
        .ToList();

    private readonly Dictionary<string, Func<VMwareClient, IEnumerable>> handlerFunctions = new()
    {
        { nameof(VMwareDatacenter), VMwareDatacenter.Query },
        { nameof(VMwareDatastore), VMwareDatastore.Query },
        { nameof(VMwareFolder), VMwareFolder.Query },
        { nameof(VMwareHost), VMwareHost.Query },
        { nameof(VMwareNetwork), VMwareNetwork.Query },
        { nameof(VMwareResourcePool), VMwareResourcePool.Query },
        { nameof(VMwareStoragePolicy), VMwareStoragePolicy.Query },
        { nameof(External.VMwareVM), External.VMwareVM.Query },
        { "VMwareVM_Quick", VMwareVMSlim.Query },
        { "VMwareVM_WithoutTools", VMwareVMSlim.QueryWithoutToolsInstalled },
        { "VMwareVM_PoweredOff", VMwareVMSlim.QueryPoweredOff },
        { "VMwareVM_PoweredOn", VMwareVMSlim.QueryPoweredOn },
        { "VMwareVM_Suspended", VMwareVMSlim.QuerySuspended },
        { "VMwareVM_25%Free", VMwareVMSlim.QueryDiskspace25 },
        { "VMwareVM_10%Free", VMwareVMSlim.QueryDiskspace10 },
        { "VMwareVM_IsoAttached", VMwareVMSlim.QueryIsoAttached }
    };

    protected override void ExecuteInternal(VMwareClient vmware)
    {
        var objectTypes = GetArgValuesTrimmed();
        if (objectTypes.IsEmpty()) throw ArgsException.ValueNotSpecified(nameof(objectTypes));

        var h = new HashSet<string>(ObjectTypeNames, StringComparer.OrdinalIgnoreCase);
        foreach (var objectType in objectTypes)
        {
            if (!h.Contains(objectType)) { throw new ArgsException(nameof(objectType), $"Invalid <{nameof(objectType)}> specified: {objectType}"); }
        }

        foreach (var objectType in objectTypes)
        {
            var func = GetFunc(objectType);
            if (func == null) throw new NotImplementedException("Object type " + objectType + " has not been implemented");

            foreach (var obj in func(vmware)) log.Info(obj.ToString());

            log.Info("");
        }
    }

    private Func<VMwareClient, IEnumerable> GetFunc(string objectType)
    {
        objectType = "VMware" + objectType;
        foreach (var kvp in handlerFunctions)
        {
            if (objectType.EqualsCaseInsensitive(kvp.Key)) { return kvp.Value; }
        }

        return null;
    }
}
