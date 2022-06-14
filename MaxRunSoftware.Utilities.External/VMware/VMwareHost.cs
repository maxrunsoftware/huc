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
using Newtonsoft.Json.Linq;

namespace MaxRunSoftware.Utilities.External;

public class VMwareHost : VMwareObject
{
    public enum HostPowerState
    {
        Unknown,
        PoweredOn,
        PoweredOff,
        Standby
    }

    public enum HostConnectionState
    {
        Unknown,
        Connected,
        Disconnected,
        NotResponding
    }

    public string Name { get; }
    public string Host { get; }
    public HostConnectionState ConnectionState { get; }
    public HostPowerState PowerState { get; }

    // ReSharper disable once UnusedParameter.Local
    public VMwareHost(VMwareClient vmware, JToken obj)
    {
        Name = obj.ToString("name");
        Host = obj.ToString("host");

        var connectionState = obj.ToString("connection_state");
        if (connectionState == null)
        {
            ConnectionState = HostConnectionState.Unknown;
        }
        else if (connectionState.EqualsCaseInsensitive("CONNECTED"))
        {
            ConnectionState = HostConnectionState.Connected;
        }
        else if (connectionState.EqualsCaseInsensitive("DISCONNECTED"))
        {
            ConnectionState = HostConnectionState.Disconnected;
        }
        else if (connectionState.EqualsCaseInsensitive("NOT_RESPONDING"))
        {
            ConnectionState = HostConnectionState.NotResponding;
        }

        var powerState = obj.ToString("power_state");
        if (powerState == null)
        {
            PowerState = HostPowerState.Unknown;
        }
        else if (powerState.EqualsCaseInsensitive("POWERED_OFF"))
        {
            PowerState = HostPowerState.PoweredOff;
        }
        else if (powerState.EqualsCaseInsensitive("POWERED_ON"))
        {
            PowerState = HostPowerState.PoweredOn;
        }
        else if (powerState.EqualsCaseInsensitive("STANDBY"))
        {
            PowerState = HostPowerState.Standby;
        }
    }

    public static IEnumerable<VMwareHost> Query(VMwareClient vmware)
    {
        foreach (var obj in vmware.GetValueArray("/rest/vcenter/host"))
        {
            yield return new VMwareHost(vmware, obj);
        }
    }
}
