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
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using System.Text;

namespace HavokMultimedia.Utilities.Console.External
{
    public class Ldap : IDisposable
    {
        private static readonly ILogger log = Program.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly object locker = new object();

        /// <summary>
        /// The object that manages the connection with the LDAP server.
        /// </summary>
        private readonly LdapConnection connection;

        /// <summary>
        /// The distinguished name of the directory object where all searches will have as their base. Defaults to the first naming context found in the RootDSE.
        /// </summary>
        private readonly string searchBaseDNdefault;

        private readonly string server;
        private readonly ushort port;
        private readonly string userName;
        private readonly string password;
        private readonly string domainName;

        private readonly System.DirectoryServices.DirectoryEntry searchRoot;
        private readonly System.DirectoryServices.DirectorySearcher searcher;
        private readonly System.DirectoryServices.AccountManagement.PrincipalContext context;

        private bool isDisposed;
        private LdapQueryConfig queryConfig;

        /// <summary>
        /// The default unencrypted port used for LDAP servers.
        /// </summary>
        public const ushort LDAP_PORT = 389; // 3268; // 389; //  https://stackoverflow.com/a/43864640

        /// <summary>
        /// The default SSL encrypted port for LDAP servers.
        /// </summary>
        public const ushort LDAP_SSL_PORT = 636;

        /// <summary>
        /// The rootDSE entry for the connected LDAP server.
        /// </summary>
        public SearchResultEntry RootDSE { get; }

        public LdapEntryAttributeCollection RootDSEAttributes { get; }

        public IReadOnlyList<string> SupportedLdapVersions { get; } = new List<string>().AsReadOnly();

        /// <summary>
        /// The Naming Contexts (The base DNs that this server hosts.) for the LDAP server as defined in the RootDSE entry.
        /// </summary>
        public IReadOnlyList<string> NamingContexts { get; } = new List<string>().AsReadOnly();

        public string DefaultNamingContext { get; }

        /// <summary>
        /// A list of other servers that can fulfill LDAP requests if the one we're connected to becomes unavailable.
        /// May be null if there are no other servers available.
        /// </summary>
        public IReadOnlyList<string> AlternateServers { get; } = new List<string>().AsReadOnly();

        public IReadOnlyList<string> SupportedControls { get; } = new List<string>().AsReadOnly();

        public bool IsPagingSupported => SupportedControls.Contains("1.2.840.113556.1.4.319");

        public LdapQueryConfig DefaultQueryConfig
        {
            get
            {
                lock (locker)
                {
                    var o = queryConfig;
                    if (o == null) o = queryConfig = new LdapQueryConfig();
                    return o;
                }
            }
            set
            {
                lock (locker)
                {
                    queryConfig = value ?? new LdapQueryConfig();
                }
            }
        }

