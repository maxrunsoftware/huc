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
using System.Security.Cryptography;
using System.Security.Principal;

namespace HavokMultimedia.Utilities.Console.External
{
    /// <summary>
    /// ActiveDirectory is a class that allows for the query and manipulation
    /// of Active Directory objects.
    /// </summary>
    public class ActiveDirectory : IDisposable
    {
        private readonly ActiveDirectoryObjectCache cache = new ActiveDirectoryObjectCache();
        protected static readonly ILogger log = Program.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string username;
        private string password;
        private string ipaddress;

        /// <summary>
        /// The default first site name in Active Directory.
        /// </summary>
        public const string DEFAULT_FIRST_SITE_NAME = "Default-First-Site-Name";

        /// <summary>
        /// The maximum number of characters supported for a group's name in Active Directory.
        /// </summary>
        public const int GROUP_NAME_MAX_CHARS = 63;

        /// <summary>
        /// The size of page to use when searching Active Directory. This number is based upon hardcoded Microsoft limits within Active Directory's architecture.
        /// </summary>
        public const int PAGE_SIZE = 1000;

        /// <summary>
        /// The maximum number of values that can be retrieved from a multi-value attribute in a single search request. Windows 2000 DCs do not support this value and default to a maximum of 1000.
        /// </summary>
        public const int MAX_NUM_MULTIVALUE_ATTRIBUTES = 1500;

        /// <summary>
        /// The object that manages the LDAP connection with the AD controller.
        /// </summary>
        public Ldap Ldap { get; }

        public bool FilterAttributes { get; set; } = false;

        /// <summary>
        /// The base distinguished name (DN) of Active Directory.
        /// </summary>
        public string DistinguishedName => Ldap?.DefaultNamingContext;

        /// <summary>
        /// The domain name of the Active Directory.
        /// </summary>
        public string Name
        {
            get
            {
                var domain = Ldap.EntryGet("(distinguishedName=" + DistinguishedName + ")", new LdapQueryConfig(baseDn: DistinguishedName, searchScope: SearchScope.Base, attributes: "canonicalName".Yield())).FirstOrDefault();
                var canonicalName = domain?.GetString("canonicalName");
                return canonicalName?.Replace("/", "");
            }
        }

        /// <summary>
        /// The NT style domain name of the Active Directory.
        /// </summary>
        public string NTName
        {
            get
            {
                var domain = Ldap.EntryGet("(distinguishedName=" + DistinguishedName + ")", new LdapQueryConfig(baseDn: DistinguishedName, searchScope: SearchScope.Base, attributes: "msDS-PrincipalName".Yield())).FirstOrDefault();
                var ntName = domain?.GetString("msDS-PrincipalName");
                return ntName?.Replace(@"\", "");
            }
        }

        /// <summary>
        /// The SYSTEM sid.
        /// </summary>
        public SecurityIdentifier WellKnownSid_System => new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);

        /// <summary>
        /// The distinguished name of the Administrators group for this domain.
        /// </summary>
        public string AdministratorsGroupDN => "CN=Administrators,CN=Builtin," + DistinguishedName;

        /// <summary>
        /// The distinguished name of the Domain Administrators group for this domain.
        /// </summary>
        public string DomainAdminsGroupDN => "CN=Domain Admins,CN=Users," + DistinguishedName;

        /// <summary>
        /// The distinguished name of the Domain Users group for this domain.
        /// </summary>
        public string DomainUsersGroupDN => "CN=Domain Users,CN=Users," + DistinguishedName;

        public string DomainUsersDN
        {
            get
            {
                var ou = DomainUsersGroupDN;
                var ouList = ou.Split(",").ToList();
                ouList.PopHead();
                ou = ouList.ToStringDelimited(",");
                return ou;
            }
        }

        /// <summary>
        /// The distinguished name of the Enterprise Administrators group for this domain.
        /// </summary>
        public string EnterpriseAdminsGroupDN => "CN=Enterprise Admins,CN=Users," + DistinguishedName;

