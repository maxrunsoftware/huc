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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    [SuppressBanner]
    public abstract class ActiveDirectoryListBase : ActiveDirectoryBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddParameter(nameof(propertiesToInclude), "pi", "Comma or space seperated list of which LDAP properties to include (" + DefaultColumnsToInclude.ToStringDelimited(",") + ")");
            help.AddDetail("Output is tab delimited");
            help.AddDetail("LDAP Properties Available:");
            var lines = ActiveDirectoryObject.GetPropertyNames(includeExpensiveObjects: false)
                .OrderBy(o => o, StringComparer.OrdinalIgnoreCase)
                .ToStringsColumns(4);
            foreach (var line in lines) help.AddDetail("  " + line);
        }

        private IList<string> propertiesToInclude;

        protected override void ExecuteInternal(ActiveDirectory ad)
        {
            propertiesToInclude = GetArgParameterOrConfig(nameof(propertiesToInclude), "pi", DefaultColumnsToInclude.ToStringDelimited(","))
                .Split(new char[] { ',', ' ' }).TrimOrNull().WhereNotNull();
            log.Debug(propertiesToInclude, nameof(propertiesToInclude));
            var allPropertyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in ActiveDirectoryObject.GetPropertyNames()) allPropertyNames[p] = p;

            var properties = new List<string>();
            foreach (var allPropertyName in allPropertyNames.Keys)
            {
                foreach (var propertyToInclude in propertiesToInclude)
                {
                    if (allPropertyName.EqualsWildcard(propertyToInclude, true)) properties.Add(allPropertyName);
                }
            }
            properties = properties.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var objects = ad.GetAll().OrEmpty();
            log.Info(properties.ToStringDelimited("\t"));
            foreach (var obj in objects.OrderBy(o => o.DistinguishedName, StringComparer.OrdinalIgnoreCase))
            {
                if (IsValidObject(obj))
                {
                    var propValues = obj.GetPropertiesStrings();
                    var sb = new StringBuilder();
                    var first = true;
                    foreach (var property in properties)
                    {
                        if (first) first = false;
                        else sb.Append("\t");
                        if (propValues.TryGetValue(property, out var val)) sb.Append(val ?? string.Empty);
                    }

                    log.Info(sb.ToString());
                }
            }
        }

        public virtual string[] DefaultColumnsToInclude => new string[] {
        nameof(ActiveDirectoryObject.ObjectName),
        nameof(ActiveDirectoryObject.DistinguishedName)
        };



        protected abstract bool IsValidObject(ActiveDirectoryObject obj);
    }
}
