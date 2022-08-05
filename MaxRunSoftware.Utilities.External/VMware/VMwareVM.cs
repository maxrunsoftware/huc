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

using Newtonsoft.Json.Linq;

// ReSharper disable RedundantCast

namespace MaxRunSoftware.Utilities.External;

public class VMwareVM : VMwareObject
{
    public enum VmHardwareConnectionState
    {
        Connected,
        RecoverableError,
        UnrecoverableError,
        NotConnected,
        Unknown
    }

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
        public string BackingIsoFileName => BackingIsoFile?.Split("/").TrimOrNull().WhereNotNull().LastOrDefault();
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

    public enum VMPowerState
    {
        Unknown,
        PoweredOff,
        PoweredOn,
        Suspended
    }

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

    public VMwareVM(VMwareClient vmware, JToken obj)
    {
        VM = obj.ToString("vm");
        Name = obj.ToString("name");
        MemorySizeMB = obj.ToLong("memory_size_MiB");
        CpuCount = obj.ToInt("cpu_count");
        PowerState = obj.ToPowerState("power_state");

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

        if (obj == null && localFilesystem.Length == 0) { IsVMwareToolsInstalled = false; }
        else { IsVMwareToolsInstalled = true; }

        obj = QueryValueObjectSafe(vmware, $"/rest/vcenter/vm/{VM}/library-item");
        if (obj != null) LibraryItem = obj.ToString("check_out", "library_item");
    }

    public static IEnumerable<VMwareVM> Query(VMwareClient vmware)
    {
        foreach (var obj in vmware.GetValueArray("/rest/vcenter/vm").OrderBy(o => o["name"]?.ToString(), StringComparer.OrdinalIgnoreCase)) yield return new VMwareVM(vmware, obj);
    }

    private static VMwareVM QueryBy(VMwareClient vmware, string fieldName, string fieldValue)
    {
        var obj = vmware
            .GetValueArray("/rest/vcenter/vm")
            .OrderBy(o => o["name"]?.ToString(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(o => o[fieldName]?.ToString() != null && string.Equals(o[fieldName]?.ToString(), fieldValue, StringComparison.OrdinalIgnoreCase));

        if (obj == null) return null;

        return new VMwareVM(vmware, obj);
    }

    public static VMwareVM QueryByName(VMwareClient vmware, string name) => QueryBy(vmware, "name", name);

    public static VMwareVM QueryByVM(VMwareClient vmware, string vm) => QueryBy(vmware, "vm", vm);
}