        /// <summary>
        /// Constructs an Active Directory object with a base of the specified OU. Binds to Active Directory.
        /// </summary>
        /// <param name="server">The DNS style domain name of the Active Directory to connect to.</param>
        /// <param name="userName">The username of the account in AD to use when making the connection.</param>
        /// <param name="password">The password of the account.</param>
        /// <param name="siteName">(Optional)The name of a site in Active Directory to use the domain controllers from. Defaults to DEFAULT_FIRST_SITE_NAME if not supplied.</param>
        /// <param name="ouDn">(Optional)The distinguished name of the OU to use as a base for operations or use DistinguishedName if null.</param>
        /// <param name="ldapEncrypted">(Optional)Whether to use SSL or not for the connection.</param>
        public ActiveDirectory(string server = null, ushort ldapPort = Ldap.LDAP_PORT, string userName = null, string password = null, string siteName = DEFAULT_FIRST_SITE_NAME, string ouDn = null, string domainName = null)
        {
            server = server.TrimOrNull();
            if (server == null)
            {
                using (var domain = System.DirectoryServices.ActiveDirectory.Domain.GetComputerDomain())
                {
                    server = domain.Name.TrimOrNull();
                }
            }
            this.ipaddress = server.CheckNotNull(nameof(server));
            ouDn = ouDn.TrimOrNull();
            this.username = userName = userName.TrimOrNull();
            this.password = password = password.TrimOrNull();
            siteName = siteName.TrimOrNull();
            domainName = domainName.TrimOrNull();

            var domainControllers = new List<string>();
            if (siteName != null) domainControllers = GetSiteDomainControllers(server, siteName); // Get a list of domain controllers from a specific site, if one was supplied.

            if (domainControllers.Count == 0) domainControllers.Add(server); // Create the connection to the domain controller serving the current computer.

            var useLogonCredentials = false;
            if (userName == null) useLogonCredentials = true;

            log.Debug($"Attempting LDAP connection to {domainControllers.FirstOrDefault()}:{ldapPort} with user {userName}");
            try
            {
                Ldap = new Ldap(
                    domainControllers.FirstOrDefault(),
                    ldapPort,
                    authType: AuthType.Negotiate,
                    userName: userName,
                    password: password,
                    domainName: domainName,
                    useLogonCredentials: useLogonCredentials,
                    searchBaseDNdefault: ouDn ?? DistinguishedName
                    );
            }
            catch (Exception)
            {
                log.Error($"Failed LDAP connection to {domainControllers.FirstOrDefault()}:{ldapPort} with user {userName}");
                throw;
            }
            log.Debug($"Success LDAP connection to {domainControllers.FirstOrDefault()}:{ldapPort} with user {userName}");

            log.Debug($"{nameof(DistinguishedName)}: {DistinguishedName}");
            log.Debug($"{nameof(Name)}: {Name}");
            log.Debug($"{nameof(NTName)}: {NTName}");
            log.Debug($"{nameof(WellKnownSid_System)}: {WellKnownSid_System}");
            log.Debug($"{nameof(AdministratorsGroupDN)}: {AdministratorsGroupDN}");
            log.Debug($"{nameof(DomainAdminsGroupDN)}: {DomainAdminsGroupDN}");
            log.Debug($"{nameof(DomainUsersGroupDN)}: {DomainUsersGroupDN}");
            log.Debug($"{nameof(DomainUsersDN)}: {DomainUsersDN}");
            log.Debug($"{nameof(EnterpriseAdminsGroupDN)}: {EnterpriseAdminsGroupDN}");
        }

        #region Methods Instance

        /// <summary>
        /// Appends the distinguished name of this Active Directory domain to the relative path to the root supplied.
        /// </summary>
        /// <param name="pathToRoot">The relative path to the root of this domain.</param>
        /// <returns>The absolute path including this domain's distinguished name. Null if a null string is supplied.</returns>
        public string AppendDistinguishedName(string pathToRoot)
        {
            if (!string.IsNullOrWhiteSpace(pathToRoot))
            {
                // The string is valid. Return the absolute path.
                return pathToRoot + "," + DistinguishedName;
            }
            else
            {
                // The string is null or full of whitespace.
                // Check if the string is empty.
                if (pathToRoot != null)
                {
                    return DistinguishedName;
                }
                return null;
            }
        }

