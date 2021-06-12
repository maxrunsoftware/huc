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
using System.Collections.Generic;
using System.Linq;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class VMwareVM : VMwareBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Interacts with a specific VM");
            help.AddParameter(nameof(wildcard), "w", "Enables wildcard matching on VM Name with * and ?   USE WITH CAUTION (false)");
            help.AddValue("<VM ID or Name> <Action>");
            help.AddDetail("Use EXTREME CAUTION when enabling wildcard mode. Best to test with Action=None to see which VMs will be affected");
            help.AddDetail("Actions:");
            foreach (var action in Util.GetEnumItems<Action>()) help.AddDetail("  " + action);
        }

        private bool wildcard;

        protected override void ExecuteInternal(VMware vmware)
        {
            var vm = GetArgValueTrimmed(0);
            vm.CheckValueNotNull(nameof(vm), log);

            var actionString = GetArgValueTrimmed(1);
            actionString.CheckValueNotNull(nameof(actionString), log);

            var actionN = Util.GetEnumItemNullable<Action>(actionString);
            if (actionN == null) throw new ArgsException("action", "Invalid action [" + actionString + "] specified");
            var action = actionN.Value;

            wildcard = GetArgParameterOrConfigBool(nameof(wildcard), "w", false);

            var vms = vmware.VMsSlim;

            var vmsAction = new List<VMwareVMSlim>();
            if (wildcard && (vm.Contains('*') || vm.Contains('?')))
            {
                log.Debug("Wildcard matching on " + vm);
                foreach (var v in vms)
                {
                    if (v.Name.EqualsWildcard(vm)) vmsAction.Add(v);
                }
            }
            else
            {
                var foundVM = vms.Where(o => vm.EqualsCaseInsensitive(o.VM)).FirstOrDefault();
                if (foundVM == null) foundVM = vms.Where(o => vm.EqualsCaseInsensitive(o.Name)).FirstOrDefault();
                if (foundVM == null) throw new ArgsException(nameof(vm), "VM not found: " + vm);
                vmsAction.Add(foundVM);
            }

            foreach (var v in vmsAction)
            {
                if (action == Action.None) log.Info("Doing nothing for " + v.Name);
                else if (action == Action.Shutdown) { log.Info("Shutting down guest operating system " + v.Name); v.Shutdown(vmware); }
                else if (action == Action.Reboot) { log.Info("Rebooting guest operating system " + v.Name); v.Reboot(vmware); }
                else if (action == Action.Standby) { log.Info("Standby guest operating system " + v.Name); v.Standby(vmware); }
                else if (action == Action.Reset) { log.Info("Resetting " + v.Name); v.Reset(vmware); }
                else if (action == Action.Start) { log.Info("Starting " + v.Name); v.Start(vmware); }
                else if (action == Action.Stop) { log.Info("Stopping " + v.Name); v.Stop(vmware); }
                else if (action == Action.Suspend) { log.Info("Suspending " + v.Name); v.Suspend(vmware); }
                else if (action == Action.DetachISOs) { log.Info("Detching ISOs " + v.Name); v.DetachISOs(vmware); }
                else throw new NotImplementedException(nameof(Action) + " [" + action + "] has not been implemented yet");
            }
        }

        public enum Action { None, Shutdown, Reboot, Standby, Reset, Start, Stop, Suspend, DetachISOs }
    }



}
