/*
Copyright (c) 2020 Steven Foster (steven.d.foster@gmail.com)

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

namespace HavokMultimedia.Utilities.Console.External
{
    public class ActiveDirectoryObjectCache
    {
        private static readonly IEnumerable<ActiveDirectoryObject> EMPTY = Enumerable.Empty<ActiveDirectoryObject>();
        private static readonly ILogger log = Program.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Dictionary<string, List<ActiveDirectoryObject>> cache = new Dictionary<string, List<ActiveDirectoryObject>>(StringComparer.OrdinalIgnoreCase);

        public void Add(string filter, IEnumerable<ActiveDirectoryObject> activeDirectoryObjects, LdapQueryConfig queryConfig)
        {
            var hc = queryConfig.GetHashCode().ToString();
            var queryKey = (filter ?? string.Empty) + hc;
            var objs = cache[queryKey] = activeDirectoryObjects.OrEmpty().ToList();

            foreach (var obj in objs)
            {
                var l = new List<ActiveDirectoryObject>(1) { obj };

                var distinguishedName = obj.DistinguishedName;
                if (distinguishedName != null)
                {
                    var key = nameof(ActiveDirectoryObject.DistinguishedName);
                    var val = distinguishedName;
                    cache[$"({key}={val})" + hc] = l;
                }

                var objectGuid = obj.ObjectGUID;
                if (!objectGuid.Equals(Guid.Empty))
                {
                    var key = nameof(ActiveDirectoryObject.ObjectGUID);
                    var val = Ldap.Guid2String(objectGuid);
                    cache[$"({key}={val})" + hc] = l;
                }

                var samAccountName = obj.SAMAccountName;
                if (samAccountName != null)
                {
                    var key = nameof(ActiveDirectoryObject.SAMAccountName);
                    var val = samAccountName;
                    cache[$"({key}={val})" + hc] = l;
                }
            }
        }

        public IEnumerable<ActiveDirectoryObject> Get(string filter, LdapQueryConfig queryConfig)
        {
            var hc = queryConfig.GetHashCode().ToString();
            var queryKey = (filter ?? string.Empty) + hc;

            if (cache.TryGetValue(queryKey, out var l))
            {
                return l;
            }
            return null;
        }

        public void Clear() => cache.Clear();
    }
}
