        #region GetObjects

        /// <summary>
        /// Gets an entry given its distinguished name.
        /// </summary>
        /// <param name="distinguishedName">The distinguished name of the entry to get.</param>
        /// <returns>The SearchResultEntry object found, or null if not found.</returns>
        public ActiveDirectoryObject GetObjectByDistinguishedName(string distinguishedName, LdapQueryConfig queryConfig = null, bool useCache = true) => GetObjectsByAttribute("distinguishedName", distinguishedName, queryConfig: queryConfig, useCache: useCache).FirstOrDefault();

        public ActiveDirectoryObject GetObjectBySAMAccountName(string sAMAccountName, LdapQueryConfig queryConfig = null, bool useCache = true) => GetObjectsByAttribute("sAMAccountName", sAMAccountName, queryConfig: queryConfig, useCache: useCache).FirstOrDefault();

        /// <summary>
        /// Gets an entry given its GUID.
        /// </summary>
        /// <param name="objectGuid">The GUID of the entry to get.</param>
        /// <returns>The SearchResultEntry object found, or null if not found.</returns>
        public ActiveDirectoryObject GetObjectByObjectGuid(Guid objectGuid, LdapQueryConfig queryConfig = null, bool useCache = true) => GetObjectsByAttribute("objectGUID", Ldap.Guid2String(objectGuid), queryConfig: queryConfig, useCache: useCache).FirstOrDefault();

        /// <summary>
        /// Gets all entries in a search given an LDAP search filter.
        /// </summary>
        /// <param name="filter">The LDAP search filter string that will find the entries.</param>
        /// <returns>A list of SearchResultEntry objects, or null if not found.</returns>
        public List<ActiveDirectoryObject> GetObjects(string filter, LdapQueryConfig queryConfig = null, bool useCache = true)
        {
            if (queryConfig == null) queryConfig = Ldap.DefaultQueryConfig;

            if (useCache)
            {
                var values = cache.Get(filter, queryConfig);
                if (values != null)
                {
                    var l = values.ToList();
                    log.Trace($"Using cache of [{l.Count}] objects for query: " + filter);
                    return l;
                }
            }

            log.Debug($"Issuing LDAP Query: " + (filter ?? "ALL-OBJECTS") + "  " + queryConfig);
            var attributeCollections = Ldap.EntryGet(filter, queryConfig);
            log.Debug($"Query filter[{filter}]  {queryConfig}  retrieved {attributeCollections.Count} objects");
            var objs = ActiveDirectoryObject.Create(this, attributeCollections).ToList();
            cache.Add(filter, objs, queryConfig);
            return objs;
        }

        /// <summary>
        /// Gets entries that match a given wildcarded (*) attribute value in the supplied attribute.
        /// </summary>
        /// <param name="attributeName">The name of the attribute to search against.</param>
        /// <param name="attributeValue">The value to search for in the attribute.</param>
        /// <returns>The list of SearchResultEntry(s) found, or null if not found.</returns>
        public List<ActiveDirectoryObject> GetObjectsByAttribute(string attributeName, string attributeValue, LdapQueryConfig queryConfig = null, bool useCache = true) => GetObjects("(" + attributeName.CheckNotNullTrimmed(nameof(attributeName)) + "=" + attributeValue.CheckNotNullTrimmed(nameof(attributeValue)) + ")", queryConfig: queryConfig, useCache: useCache);

        #endregion GetObjects

        #region ActiveDirectory specific functions

        #endregion ActiveDirectory specific functions

        #region Queries

        /// <summary>
        /// Gets all objects in the Active Directory.
        /// </summary>
        /// <returns>A list of all objects in the Active Directory.</returns>
        public List<ActiveDirectoryObject> GetAll(LdapQueryConfig queryConfig = null, bool useCache = false) => GetObjects(null, queryConfig: queryConfig, useCache: useCache);

