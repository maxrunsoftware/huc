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
using System.Collections.Generic;
using System.Linq;
using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class VMwareVM : VMwareBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Interacts with a specific VM");
        help.AddParameter(nameof(wildcard), "w", "Enables wildcard matching on VM Name with * and ?   USE WITH CAUTION (false)");
        help.AddValue("<VM ID or Name> <Action>");
        help.AddDetail("Use CAUTION when enabling wildcard mode. Best to test with Action=None to see which VMs will be affected");
        help.AddDetail("Actions:");
        var maxLength = Util.GetEnumItems<Action>().Select(o => o.ToString().Length).Max();
        foreach (var action in Util.GetEnumItems<Action>()) help.AddDetail("  " + action.ToString().PadRight(maxLength) + "  " + DescriptionAttribute.Get(action).Description);

        help.AddExample(HelpExamplePrefix + " MyVM1 " + Action.Reboot);
        help.AddExample(HelpExamplePrefix + " vm-1394 Suspend" + Action.Shutdown);
        help.AddExample(HelpExamplePrefix + " -w MyVM? " + Action.None);
    }

    private bool wildcard;


    protected override void ExecuteInternal(VMwareClient vmware)
    {
        var vm = GetArgValueTrimmed(0);
        vm.CheckValueNotNull(nameof(vm), log);

        var actionString = GetArgValueTrimmed(1);
        actionString.CheckValueNotNull(nameof(actionString), log);

        var actionN = Util.GetEnumItemNullable<Action>(actionString);
        if (actionN == null) throw new ArgsException("action", "Invalid action [" + actionString + "] specified");

        var action = actionN.Value;

        wildcard = GetArgParameterOrConfigBool(nameof(wildcard), "w", false);

        var vms = vmware.VMsSlim.ToList();

        var vmsAction = new List<VMwareVMSlim>();
        if (wildcard && (vm.Contains('*') || vm.Contains('?')))
        {
            log.Debug("Wildcard matching on " + vm);
            foreach (var v in vms)
                if (v.Name.EqualsWildcard(vm))
                    vmsAction.Add(v);
        }
        else
        {
            var foundVM = vms.FirstOrDefault(o => vm.EqualsCaseInsensitive(o.VM)) ?? vms.FirstOrDefault(o => vm.EqualsCaseInsensitive(o.Name));
            if (foundVM == null) throw new ArgsException(nameof(vm), "VM not found: " + vm);

            vmsAction.Add(foundVM);
        }

        foreach (var v in vmsAction)
        {
            log.Info(DescriptionAttribute.Get(action).Message + v.Name);
            if (action == Action.None)
                log.Debug("Doing nothing");
            else if (action == Action.Shutdown)
                v.Shutdown(vmware);
            else if (action == Action.Reboot)
                v.Reboot(vmware);
            else if (action == Action.Standby)
                v.Standby(vmware);
            else if (action == Action.Reset)
                v.Reset(vmware);
            else if (action == Action.Start)
                v.Start(vmware);
            else if (action == Action.Stop)
                v.Stop(vmware);
            else if (action == Action.Suspend)
                v.Suspend(vmware);
            else if (action == Action.DetachISOs)
                v.DetachISOs(vmware);
            else
                throw new NotImplementedException(nameof(Action) + " [" + action + "] has not been implemented yet");
        }
    }


    private class DescriptionAttribute : Attribute
    {
        public string Description { get; }
        public string Message { get; }

        public DescriptionAttribute(string description, string message)
        {
            Description = description;
            Message = message;
        }

        public static DescriptionAttribute Get(Action action)
        {
            // https://stackoverflow.com/a/1799401
            var enumType = typeof(Action);
            var memberInfos = enumType.GetMember(action.ToString());
            var enumValueMemberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == enumType);
            if (enumValueMemberInfo == null) throw new NotImplementedException("Could not find " + enumType);

            var valueAttributes = enumValueMemberInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
            var attr = (DescriptionAttribute)valueAttributes[0];
            return attr;
        }
    }

    private enum Action
    {
        [Description("Does nothing", "Doing nothing for ")]
        None,

        [Description("Shuts down the guest operating system", "Shutting down guest operating system ")]
        Shutdown,

        [Description("Reboots the guest operating system", "Rebooting guest operating system ")]
        Reboot,

        [Description("Standby the guest operating system", "Standby guest operating system ")]
        Standby,

        [Description("Does a hard reset on the VM", "Resetting ")]
        Reset,

        [Description("Starts a stopped or suspended VM", "Starting ")]
        Start,

        [Description("Does a hard power off on the VM", "Stopping ")]
        Stop,

        [Description("Suspends/Pauses the VM", "Suspending ")]
        Suspend,

        [Description("Detaches any ISO files currently attached to the VM", "Detaching ISOs ")]
        DetachISOs
    }
}
