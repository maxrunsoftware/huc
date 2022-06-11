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

using System;
using System.Text;
using Newtonsoft.Json.Linq;

namespace MaxRunSoftware.Utilities.External;

public static class VMwareExtensions
{
    public static string ToString(this JToken token, params string[] keys)
    {
        if (token == null) return null;
        foreach (var key in keys)
        {
            token = token[key];
            if (token == null) return null;
        }
        return token.ToString().TrimOrNull();
    }
    public static bool ToBool(this JToken token, bool ifNull, params string[] keys)
    {
        return ToBool(token, keys) ?? ifNull;
    }
    public static bool? ToBool(this JToken token, params string[] keys)
    {
        var val = ToString(token, keys);
        if (val == null) return null;
        return val.ToBool();
    }
    public static uint ToUInt(this JToken token, uint ifNull, params string[] keys)
    {
        return ToUInt(token, keys) ?? ifNull;
    }
    public static uint? ToUInt(this JToken token, params string[] keys)
    {
        var val = ToString(token, keys);
        if (val == null) return null;
        return val.ToUInt();
    }
    public static int ToInt(this JToken token, int ifNull, params string[] keys)
    {
        return ToInt(token, keys) ?? ifNull;
    }
    public static int? ToInt(this JToken token, params string[] keys)
    {
        var val = ToString(token, keys);
        if (val == null) return null;
        return val.ToInt();
    }
    public static Guid ToGuid(this JToken token, Guid ifNull, params string[] keys)
    {
        return ToGuid(token, keys) ?? ifNull;
    }
    public static Guid? ToGuid(this JToken token, params string[] keys)
    {
        var val = ToString(token, keys);
        if (val == null) return null;
        var sb = new StringBuilder();
        for (int i = 0; i < val.Length; i++)
        {
            if (val[i].In("0123456789abcdef".ToCharArray())) sb.Append(val[i]);
        }
        return sb.ToString().ToGuid();
    }
    public static long ToLong(this JToken token, long ifNull, params string[] keys)
    {
        return ToLong(token, keys) ?? ifNull;
    }
    public static long? ToLong(this JToken token, params string[] keys)
    {
        var val = ToString(token, keys);
        if (val == null) return null;
        return val.ToLong();
    }
    public static VMwareVM.VmHardwareConnectionState ToConnectionState(this JToken token, params string[] keys)
    {
        var s = token.ToString(keys);
        if (s == null) return VMwareVM.VmHardwareConnectionState.Unknown;
        else if (s.EqualsCaseInsensitive("CONNECTED")) return VMwareVM.VmHardwareConnectionState.Connected;
        else if (s.EqualsCaseInsensitive("RECOVERABLE_ERROR")) return VMwareVM.VmHardwareConnectionState.RecoverableError;
        else if (s.EqualsCaseInsensitive("UNRECOVERABLE_ERROR")) return VMwareVM.VmHardwareConnectionState.UnrecoverableError;
        else if (s.EqualsCaseInsensitive("NOT_CONNECTED")) return VMwareVM.VmHardwareConnectionState.NotConnected;
        else if (s.EqualsCaseInsensitive("UNKNOWN")) return VMwareVM.VmHardwareConnectionState.Unknown;
        else return VMwareVM.VmHardwareConnectionState.Unknown;
    }

    public static VMwareVM.VMPowerState ToPowerState(this JToken token, params string[] keys)
    {
        var s = token.ToString(keys);
        if (s == null) return VMwareVM.VMPowerState.Unknown;
        if (s.EqualsCaseInsensitive("POWERED_OFF")) return VMwareVM.VMPowerState.PoweredOff;
        else if (s.EqualsCaseInsensitive("POWERED_ON")) return VMwareVM.VMPowerState.PoweredOn;
        else if (s.EqualsCaseInsensitive("SUSPENDED")) return VMwareVM.VMPowerState.Suspended;
        else return VMwareVM.VMPowerState.Unknown;
    }
}