        /// <summary>
        /// Gets all users in the Active Directory.
        /// </summary>
        /// <returns>A list of all users in the Active Directory.</returns>
        public List<ActiveDirectoryObject> GetUsers(LdapQueryConfig queryConfig = null, bool useCache = false) => GetObjects("(&(objectCategory=person)(objectClass=user))", queryConfig: queryConfig, useCache: useCache);

        /// <summary>
        /// Gets all OUs in the Active Directory.
        /// </summary>
        /// <returns>A list of all OUs in the Active Directory.</returns>
        public List<ActiveDirectoryObject> GetOUs(LdapQueryConfig queryConfig = null, bool useCache = false) => GetObjects("(objectCategory=organizationalUnit)", queryConfig: queryConfig, useCache: useCache);

        /// <summary>
        /// Gets of all user accounts that were modified within the specified time frame.
        /// </summary>
        /// <param name="startDate">The lower boundary of the time frame.</param>
        /// <param name="endDate">The upper boundary of the time frame.</param>
        /// <returns>Returns a list of all users that were during the specified period of time.</returns>
        public List<ActiveDirectoryObject> GetUsersByModified(DateTime startDate, DateTime endDate, LdapQueryConfig queryConfig = null, bool useCache = false) => GetObjects(string.Format("(&(objectCategory=person)(objectClass=user)(whenChanged>={0})(whenChanged<={1}))", startDate.ToUniversalTime().ToString("yyyyMMddHHmmss.s") + "Z", endDate.ToUniversalTime().ToString("yyyyMMddHHmmss.s") + "Z"), queryConfig: queryConfig, useCache: useCache);

        /// <summary>
        /// Gets of all user accounts that lastLogonTimestamp is between a specific date
        /// </summary>
        /// <param name="startDate">The lower boundary of the time frame.</param>
        /// <param name="endDate">The upper boundary of the time frame.</param>
        /// <returns>Returns a list of all users that were during the specified period of time.</returns>
        public List<ActiveDirectoryObject> GetUsersByLastLogonTimestamp(DateTime startDate, DateTime endDate, LdapQueryConfig queryConfig = null, bool useCache = false) => GetObjects(string.Format("(&(objectCategory=person)(objectClass=user)(lastLogonTimestamp>={0})(lastLogonTimestamp<={1}))", startDate.ToFileTimeUtc(), endDate.ToFileTimeUtc()), queryConfig: queryConfig, useCache: useCache);

        /// <summary>
        /// Gets of all user accounts that lastLogonTimestamp is between a specific date
        /// </summary>
        /// <returns>Returns a list of all users that were during the specified period of time.</returns>
        public List<ActiveDirectoryObject> GetUsersByLastLogonTimestampNull(LdapQueryConfig queryConfig = null, bool useCache = false) => GetObjects("(&(objectCategory=person)(objectClass=user)(!lastlogontimestamp=*))", queryConfig: queryConfig, useCache: useCache);

        /// <summary>
        /// Gets all computers in the Active Directory.
        /// </summary>
        /// <returns>A list of all computers in the Active Directory.</returns>
        public List<ActiveDirectoryObject> GetComputers(LdapQueryConfig queryConfig = null, bool useCache = false) => GetObjects("(&(objectCategory=computer)(objectClass=computer))", queryConfig: queryConfig, useCache: useCache);

        /// <summary>
        /// Gets all groups in the Active Directory.
        /// </summary>
        /// <returns>A list of all groups in the Active Directory.</returns>
        public List<ActiveDirectoryObject> GetGroups(LdapQueryConfig queryConfig = null, bool useCache = false) => GetObjects("(objectClass=group)", queryConfig: queryConfig, useCache: useCache);

        /// <summary>
        /// Gets all empty groups in the Active Directory.
        /// </summary>
        /// <returns>A list of all groups in the Active Directory.</returns>
        public List<ActiveDirectoryObject> GetGroupsEmpty(LdapQueryConfig queryConfig = null, bool useCache = false) => GetObjects("(&(objectClass=group)(!member=*))", queryConfig: queryConfig, useCache: useCache);

        #endregion Queries

        #region Actions

