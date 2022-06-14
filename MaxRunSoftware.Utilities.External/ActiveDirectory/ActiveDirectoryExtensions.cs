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

using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.Protocols;
using System.Linq;

namespace MaxRunSoftware.Utilities.External;

public static class ActiveDirectoryExtensions
{
    public static void Add(this List<DirectoryAttribute> list, string name, params string[] values)
    {
        var attrValues = values.OrEmpty().TrimOrNull().WhereNotNull().ToArray();
        if (attrValues.Length < 1)
        {
            return;
        }

        list.Add(attrValues.Length == 1 ? new DirectoryAttribute(name, attrValues[0]) : new DirectoryAttribute(name, attrValues.Cast<object>().ToArray()));
    }

    public static List<DirectoryAttribute> ToDirectoryAttributes(this IDictionary<string, List<string>> directoryAttributes)
    {
        var list = new List<DirectoryAttribute>();
        foreach (var kvp in directoryAttributes)
        {
            list.Add(kvp.Key, kvp.Value.ToArray());
        }

        return list;
    }

    private static T FindBySamAccountName<T>(T principal, string samAccountName) where T : Principal
    {
        var s = new PrincipalSearcher();
        principal.SamAccountName = samAccountName;
        s.QueryFilter = principal;
        foreach (var p in s.FindAll())
        {
            if (p is T pp)
            {
                return pp;
            }
        }

        return null;
    }

    public static UserPrincipal FindUserBySamAccountName(this PrincipalContext context, string samAccountName)
    {
        return FindBySamAccountName(new UserPrincipal(context), samAccountName);
    }

    public static UserPrincipal FindUserBySamAccountNameRequired(this PrincipalContext context, string samAccountName)
    {
        var p = FindUserBySamAccountName(context, samAccountName);
        if (p == null)
        {
            throw new Exception($"User {samAccountName} not found");
        }

        return p;
    }

    public static GroupPrincipal FindGroupBySamAccountName(this PrincipalContext context, string samAccountName)
    {
        return FindBySamAccountName(new GroupPrincipal(context), samAccountName);
    }

    public static GroupPrincipal FindGroupBySamAccountNameRequired(this PrincipalContext context, string samAccountName)
    {
        var p = FindGroupBySamAccountName(context, samAccountName);
        if (p == null)
        {
            throw new Exception($"Group {samAccountName} not found");
        }

        return p;
    }

    public static ComputerPrincipal FindComputerBySamAccountName(this PrincipalContext context, string samAccountName)
    {
        return FindBySamAccountName(new ComputerPrincipal(context), samAccountName);
    }

    public static ComputerPrincipal FindComputerBySamAccountNameRequired(this PrincipalContext context, string samAccountName)
    {
        var p = FindComputerBySamAccountName(context, samAccountName);
        if (p == null)
        {
            throw new Exception($"Computer {samAccountName} not found");
        }

        return p;
    }

    /// <summary>
    /// Gets all objects in the Active Directory.
    /// </summary>
    /// <returns>A list of all objects in the Active Directory.</returns>
    public static List<ActiveDirectoryObject> GetAll(this ActiveDirectoryCore ad, LdapQueryConfig queryConfig = null, bool useCache = false)
    {
        return ad.GetObjects(null, queryConfig, useCache);
    }

    /// <summary>
    /// Gets all users in the Active Directory.
    /// </summary>
    /// <returns>A list of all users in the Active Directory.</returns>
    public static List<ActiveDirectoryObject> GetUsers(this ActiveDirectoryCore ad, LdapQueryConfig queryConfig = null, bool useCache = false)
    {
        return ad.GetObjects("(&(objectCategory=person)(objectClass=user))", queryConfig, useCache);
    }

    /// <summary>
    /// Gets all OUs in the Active Directory.
    /// </summary>
    /// <returns>A list of all OUs in the Active Directory.</returns>
    public static List<ActiveDirectoryObject> GetOUs(this ActiveDirectoryCore ad, LdapQueryConfig queryConfig = null, bool useCache = false)
    {
        return ad.GetObjects("(objectCategory=organizationalUnit)", queryConfig, useCache);
    }

    /// <summary>
    /// Gets a specific OU in the Active Directory.
    /// </summary>
    /// <returns>A list of all OUs in the Active Directory.</returns>
    public static ActiveDirectoryObject GetOUByName(this ActiveDirectoryCore ad, string ouName, LdapQueryConfig queryConfig = null, bool useCache = false)
    {
        return GetOUs(ad, queryConfig, useCache).Where(o => o.Name.TrimOrNull() != null).FirstOrDefault(o => o.Name.TrimOrNull().EqualsCaseInsensitive(ouName));
    }

    /// <summary>
    /// Gets a specific OU in the Active Directory.
    /// </summary>
    /// <returns>A list of all OUs in the Active Directory.</returns>
    public static ActiveDirectoryObject GetOUByDistinguishedName(this ActiveDirectoryCore ad, string ouDistinguishedName, LdapQueryConfig queryConfig = null, bool useCache = false)
    {
        return GetOUs(ad, queryConfig, useCache).Where(o => o.DistinguishedName.TrimOrNull() != null).FirstOrDefault(o => o.Name.TrimOrNull().EqualsCaseInsensitive(ouDistinguishedName));
    }

