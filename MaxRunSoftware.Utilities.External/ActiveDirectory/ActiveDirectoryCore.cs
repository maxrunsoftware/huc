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
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace MaxRunSoftware.Utilities.External;

/// <summary>
/// ActiveDirectory is a class that allows for the query and manipulation
/// of Active Directory objects.
/// </summary>
public class ActiveDirectoryCore : IDisposable
{
    private readonly ActiveDirectoryObjectCache cache = new();

    // ReSharper disable once InconsistentNaming
    protected static readonly ILogger log = Logging.LogFactory.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

    protected readonly string username;
    protected readonly string password;
    protected readonly string ipaddress;

    /// <summary>
    /// The default first site name in Active Directory.
    /// </summary>
    public const string DEFAULT_FIRST_SITE_NAME = "Default-First-Site-Name";

    /// <summary>
    /// The object that manages the LDAP connection with the AD controller.
    /// </summary>
    public Ldap Ldap { get; }

    public bool FilterAttributes { get; set; } = false;

    /// <summary>
    /// The base distinguished name (DN) of Active Directory.
    /// </summary>
    public string DistinguishedName => Ldap?.DefaultNamingContext;

    public string DomainName
    {
        get
        {
            var dn = DistinguishedName.TrimOrNull();
            if (dn == null)
            {
                return null;
            }

            var dnParts = dn.Split(",");
            var sb = new StringBuilder();
            foreach (var dnPart in dnParts.TrimOrNull().WhereNotNull())
            {
                if (dnPart.Contains('='))
                {
                    var segmentParts = dnPart.Split('=', 2);
                    var namePart = segmentParts[1].TrimOrNull();
                    if (namePart == null)
                    {
                        continue;
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append(".");
                    }

                    sb.Append(namePart);
                }
                else
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(".");
                    }

                    sb.Append(dnPart);
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// The domain name of the Active Directory.
    /// </summary>
    public string Name
    {
        get
        {
            var qc = new LdapQueryConfig(
                DistinguishedName,
                SearchScope.Base,
                attributes: "canonicalName".Yield()
            );
            var domain = Ldap.EntryGet("(distinguishedName=" + DistinguishedName + ")", qc).FirstOrDefault();
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
            var qc = new LdapQueryConfig(
                DistinguishedName,
                SearchScope.Base,
                attributes: "msDS-PrincipalName".Yield()
            );
            var domain = Ldap.EntryGet("(distinguishedName=" + DistinguishedName + ")", qc).FirstOrDefault();
            var ntName = domain?.GetString("msDS-PrincipalName");
            return ntName?.Replace(@"\", "");
        }
    }

    /// <summary>
    /// The SYSTEM sid.
    /// </summary>
    public SecurityIdentifier WellKnownSidSystem => new(WellKnownSidType.LocalSystemSid, null);

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

    /// <summary>
    /// The distinguished name of the Users OU for this domain.
    /// </summary>
    public string DomainUsersDN => DomainUsersGroupDN.Split(",").MinusHead().ToStringDelimited(",");

    /// <summary>
    /// The distinguished name of the Enterprise Administrators group for this domain.
    /// </summary>
    public string EnterpriseAdminsGroupDN => "CN=Enterprise Admins,CN=Users," + DistinguishedName;

    /// <summary>
    /// Constructs an Active Directory object with a base of the specified OU. Binds to Active Directory.
    /// </summary>
    /// <param name="server">The DNS style domain name of the Active Directory to connect to.</param>
    /// <param name="ldapPort">The LDAP port number to use</param>
    /// <param name="userName">The username of the account in AD to use when making the connection.</param>
    /// <param name="password">The password of the account.</param>
    /// <param name="siteName">
    /// (Optional)The name of a site in Active Directory to use the domain controllers from. Defaults to
    /// DEFAULT_FIRST_SITE_NAME if not supplied.
    /// </param>
    /// <param name="ouDn">
    /// (Optional)The distinguished name of the OU to use as a base for operations or use DistinguishedName
    /// if null.
    /// </param>
    /// <param name="domainName">(Optional)The domain name to use for the connection.</param>
    public ActiveDirectoryCore(
        string server = null,
        ushort ldapPort = Ldap.LDAP_PORT,
        string userName = null,
        string password = null,
        string siteName = DEFAULT_FIRST_SITE_NAME,
        string ouDn = null,
        string domainName = null
    )
    {
        server = server.TrimOrNull();
        if (server == null)
        {
            using (var domain = Domain.GetComputerDomain())
            {
                server = domain.Name.TrimOrNull();
            }
        }

        ipaddress = server.CheckNotNull(nameof(server));
        ouDn = ouDn.TrimOrNull();
        username = userName = userName.TrimOrNull();
        this.password = password = password.TrimOrNull();
        siteName = siteName.TrimOrNull();
        domainName = domainName.TrimOrNull();

        var domainControllers = new List<string>();
        if (siteName != null)
        {
            domainControllers = GetSiteDomainControllers(server, siteName); // Get a list of domain controllers from a specific site, if one was supplied.
        }

        if (domainControllers.Count == 0)
        {
            domainControllers.Add(server); // Create the connection to the domain controller serving the current computer.
        }

        var useLogonCredentials = userName == null;

        log.Debug($"Attempting LDAP connection to {domainControllers.FirstOrDefault()}:{ldapPort} with user {userName}");
        try
        {
            Ldap = new Ldap(
                domainControllers.FirstOrDefault(),
                ldapPort,
                AuthType.Negotiate,
                userName,
                password,
                domainName,
                useLogonCredentials,
                ouDn ?? DistinguishedName
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
        log.Debug($"{nameof(WellKnownSidSystem)}: {WellKnownSidSystem}");
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

        // The string is null or full of whitespace.
        // Check if the string is empty.
        if (pathToRoot != null)
        {
            return DistinguishedName;
        }

        return null;
    }

    #region GetObjects

    /// <summary>
    /// Gets an entry given its distinguished name.
    /// </summary>
    /// <param name="distinguishedName">The distinguished name of the entry to get.</param>
    /// <param name="queryConfig">Configuration to use for the query</param>
    /// <param name="useCache">Whether to query the cached objects instead of live objects</param>
    /// <returns>The SearchResultEntry object found, or null if not found.</returns>
    public ActiveDirectoryObject GetObjectByDistinguishedName(string distinguishedName, LdapQueryConfig queryConfig = null, bool useCache = true)
    {
        return GetObjectsByAttribute("distinguishedName", distinguishedName, queryConfig, useCache).FirstOrDefault();
    }

    public ActiveDirectoryObject GetObjectBySAMAccountName(string samAccountName, LdapQueryConfig queryConfig = null, bool useCache = true)
    {
        return GetObjectsByAttribute("sAMAccountName", samAccountName, queryConfig, useCache).FirstOrDefault();
    }

    /// <summary>
    /// Gets an entry given its GUID.
    /// </summary>
    /// <param name="objectGuid">The GUID of the entry to get.</param>
    /// <param name="queryConfig">Configuration to use for the query</param>
    /// <param name="useCache">Whether to query the cached objects instead of live objects</param>
    /// <returns>The SearchResultEntry object found, or null if not found.</returns>
    public ActiveDirectoryObject GetObjectByObjectGuid(Guid objectGuid, LdapQueryConfig queryConfig = null, bool useCache = true)
    {
        return GetObjectsByAttribute("objectGUID", Ldap.Guid2String(objectGuid), queryConfig, useCache).FirstOrDefault();
    }

    /// <summary>
    /// Gets all entries in a search given an LDAP search filter.
    /// </summary>
    /// <param name="filter">The LDAP search filter string that will find the entries.</param>
    /// <param name="queryConfig">Configuration to use for the query</param>
    /// <param name="useCache">Whether to query the cached objects instead of live objects</param>
    /// <returns>A list of SearchResultEntry objects, or null if not found.</returns>
    public List<ActiveDirectoryObject> GetObjects(string filter, LdapQueryConfig queryConfig = null, bool useCache = true)
    {
        queryConfig ??= Ldap.DefaultQueryConfig;

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

        log.Debug("Issuing LDAP Query: " + (filter ?? "ALL-OBJECTS") + "  " + queryConfig);
        var attributeCollections = Ldap.EntryGet(filter, queryConfig);
        log.Debug($"Query filter[{filter}]  {queryConfig}  retrieved {attributeCollections.Count} objects");
        var objs = ActiveDirectoryObject.Create(this, attributeCollections).ToList();
        cache.Add(filter, objs, queryConfig);
        return objs;
    }

    /// <summary>
    /// Gets entries that match a given wild carded (*) attribute value in the supplied attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute to search against.</param>
    /// <param name="attributeValue">The value to search for in the attribute.</param>
    /// <param name="queryConfig">Configuration to use for the query</param>
    /// <param name="useCache">Whether to query the cached objects instead of live objects</param>
    /// <returns>The list of SearchResultEntry(s) found, or null if not found.</returns>
    public List<ActiveDirectoryObject> GetObjectsByAttribute(string attributeName, string attributeValue, LdapQueryConfig queryConfig = null, bool useCache = true)
    {
        return GetObjects("(" + attributeName.CheckNotNullTrimmed(nameof(attributeName)) + "=" + attributeValue.CheckNotNullTrimmed(nameof(attributeValue)) + ")", queryConfig, useCache);
    }

    #endregion GetObjects

    #region Actions

    private ActiveDirectoryObject AddObject(string samAccountName, string ouDistinguishedName, int? groupType)
    {
        samAccountName = samAccountName.CheckNotNullTrimmed(nameof(samAccountName));
        ouDistinguishedName = ouDistinguishedName.CheckNotNullTrimmed(nameof(ouDistinguishedName));

        if (GetObjectByDistinguishedName(ouDistinguishedName) == null)
        {
            throw new ArgumentException("The OU provided does not exist in Active Directory.");
        }

        var objectDistinguishedName = "CN=" + samAccountName + "," + ouDistinguishedName;

        var attributes = new SortedDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        //attributes.AddToList("sAMAccountName", sAMAccountName);

        if (groupType == null) // user
        {
            //attributes.Add("objectClass", "user");
            //attributes.Add("userPrincipalName", sAMAccountName + "@" + Name);
            attributes.AddToList("cn", samAccountName);
            //attributes.Add("userPrincipalName", sAMAccountName);
            //attributes.Add("GivenName", sAMAccountName);
            //attributes.AddToList("sn", sAMAccountName);

            attributes.AddToList("uid", samAccountName);
            attributes.AddToList("ou", "users");

            attributes.AddToList("objectClass", "top", "account", "simpleSecurityObject");
            var userPassword = "badPassword1!";
            string encodedPassword;
            using (var sha1 = SHA1.Create())
            {
                var digest = Convert.ToBase64String(sha1.ComputeHash(Encoding.UTF8.GetBytes(userPassword)));
                encodedPassword = "{SHA}" + digest;
            }

            attributes.AddToList("userPassword", encodedPassword);
        }
        else // group
        {
            if (!IsGroupNameValid(samAccountName))
            {
                throw new ArgumentException("The SAM Account Name '" + samAccountName + "' is not a valid group name.");
            }

            attributes.AddToList("objectClass", "group");
            //attributes.Add(new DirectoryAttribute("groupType", BitConverter.GetBytes(groupType.Value)));
            attributes.AddToList("groupType", groupType.Value.ToString());
        }


        log.Debug("Ldap.Add(" + objectDistinguishedName + ", [" + attributes.Select(o => o.Key + ":" + o.Value).ToStringDelimited(", ") + "])");
        Ldap.EntryAdd(objectDistinguishedName, attributes.ToDirectoryAttributes().ToArray());

        return GetObjectByDistinguishedName(objectDistinguishedName);
    }

    public bool DeleteObject(ActiveDirectoryObject activeDirectoryObject)
    {
        return activeDirectoryObject != null && Ldap.EntryDelete(activeDirectoryObject.DistinguishedName);
    }

    /// <summary>
    /// Creates a new group within Active Directory given it's proposed name, the distinguished name of the OU to place it in,
    /// and other optional attributes.
    /// </summary>
    /// <param name="samAccountName">The proposed SAM Account name for the group.</param>
    /// <param name="ouDistinguishedName">The distinguished name for the OU to place the group within.</param>
    /// <param name="groupType">A uint from the ActiveDirectory.GroupType enum representing the type of group to create.</param>
    /// <returns>The newly created group object.</returns>
    public ActiveDirectoryObject AddGroup(string samAccountName, string ouDistinguishedName, ActiveDirectoryGroupType groupType)
    {
        return AddObject(samAccountName, ouDistinguishedName, (int)groupType);
    }

    /// <summary>
    /// Moves and / or renames an object in Active Directory.
    /// </summary>
    /// <param name="activeDirectoryObject">The GUID of the object to move and / or rename.</param>
    /// <param name="parentObjectDistinguishedName">
    /// (Optional: Required only if moving) The GUID of the new parent object for
    /// the object (if moving).
    /// </param>
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
        if (dn1 == null || dn2 == null)
        {
            return false;
        }

        var dn1Parts = dn1.Split(",").TrimOrNull().WhereNotNull().ToArray();
        var dn2Parts = dn2.Split(",").TrimOrNull().WhereNotNull().ToArray();
        if (dn1Parts.Length != dn2Parts.Length)
        {
            return false;
        }

        if (dn1Parts.Length == 0)
        {
            return false; // No DN to match
        }

        for (var i = 0; i < dn1Parts.Length; i++)
        {
            if (!string.Equals(dn1Parts[i], dn2Parts[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the domain controllers associated with a specific Active Directory site from the Active Directory's DNS SRV
    /// records.
    /// </summary>
    /// <param name="domainName">The DNS domain name of the Active Directory to retrieve the domain controllers for.</param>
    /// <param name="siteName">The name of the site to retrieve the domain controllers for.</param>
    /// <returns>
    /// A list containing the FQDNs of the domain controllers in the specified site, or an empty list if they could
    /// not be retrieved.
    /// </returns>
    public static List<string> GetSiteDomainControllers(string domainName, string siteName)
    {
        domainName = domainName.TrimOrNull();
        siteName = siteName.TrimOrNull();
        if (domainName == null || siteName == null)
        {
            return new List<string>();
        }

        // ReSharper disable once CommentTypo
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
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        if (name.Length > 63)
        {
            return false; // Check whether the length of the name is less than or equal to 63 characters.
        }

        if (name.StartsWith(" "))
        {
            return false;
        }

        if (name.StartsWith("."))
        {
            return false;
        }

        if (name.ToCharArray().All(c => !char.IsLetter(c)))
        {
            return false; // must contain at least 1 letter
        }

        return true;
    }

    #endregion Methods Static

    #region IDisposable

    /// <summary>
    /// Releases underlying resources associated with the Active Directory connection.
    /// </summary>
    public virtual void Dispose()
    {
        if (Ldap != null)
        {
            try
            {
                Ldap.Dispose();
            }
            catch (Exception e)
            {
                log.Warn("Error disposing of " + Ldap.GetType().FullName, e);
            }
        }
    }

    #endregion IDisposable
}