        private ActiveDirectoryObject AddObject(string sAMAccountName, string ouDistinguishedName, int? groupType)
        {
            sAMAccountName = sAMAccountName.CheckNotNullTrimmed(nameof(sAMAccountName));
            ouDistinguishedName = ouDistinguishedName.CheckNotNullTrimmed(nameof(ouDistinguishedName));

            if (GetObjectByDistinguishedName(ouDistinguishedName) == null) throw new ArgumentException("The OU provided does not exist in Active Directory.");

            var objectDistinguishedName = "CN=" + sAMAccountName + "," + ouDistinguishedName;

            var attributes = new SortedDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            //attributes.AddToList("sAMAccountName", sAMAccountName);

            if (groupType == null) // user
            {
                //attributes.Add("objectClass", "user");
                //attributes.Add("userPrincipalName", sAMAccountName + "@" + Name);
                attributes.AddToList("cn", sAMAccountName);
                //attributes.Add("userPrincipalName", sAMAccountName);
                //attributes.Add("GivenName", sAMAccountName);
                //attributes.AddToList("sn", sAMAccountName);

                attributes.AddToList("uid", sAMAccountName);
                attributes.AddToList("ou", "users");

                attributes.AddToList("objectClass", "top", "account", "simpleSecurityObject");
                var userpassword = "badPassword1!";
                string encodedPassword;
                using (var sha1 = new SHA1Managed())
                {
                    var digest = Convert.ToBase64String(sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(userpassword)));
                    encodedPassword = "{SHA}" + digest;
                }
                attributes.AddToList("userPassword", encodedPassword);

            }
            else // group
            {
                if (!IsGroupNameValid(sAMAccountName)) throw new ArgumentException("The SAM Account Name '" + sAMAccountName + "' is not a valid group name.");
                attributes.AddToList("objectClass", "group");
                //attributes.Add(new DirectoryAttribute("groupType", BitConverter.GetBytes(groupType.Value)));
                attributes.AddToList("groupType", groupType.Value.ToString());
            }


            log.Debug("Ldap.Add(" + objectDistinguishedName + ", [" + attributes.Select(o => o.Key + ":" + o.Value).ToStringDelimited(", ") + "])");
            Ldap.EntryAdd(objectDistinguishedName, attributes.ToDirectoryAttributes().ToArray());

            return GetObjectByDistinguishedName(objectDistinguishedName);
        }

        public bool DeleteObject(ActiveDirectoryObject activeDirectoryObject) => activeDirectoryObject == null ? false : Ldap.EntryDelete(activeDirectoryObject.DistinguishedName);

        /// <summary>
        /// Creates a new group within Active Directory given it's proposed name, the distinguished name of the OU to place it in, and other optional attributes.
        /// </summary>
        /// <param name="sAMAccountName">The proposed SAM Account name for the group.</param>
        /// <param name="ouDistinguishedName">The distinguished name for the OU to place the group within.</param>
        /// <param name="groupType">A uint from the ActiveDirectory.GroupType enum representing the type of group to create.</param>
        /// <returns>The newly created group object.</returns>
        public ActiveDirectoryObject AddGroup(string sAMAccountName, string ouDistinguishedName, ActiveDirectoryGroupType groupType) => AddObject(sAMAccountName, ouDistinguishedName, (int)groupType);

        #region PrincipalContext methods

        private PrincipalContext OpenPrincipalContext()
        {
            return new PrincipalContext(ContextType.Domain, ipaddress, null, ContextOptions.Negotiate, username, password);
        }

        public void AddUserToGroup(string userSamAccountName, string groupSamAccountName)
        {
            using (var context = OpenPrincipalContext())
            {
                var user = context.FindUserBySamAccountNameRequired(userSamAccountName);
                var group = context.FindGroupBySamAccountNameRequired(groupSamAccountName);

                if (group.Members.Contains(context, IdentityType.SamAccountName, user.SamAccountName))
                {
                    log.Debug($"User {userSamAccountName} is already a member of group {groupSamAccountName}");
                }
                else
                {
                    group.Members.Add(user);
                    group.Save();
                }

            }
        }

