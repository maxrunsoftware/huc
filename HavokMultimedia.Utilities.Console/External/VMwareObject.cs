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
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
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
            public IEnumerable<string> IsosAttached => CDRoms
                .Where(o => o.BackingType.StartsWith("ISO", StringComparison.OrdinalIgnoreCase))
                .Select(o => o.BackingIsoFile.Split("/").TrimOrNull().WhereNotNull().LastOrDefault())
                .WhereNotNull();

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
        public class GuestLocalFilesystem : VMwareObject
        {
            public string Key { get; }
            public long? FreeSpace { get; }
            public long? Capacity { get; }
            public long? Used { get; }
            public byte? PercentFree { get; }
            public byte? PercentUsed { get; }

            public GuestLocalFilesystem(JToken obj)
            {
                Key = obj["key"]?.ToString();
                var sFreeSpace = obj["value"]?["free_space"]?.ToString();
                if (sFreeSpace != null) FreeSpace = long.Parse(sFreeSpace);

                var sCapacity = obj["value"]?["capacity"]?.ToString();
                if (sCapacity != null) Capacity = long.Parse(sCapacity);

                if (FreeSpace == null || Capacity == null) return;

                Used = Capacity.Value - FreeSpace.Value;
                PercentFree = (byte)((double)FreeSpace.Value / (double)Capacity.Value * (double)100);
                PercentUsed = (byte)((double)100 - (double)PercentFree.Value);
            }
        }

        public class CDROM : VMwareObject
        {
            public string Key { get; }
            public string StartConnected { get; }
            public string AllowGuestControl { get; }
            public string Label { get; }
            public string State { get; }
            public string Type { get; }
            public string BackingDeviceAccessType { get; }
            public string BackingType { get; }
            public string BackingIsoFile { get; }
            public string IdePrimary { get; }
            public string IdeMaster { get; }
            public string SataBus { get; }
            public string SataUnit { get; }
            public string ScsiBus { get; }
            public string ScsiUnit { get; }

            public CDROM(JToken obj)
            {
                Key = obj["key"]?.ToString();
                Label = obj["value"]?["label"]?.ToString();
                Type = obj["value"]?["type"]?.ToString();
                StartConnected = obj["value"]?["start_connected"]?.ToString();
                AllowGuestControl = obj["value"]?["allow_guest_control"]?.ToString();
                State = obj["value"]?["state"]?.ToString();

                BackingIsoFile = obj["value"]?["backing"]?["iso_file"]?.ToString();
                BackingType = obj["value"]?["backing"]?["type"]?.ToString();
                BackingDeviceAccessType = obj["value"]?["backing"]?["device_access_type"]?.ToString();

                IdePrimary = obj["value"]?["ide"]?["primary"]?.ToString();
                IdeMaster = obj["value"]?["ide"]?["master"]?.ToString();
                ScsiBus = obj["value"]?["scsi"]?["bus"]?.ToString();
                ScsiUnit = obj["value"]?["scsi"]?["unit"]?.ToString();
                SataBus = obj["value"]?["sata"]?["bus"]?.ToString();
                SataUnit = obj["value"]?["sata"]?["unit"]?.ToString();
            }
        }

        public class Disk : VMwareObject
        {
            public string Key { get; }
            public string Label { get; }
            public string Type { get; }
            public string Capacity { get; }
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
                Key = obj["key"]?.ToString();
                Label = obj["value"]?["label"]?.ToString();
                Type = obj["value"]?["type"]?.ToString();
                Capacity = obj["value"]?["capacity"]?.ToString();

                IdePrimary = obj["value"]?["ide"]?["primary"]?.ToString();
                IdeMaster = obj["value"]?["ide"]?["master"]?.ToString();
                ScsiBus = obj["value"]?["scsi"]?["bus"]?.ToString();
                ScsiUnit = obj["value"]?["scsi"]?["unit"]?.ToString();
                SataBus = obj["value"]?["sata"]?["bus"]?.ToString();
                SataUnit = obj["value"]?["sata"]?["unit"]?.ToString();
                BackingVmdkFile = obj["value"]?["backing"]?["vmdk_file"]?.ToString();
                BackingType = obj["value"]?["backing"]?["type"]?.ToString();
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
                Key = obj["key"]?.ToString();
                PciSlotNumber = obj["value"]?["pci_slot_number"]?.ToString();
                PciSlotNumber = obj["value"]?["label"]?.ToString();
                PciSlotNumber = obj["value"]?["type"]?.ToString();
                PciSlotNumber = obj["value"]?["sharing"]?.ToString();

                ScsiBus = obj["value"]?["scsi"]?["bus"]?.ToString();
                ScsiUnit = obj["value"]?["scsi"]?["unit"]?.ToString();
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
                Key = obj["key"]?.ToString();
                Bus = obj["value"]?["bus"]?.ToString();
                PciSlotNumber = obj["value"]?["pci_slot_number"]?.ToString();
                Label = obj["value"]?["label"]?.ToString();
                Type = obj["value"]?["type"]?.ToString();
            }
        }

        public class Floppy : VMwareObject
        {
            public string Key { get; }
            public string StartConnected { get; }
            public string BackingType { get; }
            public string AllowGuestControl { get; }
            public string Label { get; }
            public string State { get; }

            public Floppy(JToken obj)
            {
                Key = obj["key"]?.ToString();
                StartConnected = obj["value"]?["start_connected"]?.ToString();
                Label = obj["value"]?["label"]?.ToString();
                AllowGuestControl = obj["value"]?["allow_guest_control"]?.ToString();
                State = obj["value"]?["state"]?.ToString();
                BackingType = obj["value"]?["backing"]?["type"]?.ToString();
            }
        }

        public class Nic : VMwareObject
        {
            public string Key { get; }
            public string StartConnected { get; }
            public string PciSlotNumber { get; }
            public string MacAddress { get; }
            public string MacType { get; }
            public string AllowGuestControl { get; }
            public string WakeOnLanEnabled { get; }
            public string Label { get; }
            public string State { get; }
            public string Type { get; }
            public string BackingConnectionCookie { get; }
            public string BackingDistributedSwitchUUID { get; }
            public string BackingDistributedPort { get; }
            public string BackingType { get; }
            public string BackingNetwork { get; }

            public Nic(JToken obj)
            {
                Key = obj["key"]?.ToString();
                StartConnected = obj["value"]?["start_connected"]?.ToString();
                PciSlotNumber = obj["value"]?["pci_slot_number"]?.ToString();
                MacAddress = obj["value"]?["mac_address"]?.ToString();
                MacType = obj["value"]?["mac_type"]?.ToString();
                AllowGuestControl = obj["value"]?["allow_guest_control"]?.ToString();
                WakeOnLanEnabled = obj["value"]?["wake_on_lan_enabled"]?.ToString();
                Label = obj["value"]?["label"]?.ToString();
                State = obj["value"]?["state"]?.ToString();
                Type = obj["value"]?["type"]?.ToString();

                BackingConnectionCookie = obj["value"]?["backing"]?["connection_cookie"]?.ToString();
                BackingDistributedSwitchUUID = obj["value"]?["backing"]?["distributed_switch_uuid"]?.ToString();
                BackingDistributedPort = obj["value"]?["backing"]?["distributed_port"]?.ToString();
                BackingType = obj["value"]?["backing"]?["type"]?.ToString();
                BackingNetwork = obj["value"]?["backing"]?["network"]?.ToString();
            }
        }

        public string VM { get; }
        public string Name { get; }
        public string MemorySizeMB { get; }
        public string CpuCount { get; }
        public string PowerState { get; }
        public string MemoryHotAddEnabled { get; }
        public string CpuCoresPerSocket { get; }
        public string CpuHotRemoveEnabled { get; }
        public string CpuHotAddEnabled { get; }
        public string BootDelay { get; }
        public string BootRetryDelay { get; }
        public string BootEnterSetupMode { get; }
        public string BootType { get; }
        public string BootRetry { get; }
        public string BootEfiLegacyBoot { get; }
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
        public VMwareVM(VMware vmware, JToken obj)
        {
            VM = obj["vm"]?.ToString();
            Name = obj["name"]?.ToString();
            MemorySizeMB = obj["memory_size_MiB"]?.ToString();
            CpuCount = obj["cpu_count"]?.ToString();
            PowerState = obj["power_state"]?.ToString();

            obj = QueryValueObjectSafe(vmware, "/rest/vcenter/vm/" + VM);
            if (obj == null) return;
            GuestOS = obj["guest_OS"]?.ToString();

            MemoryHotAddEnabled = obj["memory"]?["hot_add_enabled"]?.ToString();
            CpuHotRemoveEnabled = obj["cpu"]?["hot_remove_enabled"]?.ToString();
            CpuHotAddEnabled = obj["cpu"]?["hot_add_enabled"]?.ToString();
            CpuCoresPerSocket = obj["cpu"]?["cores_per_socket"]?.ToString();

            CDRoms = obj["cdroms"].OrEmpty().Select(o => new CDROM((JObject)o)).ToList();
            Disks = obj["disks"].OrEmpty().Select(o => new Disk((JObject)o)).ToList();
            ScsiAdapters = obj["scsi_adapters"].OrEmpty().Select(o => new ScsiAdapter((JObject)o)).ToList();
            Nics = obj["nics"].OrEmpty().Select(o => new Nic((JObject)o)).ToList();

            BootDelay = obj["boot"]?["delay"]?.ToString();
            BootRetryDelay = obj["boot"]?["retry_delay"]?.ToString();
            BootEnterSetupMode = obj["boot"]?["enter_setup_mode"]?.ToString();
            BootType = obj["boot"]?["type"]?.ToString();
            BootRetry = obj["boot"]?["retry"]?.ToString();
            BootEfiLegacyBoot = obj["boot"]?["efi_legacy_boot"]?.ToString();
            BootNetworkProtocol = obj["boot"]?["network_protocol"]?.ToString();

            HardwareUpgradePolicy = obj["hardware"]?["upgrade_policy"]?.ToString();
            HardwareUpgradeStatus = obj["hardware"]?["upgrade_status"]?.ToString();
            HardwareVersion = obj["hardware"]?["version"]?.ToString();

            obj = QueryValueObjectSafe(vmware, $"/rest/vcenter/vm/{VM}/guest/identity");
            if (obj != null)
            {
                IdentityFullNameDefaultMessage = obj["full_name"]?["default_message"]?.ToString();
                IdentityFullNameId = obj["full_name"]?["id"]?.ToString();
                IdentityName = obj["name"]?.ToString();
                IdentityIpAddress = obj["ip_address"]?.ToString(); // Only 1 IP supported: https://github.com/vmware-archive/vsphere-automation-sdk-rest/issues/21
                IdentityFamily = obj["family"]?.ToString();
                IdentityHostName = obj["host_name"]?.ToString();
            }

            var localFilesystem = QueryValueArraySafe(vmware, $"/rest/vcenter/vm/{VM}/guest/local-filesystem").ToArray();
            GuestLocalFilesystems = localFilesystem.Select(o => new GuestLocalFilesystem(o)).ToList();

            if (obj == null && localFilesystem.Length == 0) IsVMwareToolsInstalled = false;
            else IsVMwareToolsInstalled = true;
        }

        public static IEnumerable<VMwareVM> Query(VMware vmware)
        {
            foreach (var obj in vmware.GetValueArray("/rest/vcenter/vm").OrderBy(o => o["name"]?.ToString(), StringComparer.OrdinalIgnoreCase))
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

        public VMwareDatacenter(VMware vmware, JToken obj)
        {
            Name = obj["name"]?.ToString();
            Datacenter = obj["datacenter"]?.ToString();

            obj = QueryValueObjectSafe(vmware, "/rest/vcenter/datacenter/" + Datacenter);
            if (obj != null)
            {
                DatastoreFolder = obj["datastore_folder"]?.ToString();
                HostFolder = obj["host_folder"]?.ToString();
                NetworkFolder = obj["network_folder"]?.ToString();
                VMFolder = obj["vm_folder"]?.ToString();
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
                Key = obj["key"]?.ToString();
                VmHome = obj["value"]?["vm_home"]?.ToString();
                Disks = obj["value"]?["disks"].OrEmpty().Select(o => o?.ToString()).WhereNotNull().ToList();
            }

        }

        public string Name { get; }
        public string Description { get; }
        public string Policy { get; }
        public IReadOnlyList<Disk> Disks { get; }

        public VMwareStoragePolicy(VMware vmware, JToken obj)
        {
            Name = obj["name"]?.ToString();
            Description = obj["description"]?.ToString();
            Policy = obj["policy"]?.ToString();

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
        public string FreeSpace { get; }
        public string Capacity { get; }
        public string Accessible { get; }
        public string MultipleHostAccess { get; }
        public string ThinProvisioningSupported { get; }

        public VMwareDatastore(VMware vmware, JToken obj)
        {
            Name = obj["name"]?.ToString();
            Datastore = obj["datastore"]?.ToString();
            Type = obj["type"]?.ToString();
            FreeSpace = obj["free_space"]?.ToString();
            Capacity = obj["capacity"]?.ToString();

            obj = QueryValueObjectSafe(vmware, "/rest/vcenter/datastore/" + Datastore);
            if (obj != null)
            {
                Accessible = obj["accessible"]?.ToString();
                MultipleHostAccess = obj["multiple_host_access"]?.ToString();
                ThinProvisioningSupported = obj["thin_provisioning_supported"]?.ToString();
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
            Name = obj["name"]?.ToString();
            Folder = obj["folder"]?.ToString();
            Type = obj["type"]?.ToString();
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
        public string Name { get; }
        public string Host { get; }
        public string ConnectionState { get; }
        public string PowerState { get; }

        public VMwareHost(VMware vmware, JToken obj)
        {
            Name = obj["name"]?.ToString();
            Host = obj["host"]?.ToString();
            ConnectionState = obj["connection_state"]?.ToString();
            PowerState = obj["power_state"]?.ToString();
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
            Name = obj["name"]?.ToString();
            Network = obj["network"]?.ToString();
            Type = obj["type"]?.ToString();
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
            Name = obj["name"]?.ToString();
            ResourcePool = obj["resource_pool"]?.ToString();
        }

        public static IEnumerable<VMwareResourcePool> Query(VMware vmware)
        {
            foreach (var obj in vmware.GetValueArray("/rest/vcenter/resource-pool"))
            {
                yield return new VMwareResourcePool(vmware, obj);
            }
        }
    }

}