        /// <summary>
        /// Establishes a connection with an LDAP server that can be used to query or modify its contents.
        /// <param name="servers">A list of servers by fully qualified domain name, host name, ip address, or null.</param>
        /// <param name="portNumber">The port number on the LDAP server that is listening for requests.</param>
        /// <param name="authType">(Optional) The type of authentication to use when connecting with the server. By default this is set to Anonymous (i.e. no credentials required).</param>
        /// <param name="userName">(Optional) The user name to use when connecting to the LDAP server.</param>
        /// <param name="password">(Optional) The password to use with the user name provided to connect to the LDAP server.</param>
        /// <param name="domainName">(Optional) The domain or computer name associated with the user credentials provided.</param>
        /// <param name="useLogonCredentials">(Optional) If enabled, the LDAP connection will use the logon credentials from the current session. Disabled by default.</param>
        /// <param name="searchBaseDNdefault"></param>
        /// </summary>
        public Ldap(string server, ushort portNumber, AuthType authType = AuthType.Anonymous, string userName = null, string password = null, string domainName = null, bool useLogonCredentials = false, string searchBaseDNdefault = null)
        {
            this.server = server = server.CheckNotNullTrimmed(nameof(server));
            port = portNumber = portNumber.CheckNotZeroNotNegative(nameof(portNumber));

            userName = userName.TrimOrNull();
            domainName = domainName.TrimOrNull();
            if (domainName == null && userName != null && userName.IndexOf("\\") >= 0)
            {
                var uparts = userName.Split('\\').TrimOrNull().WhereNotNull().ToArray();
                if (uparts.Length == 1) userName = uparts[0];
                if (uparts.Length > 1)
                {
                    domainName = uparts[0];
                    userName = uparts[1];
                }
            }
            this.userName = userName = userName.TrimOrNull();
            this.domainName = domainName = domainName.TrimOrNull();
            this.password = password;
            this.searchBaseDNdefault = searchBaseDNdefault.TrimOrNull();

            log.Debug("server: " + server);
            log.Debug("portNumber: " + portNumber);
            log.Debug("authType: " + authType);
            log.Debug("userName: " + userName);
            log.Debug("domainName: " + domainName);
            log.Debug("useLogonCredentials: " + useLogonCredentials);
            log.Debug("searchBaseDNdefault: " + searchBaseDNdefault);

            // Setup the server information for the connection.
            var directoryIdentifier = new LdapDirectoryIdentifier(server, portNumber, false, false);

            // Create the connection to the server(s).
            if (useLogonCredentials)
            {
                connection = new LdapConnection(directoryIdentifier);
            }
            else
            {
                // Setup the credential to use when accessing the server. (Or null for Anonymous.)
                NetworkCredential credential = null;
                if (authType != AuthType.Anonymous)
                {
                    credential = new NetworkCredential(userName, password);
                    if (domainName != null) credential.Domain = domainName; // A domain was provided. Use it when creating the credential.
                }

                connection = new LdapConnection(directoryIdentifier, credential, authType);
            }
            if (portNumber == LDAP_SSL_PORT) connection.SessionOptions.SecureSocketLayer = true;

            // Gather information about the LDAP server(s) from the RootDSE entry.
            var rootDSESearchResponse = (SearchResponse)connection.SendRequest(new SearchRequest(null, "(objectClass=*)", SearchScope.Base));
            if (rootDSESearchResponse != null && rootDSESearchResponse.ResultCode == ResultCode.Success && rootDSESearchResponse.Entries.Count > 0)
            {
                // Save the rootDSE for access by API clients.
                RootDSE = rootDSESearchResponse.Entries[0];
                if (RootDSE != null)
                {
                    RootDSEAttributes = new LdapEntryAttributeCollection(RootDSE);

                    SupportedLdapVersions = RootDSEAttributes.GetStrings("supportedLDAPVersion").ToList().AsReadOnly();
                    NamingContexts = RootDSEAttributes.GetStrings("namingContexts").ToList().AsReadOnly();
                    DefaultNamingContext = RootDSEAttributes.GetString("defaultNamingContext");

                    if (this.searchBaseDNdefault == null) this.searchBaseDNdefault = NamingContexts.FirstOrDefault();
                    AlternateServers = RootDSEAttributes.GetStrings("altServer").ToList().AsReadOnly();
                    SupportedControls = RootDSEAttributes.GetStrings("supportedControl").ToList().AsReadOnly();
                }
            }

            // Bind to the ldap server with the connection credentials if supplied.
            if (connection.AuthType != AuthType.Anonymous) connection.Bind();
            //connection.SessionOptions.ProtocolVersion = 3;

            var ldapConnectionString = "LDAP://" + this.server + ":" + port;
            var usernameAndDomain = this.userName;
            if (domainName != null) usernameAndDomain = domainName + "\\" + usernameAndDomain;
            searchRoot = new System.DirectoryServices.DirectoryEntry(ldapConnectionString, usernameAndDomain, this.password);
            searcher = new System.DirectoryServices.DirectorySearcher(searchRoot);
            context = new System.DirectoryServices.AccountManagement.PrincipalContext(System.DirectoryServices.AccountManagement.ContextType.Domain, this.server, usernameAndDomain, this.password);
        }

        private bool MakeRequest(DirectoryRequest request)
        {
            request.CheckNotNull(nameof(request));
            var response = connection.SendRequest(request);
            if (response == null) return false;
            if (response.ResultCode == ResultCode.Success) return true;
            log.Warn(request.GetType().Name + " request failed. " + response.ResultCode + " - " + response.ErrorMessage);
            return false;
        }

