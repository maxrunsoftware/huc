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
using HavokMultimedia.Utilities;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    [SuppressBanner]
    public class VMwareQuery : VMwareBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Queries a VMware path for REST data");
            help.AddValue("<VMware query path>");
            help.AddExample("/rest/vcenter/vm");
        }

        protected override void ExecuteInternal(VMware vmware)
        {
            var path = GetArgValueTrimmed(0);
            log.Debug($"{nameof(path)}: {path}");
            if (path == null) throw new ArgsException(nameof(path), $"No <{nameof(path)}> specified");

            var obj = vmware.Query(path);
            var json = VMware.ToJson(obj);
            log.Info(json);
        }
    }
}
