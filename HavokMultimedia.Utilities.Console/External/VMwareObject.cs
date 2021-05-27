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

using System.Collections.Generic;
using System.Dynamic;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace HavokMultimedia.Utilities.Console.External
{
    public abstract class VMwareObject
    {
        public static bool HasValue(dynamic obj, string propertyName) => Util.DynamicHasProperty(obj, propertyName);

        protected PropertyInfo[] GetProperties()
        {
            var list = new List<PropertyInfo>();
            foreach (var prop in GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (!prop.CanRead) continue;
                list.Add(prop);
            }
            return list.ToArray();
        }
    }

    public class VMwareVM : VMwareObject
    {
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

            public CDROM(dynamic obj)
            {
                Key = obj.key;
                obj = obj.value;
                StartConnected = obj.start_connected;
                AllowGuestControl = obj.allow_guest_control;
                Label = obj.label;
                State = obj.state;
                Type = obj.type;

                if (HasValue(obj, "backing"))
                {
                    var obj2 = obj.backing;
                    if (HasValue(obj2, "iso_file")) BackingIsoFile = obj2.iso_file;
                    if (HasValue(obj2, "type")) BackingType = obj2.type;
                    if (HasValue(obj2, "device_access_type")) BackingDeviceAccessType = obj2.device_access_type;
                }

                if (HasValue(obj, "ide"))
                {
                    var obj2 = obj.ide;
                    if (HasValue(obj2, "primary")) IdePrimary = obj2.primary;
                    if (HasValue(obj2, "master")) IdeMaster = obj2.master;
                }
                if (HasValue(obj, "sata"))
                {
                    var obj2 = obj.sata;
                    if (HasValue(obj2, "bus")) SataBus = obj2.bus;
                    if (HasValue(obj2, "unit")) SataUnit = obj2.unit;
                }
                if (HasValue(obj, "scsi"))
                {
                    var obj2 = obj.scsi;
                    if (HasValue(obj2, "bus")) ScsiBus = obj2.bus;
                    if (HasValue(obj2, "unit")) ScsiUnit = obj2.unit;
                }
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

            public Disk(dynamic obj)
            {
                Key = obj.key;
                obj = obj.value;
                Label = obj.label;
                Type = obj.type;
                Capacity = obj.capacity;

                if (HasValue(obj, "ide"))
                {
                    var obj2 = obj.ide;
                    if (HasValue(obj2, "primary")) IdePrimary = obj2.primary;
                    if (HasValue(obj2, "master")) IdeMaster = obj2.master;
                }
                if (HasValue(obj, "sata"))
                {
                    var obj2 = obj.sata;
                    if (HasValue(obj2, "bus")) SataBus = obj2.bus;
                    if (HasValue(obj2, "unit")) SataUnit = obj2.unit;
                }
                if (HasValue(obj, "scsi"))
                {
                    var obj2 = obj.scsi;
                    if (HasValue(obj2, "bus")) ScsiBus = obj2.bus;
                    if (HasValue(obj2, "unit")) ScsiUnit = obj2.unit;
                }

                if (HasValue(obj, "backing"))
                {
                    var obj2 = obj.sata;
                    if (HasValue(obj2, "vmdk_file")) BackingVmdkFile = obj2.vmdk_file;
                    if (HasValue(obj2, "type")) BackingType = obj2.type;
                }

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

            public ScsiAdapter(dynamic obj)
            {
                Key = obj.key;
                obj = obj.value;
                PciSlotNumber = obj.pci_slot_number;
                Label = obj.label;
                Type = obj.type;
                Sharing = obj.sharing;


                if (HasValue(obj, "scsi"))
                {
                    var obj2 = obj.scsi;
                    if (HasValue(obj2, "bus")) ScsiBus = obj2.bus;
                    if (HasValue(obj2, "unit")) ScsiUnit = obj2.unit;
                }



            }
        }

        public class SataAdapter : VMwareObject
        {
            public string Key { get; }
            public string Bus { get; }
            public string PciSlotNumber { get; }
            public string Label { get; }
            public string Type { get; }

            public SataAdapter(dynamic obj)
            {
                Key = obj.key;
                obj = obj.value;
                Bus = obj.bus;
                PciSlotNumber = obj.pci_slot_number;
                Label = obj.label;
                Type = obj.type;



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

            public Floppy(dynamic obj)
            {
                Key = obj.key;
                obj = obj.value;
                StartConnected = obj.start_connected;
                Label = obj.label;
                AllowGuestControl = obj.allow_guest_control;
                State = obj.state;

                if (HasValue(obj, "backing"))
                {
                    var obj2 = obj.scsi;
                    if (HasValue(obj2, "type")) BackingType = obj2.type;
                }



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


            public Nic(dynamic obj)
            {
                Key = obj.key;
                obj = obj.value;
                StartConnected = obj.start_connected;
                PciSlotNumber = obj.pci_slot_number;
                MacAddress = obj.mac_address;
                MacType = obj.mac_type;
                AllowGuestControl = obj.allow_guest_control;
                WakeOnLanEnabled = obj.wake_on_lan_enabled;
                Label = obj.label;
                State = obj.state;
                Type = obj.type;

                if (HasValue(obj, "backing"))
                {
                    var obj2 = obj.scsi;
                    if (HasValue(obj2, "connection_cookie")) BackingConnectionCookie = obj2.connection_cookie;
                    if (HasValue(obj2, "distributed_switch_uuid")) BackingDistributedSwitchUUID = obj2.distributed_switch_uuid;
                    if (HasValue(obj2, "distributed_port")) BackingDistributedPort = obj2.distributed_port;
                    if (HasValue(obj2, "type")) BackingType = obj2.type;
                    if (HasValue(obj2, "network")) BackingNetwork = obj2.network;
                }



            }
        }
        public string VM { get; }
        public string Name { get; }
        public string MemorySizeMB { get; }
        public string MemoryHotAddEnabled { get; }
        public string CpuCount { get; }
        public string CpuCoresPerSocket { get; }
        public string CpuHotRemoveEnabled { get; }
        public string CpuHotAddEnabled { get; }
        public string PowerState { get; }

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
        public VMwareVM(VMware vmware, dynamic obj)
        {
            VM = obj.vm;
            Name = obj.name;
            MemorySizeMB = obj.memory_size_MiB;
            CpuCount = obj.cpu_count;
            PowerState = obj.power_state;

            obj = vmware.Query("/rest/vcenter/vm/" + VM).value;
            if (HasValue(obj, "memory"))
            {
                var obj2 = obj.memory;
                if (HasValue(obj2, "hot_add_enabled")) MemoryHotAddEnabled = obj2.hot_add_enabled;
            }

            var cdroms = new List<CDROM>();
            if (HasValue(obj, "cdroms"))
            {
                foreach (var o in obj.cdroms)
                {
                    cdroms.Add(new CDROM(o));
                }
            }
            CDRoms = cdroms;

            var disks = new List<Disk>();
            if (HasValue(obj, "disks"))
            {
                foreach (var o in obj.disks)
                {
                    disks.Add(new Disk(o));
                }
            }
            Disks = disks;

            if (HasValue(obj, "cpu"))
            {
                var obj2 = obj.cpu;
                if (HasValue(obj2, "hot_remove_enabled")) CpuHotRemoveEnabled = obj2.hot_remove_enabled;
                if (HasValue(obj2, "hot_add_enabled")) CpuHotAddEnabled = obj2.hot_add_enabled;
                if (HasValue(obj2, "cores_per_socket")) CpuCoresPerSocket = obj2.cores_per_socket;
            }

            var scsiAdapters = new List<ScsiAdapter>();
            if (HasValue(obj, "scsi_adapters"))
            {
                foreach (var o in obj.cdroms)
                {
                    scsiAdapters.Add(new ScsiAdapter(o));
                }
            }
            ScsiAdapters = scsiAdapters;

            var nics = new List<Nic>();
            if (HasValue(obj, "nics"))
            {
                foreach (var o in obj.nics)
                {
                    nics.Add(new Nic(o));
                }
            }
            Nics = nics;

            if (HasValue(obj, "boot"))
            {
                var obj2 = obj.boot;
                if (HasValue(obj2, "delay")) BootDelay = obj2.delay;
                if (HasValue(obj2, "retry_delay")) BootRetryDelay = obj2.retry_delay;
                if (HasValue(obj2, "enter_setup_mode")) BootEnterSetupMode = obj2.enter_setup_mode;
                if (HasValue(obj2, "type")) BootType = obj2.type;
                if (HasValue(obj2, "retry")) BootRetry = obj2.retry;
                if (HasValue(obj2, "efi_legacy_boot")) BootEfiLegacyBoot = obj2.efi_legacy_boot;
                if (HasValue(obj2, "network_protocol")) BootNetworkProtocol = obj2.network_protocol;
            }

            GuestOS = obj.guest_OS;

            if (HasValue(obj, "hardware"))
            {
                var obj2 = obj.hardware;
                if (HasValue(obj2, "upgrade_policy")) HardwareUpgradePolicy = obj2.upgrade_policy;
                if (HasValue(obj2, "upgrade_status")) HardwareUpgradeStatus = obj2.upgrade_status;
                if (HasValue(obj2, "version")) HardwareVersion = obj2.version;
            }

            obj = vmware.Query("/rest/vcenter/vm/" + VM + "/guest/identity").value;
            if (HasValue(obj, "full_name"))
            {
                var obj2 = obj.full_name;
                if (HasValue(obj2, "default_message")) IdentityFullNameDefaultMessage = obj2.default_message;
                if (HasValue(obj2, "id")) IdentityFullNameId = obj2.id;
            }
            IdentityName = obj.name;
            // Only 1 IP supported: https://github.com/vmware-archive/vsphere-automation-sdk-rest/issues/21
            IdentityIpAddress = obj.ip_address;
            IdentityFamily = obj.family;
            IdentityHostName = obj.host_name;
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
        public BigInteger FreeSpace { get; }
        public BigInteger Capacity { get; }

        public string Accessible { get; }
        public string MultipleHostAccess { get; }
        public string ThinProvisioningSupported { get; }


        public VMwareDatastore(VMware vmware, dynamic obj)
        {
            Name = obj.name;
            Datastore = obj.datastore;
            Type = obj.type;
            FreeSpace = BigInteger.Parse((string)obj.free_space);
            Capacity = BigInteger.Parse((string)obj.capacity);

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

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(GetType().NameFormatted());
            sb.Append("[");
            bool first = true;
            foreach (var property in GetProperties())
            {
                if (first) first = false;
                else sb.Append(", ");

                var val = Util.GetPropertyValue(this, property.Name);
                sb.Append(property.Name);
                sb.Append(":");
                sb.Append(val.ToStringGuessFormat());

            }
            sb.Append("]");
            return sb.ToString();
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
}
