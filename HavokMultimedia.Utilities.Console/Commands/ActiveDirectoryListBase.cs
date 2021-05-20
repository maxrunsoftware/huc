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
    public abstract class ActiveDirectoryListBase : ActiveDirectoryBase
    {
        protected enum Format { Display, TAB }
        protected abstract string Summary { get; }
        protected virtual string Example => "-h=192.168.1.5 -u=administrator -p=testpass";
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary(Summary);
            help.AddExample(Example);
            help.AddParameter("format", "f", "Format of the data (" + nameof(Format.Display) + ")  " + DisplayEnumOptions<Format>());
        }



        protected override void ExecuteInternal()
        {
            base.ExecuteInternal();
            var format = GetArgParameterOrConfigEnum("format", "f", Format.Display);

            using (var ad = GetActiveDirectory())
            {
                var objects = ad.GetAll().OrEmpty();
                foreach (var obj in objects.OrderBy(o => o.DistinguishedName, StringComparer.OrdinalIgnoreCase))
                {
                    if (IsValidObject(obj)) log.Info(Display(obj, format));
                }
            }

        }

        protected virtual string Display(ActiveDirectoryObject obj, Format format)
        {
            if (format == Format.Display) return ObjName(obj) + "  -->  " + obj.DistinguishedName;
            if (format == Format.TAB) return ObjName(obj) + "\t" + obj.DistinguishedName;
            throw new NotImplementedException($"format [{format}] is not implemented");
        }

        protected string ObjName(ActiveDirectoryObject obj) => obj.LogonNamePreWindows2000 ?? obj.LogonName ?? obj.Name;

        protected abstract bool IsValidObject(ActiveDirectoryObject obj);
    }
}
