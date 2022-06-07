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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace MaxRunSoftware.Utilities.External
{
    public class VMwareVMSlim : VMwareObject
    {
        public void Shutdown(VMwareClient vmware) => vmware.Post($"/rest/vcenter/vm/{VM}/guest/power", "action", "shutdown");
        public void Reboot(VMwareClient vmware) => vmware.Post($"/rest/vcenter/vm/{VM}/guest/power", "action", "reboot");
        public void Standby(VMwareClient vmware) => vmware.Post($"/rest/vcenter/vm/{VM}/guest/power", "action", "standby");

        public void Reset(VMwareClient vmware) => vmware.Post($"/rest/vcenter/vm/{VM}/power/reset");
        public void Start(VMwareClient vmware) => vmware.Post($"/rest/vcenter/vm/{VM}/power/start");
        public void Stop(VMwareClient vmware) => vmware.Post($"/rest/vcenter/vm/{VM}/power/stop");
        public void Suspend(VMwareClient vmware) => vmware.Post($"/rest/vcenter/vm/{VM}/power/suspend");

        public void CDRomDisconnect(VMwareClient vmware, string key) => vmware.Post($"/rest/vcenter/vm/{VM}/hardware/cdrom/{key}/disconnect");
        public void CDRomDelete(VMwareClient vmware, string key)
        {
            /*
              {"cdrom":"1000"}
            */
            string json;
            using (var w = new JsonWriter())
            {
                using (w.Object())
                {
                    w.Property("cdrom", key);
                }
                json = w.ToString();
            }

            vmware.Post($"/rest/com/vmware/vcenter/iso/image/id:{VM}?~action=unmount", contentJson: json);
        }

        public void CDRomUpdate_ClientDevice(VMwareClient vmware, string key)
        {
            /*
            {
                "spec": {
                    "allow_guest_control": false,
                    "backing": {
                        "device_access_type": "enum",
                        "host_device": "string",
                        "iso_file": "string",
                        "type": "enum"
                    },
                    "start_connected": false
                }
            }
            */
            string json;
            using (var w = new JsonWriter())
            {
                using (w.Object())
                {
                    using (w.Object("spec"))
                    {
                        w.Property("allow_guest_control", true);
                        w.Property("start_connected", false);
                        using (w.Object("backing"))
                        {
                            w.Property("device_access_type", "EMULATION");
                            w.Property("type", "CLIENT_DEVICE");
                        }
                    }
                }
                json = w.ToString();
            }

            vmware.Patch($"/rest/vcenter/vm/{VM}/hardware/cdrom/{key}", contentJson: json);
        }

        public void DetachISOs(VMwareClient vmware)
        {
            var vmfull = VMwareVM.QueryByVM(vmware, VM);
            if (vmfull == null) throw new Exception("Could not find VM: " + VM + "  " + Name); // should not happen
            foreach (var cdrom in vmfull.CDRoms)
            {
                if (!cdrom.BackingType.StartsWith("ISO", StringComparison.OrdinalIgnoreCase)) continue;
                if (cdrom.State == VMwareVM.VmHardwareConnectionState.Connected)
                {
                    CDRomDisconnect(vmware, cdrom.Key);
                }
                CDRomUpdate_ClientDevice(vmware, cdrom.Key);
            }
        }

        public string VM { get; }
        public string Name { get; }
        public long? MemorySizeMB { get; }
        public int? CpuCount { get; }
        public VMwareVM.VMPowerState PowerState { get; }

        public VMwareVMSlim(VMwareClient vmware, JToken obj)
        {
            VM = obj["vm"]?.ToString();
            Name = obj["name"]?.ToString();
            MemorySizeMB = obj.ToLong("memory_size_MiB");
            CpuCount = obj.ToInt("cpu_count");
            PowerState = obj.ToPowerState("power_state");
        }

        public static IEnumerable<VMwareVMSlim> Query(VMwareClient vmware)
        {
            return vmware.GetValueArray("/rest/vcenter/vm")
                .Select(o => new VMwareVMSlim(vmware, o))
                .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase);
        }

        public static IEnumerable<VMwareVMSlim> QueryWithoutToolsInstalled(VMwareClient vmware)
        {
            var slims = Query(vmware);
            var d = new Dictionary<string, VMwareVMSlim>(StringComparer.OrdinalIgnoreCase);
            foreach (var slim in slims) d[slim.VM] = slim;

            foreach (var full in VMwareVM.Query(vmware))
            {
                if (!full.IsVMwareToolsInstalled) yield return d[full.VM];
            }
        }

        private class SlimDiskSpace : VMwareVMSlim
        {
            public IReadOnlyList<VMwareVM.GuestLocalFilesystem> Filesystems { get; set; }
            public int PercentFreeThreshhold { get; set; }
            public SlimDiskSpace(VMwareClient vmware, JToken obj) : base(vmware, obj) { }
            public IEnumerable<VMwareVM.GuestLocalFilesystem> FileSystemsCrossingThreshold => Filesystems.Where(o => o.PercentFree != null && o.PercentFree.Value <= PercentFreeThreshhold).OrderBy(o => o.Key);
            public override string ToString() => base.ToString() + "  " + FileSystemsCrossingThreshold.Select(o => "(" + o.PercentFree.Value + "% free) " + o.Key).ToStringDelimited("    ");
        }
        public static IEnumerable<VMwareVMSlim> QueryDiskspace10(VMwareClient vmware) => QueryDiskspace(vmware, 10);
        public static IEnumerable<VMwareVMSlim> QueryDiskspace25(VMwareClient vmware) => QueryDiskspace(vmware, 25);
        public static IEnumerable<VMwareVMSlim> QueryDiskspace(VMwareClient vmware, int percentFreeThreshhold)
        {
            var dsobjs = vmware.GetValueArray("/rest/vcenter/vm")
                .Select(o => new SlimDiskSpace(vmware, o))
                .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase);
            var d = new Dictionary<string, SlimDiskSpace>(StringComparer.OrdinalIgnoreCase);
            foreach (var dsobj in dsobjs) d[dsobj.VM] = dsobj;


            var fullvms = VMwareVM.Query(vmware);

            foreach (var fullvm in fullvms)
            {
                var dsobj = d[fullvm.VM];
                dsobj.Filesystems = fullvm.GuestLocalFilesystems;
                dsobj.PercentFreeThreshhold = percentFreeThreshhold;
                if (!dsobj.FileSystemsCrossingThreshold.IsEmpty()) yield return dsobj;
            }
        }

        private class SlimIsoFile : VMwareVMSlim
        {
            public IReadOnlyList<VMwareVM.CDROM> CDRoms { get; set; }
            public int PercentFreeThreshhold { get; set; }
            public SlimIsoFile(VMwareClient vmware, JToken obj) : base(vmware, obj) { }
            public IEnumerable<string> IsosAttached
            {
                get
                {
                    var list = new List<string>();

                    foreach (var cdrom in CDRoms)
                    {
                        if (!cdrom.BackingType.StartsWith("ISO", StringComparison.OrdinalIgnoreCase)) continue;
                        var filename = cdrom.BackingIsoFileName;
                        if (filename == null) continue;
                        if (cdrom.State == VMwareVM.VmHardwareConnectionState.Connected) filename = filename + " (" + cdrom.State + ")";
                        list.Add(filename);
                    }

                    return list;
                }
            }

            public override string ToString() => base.ToString() + "  " + IsosAttached.ToStringDelimited("    ");
        }
        public static IEnumerable<VMwareVMSlim> QueryIsoAttached(VMwareClient vmware)
        {
            var dsobjs = vmware.GetValueArray("/rest/vcenter/vm")
                .Select(o => new SlimIsoFile(vmware, o))
                .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase);
            var d = new Dictionary<string, SlimIsoFile>(StringComparer.OrdinalIgnoreCase);
            foreach (var dsobj in dsobjs) d[dsobj.VM] = dsobj;

            var fullvms = VMwareVM.Query(vmware);

            foreach (var fullvm in fullvms)
            {
                var dsobj = d[fullvm.VM];
                dsobj.CDRoms = fullvm.CDRoms;
                if (!dsobj.IsosAttached.IsEmpty()) yield return dsobj;
            }
        }

        public static IEnumerable<VMwareVMSlim> QueryPoweredOff(VMwareClient vmware) => Query(vmware).Where(o => o.PowerState != VMwareVM.VMPowerState.PoweredOn);
        public static IEnumerable<VMwareVMSlim> QueryPoweredOn(VMwareClient vmware) => Query(vmware).Where(o => o.PowerState == VMwareVM.VMPowerState.PoweredOn);
        public static IEnumerable<VMwareVMSlim> QuerySuspended(VMwareClient vmware) => Query(vmware).Where(o => o.PowerState == VMwareVM.VMPowerState.Suspended);


        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(VM.PadRight(10));
            sb.Append(" ");
            if (PowerState == VMwareVM.VMPowerState.PoweredOn) sb.Append("         ");
            else if (PowerState == VMwareVM.VMPowerState.PoweredOff) sb.Append("   OFF   ");
            else if (PowerState == VMwareVM.VMPowerState.Suspended) sb.Append("SUSPENDED");
            else sb.Append(" UNKNOWN ");
            sb.Append("  " + Name);
            return sb.ToString();
        }
    }

}