        public void RemoveUserFromGroup(string userSamAccountName, string groupSamAccountName)
        {
            using (var context = OpenPrincipalContext())
            {
                var user = context.FindUserBySamAccountNameRequired(userSamAccountName);
                var group = context.FindGroupBySamAccountNameRequired(groupSamAccountName);

                if (group.Members.Contains(context, IdentityType.SamAccountName, user.SamAccountName))
                {
                    group.Members.Remove(user);
                    group.Save();
                }
                else
                {
                    log.Debug($"User {userSamAccountName} is not a member of group {groupSamAccountName}");
                }
            }
        }

        public void AddUser(
            string samAccountName,
            string displayName = null,
            string firstName = null,
            string lastName = null,
            string name = null,
            string emailAddress = null
            )
        {
            // https://stackoverflow.com/a/2305871
            using (var pc = OpenPrincipalContext())
            {
                using (var up = new UserPrincipal(pc))
                {
                    up.SamAccountName = samAccountName;
                    up.DisplayName = displayName ?? samAccountName;
                    up.GivenName = firstName ?? samAccountName;
                    up.Surname = lastName ?? samAccountName;
                    up.Name = name ?? samAccountName;
                    if (emailAddress != null) up.EmailAddress = emailAddress;
                    up.Enabled = true;
                    //up.ExpirePasswordNow();
                    up.Save();
                }
            }

            var adobj = GetObjectBySAMAccountName(samAccountName);
            MoveObject(adobj, DomainUsersDN);
        }

        public void AddGroup(string samAccountName, ActiveDirectoryGroupType groupType = ActiveDirectoryGroupType.GlobalSecurityGroup)
        {
            if (!IsGroupNameValid(samAccountName)) throw new Exception($"Groupname {samAccountName} is an invalid name");

            // https://stackoverflow.com/a/2305871
            using (var pc = OpenPrincipalContext())
            {
                using (var up = new GroupPrincipal(pc))
                {
                    up.SamAccountName = samAccountName;
                    if (groupType == ActiveDirectoryGroupType.GlobalDistributionGroup)
                    {
                        up.IsSecurityGroup = false;
                        up.GroupScope = GroupScope.Global;
                    }
                    else if (groupType == ActiveDirectoryGroupType.GlobalSecurityGroup)
                    {
                        up.IsSecurityGroup = true;
                        up.GroupScope = GroupScope.Global;
                    }
                    else if (groupType == ActiveDirectoryGroupType.LocalDistributionGroup)
                    {
                        up.IsSecurityGroup = false;
                        up.GroupScope = GroupScope.Local;
                    }
                    else if (groupType == ActiveDirectoryGroupType.LocalSecurityGroup)
                    {
                        up.IsSecurityGroup = true;
                        up.GroupScope = GroupScope.Local;
                    }
                    else if (groupType == ActiveDirectoryGroupType.UniversalDistributionGroup)
                    {
                        up.IsSecurityGroup = false;
                        up.GroupScope = GroupScope.Universal;
                    }
                    else if (groupType == ActiveDirectoryGroupType.UniversalSecurityGroup)
                    {
                        up.IsSecurityGroup = true;
                        up.GroupScope = GroupScope.Universal;
                    }
                    else
                    {
                        throw new Exception("Unsupported " + nameof(groupType) + " " + groupType);
                    }

                    up.Save();
                }
            }
        }

        public void RemoveUser(string samAccountName)
        {
            using (var context = OpenPrincipalContext())
            {
                var p = context.FindUserBySamAccountNameRequired(samAccountName);
                p.Delete();
            }
        }

        public void RemoveGroup(string samAccountName)
        {
            using (var context = OpenPrincipalContext())
            {
                var p = context.FindGroupBySamAccountNameRequired(samAccountName);
                p.Delete();
            }
        }

        public string GetUserOU(string samAccountName)
        {
            using (var context = OpenPrincipalContext())
            {
                var user = context.FindUserBySamAccountNameRequired(samAccountName);
                var de = user.GetUnderlyingObject() as System.DirectoryServices.DirectoryEntry;
                if (de == null) return null;

                var deParent = de.Parent;
                if (deParent == null) return null;

                return deParent?.Properties["distinguishedName"]?.Value?.ToString();
            }
        }