        #region SearchResultEntry

        /// <summary>
        /// Searches the LDAP directory for entries that match the specified search filter.
        /// </summary>
        /// <param name="filter">The filter that defines the entries to find.</param>
        /// <param name="config">The options to use for the query</param>
        /// <returns>A collection of search result entries found.</returns>
        public List<SearchResultEntry> SearchResultEntryGet(string filter, LdapQueryConfig config)
        {
            config = config ?? DefaultQueryConfig;
            filter = filter.TrimOrNull();
            var baseDn = config.BaseDn ?? searchBaseDNdefault;

            // Set the search base and scope for the search if provided.
            SearchRequest request;
            if (config.Attributes.Count == 0)
            {
                request = new SearchRequest(baseDn, filter, config.Scope);
            }
            else
            {
                request = new SearchRequest(baseDn, filter, config.Scope, config.Attributes.ToArray());
            }



            // Add a directory control that makes the search use pages for returning large result sets.
            var pageResultRequestControl = new PageResultRequestControl(config.QueryPageSize);
            request.Controls.Add(pageResultRequestControl);

            var searchOptionsControl = new SearchOptionsControl(SearchOption.DomainScope);
            request.Controls.Add(searchOptionsControl);

            connection.SessionOptions.ReferralChasing = config.ChaseReferrals ? ReferralChasingOptions.All : ReferralChasingOptions.None;

            // Create a list to hold our results while we request all of the results in pages.
            var results = new List<SearchResultEntry>();

            while (true)
            {
                // Add the page request control that manages the paged searched, and send the request for results.
                log.Debug("Sending request filter: " + filter + "   Attributes:[" + string.Join(", ", config.Attributes) + "]   DistinguishedName:[" + (config.BaseDn ?? searchBaseDNdefault) + "]   Scope:[" + config.Scope + "]");

                //System.DirectoryServices.Protocols.DirectoryOperationException: 'The server does not support the control. The control is critical.'

                var response = (SearchResponse)connection.SendRequest(request);

                if (response == null) return results; // No response was received.

                // A response was received.
                // Get the paging response control to allow us to gather the results in batches.
                foreach (var control in response.Controls)
                {
                    if (control is PageResultResponseControl pageResultResponseControl)
                    {
                        // Update the cookie in the request control to gather the next page of the query.
                        pageResultRequestControl.Cookie = pageResultResponseControl.Cookie;

                        // Break out of the loop now that we've copied the cookie.
                        break;
                    }
                }

                log.Debug("Received response: " + response.ResultCode);
                if (response.ResultCode == ResultCode.Success)
                {
                    var count = 0;
                    // Add the results to the list.
                    foreach (SearchResultEntry entry in response.Entries)
                    {
                        results.Add(entry);
                        count++;
                    }
                    log.Debug("Retrieved " + count + " SearchResultEntry. Total SearchResultEntry retrieved so far: " + results.Count);
                }
                else
                {
                    log.Warn("Error retrieving LDAP results " + response.ResultCode + " " + response.ErrorMessage.TrimOrNull());
                    return results;
                }

                // Check whether the cookies is empty and all the results have been gathered.
                if (pageResultRequestControl.Cookie.Length == 0) break; // The cookie is empty. We're done gathing results.
            }

            return results;
        }

        /// <summary>
        /// Gets an entry given its GUID.
        /// </summary>
        /// <param name="objectGuid">The GUID of the entry to get.</param>
        /// <param name="config">The options to use for the query</param>
        /// <returns>The SearchResultEntry object found, or null if not found.</returns>
        public SearchResultEntry SearchResultEntryGetByObjectGuid(Guid objectGuid, LdapQueryConfig config)
        {
            // Create an LDAP search filter string that will find the entry in the directory with the specified GUID.
            var guidBuilder = new StringBuilder(48);
            foreach (var guidByte in objectGuid.ToByteArray())
            {
                guidBuilder.Append('\\' + guidByte.ToString("x2"));
            }
            return SearchResultEntryGet($"(objectGUID={guidBuilder.ToString()})", config).FirstOrDefault();
        }

