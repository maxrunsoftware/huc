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
            help.AddParameter("action", "a", "Action to take (" + nameof(Action.None) + ")  " + DisplayEnumOptions<Action>());
            help.AddParameter("vm", "v", "VM ID or Name");
        }
        protected override void ExecuteInternal(VMware vmware)
        {
            var action = GetArgParameterOrConfigEnum("action", "a", Action.None);
            var vm = GetArgParameterOrConfigRequired("vm", "v");

            var vms = vmware.VMsSlim;

            var foundVM = vms.Where(o => vm.EqualsCaseInsensitive(o.VM)).FirstOrDefault();
            if (foundVM == null) foundVM = vms.Where(o => vm.EqualsCaseInsensitive(o.Name)).FirstOrDefault();
            if (foundVM == null) throw new ArgsException(nameof(vm), "No VM found for " + vm);

            if (action == Action.None) log.Info("Doing nothing");
            else if (action == Action.Shutdown) { log.Info("Shutting down guest operating system " + foundVM.Name); foundVM.Shutdown(vmware); }
            else if (action == Action.Reboot) { log.Info("Rebooting guest operating system " + foundVM.Name); foundVM.Reboot(vmware); }
            else if (action == Action.Standby) { log.Info("Standby guest operating system " + foundVM.Name); foundVM.Standby(vmware); }
            else if (action == Action.Reset) { log.Info("Resetting " + foundVM.Name); foundVM.Reset(vmware); }
            else if (action == Action.Start) { log.Info("Starting " + foundVM.Name); foundVM.Start(vmware); }
            else if (action == Action.Stop) { log.Info("Stopping " + foundVM.Name); foundVM.Stop(vmware); }
            else if (action == Action.Suspend) { log.Info("Suspending " + foundVM.Name); foundVM.Suspend(vmware); }
            else throw new NotImplementedException(nameof(Action) + " [" + action + "] has not been implemented yet");
        }

        public enum Action { None, Shutdown, Reboot, Standby, Reset, Start, Stop, Suspend }
    }



}
