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
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.Protocols;
using System.Linq;

namespace MaxRunSoftware.Utilities.Console.External
{
    public static class ActiveDirectoryExtensions
    {
        public static void Add(this List<DirectoryAttribute> list, string name, params string[] values)
        {
            var attrValues = new List<string>();
            foreach (var value in values.OrEmpty()) attrValues.Add(value);
            attrValues = attrValues.TrimOrNull().WhereNotNull().ToList();
            DirectoryAttribute a;
            if (attrValues.Count < 2) a = new DirectoryAttribute(name, attrValues.First());
            else a = new DirectoryAttribute(name, attrValues.ToArray());
            list.Add(a);
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
            PrincipalSearcher s = new PrincipalSearcher();
            principal.SamAccountName = samAccountName;
            s.QueryFilter = principal;
            foreach (Principal p in s.FindAll())
            {
                if (p is T pp) return pp;
            }
            return null;
        }

        public static UserPrincipal FindUserBySamAccountName(this PrincipalContext context, string samAccountName) => FindBySamAccountName(new UserPrincipal(context), samAccountName);
        public static UserPrincipal FindUserBySamAccountNameRequired(this PrincipalContext context, string samAccountName)
        {
            var p = FindUserBySamAccountName(context, samAccountName);
            if (p == null) throw new Exception($"User {samAccountName} not found");
            return p;
        }

        public static GroupPrincipal FindGroupBySamAccountName(this PrincipalContext context, string samAccountName) => FindBySamAccountName(new GroupPrincipal(context), samAccountName);
        public static GroupPrincipal FindGroupBySamAccountNameRequired(this PrincipalContext context, string samAccountName)
        {
            var p = FindGroupBySamAccountName(context, samAccountName);
            if (p == null) throw new Exception($"Group {samAccountName} not found");
            return p;
        }

        public static ComputerPrincipal FindComputerBySamAccountName(this PrincipalContext context, string samAccountName) => FindBySamAccountName(new ComputerPrincipal(context), samAccountName);
        public static ComputerPrincipal FindComputerBySamAccountNameRequired(this PrincipalContext context, string samAccountName)
        {
            var p = FindComputerBySamAccountName(context, samAccountName);
            if (p == null) throw new Exception($"Computer {samAccountName} not found");
            return p;
        }

        /// <summary>
        /// Gets all objects in the Active Directory.
        /// </summary>
        /// <returns>A list of all objects in the Active Directory.</returns>
        public static List<ActiveDirectoryObject> GetAll(this ActiveDirectoryCore ad, LdapQueryConfig queryConfig = null, bool useCache = false) => ad.GetObjects(null, queryConfig: queryConfig, useCache: useCache);

        /// <summary>
        /// Gets all users in the Active Directory.
        /// </summary>
        /// <returns>A list of all users in the Active Directory.</returns>
        public static List<ActiveDirectoryObject> GetUsers(this ActiveDirectoryCore ad, LdapQueryConfig queryConfig = null, bool useCache = false) => ad.GetObjects("(&(objectCategory=person)(objectClass=user))", queryConfig: queryConfig, useCache: useCache);

        /// <summary>
        /// Gets all OUs in the Active Directory.
        /// </summary>
        /// <returns>A list of all OUs in the Active Directory.</returns>
        public static List<ActiveDirectoryObject> GetOUs(this ActiveDirectoryCore ad, LdapQueryConfig queryConfig = null, bool useCache = false) => ad.GetObjects("(objectCategory=organizationalUnit)", queryConfig: queryConfig, useCache: useCache);

        /// <summary>
        /// Gets a specific OU in the Active Directory.
        /// </summary>
        /// <returns>A list of all OUs in the Active Directory.</returns>
        public static ActiveDirectoryObject GetOUByName(this ActiveDirectoryCore ad, string ouName, LdapQueryConfig queryConfig = null, bool useCache = false) => GetOUs(ad, queryConfig, useCache).Where(o => o.Name.TrimOrNull() != null).Where(o => o.Name.TrimOrNull().EqualsCaseInsensitive(ouName)).FirstOrDefault();

        /// <summary>
        /// Gets a specific OU in the Active Directory.
        /// </summary>
        /// <returns>A list of all OUs in the Active Directory.</returns>
        public static ActiveDirectoryObject GetOUByDistinguishedName(this ActiveDirectoryCore ad, string ouDistinguishedName, LdapQueryConfig queryConfig = null, bool useCache = false) => GetOUs(ad, queryConfig, useCache).Where(o => o.DistinguishedName.TrimOrNull() != null).Where(o => o.Name.TrimOrNull().EqualsCaseInsensitive(ouDistinguishedName)).FirstOrDefault();