        public void ChangePassword(string samAccountName, string password)
        {
            using (var context = OpenPrincipalContext())
            {
                var user = context.FindUserBySamAccountNameRequired(samAccountName);

                user.SetPassword(password);
                user.Enabled = true;
                user.UnlockAccount();
                user.Save();
            }
        }

        public void MoveUser(string samAccountName, string newOUSAMAccountName)
        {
            string userDN;
            using (var context = OpenPrincipalContext())
            {
                var user = context.FindUserBySamAccountNameRequired(samAccountName);
                userDN = user.DistinguishedName;
            }

            var userobj = GetObjectBySAMAccountName(samAccountName);
            if (userobj == null) throw new Exception($"Could not locate user object for {userDN}");

            if (newOUSAMAccountName.EqualsCaseInsensitive("Users"))
            {
                MoveObject(userobj, DomainUsersDN);
            }
            else
            {
                var ou = GetOUs().Where(o => newOUSAMAccountName.EqualsCaseInsensitive(o.Name)).FirstOrDefault();
                if (ou == null) throw new Exception($"Could not find OU named {newOUSAMAccountName}");

                log.Debug($"Moving user {userobj.DistinguishedName} to {ou.DistinguishedName}");
                MoveObject(userobj, ou.DistinguishedName);
            }
        }

        public void MoveGroup(string samAccountName, string newOUSAMAccountName)
        {
            string groupDN;
            using (var context = OpenPrincipalContext())
            {
                var group = context.FindGroupBySamAccountNameRequired(samAccountName);
                groupDN = group.DistinguishedName;
            }

            var groupobj = GetObjectBySAMAccountName(samAccountName);
            if (groupobj == null) throw new Exception($"Could not locate group object for {groupDN}");

            if (newOUSAMAccountName.EqualsCaseInsensitive("Users"))
            {
                MoveObject(groupobj, DomainUsersDN);
            }
            else
            {
                var ou = GetOUs().Where(o => newOUSAMAccountName.EqualsCaseInsensitive(o.Name)).FirstOrDefault();
                if (ou == null) throw new Exception($"Could not find OU named {newOUSAMAccountName}");

                log.Debug($"Moving group {groupobj.DistinguishedName} to {ou.DistinguishedName}");
                MoveObject(groupobj, ou.DistinguishedName);
            }
        }

        #endregion PrincipalContext methods

        /// <summary>
        /// Moves and / or renames an object in Active Directory.
        /// </summary>
        /// <param name="activeDirectoryObject">The GUID of the object to move and / or rename.</param>
        /// <param name="parentObjectDistinguishedName">(Optional: Required only if moving) The GUID of the new parent object for the object (if moving).</param>
        /// <returns>True if the object was moved or renamed, false otherwise.</returns>
        public ActiveDirectoryObject MoveObject(ActiveDirectoryObject activeDirectoryObject, string parentObjectDistinguishedName)
        {
            activeDirectoryObject.CheckNotNull(nameof(activeDirectoryObject));
            parentObjectDistinguishedName.CheckNotNullTrimmed(nameof(parentObjectDistinguishedName));
            //if (activeDirectoryObject.ObjectGUID == null) throw new ArgumentException("Cannot move object " + activeDirectoryObject + " because it does not have an " + nameof(ActiveDirectoryObject.ObjectGUID));

            Ldap.EntryMoveRename(activeDirectoryObject.DistinguishedName, parentObjectDistinguishedName, activeDirectoryObject.CN);

            return GetObjectByObjectGuid(activeDirectoryObject.ObjectGUID);
        }

