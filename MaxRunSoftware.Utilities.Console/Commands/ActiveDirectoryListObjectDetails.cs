﻿// Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)
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

public class ActiveDirectoryListObjectDetails : ActiveDirectoryBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Lists object details of an object in an ActiveDirectory");
        help.AddValue("<object name>");
        help.AddExample(HelpExamplePrefix + " Administrator");
        help.AddExample(HelpExamplePrefix + " Users");
    }

    protected override void ExecuteInternal(ActiveDirectory ad)
    {
        var objectName = GetArgValueTrimmed(0);
        objectName.CheckValueNotNull(nameof(objectName), log);

        var activeDirectoryObjects = new List<ActiveDirectoryObject>();
        foreach (var activeDirectoryObject in ad.GetAll())
        {
            var propertiesToMatch = new List<string>
            {
                activeDirectoryObject.SAMAccountName,
                activeDirectoryObject.LogonName,
                activeDirectoryObject.LogonNamePreWindows2000,
                //activeDirectoryObject.DistinguishedName,
                activeDirectoryObject.Name,
                activeDirectoryObject.DisplayName,
                activeDirectoryObject.DisplayNamePrintable
            };

            if (!objectName.Contains('*') && !objectName.Contains("?"))
                // don't search DistinguishedName on wildcard
            {
                propertiesToMatch.Add(activeDirectoryObject.DistinguishedName);
            }

            foreach (var propertyToMatch in propertiesToMatch.TrimOrNull().WhereNotNull())
            {
                if (propertyToMatch.EqualsWildcard(objectName, true))
                {
                    activeDirectoryObjects.Add(activeDirectoryObject);
                    break;
                }
            }
        }

        if (activeDirectoryObjects.IsEmpty()) { log.Info("Object " + objectName + " not found"); }
        else
        {
            if (activeDirectoryObjects.Count > 1)
            {
                log.Info("Found " + activeDirectoryObjects.Count + " objects with name " + objectName);
                log.Info("");
            }

            foreach (var activeDirectoryObject in activeDirectoryObjects)
            {
                if (activeDirectoryObjects.Count > 1) log.Info(" ---  " + activeDirectoryObject.DistinguishedName + "  --- ");

                foreach (var kvp in activeDirectoryObject.GetPropertiesStrings().OrderBy(o => o.Key, StringComparer.OrdinalIgnoreCase)) log.Info(kvp.Key + ": " + kvp.Value);

                log.Info("");
            }
        }
    }
}