        /// <summary>
        /// Gets of all user accounts that were modified within the specified time frame.
        /// </summary>
        /// <param name="startDate">The lower boundary of the time frame.</param>
        /// <param name="endDate">The upper boundary of the time frame.</param>
        /// <returns>Returns a list of all users that were during the specified period of time.</returns>
        public static List<ActiveDirectoryObject> GetUsersByModified(this ActiveDirectoryCore ad, DateTime startDate, DateTime endDate, LdapQueryConfig queryConfig = null, bool useCache = false) => ad.GetObjects(string.Format("(&(objectCategory=person)(objectClass=user)(whenChanged>={0})(whenChanged<={1}))", startDate.ToUniversalTime().ToString("yyyyMMddHHmmss.s") + "Z", endDate.ToUniversalTime().ToString("yyyyMMddHHmmss.s") + "Z"), queryConfig: queryConfig, useCache: useCache);

        /// <summary>
        /// Gets of all user accounts that lastLogonTimestamp is between a specific date
        /// </summary>
        /// <param name="startDate">The lower boundary of the time frame.</param>
        /// <param name="endDate">The upper boundary of the time frame.</param>
        /// <returns>Returns a list of all users that were during the specified period of time.</returns>
        public static List<ActiveDirectoryObject> GetUsersByLastLogonTimestamp(this ActiveDirectoryCore ad, DateTime startDate, DateTime endDate, LdapQueryConfig queryConfig = null, bool useCache = false) => ad.GetObjects(string.Format("(&(objectCategory=person)(objectClass=user)(lastLogonTimestamp>={0})(lastLogonTimestamp<={1}))", startDate.ToFileTimeUtc(), endDate.ToFileTimeUtc()), queryConfig: queryConfig, useCache: useCache);

        /// <summary>
        /// Gets of all user accounts that lastLogonTimestamp is between a specific date
        /// </summary>
        /// <returns>Returns a list of all users that were during the specified period of time.</returns>
        public static List<ActiveDirectoryObject> GetUsersByLastLogonTimestampNull(this ActiveDirectoryCore ad, LdapQueryConfig queryConfig = null, bool useCache = false) => ad.GetObjects("(&(objectCategory=person)(objectClass=user)(!lastlogontimestamp=*))", queryConfig: queryConfig, useCache: useCache);

        /// <summary>
        /// Gets all computers in the Active Directory.
        /// </summary>
        /// <returns>A list of all computers in the Active Directory.</returns>
        public static List<ActiveDirectoryObject> GetComputers(this ActiveDirectoryCore ad, LdapQueryConfig queryConfig = null, bool useCache = false) => ad.GetObjects("(&(objectCategory=computer)(objectClass=computer))", queryConfig: queryConfig, useCache: useCache);

        /// <summary>
        /// Gets all groups in the Active Directory.
        /// </summary>
        /// <returns>A list of all groups in the Active Directory.</returns>
        public static List<ActiveDirectoryObject> GetGroups(this ActiveDirectoryCore ad, LdapQueryConfig queryConfig = null, bool useCache = false) => ad.GetObjects("(objectClass=group)", queryConfig: queryConfig, useCache: useCache);

        /// <summary>
        /// Gets all empty groups in the Active Directory.
        /// </summary>
        /// <returns>A list of all groups in the Active Directory.</returns>
        public static List<ActiveDirectoryObject> GetGroupsEmpty(this ActiveDirectoryCore ad, LdapQueryConfig queryConfig = null, bool useCache = false) => ad.GetObjects("(&(objectClass=group)(!member=*))", queryConfig: queryConfig, useCache: useCache);


        public static string DistinguishedName(this System.DirectoryServices.DirectoryEntry entry) => entry.GetString("distinguishedName");
        public static string GetString(this System.DirectoryServices.DirectoryEntry entry, string key) => entry.GetStrings(key).TrimOrNull().WhereNotNull().FirstOrDefault();
        public static IEnumerable<string> GetStrings(this System.DirectoryServices.DirectoryEntry entry, string key) => entry.GetObjects(key).Select(o => o.ToStringGuessFormat());
        public static IEnumerable<object> GetObjects(this System.DirectoryServices.DirectoryEntry entry, string key)
        {
            if (entry.Properties.Contains(key))
            {
                var properties = entry.Properties[key];
                if (properties != null)
                {

                    foreach (var property in properties)
                    {
                        if (property != null) yield return property;
                    }
                }
            }
        }

    }



}
