    /// <summary>
    /// Gets of all user accounts that were modified within the specified time frame.
    /// </summary>
    /// <param name="ad">The ActiveDirectoryCore object to query against</param>
    /// <param name="startDate">The lower boundary of the time frame.</param>
    /// <param name="endDate">The upper boundary of the time frame.</param>
    /// <param name="queryConfig">Configuration to use for the query</param>
    /// <param name="useCache">Whether to query the cached objects instead of live objects</param>
    /// <returns>Returns a list of all users that were during the specified period of time.</returns>
    // ReSharper disable once UseStringInterpolation
    // ReSharper disable StringLiteralTypo
    public static List<ActiveDirectoryObject> GetUsersByModified(this ActiveDirectoryCore ad, DateTime startDate, DateTime endDate, LdapQueryConfig queryConfig = null, bool useCache = false)
    {
        return ad.GetObjects(string.Format("(&(objectCategory=person)(objectClass=user)(whenChanged>={0})(whenChanged<={1}))", startDate.ToUniversalTime().ToString("yyyyMMddHHmmss.s") + "Z", endDate.ToUniversalTime().ToString("yyyyMMddHHmmss.s") + "Z"), queryConfig, useCache);
    }
    // ReSharper restore StringLiteralTypo

    /// <summary>
    /// Gets of all user accounts that lastLogonTimestamp is between a specific date
    /// </summary>
    /// <param name="ad">The ActiveDirectoryCore object to query against</param>
    /// <param name="startDate">The lower boundary of the time frame.</param>
    /// <param name="endDate">The upper boundary of the time frame.</param>
    /// <param name="queryConfig">Configuration to use for the query</param>
    /// <param name="useCache">Whether to query the cached objects instead of live objects</param>
    /// <returns>Returns a list of all users that were during the specified period of time.</returns>
    // ReSharper disable once UseStringInterpolation
    public static List<ActiveDirectoryObject> GetUsersByLastLogonTimestamp(this ActiveDirectoryCore ad, DateTime startDate, DateTime endDate, LdapQueryConfig queryConfig = null, bool useCache = false)
    {
        return ad.GetObjects(string.Format("(&(objectCategory=person)(objectClass=user)(lastLogonTimestamp>={0})(lastLogonTimestamp<={1}))", startDate.ToFileTimeUtc(), endDate.ToFileTimeUtc()), queryConfig, useCache);
    }

    /// <summary>
    /// Gets of all user accounts that lastLogonTimestamp is between a specific date
    /// </summary>
    /// <returns>Returns a list of all users that were during the specified period of time.</returns>
    // ReSharper disable StringLiteralTypo
    public static List<ActiveDirectoryObject> GetUsersByLastLogonTimestampNull(this ActiveDirectoryCore ad, LdapQueryConfig queryConfig = null, bool useCache = false)
    {
        return ad.GetObjects("(&(objectCategory=person)(objectClass=user)(!lastlogontimestamp=*))", queryConfig, useCache);
    }
    // ReSharper restore StringLiteralTypo

    /// <summary>
    /// Gets all computers in the Active Directory.
    /// </summary>
    /// <returns>A list of all computers in the Active Directory.</returns>
    public static List<ActiveDirectoryObject> GetComputers(this ActiveDirectoryCore ad, LdapQueryConfig queryConfig = null, bool useCache = false)
    {
        return ad.GetObjects("(&(objectCategory=computer)(objectClass=computer))", queryConfig, useCache);
    }

    /// <summary>
    /// Gets all groups in the Active Directory.
    /// </summary>
    /// <returns>A list of all groups in the Active Directory.</returns>
    public static List<ActiveDirectoryObject> GetGroups(this ActiveDirectoryCore ad, LdapQueryConfig queryConfig = null, bool useCache = false)
    {
        return ad.GetObjects("(objectClass=group)", queryConfig, useCache);
    }

    /// <summary>
    /// Gets all empty groups in the Active Directory.
    /// </summary>
    /// <returns>A list of all groups in the Active Directory.</returns>
    public static List<ActiveDirectoryObject> GetGroupsEmpty(this ActiveDirectoryCore ad, LdapQueryConfig queryConfig = null, bool useCache = false)
    {
        return ad.GetObjects("(&(objectClass=group)(!member=*))", queryConfig, useCache);
    }


    public static string DistinguishedName(this DirectoryEntry entry)
    {
        return entry.GetString("distinguishedName");
    }

    public static string GetString(this DirectoryEntry entry, string key)
    {
        return entry.GetStrings(key).TrimOrNull().WhereNotNull().FirstOrDefault();
    }

    public static IEnumerable<string> GetStrings(this DirectoryEntry entry, string key)
    {
        return entry.GetObjects(key).Select(o => o.ToStringGuessFormat());
    }

    public static IEnumerable<object> GetObjects(this DirectoryEntry entry, string key)
    {
        if (entry.Properties.Contains(key))
        {
            var properties = entry.Properties[key];
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    if (property != null)
                    {
                        yield return property;
                    }
                }
            }
        }
    }
}
