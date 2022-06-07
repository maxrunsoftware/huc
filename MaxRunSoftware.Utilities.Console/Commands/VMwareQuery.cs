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

using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    [SuppressBanner]
    public class VMwareQuery : VMwareBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Queries a VMware path for REST data");
            help.AddValue("<VMware query path>");
            help.AddExample(HelpExamplePrefix + " /rest/vcenter/vm");
            help.AddDetail("https://developer.vmware.com/docs/vsphere-automation/latest/vcenter/index.html");
        }

        protected override void ExecuteInternal(VMwareClient vmware)
        {
            var path = GetArgValueTrimmed(0);
            path.CheckValueNotNull(nameof(path), log);

            var obj = vmware.Get(path);
            var json = VMwareClient.FormatJson(obj);
            log.Info(json);
        }
    }
}