        public SearchResultEntry SearchResultEntryGetByDistinguishedName(string distinguishedName, LdapQueryConfig config) => SearchResultEntryGet("(&(distinguishedName=" + distinguishedName.CheckNotNullTrimmed(nameof(distinguishedName)) + "))", config).FirstOrDefault();

        public List<LdapEntryAttributeCollection> EntryGet(string filter, LdapQueryConfig config)
        {
            var list = new List<LdapEntryAttributeCollection>();
            var entries = SearchResultEntryGet(filter, config);
            foreach (var entry in entries)
            {
                var c = new LdapEntryAttributeCollection(entry, this);
                list.Add(c);
            }
            return list;
        }

        /// <summary>
        /// Adds an entry to the LDAP directory with the specified distinguished name and attributes.
        /// </summary>
        /// <param name="distinguishedName">The distinguished name of the entry to add.</param>
        /// <param name="attributes">The attributes for the entry to add.</param>
        /// <returns>True if added, false otherwise.</returns>
        public bool EntryAdd(string distinguishedName, DirectoryAttribute[] attributes)
        {
            distinguishedName = distinguishedName.CheckNotNullTrimmed(nameof(distinguishedName));
            var request = new AddRequest(distinguishedName, attributes);
            return MakeRequest(request);
        }

        /// <summary>
        /// Deletes an entry from the LDAP directory with the specified distinguished name.
        /// </summary>
        /// <param name="distinguishedName">The distinguished name of the entry to delete.</param>
        /// <returns>True if deleted, false otherwise.</returns>
        public bool EntryDelete(string distinguishedName)
        {
            distinguishedName = distinguishedName.CheckNotNullTrimmed(nameof(distinguishedName));
            var request = new DeleteRequest(distinguishedName);
            return MakeRequest(request);
        }

        /// <summary>
        /// Moves and / or renames an entry in the directory.
        /// </summary>
        /// <param name="distinguishedName">The distinguished name of the entry to move or rename.</param>
        /// <param name="newParentDistinguishedName">The distinguished name of the entry's new parent entry in the directory (if moving), or its current parent entry (if renaming).</param>
        /// <param name="newCommonName">The new common name of entry.</param>
        /// <returns>True if moved or renamed, false otherwise.</returns>
        public bool EntryMoveRename(string distinguishedName, string newParentDistinguishedName, string newCommonName)
        {
            distinguishedName = distinguishedName.CheckNotNullTrimmed(nameof(distinguishedName));
            newParentDistinguishedName = newParentDistinguishedName.CheckNotNullTrimmed(nameof(newParentDistinguishedName));
            newCommonName = newCommonName.CheckNotNullTrimmed(nameof(newCommonName));
            if (!newCommonName.StartsWith("CN=", StringComparison.OrdinalIgnoreCase)) newCommonName = "CN=" + newCommonName;
            var request = new ModifyDNRequest(distinguishedName, newParentDistinguishedName, newCommonName);
            return MakeRequest(request);
        }

        #endregion SearchResultEntry

        #region ActiveDirectory specific functions

        public static bool Authenticate(string server, int portNumber, string userName, string password, string domainName = null)
        {
            const int ldapErrorInvalidCredentials = 0x31;

            server = server = server.CheckNotNullTrimmed(nameof(server));
            portNumber = portNumber.CheckNotZeroNotNegative(nameof(portNumber));
            userName = userName.CheckNotNullTrimmed(nameof(userName));
            password = password.CheckNotNull(nameof(password));
            domainName = domainName.TrimOrNull();

            // Setup the server information for the connection.
            var directoryIdentifier = new LdapDirectoryIdentifier(server.Yield().ToArray(), portNumber, false, false);

            var credential = new NetworkCredential(userName, password);
            if (domainName != null) credential.Domain = domainName; // A domain was provided. Use it when creating the credential.

            var un = (credential.Domain == null ? "" : credential.Domain + "\\") + credential.UserName;
            try
            {
                using (var connection = new LdapConnection(directoryIdentifier, credential, AuthType.Negotiate))
                {
                    connection.Bind();
                    log.Debug("Successfully authenticated user: " + un);
                    return true;
                }
            }
            catch (LdapException ldapException)
            {
                // Invalid credentials throw an exception with a specific error code
                if (ldapException.ErrorCode.Equals(ldapErrorInvalidCredentials))
                {
                    log.Debug("Failed authenticating user invalid credentials: " + un);
                    return false;
                }

                log.Warn("Error authenticating user: " + un, ldapException);

                //return false;
                throw;
            }
        }

