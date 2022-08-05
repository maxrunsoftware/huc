// Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)
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

namespace MaxRunSoftware.Utilities.External;

public class ActiveDirectoryObjectCache
{
    //private static readonly IEnumerable<ActiveDirectoryObject> empty = Enumerable.Empty<ActiveDirectoryObject>();
    private static readonly ILogger log = Logging.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

    private readonly Dictionary<string, List<ActiveDirectoryObject>> cache = new(StringComparer.OrdinalIgnoreCase);

    public void Add(string filter, IEnumerable<ActiveDirectoryObject> activeDirectoryObjects, LdapQueryConfig queryConfig)
    {
        var hc = queryConfig.GetHashCode().ToString();
        var queryKey = (filter ?? string.Empty) + hc;
        var objs = activeDirectoryObjects.OrEmpty().ToList();
        Add(queryKey, objs);

        foreach (var obj in objs)
        {
            var distinguishedName = obj.DistinguishedName;
            if (distinguishedName != null)
            {
                var key = nameof(ActiveDirectoryObject.DistinguishedName);
                var val = distinguishedName;
                Add($"({key}={val})" + hc, obj);
            }

            var objectGuid = obj.ObjectGUID;
            if (!objectGuid.Equals(Guid.Empty))
            {
                var key = nameof(ActiveDirectoryObject.ObjectGUID);
                var val = Ldap.Guid2String(objectGuid);
                Add($"({key}={val})" + hc, obj);
            }

            var samAccountName = obj.SAMAccountName;
            if (samAccountName != null)
            {
                var key = nameof(ActiveDirectoryObject.SAMAccountName);
                var val = samAccountName;
                Add($"({key}={val})" + hc, obj);
            }
        }
    }

    private void Add(string cacheKey, List<ActiveDirectoryObject> objects)
    {
        if (objects.Count == 0) return;

        if (objects.Count == 1) { log.Trace($"Cache[{cacheKey}]: " + objects.First()); }
        else
        {
            for (var i = 0; i < objects.Count; i++) log.Trace($"Cache[{cacheKey}][{i}]: " + objects[i]);
        }

        cache[cacheKey] = objects;
    }

    private void Add(string cacheKey, ActiveDirectoryObject obj) => Add(cacheKey, new List<ActiveDirectoryObject>(1) { obj });

    public IEnumerable<ActiveDirectoryObject> Get(string filter, LdapQueryConfig queryConfig)
    {
        var hc = queryConfig.GetHashCode().ToString();
        var queryKey = (filter ?? string.Empty) + hc;

        if (cache.TryGetValue(queryKey, out var l)) return l;

        return null;
    }

    public void Clear() => cache.Clear();
}