        /// <summary>
        /// Moves and / or renames an object in Active Directory.
        /// </summary>
        /// <param name="activeDirectoryObject">The GUID of the object to move and / or rename.</param>
        /// <param name="newCommonName">The new common name.</param>
        /// <returns>True if the object was moved or renamed, false otherwise.</returns>
        public ActiveDirectoryObject RenameObject(ActiveDirectoryObject activeDirectoryObject, string newCommonName)
        {
            activeDirectoryObject.CheckNotNull(nameof(activeDirectoryObject));
            newCommonName.CheckNotNullTrimmed(nameof(newCommonName));
            //if (activeDirectoryObject.ObjectGUID == null) throw new ArgumentException("Cannot move object " + activeDirectoryObject + " because it does not have an " + nameof(ActiveDirectoryObject.ObjectGUID));

            Ldap.EntryMoveRename(activeDirectoryObject.DistinguishedName, activeDirectoryObject.OrganizationalUnit, newCommonName);

            return GetObjectByObjectGuid(activeDirectoryObject.ObjectGUID);
        }

        #endregion Actions

        #endregion Methods Instance

        #region Methods Static

        public static bool MatchesDN(string dn1, string dn2)
        {
            dn1 = dn1.TrimOrNull();
            dn2 = dn2.TrimOrNull();
            if (dn1 == null || dn2 == null) return false;
            var dn1Parts = dn1.Split(",").TrimOrNull().WhereNotNull().ToArray();
            var dn2Parts = dn2.Split(",").TrimOrNull().WhereNotNull().ToArray();
            if (dn1Parts.Length != dn2Parts.Length) return false;
            if (dn1Parts.Length == 0) return false; // No DN to match
            for (int i = 0; i < dn1Parts.Length; i++)
            {
                if (!string.Equals(dn1Parts[i], dn2Parts[i], StringComparison.OrdinalIgnoreCase)) return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the domain controllers associated with a specific Active Directory site from the Active Directory's DNS SRV records.
        /// </summary>
        /// <param name="domainName">The DNS domain name of the Active Directory to retrieve the domain controllers for.</param>
        /// <param name="siteName">The name of the site to retrieve the domain controllers for.</param>
        /// <returns>A list containing the FQDNs of the domain controllers in the specified site, or an empty list if they could not be retrieved.</returns>
        public static List<string> GetSiteDomainControllers(string domainName, string siteName)
        {
            domainName = domainName.TrimOrNull();
            siteName = siteName.TrimOrNull();
            if (domainName == null || siteName == null) return new List<string>();

            /*
                    DnsQueryRequest request = new DnsQueryRequest();
                    DnsQueryResponse response = request.Resolve("_ldap._tcp." + siteName + "._sites.dc._msdcs." + domainName, NsType.SRV, NsClass.INET, System.Net.Sockets.ProtocolType.Tcp);
                    IDnsRecord[] records = response.Answers;
                    List<string> domainControllers = new List<string>();
                    foreach (IDnsRecord record in records)
                    {
                        domainControllers.Add((record as SrvRecord).HostName);
                    }
                    return domainControllers;
             */
            return new List<string>();
        }

        /// <summary>
        /// Checks whether the group name supplied conforms to the limitations imposed by Active Directory.
        /// Active Directory Group Name Limitations:
        /// 63 character length limit
        /// Can not consist solely of numbers, periods, or spaces.
        /// There must be no leading periods or spaces.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>True if it meets the limitations, false otherwise.</returns>
        public static bool IsGroupNameValid(string name)
        {
            // Check whether the name supplied is valid.
            if (!string.IsNullOrEmpty(name))
            {
                // Check whether the length of the name is less than or equal to 63 characters.
                if (name.Length <= GROUP_NAME_MAX_CHARS)
                {
                    // The name is of an appropriate length.

                    // Check whether the name begins with a period or space.
                    if ((name[0] != ' ') && (name[0] != '.'))
                    {
                        // The name does not begin with a period or space.

                        // Check whether the string contains letters.
                        foreach (var c in name)
                        {
                            if (char.IsLetter(c))
                            {
                                // The name contains a letter and is therefore valid.
                                return true;
                            }
                        }
                    }
                }
            }
            // The name is not valid.
            return false;
        }

        #endregion Methods Static

        #region IDisposable

        /// <summary>
        /// Releases underlying resources associated with the Active Directory connection.
        /// </summary>
        public void Dispose() => Ldap.Dispose();

        #endregion IDisposable
    }



}
