        public System.DirectoryServices.DirectoryEntry GetDirectoryEntryByDistinguishedName(string distinguishedName)
        {
            searcher.Filter = "(distinguishedName=" + distinguishedName + ")";
            return searcher.FindOne()?.GetDirectoryEntry();
        }

        public System.DirectoryServices.AccountManagement.UserPrincipal GetUserPrincipalByDistinguishedName(string distinguishedName) => System.DirectoryServices.AccountManagement.UserPrincipal.FindByIdentity(context, System.DirectoryServices.AccountManagement.IdentityType.DistinguishedName, distinguishedName);

        #endregion ActiveDirectory specific functions

        #region Attribute

        /// <summary>
        /// Adds, Replaces, or Deletes an attribute's value in the specified entry in the directory.
        /// </summary>
        /// <param name="distinguishedName">The distinguished name of the entry to add or replace an attribute of.</param>
        /// <param name="attributeName">The name of the attribute to add or replace a value for.</param>
        /// <param name="values">The values associated with the attribute to add or replace. If null is supplied then the attribute is deleted</param>
        /// <returns>True if added or replaced or deleted, false otherwise.</returns>
        public bool AttributeSave(string distinguishedName, string attributeName, object[] values)
        {
            distinguishedName = distinguishedName.CheckNotNullTrimmed(nameof(distinguishedName));
            attributeName = attributeName.CheckNotNullTrimmed(nameof(attributeName));
            var vals = (values ?? Enumerable.Empty<object>()).WhereNotNull().ToArray();
            var request = vals.Length == 0 ? new ModifyRequest(distinguishedName, DirectoryAttributeOperation.Delete, attributeName) : new ModifyRequest(distinguishedName, DirectoryAttributeOperation.Replace, attributeName, vals);
            request.Controls.Add(new PermissiveModifyControl());
            return MakeRequest(request);
        }

        #endregion Attribute

        #region IDisposable

        /// <summary>
        /// Closes the LDAP connection and frees all resources associated with it.
        /// </summary>
        public void Dispose()
        {
            lock (this)
            {
                if (isDisposed) return;
                isDisposed = true;
            }

            if (connection != null)
            {
                try { connection.Dispose(); }
                catch (Exception e) { log.Warn($"Error disposing of {connection.GetType().FullNameFormatted()}: {e.Message}", e); }
            }

            if (searcher != null)
            {
                try { searcher.Dispose(); }
                catch (Exception e) { log.Warn($"Error disposing of {searcher.GetType().FullNameFormatted()}: {e.Message}", e); }
            }

            if (searchRoot != null)
            {
                try { searchRoot.Close(); }
                catch (Exception e) { log.Warn($"Error closing of {searchRoot.GetType().FullNameFormatted()}: {e.Message}", e); }

                try { searchRoot.Dispose(); }
                catch (Exception e) { log.Warn($"Error disposing of {searchRoot.GetType().FullNameFormatted()}: {e.Message}", e); }
            }

            if (context != null)
            {
                try { context.Dispose(); }
                catch (Exception e) { log.Warn($"Error disposing of {context.GetType().FullNameFormatted()}: {e.Message}", e); }
            }
        }

        #endregion IDisposable

        #region Guid Conversion

        public static string Guid2String(Guid guid)
        {
            var guidBuilder = new StringBuilder(48);
            foreach (var guidByte in guid.ToByteArray())
            {
                guidBuilder.Append('\\' + guidByte.ToString("x2"));
            }
            return guidBuilder.ToString();
        }

        public static Guid Bytes2Guid(byte[] bytes)
        {
            if (bytes == null) return Guid.Empty;
            if (bytes.Length == 0) return Guid.Empty;
            return new Guid(bytes);
        }

        #endregion Guid Conversion
    }
}
