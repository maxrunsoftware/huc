﻿/*
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
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public abstract class VMwareListBase<T> : VMwareBase where T : VMwareObject
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Lists the " + typeof(T).NameFormatted().Substring(6) + "s in a VMware VCenter");
        }

        protected abstract Func<VMware, IEnumerable<T>> GetObjectsFunc { get; }
        protected override void ExecuteInternal(VMware vmware)
        {
            foreach (var o in GetObjectsFunc(vmware))
            {
                log.Info(o.ToString());
                log.Info("");
            }
        }
    }

}
