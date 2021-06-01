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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace HavokMultimedia.Utilities.Console.External
{
    public abstract class VMwareObject
    {
        private static readonly ILogger log = Program.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public JToken QueryValueObjectSafe(VMware vmware, string path)
        {
            try
            {
                return vmware.GetValue(path);
            }
            catch (Exception e)
            {
                log.Warn("Error querying " + path, e);
            }
            return null;
        }

        public IEnumerable<JToken> QueryValueArraySafe(VMware vmware, string path)
        {
            try
            {
                return vmware.GetValueArray(path);
            }
            catch (Exception e)
            {
                log.Warn("Error querying " + path, e);
            }
            return Array.Empty<JObject>();
        }

        protected PropertyInfo[] GetProperties()
        {
            var list = new List<PropertyInfo>();
            foreach (var prop in GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (!prop.CanRead) continue;
                list.Add(prop);
            }
            return list.OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetType().NameFormatted());
            foreach (var property in GetProperties())
            {
                var val = Util.GetPropertyValue(this, property.Name);
                if (val == null)
                {
                    sb.AppendLine("  " + property.Name + ": ");
                }
                else if (val is string)
                {
                    sb.AppendLine("  " + property.Name + ": " + val.ToStringGuessFormat());
                }
                else if (val is IEnumerable)
                {
                    int count = 0;
                    foreach (var item in (IEnumerable)val)
                    {
                        var vitem = (VMwareObject)item;
                        sb.AppendLine("  " + vitem.GetType().NameFormatted() + "[" + count + "]");
                        foreach (var prop in vitem.GetProperties())
                        {
                            sb.AppendLine("    " + prop.Name + ": " + Util.GetPropertyValue(vitem, prop.Name).ToStringGuessFormat());
                        }
                        count++;
                    }
                }
                else
                {
                    sb.AppendLine("  " + property.Name + ": " + val.ToStringGuessFormat());
                }
            }

            return sb.ToString();
        }
    }

    public class VMwareVMSlim : VMwareObject
    {
        public void Shutdown(VMware vmware) => vmware.Post($"/rest/vcenter/vm/{VM}/guest/power", "action", "shutdown");
        public void Reboot(VMware vmware) => vmware.Post($"/rest/vcenter/vm/{VM}/guest/power", "action", "reboot");
        public void Standby(VMware vmware) => vmware.Post($"/rest/vcenter/vm/{VM}/guest/power", "action", "standby");

        public void Reset(VMware vmware) => vmware.Post($"/rest/vcenter/vm/{VM}/power/reset");
        public void Start(VMware vmware) => vmware.Post($"/rest/vcenter/vm/{VM}/power/start");
        public void Stop(VMware vmware) => vmware.Post($"/rest/vcenter/vm/{VM}/power/stop");
        public void Suspend(VMware vmware) => vmware.Post($"/rest/vcenter/vm/{VM}/power/suspend");

        public void CDRomDisconnect(VMware vmware, string key) => vmware.Post($"/rest/vcenter/vm/{VM}/hardware/cdrom/{key}/disconnect");
        public void CDRomDelete(VMware vmware, string key)
        {
            /*
              {"cdrom":"1000"}
            */
            string json;
            using (var writer = new JsonWriter())
            {
                using (writer.StartObject())
                {
                    writer.Property("cdrom", key);
                }
                json = writer.ToString();
            }

            vmware.Post($"/rest/com/vmware/vcenter/iso/image/id:{VM}?~action=unmount", contentJson: json);
        }

        public void CDRomUpdateToClientDevice(VMware vmware, string key)
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
            using (var writer = new JsonWriter())
            {
                using (writer.StartObject())
                {
                    using (writer.StartObject("spec"))
                    {
                        writer.Property(("allow_guest_control", "true"), ("start_connected", "false"));
                        writer.Object("backing", ("device_access_type", "EMULATION"), ("type", "CLIENT_DEVICE"));
                    }
                }
                json = writer.ToString();
            }

            vmware.Patch($"/rest/vcenter/vm/{VM}/hardware/cdrom/{key}", contentJson: json);
        }

        public void DetachISOs(VMware vmware)
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
                CDRomUpdateToClientDevice(vmware, cdrom.Key);
            }
        }

        public string VM { get; }
        public string Name { get; }
        public string MemorySizeMB { get; }
        public string CpuCount { get; }
        public string PowerState { get; }

        private bool IsPoweredOn => PowerState == null ? false : PowerState.EndsWith("ON", StringComparison.OrdinalIgnoreCase);

        public VMwareVMSlim(VMware vmware, JToken obj)
        {
            VM = obj["vm"]?.ToString();
            Name = obj["name"]?.ToString();
            MemorySizeMB = obj["memory_size_MiB"]?.ToString();
            CpuCount = obj["cpu_count"]?.ToString();
            PowerState = obj["power_state"]?.ToString();
        }

        public static IEnumerable<VMwareVMSlim> Query(VMware vmware)
        {
            return vmware.GetValueArray("/rest/vcenter/vm")
                .Select(o => new VMwareVMSlim(vmware, o))
                .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase);
        }

        public static IEnumerable<VMwareVMSlim> QueryWithoutToolsInstalled(VMware vmware)
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
            public SlimDiskSpace(VMware vmware, JToken obj) : base(vmware, obj) { }
            public IEnumerable<VMwareVM.GuestLocalFilesystem> FileSystemsCrossingThreshold => Filesystems.Where(o => o.PercentFree != null && o.PercentFree.Value <= PercentFreeThreshhold).OrderBy(o => o.Key);
            public override string ToString() => base.ToString() + "  " + FileSystemsCrossingThreshold.Select(o => "(" + o.PercentFree.Value + "% free) " + o.Key).ToStringDelimited("    ");
        }
        public static IEnumerable<VMwareVMSlim> QueryDiskspace10(VMware vmware) => QueryDiskspace(vmware, 10);
        public static IEnumerable<VMwareVMSlim> QueryDiskspace25(VMware vmware) => QueryDiskspace(vmware, 25);
        public static IEnumerable<VMwareVMSlim> QueryDiskspace(VMware vmware, int percentFreeThreshhold)
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
            public SlimIsoFile(VMware vmware, JToken obj) : base(vmware, obj) { }
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
        public static IEnumerable<VMwareVMSlim> QueryIsoAttached(VMware vmware)
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

        public static IEnumerable<VMwareVMSlim> QueryPoweredOff(VMware vmware) => Query(vmware).Where(o => !o.IsPoweredOn);
        public static IEnumerable<VMwareVMSlim> QueryPoweredOn(VMware vmware) => Query(vmware).Where(o => o.IsPoweredOn);


        public override string ToString()
        {
            return VM.PadRight(10) + (IsPoweredOn ? "   " : "OFF") + "  " + Name;
        }



    }

    public class VMwareVM : VMwareObject
    {
        public enum VmHardwareConnectionState { Connected, RecoverableError, UnrecoverableError, NotConnected, Unknown }

        public class GuestLocalFilesystem : VMwareObject
        {
            public string Key { get; }
            public long? FreeSpace { get; }
            public long? Capacity { get; }
            public long? Used => FreeSpace == null || Capacity == null ? null : Capacity.Value - FreeSpace.Value;
            public byte? PercentFree => FreeSpace == null || Capacity == null ? null : (byte)((double)FreeSpace.Value / (double)Capacity.Value * (double)100);
            public byte? PercentUsed => PercentFree == null ? null : (byte)((double)100 - (double)PercentFree.Value);

            public GuestLocalFilesystem(JToken obj)
            {
                Key = obj.ToString("key");
                FreeSpace = obj.ToLong("value", "free_space");
                Capacity = obj.ToLong("value", "capacity");
            }
        }

        public class CDROM : VMwareObject
        {
            public string Key { get; }
            public string StartConnected { get; }
            public string AllowGuestControl { get; }
            public string Label { get; }
            public VmHardwareConnectionState State { get; }
            public string Type { get; }
            public string BackingDeviceAccessType { get; }
            public string BackingType { get; }
            public string BackingIsoFile { get; }
            public string BackingIsoFileName => BackingIsoFile == null ? null : BackingIsoFile.Split("/").TrimOrNull().WhereNotNull().LastOrDefault();
            public string IdePrimary { get; }
            public string IdeMaster { get; }
            public string SataBus { get; }
            public string SataUnit { get; }
            public string ScsiBus { get; }
            public string ScsiUnit { get; }

            public CDROM(JToken obj)
            {
                Key = obj.ToString("key");
                Label = obj.ToString("value", "label");
                Type = obj.ToString("value", "type");
                StartConnected = obj.ToString("value", "start_connected");
                AllowGuestControl = obj.ToString("value", "allow_guest_control");
                State = obj.ToConnectionState("value", "state");

                BackingIsoFile = obj.ToString("value", "backing", "iso_file");
                BackingType = obj.ToString("value", "backing", "type");
                BackingDeviceAccessType = obj.ToString("value", "backing", "device_access_type");

                IdePrimary = obj.ToString("value", "ide", "primary");
                IdeMaster = obj.ToString("value", "ide", "master");
                ScsiBus = obj.ToString("value", "scsi", "bus");
                ScsiUnit = obj.ToString("value", "scsi", "unit");
                SataBus = obj.ToString("value", "sata", "bus");
                SataUnit = obj.ToString("value", "sata", "unit");
            }
        }

        public class Disk : VMwareObject
        {
            public string Key { get; }
            public string Label { get; }
            public string Type { get; }
            public long? Capacity { get; }
            public string IdePrimary { get; }
            public string IdeMaster { get; }
            public string SataBus { get; }
            public string SataUnit { get; }
            public string ScsiBus { get; }
            public string ScsiUnit { get; }
            public string BackingVmdkFile { get; }
            public string BackingType { get; }

            public Disk(JToken obj)
            {
                Key = obj.ToString("key");
                Label = obj.ToString("value", "label");
                Type = obj.ToString("value", "type");
                Capacity = obj.ToLong("value", "capacity");

                IdePrimary = obj.ToString("value", "ide", "primary");
                IdeMaster = obj.ToString("value", "ide", "master");
                ScsiBus = obj.ToString("value", "scsi", "bus");
                ScsiUnit = obj.ToString("value", "scsi", "unit");
                SataBus = obj.ToString("value", "sata", "bus");
                SataUnit = obj.ToString("value", "sata", "unit");
                BackingVmdkFile = obj.ToString("value", "backing", "vmdk_file");
                BackingType = obj.ToString("value", "backing", "type");
            }
        }

        public class ScsiAdapter : VMwareObject
        {
            public string Key { get; }
            public string ScsiBus { get; }
            public string ScsiUnit { get; }
            public string PciSlotNumber { get; }
            public string Label { get; }
            public string Type { get; }
            public string Sharing { get; }

            public ScsiAdapter(JToken obj)
            {
                Key = obj.ToString("key");
                PciSlotNumber = obj.ToString("value", "pci_slot_number");
                Label = obj.ToString("value", "label");
                Type = obj.ToString("value", "type");
                Sharing = obj.ToString("value", "sharing");
                ScsiBus = obj.ToString("value", "scsi", "bus");
                ScsiUnit = obj.ToString("value", "scsi", "unit");
            }
        }

        public class SataAdapter : VMwareObject
        {
            public string Key { get; }
            public string Bus { get; }
            public string PciSlotNumber { get; }
            public string Label { get; }
            public string Type { get; }

            public SataAdapter(JToken obj)
            {
                Key = obj.ToString("key");
                Bus = obj.ToString("value", "bus");
                PciSlotNumber = obj.ToString("value", "pci_slot_number");
                Label = obj.ToString("value", "label");
                Type = obj.ToString("value", "type");
            }
        }

        public class Floppy : VMwareObject
        {
            public string Key { get; }
            public bool? StartConnected { get; }
            public string BackingType { get; }
            public string BackingHostDevice { get; }
            public string BackingImageFile { get; }
            public bool? BackingAutoDetect { get; }
            public bool? AllowGuestControl { get; }
            public string Label { get; }
            public VmHardwareConnectionState State { get; }

            public Floppy(JToken obj)
            {
                Key = obj.ToString("key");
                StartConnected = obj.ToBool("value", "start_connected");
                Label = obj.ToString("value", "label");
                AllowGuestControl = obj.ToBool("value", "allow_guest_control");
                State = obj.ToConnectionState("value", "state");
                BackingType = obj.ToString("value", "backing", "type");
                BackingAutoDetect = obj.ToBool("value", "backing", "auto_detect");
                BackingHostDevice = obj.ToString("value", "backing", "host_device");
                BackingImageFile = obj.ToString("value", "backing", "image_file");

            }
        }

        public class Nic : VMwareObject
        {
            public string Key { get; }
            public bool? StartConnected { get; }
            public int? PciSlotNumber { get; }
            public string MacAddress { get; }
            public string MacType { get; }
            public bool? AllowGuestControl { get; }
            public bool? WakeOnLanEnabled { get; }
            public string Label { get; }
            public VmHardwareConnectionState State { get; }
            public string Type { get; }
            public string BackingConnectionCookie { get; }
            public Guid? BackingDistributedSwitchUUID { get; }
            public int? BackingDistributedPort { get; }
            public string BackingType { get; }
            public string BackingNetwork { get; }

            public Nic(JToken obj)
            {
                Key = obj.ToString("key");
                StartConnected = obj.ToBool("value", "start_connected");
                PciSlotNumber = obj.ToInt("value", "pci_slot_number");
                MacAddress = obj.ToString("value", "mac_address");
                MacType = obj.ToString("value", "mac_type");
                AllowGuestControl = obj.ToBool("value", "allow_guest_control");
                WakeOnLanEnabled = obj.ToBool("value", "wake_on_lan_enabled");
                Label = obj.ToString("value", "label");
                State = obj.ToConnectionState("value", "state");
                Type = obj.ToString("value", "type");

                BackingConnectionCookie = obj.ToString("value", "backing", "connection_cookie");
                BackingDistributedSwitchUUID = obj.ToGuid("value", "backing", "distributed_switch_uuid");
                BackingDistributedPort = obj.ToInt("value", "backing", "distributed_port");
                BackingType = obj.ToString("value", "backing", "type");
                BackingNetwork = obj.ToString("value", "backing", "network");
            }
        }

        public enum VMPowerState { Unknown, PoweredOff, PoweredOn, Suspended }
        public string VM { get; }
        public string Name { get; }
        public long? MemorySizeMB { get; }
        public int? CpuCount { get; }
        public VMPowerState PowerState { get; }
        public bool? MemoryHotAddEnabled { get; }
        public int? CpuCoresPerSocket { get; }
        public int? CpuSocketCount => CpuCount == null || CpuCoresPerSocket == null ? null : CpuCount.Value / CpuCoresPerSocket.Value;
        public bool? CpuHotRemoveEnabled { get; }
        public bool? CpuHotAddEnabled { get; }
        public int? BootDelay { get; }
        public int? BootRetryDelay { get; }
        public bool? BootEnterSetupMode { get; }
        public string BootType { get; }
        public bool? BootRetry { get; }
        public bool? BootEfiLegacyBoot { get; }
        public string BootNetworkProtocol { get; }
        public string GuestOS { get; }
        public string HardwareUpgradePolicy { get; }
        public string HardwareUpgradeStatus { get; }
        public string HardwareVersion { get; }
        public string IdentityFullNameDefaultMessage { get; }
        public string IdentityFullNameId { get; }
        public string IdentityName { get; }
        public string IdentityIpAddress { get; }
        public string IdentityFamily { get; }
        public string IdentityHostName { get; }
        public IReadOnlyList<CDROM> CDRoms { get; }
        public IReadOnlyList<Disk> Disks { get; }
        public IReadOnlyList<ScsiAdapter> ScsiAdapters { get; }
        public IReadOnlyList<Nic> Nics { get; }
        public IReadOnlyList<GuestLocalFilesystem> GuestLocalFilesystems { get; }
        public bool IsVMwareToolsInstalled { get; }
        public string LibraryItem { get; }
        public VMwareVM(VMware vmware, JToken obj)
        {
            VM = obj.ToString("vm");
            Name = obj.ToString("name");
            MemorySizeMB = obj.ToLong("memory_size_MiB");
            CpuCount = obj.ToInt("cpu_count");

            var powerState = obj.ToString("power_state");
            if (powerState == null) PowerState = VMPowerState.Unknown;
            else if (powerState.EqualsCaseInsensitive("POWERED_OFF")) PowerState = VMPowerState.PoweredOff;
            else if (powerState.EqualsCaseInsensitive("POWERED_ON")) PowerState = VMPowerState.PoweredOn;
            else if (powerState.EqualsCaseInsensitive("SUSPENDED")) PowerState = VMPowerState.Suspended;

            obj = QueryValueObjectSafe(vmware, "/rest/vcenter/vm/" + VM);
            if (obj == null) return;
            GuestOS = obj.ToString("guest_OS");

            MemoryHotAddEnabled = obj.ToBool("memory", "hot_add_enabled");
            CpuHotRemoveEnabled = obj.ToBool("cpu", "hot_remove_enabled");
            CpuHotAddEnabled = obj.ToBool("cpu", "hot_add_enabled");
            CpuCoresPerSocket = obj.ToInt("cpu", "cores_per_socket");

            CDRoms = obj["cdroms"].OrEmpty().Select(o => new CDROM((JObject)o)).ToList();
            Disks = obj["disks"].OrEmpty().Select(o => new Disk((JObject)o)).ToList();
            ScsiAdapters = obj["scsi_adapters"].OrEmpty().Select(o => new ScsiAdapter((JObject)o)).ToList();
            Nics = obj["nics"].OrEmpty().Select(o => new Nic((JObject)o)).ToList();

            BootDelay = obj.ToInt("boot", "delay");
            BootRetryDelay = obj.ToInt("boot", "retry_delay");
            BootEnterSetupMode = obj.ToBool("boot", "enter_setup_mode");
            BootType = obj.ToString("boot", "type");
            BootRetry = obj.ToBool("boot", "retry");
            BootEfiLegacyBoot = obj.ToBool("boot", "efi_legacy_boot");
            BootNetworkProtocol = obj.ToString("boot", "network_protocol");

            HardwareUpgradePolicy = obj.ToString("hardware", "upgrade_policy");
            HardwareUpgradeStatus = obj.ToString("hardware", "upgrade_status");
            HardwareVersion = obj.ToString("hardware", "version");

            obj = QueryValueObjectSafe(vmware, $"/rest/vcenter/vm/{VM}/guest/identity");
            if (obj != null)
            {
                IdentityFullNameDefaultMessage = obj.ToString("full_name", "default_message");
                IdentityFullNameId = obj.ToString("full_name", "id");
                IdentityName = obj.ToString("name");
                IdentityIpAddress = obj.ToString("ip_address"); // Only 1 IP supported: https://github.com/vmware-archive/vsphere-automation-sdk-rest/issues/21
                IdentityFamily = obj.ToString("family");
                IdentityHostName = obj.ToString("host_name");
            }

            var localFilesystem = QueryValueArraySafe(vmware, $"/rest/vcenter/vm/{VM}/guest/local-filesystem").ToArray();
            GuestLocalFilesystems = localFilesystem.Select(o => new GuestLocalFilesystem(o)).ToList();

            if (obj == null && localFilesystem.Length == 0) IsVMwareToolsInstalled = false;
            else IsVMwareToolsInstalled = true;

            obj = QueryValueObjectSafe(vmware, $"/rest/vcenter/vm/{VM}/library-item");
            if (obj != null)
            {
                LibraryItem = obj.ToString("check_out", "library_item");
            }
        }

        public static IEnumerable<VMwareVM> Query(VMware vmware)
        {
            foreach (var obj in vmware.GetValueArray("/rest/vcenter/vm").OrderBy(o => o["name"]?.ToString(), StringComparer.OrdinalIgnoreCase))
            {
                yield return new VMwareVM(vmware, obj);
            }
        }

        private static VMwareVM QueryBy(VMware vmware, string fieldName, string fieldValue)
        {
            var obj = vmware.GetValueArray("/rest/vcenter/vm")
                .OrderBy(o => o["name"]?.ToString(), StringComparer.OrdinalIgnoreCase)
                .Where(o => o[fieldName]?.ToString() != null && string.Equals(o[fieldName]?.ToString(), fieldValue, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (obj == null) return null;
            return new VMwareVM(vmware, obj);
        }
        public static VMwareVM QueryByName(VMware vmware, string name) => QueryBy(vmware, "name", name);
        public static VMwareVM QueryByVM(VMware vmware, string vm) => QueryBy(vmware, "vm", vm);


    }

    public class VMwareDatacenter : VMwareObject
    {
        public string Name { get; }
        public string Datacenter { get; }
        public string DatastoreFolder { get; }
        public string HostFolder { get; }
        public string NetworkFolder { get; }
        public string VMFolder { get; }

        public VMwareDatacenter(VMware vmware, JToken obj)
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

        public static IEnumerable<VMwareDatacenter> Query(VMware vmware)
        {
            foreach (var obj in vmware.GetValueArray("/rest/vcenter/datacenter"))
            {
                yield return new VMwareDatacenter(vmware, obj);
            }
        }
    }

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

        public VMwareStoragePolicy(VMware vmware, JToken obj)
        {
            Name = obj.ToString("name");
            Description = obj.ToString("description");
            Policy = obj.ToString("policy");

            Disks = vmware.GetValueArray($"/rest/vcenter/storage/policies/{Policy}/vm").OrEmpty().Select(o => new Disk(o)).ToList();
        }

        public static IEnumerable<VMwareStoragePolicy> Query(VMware vmware)
        {
            foreach (var obj in vmware.GetValueArray("/rest/vcenter/storage/policies"))
            {
                yield return new VMwareStoragePolicy(vmware, obj);
            }
        }
    }

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

    public class VMwareFolder : VMwareObject
    {
        public string Name { get; }
        public string Folder { get; }
        public string Type { get; }

        public VMwareFolder(VMware vmware, JToken obj)
        {
            Name = obj.ToString("name");
            Folder = obj.ToString("folder");
            Type = obj.ToString("type");
        }

        public static IEnumerable<VMwareFolder> Query(VMware vmware)
        {
            foreach (var obj in vmware.GetValueArray("/rest/vcenter/folder"))
            {
                yield return new VMwareFolder(vmware, obj);
            }
        }
    }


    public class VMwareHost : VMwareObject
    {
        public enum HostPowerState { Unknown, PoweredOn, PoweredOff, Standby }
        public enum HostConnectionState { Unknown, Connected, Disconnected, NotResponding }
        public string Name { get; }
        public string Host { get; }
        public HostConnectionState ConnectionState { get; }
        public HostPowerState PowerState { get; }

        public VMwareHost(VMware vmware, JToken obj)
        {
            Name = obj.ToString("name");
            Host = obj.ToString("host");

            var connectionState = obj.ToString("connection_state");
            if (connectionState == null) ConnectionState = HostConnectionState.Unknown;
            else if (connectionState.EqualsCaseInsensitive("CONNECTED")) ConnectionState = HostConnectionState.Connected;
            else if (connectionState.EqualsCaseInsensitive("DISCONNECTED")) ConnectionState = HostConnectionState.Disconnected;
            else if (connectionState.EqualsCaseInsensitive("NOT_RESPONDING")) ConnectionState = HostConnectionState.NotResponding;

            var powerState = obj.ToString("power_state");
            if (powerState == null) PowerState = HostPowerState.Unknown;
            else if (powerState.EqualsCaseInsensitive("POWERED_OFF")) PowerState = HostPowerState.PoweredOff;
            else if (powerState.EqualsCaseInsensitive("POWERED_ON")) PowerState = HostPowerState.PoweredOn;
            else if (powerState.EqualsCaseInsensitive("STANDBY")) PowerState = HostPowerState.Standby;
        }

        public static IEnumerable<VMwareHost> Query(VMware vmware)
        {
            foreach (var obj in vmware.GetValueArray("/rest/vcenter/host"))
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

        public VMwareNetwork(VMware vmware, JToken obj)
        {
            Name = obj.ToString("name");
            Network = obj.ToString("network");
            Type = obj.ToString("type");
        }

        public static IEnumerable<VMwareNetwork> Query(VMware vmware)
        {
            foreach (var obj in vmware.GetValueArray("/rest/vcenter/network"))
            {
                yield return new VMwareNetwork(vmware, obj);
            }
        }
    }

    public class VMwareResourcePool : VMwareObject
    {
        public string Name { get; }
        public string ResourcePool { get; }

        public VMwareResourcePool(VMware vmware, JToken obj)
        {
            Name = obj.ToString("name");
            ResourcePool = obj.ToString("resource_pool");
        }

        public static IEnumerable<VMwareResourcePool> Query(VMware vmware)
        {
            foreach (var obj in vmware.GetValueArray("/rest/vcenter/resource-pool"))
            {
                yield return new VMwareResourcePool(vmware, obj);
            }
        }
    }


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
    }

}
