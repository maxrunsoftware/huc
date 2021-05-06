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

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class ActiveDirectoryListObjectDetails : ActiveDirectoryBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Lists object details of an object in an ActiveDirectory");
            help.AddValue("<object name>");
            help.AddExample("-h=192.168.1.5 -u=administrator -p=testpass Administrator");
            help.AddExample("-h=192.168.1.5 -u=administrator -p=testpass Users");
        }

        protected override void ExecuteInternal()
        {
            base.ExecuteInternal();

            var objectName = GetArgValueTrimmed(0);
            log.Debug(nameof(objectName) + ": " + objectName);
            if (objectName == null) throw new ArgsException(nameof(objectName), "No <" + nameof(objectName) + "> specified to search for");

            using (var ad = GetActiveDirectory())
            {
                var adobjs = ad.GetAll().OrEmpty()
                    .Where(o => objectName.EqualsCaseInsensitive(new string[] {
                        o.SAMAccountName,
                        o.LogonName,
                        o.LogonNamePreWindows2000,
                        o.DistinguishedName,
                        o.Name
                    }))
                    .ToList();


                if (adobjs.IsEmpty())
                {
                    log.Info("Object " + objectName + " not found");
                }
                else
                {
                    if (adobjs.Count > 1)
                    {
                        log.Info("Found " + adobjs.Count + " objects with name " + objectName);
                        log.Info("");
                    }
                    foreach (var adobj in adobjs)
                    {
                        if (adobjs.Count > 1) log.Info(" ---  " + adobj.DistinguishedName + "  --- ");
                        foreach (var kvp in adobj.GetPropertiesStrings().OrderBy(o => o.Key, StringComparer.OrdinalIgnoreCase))
                        {
                            log.Info(kvp.Key + ": " + kvp.Value);
                        }
                        log.Info("");
                    }

                }

            }
        }
    }
}
