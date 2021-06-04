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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace HavokMultimedia.Utilities.Console.External
{
    public class ActiveDirectoryObject : IEquatable<ActiveDirectoryObject>, IComparable<ActiveDirectoryObject>
    {
        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
        public sealed class IgnoreInPropertiesListAttribute : Attribute { }

        private static readonly ILogger log = Program.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly DateTime JAN_01_1601 = new DateTime(1601, 1, 1);
        private static readonly DateTime JAN_01_1800 = new DateTime(1800, 1, 1);
        private readonly ActiveDirectoryCore activeDirectory;

        public static IReadOnlyCollection<string> ExpensiveProperties { get; } = new string[] {
                nameof(UserCannotChangePassword),
                nameof(PasswordExpirationDate),
                nameof(IsDisabled),
                nameof(PasswordExpired)
        }.OrderBy(o => o, StringComparer.OrdinalIgnoreCase).ToHashSet();

        private Lazy<System.DirectoryServices.DirectoryEntry> directoryEntry;
        private System.DirectoryServices.DirectoryEntry DirectoryEntry => directoryEntry.Value;

        private Lazy<System.DirectoryServices.AccountManagement.UserPrincipal> userPrincipal;
        private System.DirectoryServices.AccountManagement.UserPrincipal UserPrincipal => userPrincipal.Value;

        #region Properties

        public LdapEntryAttributeCollection Attributes { get; private set; }
        public DateTime? AccountExpires => Attributes.GetDateTimeUTC("accountExpires");
        public DateTime? BadPasswordTime => Attributes.GetDateTimeUTC("badPasswordTime");
        public int? BadPwdCount => Attributes.GetInt("badPwdCount");
        public string CN => Attributes.GetString("cn");
        public string Comment { get => Attributes.GetString("comment"); set => AttributeSave("comment", value.TrimOrNull()); }
        public string Company { get => Attributes.GetString("company"); set => AttributeSave("company", value.TrimOrNull()); }
        public string CountryCode { get => Attributes.GetString("countryCode"); set => AttributeSave("countryCode", value.TrimOrNull()); }
        public DateTime? CreationTime => Attributes.GetDateTimeUTC("creationTime");
        public string DC => Attributes.GetString("dc");
        public string Department { get => Attributes.GetString("department"); set => AttributeSave("department", value.TrimOrNull()); }
        public string Description { get => Attributes.GetString("description"); set => AttributeSave("description", value.TrimOrNull()); }
        public string DisplayName { get => Attributes.GetString("displayName"); set => AttributeSave("displayName", value.TrimOrNull()); }
        public string DisplayNamePrintable { get => Attributes.GetString("displayNamePrintable"); set => AttributeSave("displayNamePrintable", value.TrimOrNull()); }
        public string DistinguishedName => Attributes.DistinguishedName;
        public string DNSHostName => Attributes.GetString("dNSHostName");
        public byte[] DNSProperty => Attributes.GetByteArray("dNSProperty");
        public byte[] DNSRecord => Attributes.GetByteArray("dnsRecord");
        public byte[] DSASignature => Attributes.GetByteArray("dSASignature");
        public string EmployeeNumber { get => Attributes.GetString("employeeNumber"); set => AttributeSave("employeeNumber", value.TrimOrNull()); }
        public string GivenName { get => Attributes.GetString("givenName"); set => AttributeSave("givenName", value.TrimOrNull()); }
        public string UID { get => Attributes.GetString("uid"); set => AttributeSave("uid", value.TrimOrNull()); }
        public string GroupType => Attributes.GetString("groupType");

        public ActiveDirectoryGroupType? GroupTypeEnum
        {
            get
            {
                var o = Attributes.GetInt("groupType");
                if (o == null) return null;
                return (ActiveDirectoryGroupType)o.Value;
            }
        }

        public string HomeDirectory { get => Attributes.GetString("homeDirectory"); set => AttributeSave("homeDirectory", value.TrimOrNull()); }
        public string HomeDrive { get => Attributes.GetString("homeDrive"); set => AttributeSave("homeDrive", value.TrimOrNull()); }
        public string HomeMDB => Attributes.GetString("homeMDB");
        public string HomeMTA => Attributes.GetString("homeMTA");
        public string Info { get => Attributes.GetString("info"); set => AttributeSave("info", value.TrimOrNull()); }
        public string Initials { get => Attributes.GetString("initials"); set => AttributeSave("initials", value.TrimOrNull()); }
        public bool? IsCriticalSystemObject => Attributes.GetBool("isCriticalSystemObject");

        public DateTime? LastLogon
        {
            get
            {
                var ll = Attributes.GetDateTimeUTC("lastLogon");
                if (ll == null) return null;
                if (ll.Value == DateTime.MinValue) return null;
                if (ll.Value == DateTime.MinValue.ToUniversalTime()) return null;
                return ll.Value;
            }
        }

        public DateTime? LastLogonTimestamp
        {
            get
            {
                var ll = Attributes.GetDateTimeUTC("lastLogonTimestamp");
                if (ll == null) return null;
                if (ll.Value == DateTime.MinValue) return null;
                if (ll.Value == DateTime.MinValue.ToUniversalTime()) return null;
                return ll.Value;
            }
        }

        public DateTime? LastSetTime => Attributes.GetDateTimeUTC("lastSetTime");
        public string Location { get => Attributes.GetString("location"); set => AttributeSave("location", value.TrimOrNull()); }
        public string LockoutTime { get => Attributes.GetString("lockoutTime"); set => AttributeSave("lockoutTime", value.TrimOrNull()); }

        public int? LogonCount => Attributes.GetInt("logonCount");
        public string Mail { get => Attributes.GetString("mail"); set => AttributeSave("mail", value.TrimOrNull()); }
        public string MailNickname { get => Attributes.GetString("mailNickname"); set => AttributeSave("mailNickname", value.TrimOrNull()); }
        public string ManagedBy { get => Attributes.GetString("managedBy"); set => AttributeSave("managedBy", value.TrimOrNull()); }
        public IEnumerable<string> ManagedObjects => Attributes.GetStrings("managedObjects");
        public IEnumerable<string> MasteredBy => Attributes.GetStrings("masteredBy");
        public long? MaxPwdAge => Attributes.GetLong("maxPwdAge");
        public IEnumerable<string> Member => Attributes.GetStrings("member"); // member;range=0-1499
        public IEnumerable<ActiveDirectoryObject> MemberObjects => GetMembers(false);
        public IEnumerable<ActiveDirectoryObject> MemberObjectsAll => GetMembers(true);
        public IEnumerable<string> MemberOf => Attributes.GetStrings("memberOf");
        public IEnumerable<ActiveDirectoryObject> MemberOfObjects => GetMemberOf(false);
        public IEnumerable<ActiveDirectoryObject> MemberOfObjectsAll => GetMemberOf(true);
        public long? MinPwdAge => Attributes.GetLong("minPwdAge");
        public int? MinPwdLength => Attributes.GetInt("minPwdLength");
        public string Name { get => Attributes.GetString("name"); set => AttributeSave("name", value.TrimOrNull()); }
        public string ObjectCategory => Attributes.GetString("objectCategory");
        public IEnumerable<string> ObjectClass => Attributes.GetStrings("objectClass");
        public Guid ObjectGUID => Attributes.ObjectGUID;
        public byte[] ObjectSid => Attributes.GetByteArray("objectSid");
        public long? ObjectVersion => Attributes.GetLong("objectVersion");
        public string OperatingSystem => Attributes.GetString("operatingSystem");
        public string OperatingSystemHotfix => Attributes.GetString("operatingSystemHotfix");
        public string OperatingSystemServicePack => Attributes.GetString("operatingSystemServicePack");
        public string OperatingSystemVersion => Attributes.GetString("operatingSystemVersion");
        public string OtherWellKnownObjects => Attributes.GetString("otherWellKnownObjects");

        public string PhysicalDeliveryOfficeName { get => Attributes.GetString("physicalDeliveryOfficeName"); set => AttributeSave("physicalDeliveryOfficeName", value.TrimOrNull()); }
        public long? PrimaryGroupID => Attributes.GetLong("primaryGroupID");
        public long? Priority => Attributes.GetLong("priority");
        public DateTime? PriorSetTime => Attributes.GetDateTimeUTC("priorSetTime");
        public string ProfilePath { get => Attributes.GetString("profilePath"); set => AttributeSave("profilePath", value.TrimOrNull()); }
        public IEnumerable<byte[]> ProtocolSettings => Attributes.GetByteArrays("protocolSettings");
        public IEnumerable<string> ProxyAddresses => Attributes.GetStrings("proxyAddresses");
        public int? PwdHistoryLength => Attributes.GetInt("pwdHistoryLength");
        public DateTime? PwdLastSet => Attributes.GetDateTimeUTC("pwdLastSet");
        public long? PwdProperties => Attributes.GetLong("pwdProperties");
        public long? Revision => Attributes.GetLong("revision");
        public string SAMAccountName { get => Attributes.GetString("sAMAccountName"); set => AttributeSave("sAMAccountName", value.TrimOrNull()); }
        public int? SAMAccountType => Attributes.GetInt("sAMAccountType");
        public byte[] SecurityIdentifier => Attributes.GetByteArray("securityIdentifier");
        public string ScriptPath { get => Attributes.GetString("sAMAccouscriptPathntName"); set => AttributeSave("scriptPath", value.TrimOrNull()); }
        public string ServerName => Attributes.GetString("serverName");
        public string Sn { get => Attributes.GetString("sn"); set => AttributeSave("sn", value.TrimOrNull()); }
        public long? SystemFlags => Attributes.GetLong("systemFlags");
        public string TargetAddress { get => Attributes.GetString("targetAddress"); set => AttributeSave("targetAddress", value.TrimOrNull()); }
        public string TelephoneNumber { get => Attributes.GetString("telephoneNumber"); set => AttributeSave("telephoneNumber", value.TrimOrNull()); }
        public string UNCName { get => Attributes.GetString("uNCName"); set => AttributeSave("uNCName", value.TrimOrNull()); }
        public string Url { get => Attributes.GetString("url"); set => AttributeSave("url", value.TrimOrNull()); }
        public int? UserAccountControl => Attributes.GetInt("userAccountControl");
        public int? UserAccountControlComputed => Attributes.GetInt("msDS-User-Account-Control-Computed") ?? Attributes.GetInt("ms-DS-User-Account-Control-Computed");
        public IEnumerable<byte[]> UserCertificate => Attributes.GetByteArrays("userCertificate");
        public byte[] UserParameters => Attributes.GetByteArray("userParameters");
        public string UserPrincipalName { get => Attributes.GetString("userPrincipalName"); set => AttributeSave("userPrincipalName", value.TrimOrNull()); }
        public int? USNChanged => Attributes.GetInt("uSNChanged");
        public int? USNCreated => Attributes.GetInt("uSNCreated");
        public int? VersionNumber => Attributes.GetInt("versionNumber");
        public IEnumerable<string> WellKnownObjects => Attributes.GetStrings("wellKnownObjects");
        public DateTime? WhenChanged => Attributes.GetDateTimeUTC("whenChanged");
        public DateTime? WhenCreated => Attributes.GetDateTimeUTC("whenCreated");
        public string WWWHomePage { get => Attributes.GetString("wWWHomePage"); set => AttributeSave("wWWHomePage", value.TrimOrNull()); }

        [IgnoreInPropertiesList]
        public IEnumerable<ActiveDirectoryObject> Children
        {
            get
            {
                foreach (var o in DirectoryEntry.Children)
                {
                    if (o is not System.DirectoryServices.DirectoryEntry de) continue; // TODO: Could we get other object types?

                    var dn = de.DistinguishedName();
                    if (dn == null) continue;
                    var ado = activeDirectory.GetObjectByDistinguishedName(dn);
                    if (ado == null) continue; // should not happen
                    yield return ado;
                }
            }
        }

        [IgnoreInPropertiesList]
        public ActiveDirectoryObject Parent
        {
            get
            {
                System.DirectoryServices.DirectoryEntry de = null;
                try
                {
                    de = DirectoryEntry.Parent;
                }
                catch (Exception e)
                {
                    log.Debug("Error retrieving parent of " + DistinguishedName, e);
                }
                if (de == null) return null;
                var dn = de.DistinguishedName();
                if (dn == null) return null;
                var ado = activeDirectory.GetObjectByDistinguishedName(dn);
                return ado;
            }
        }

        #endregion Properties

        #region Properties Custom

        public string ObjectName
        {
            get
            {
                if (SAMAccountName != null) return SAMAccountName;

                if (LogonName != null)
                {
                    if (LogonName.Contains("@"))
                    {
                        var name = LogonName.Split("@").FirstOrDefault().TrimOrNull();
                        if (name != null) return name;
                    }
                    else
                    {
                        return LogonName;
                    }
                }

                if (LogonNamePreWindows2000 != null)
                {
                    if (LogonNamePreWindows2000.Contains("\\"))
                    {
                        var name = LogonNamePreWindows2000.Split("\\").LastOrDefault().TrimOrNull();
                        if (name != null) return name;
                    }
                    else
                    {
                        return LogonNamePreWindows2000;
                    }
                }

                if (Name != null) return Name;
                if (DisplayName != null) return DisplayName;
                if (DistinguishedName != null)
                {
                    var dnPart = DistinguishedName.Split(",").FirstOrDefault();
                    if (dnPart != null)
                    {
                        if (dnPart.Contains("="))
                        {
                            var dnPartPart = dnPart.Split("=", 2).GetAtIndexOrDefault(1).TrimOrNull();
                            if (dnPartPart != null) return dnPartPart;
                            else return DistinguishedName; // just in case something goes wacky
                        }
                        else
                        {
                            return dnPart;
                        }
                    }
                }
                return DistinguishedName;
            }
        }

        public string FirstName { get => GivenName; set => GivenName = value; }
        public string LastName { get => Sn; set => Sn = value; }
        public string LogonName { get => UserPrincipalName; set => UserPrincipalName = value; }
        public string LogonNamePreWindows2000 { get => SAMAccountName; set => SAMAccountName = value; }
        public string Office { get => PhysicalDeliveryOfficeName; set => PhysicalDeliveryOfficeName = value; }
        public string WebPage { get => WWWHomePage; set => WWWHomePage = value; }
        public string LoginScript { get => ScriptPath; set => ScriptPath = value; }

        /// <summary>
        /// The distinguished name of the organizational unit or parent object containing the object.
        /// </summary>
        public string OrganizationalUnit
        {
            get
            {
                var ou = DistinguishedName;
                if (ou == null) return null;
                var ouComponents = ou.Split(',');
                return ou.Substring(ouComponents[0].Length + 1);
            }
        }

        /// <summary>
        /// Indicates if this principal is a Group.
        /// </summary>
        public bool IsGroup => IsObjectClass("group");

        /// <summary>
        /// Indicates if this principal is a User.
        /// </summary>
        public bool IsUser => IsObjectClass("user") && IsObjectCategory("person");

        public bool IsComputer => IsObjectClass("computer") && IsObjectCategory("computer");

        public bool PasswordExpired
        {
            get
            {
                var uaccb = UserAccountControlComputed;
                if (uaccb != null)
                {
                    var uacc = uaccb.Value;
                    var pe = (int)ActiveDirectoryUserAccountControl.PasswordExpired;
                    if ((uacc & pe) == pe)
                    {
                        return true;
                    }
                }

                var uacb = UserAccountControl;
                if (uacb != null)
                {
                    var uac = uacb.Value;
                    var pe = (int)ActiveDirectoryUserAccountControl.DoNotExpirePassword;
                    if ((uac & pe) == pe)
                    {
                        return false;
                    }
                }

                var pwlstn = PwdLastSet;
                if (pwlstn != null)
                {
                    var pwlst = pwlstn.Value;
                    if (pwlst < JAN_01_1800)
                    {
                        return true;
                    }
                }

                return false;
            }
            set
            {
                if (value)
                {
                    AttributeSave("pwdLastSet", "0");
                    var user = UserPrincipal;
                    if (user != null)
                    {
                        user.ExpirePasswordNow();
                        user.Save();
                    }
                }
                else
                {
                    AttributeSave("pwdLastSet", "-1");
                }
            }
        }

        public bool IsDisabled
        {
            get
            {
                if (IsUserAccountControl(ActiveDirectoryUserAccountControl.AccountDisable)) return true;
                var r = DirectoryEntryGetBool("IsAccountLocked");
                if (r == null) return false;
                return r.Value;
            }
            set
            {
                if (value)
                {
                    // Lock account
                    UserAccountControlFlagAdd(ActiveDirectoryUserAccountControl.AccountDisable);
                }
                else
                {
                    // Unlock account
                    UserAccountControlFlagRemove(ActiveDirectoryUserAccountControl.AccountDisable);
                    var de = DirectoryEntry;
                    if (de != null)
                    {
                        de.InvokeSet("IsAccountLocked", false);
                        de.Properties["LockOutTime"].Value = 0; //unlock account
                        de.CommitChanges();
                    }
                }
            }
        }

        public DateTime? PasswordExpirationDate => DirectoryEntryGetDateTime("PasswordExpirationDate");

        public string Password
        {
            set
            {
                var de = DirectoryEntry;
                if (de != null)
                {
                    de.Invoke("SetPassword", new object[] { value });
                    de.Properties["LockOutTime"].Value = 0; //unlock account
                    de.CommitChanges();
                }

                /*
                var modifyUserPassword = new DirectoryAttributeModification
                {
                    Operation = DirectoryAttributeOperation.Replace,
                    Name = "userPassword"
                };
                //var bytes = Encoding.Unicode.GetBytes("\"" + password + "\"");
                //modifyUserPassword.Add(bytes);
                modifyUserPassword.Add(password);

                var modifyRequest = new ModifyRequest(distinguishedName, modifyUserPassword);
                MakeRequest(modifyRequest);
                */
            }
        }

        public bool UserCannotChangePassword
        {
            get
            {
                var user = UserPrincipal;
                if (user == null) return false;
                return user.UserCannotChangePassword;
            }
            set
            {
                var user = UserPrincipal;
                if (user == null) return;

                user.UserCannotChangePassword = value;
                user.Save();
            }
        }

        private static string ParseFirstCN(string item)
        {
            var itemParts = item.Split(",");
            foreach (var itemPart in itemParts)
            {
                if (itemPart.ToLower().StartsWith("cn="))
                {
                    var itemPart2 = itemPart.Substring(3).TrimOrNull();
                    if (itemPart2 != null) return itemPart2;
                }
            }
            return null;
        }

        public IEnumerable<string> MemberOfNames => MemberOf.Select(o => ParseFirstCN(o)).TrimOrNull().WhereNotNull().OrderBy(o => o, StringComparer.OrdinalIgnoreCase);
        public string MemberOfNamesString => "[" + MemberOfNames.ToStringDelimited(", ") + "]";

        public IEnumerable<string> MemberNames => Member.Select(o => ParseFirstCN(o)).TrimOrNull().WhereNotNull().OrderBy(o => o, StringComparer.OrdinalIgnoreCase);
        public string MemberNamesString => "[" + MemberNames.ToStringDelimited(", ") + "]";

        #endregion Properties Custom

        #region Constructor

        private ActiveDirectoryObject(ActiveDirectoryCore activeDirectory, LdapEntryAttributeCollection attributes)
        {
            this.activeDirectory = activeDirectory.CheckNotNull(nameof(activeDirectory));
            Attributes = attributes.CheckNotNull(nameof(attributes));
            directoryEntry = new Lazy<System.DirectoryServices.DirectoryEntry>(() => activeDirectory.Ldap.GetDirectoryEntryByDistinguishedName(DistinguishedName));
            userPrincipal = new Lazy<System.DirectoryServices.AccountManagement.UserPrincipal>(() => activeDirectory.Ldap.GetUserPrincipalByDistinguishedName(DistinguishedName));
        }

        public static ActiveDirectoryObject Create(ActiveDirectoryCore activeDirectory, LdapEntryAttributeCollection attributes)
        {
            log.Debug($"Created 1 {nameof(ActiveDirectoryObject)}");
            return new ActiveDirectoryObject(activeDirectory, attributes);
        }

        public static IEnumerable<ActiveDirectoryObject> Create(ActiveDirectoryCore activeDirectory, IEnumerable<LdapEntryAttributeCollection> attributes)
        {
            activeDirectory.CheckNotNull(nameof(activeDirectory));
            int i = 0;
            foreach (var attribute in attributes.OrEmpty())
            {
                if (attribute == null) continue;
                var o = Create(activeDirectory, attribute);
                i++;
                yield return o;

            }
            log.Debug($"Created {i} {nameof(ActiveDirectoryObject)}");
        }

        #endregion Constructor

        #region UserAccountControls

        private HashSet<ActiveDirectoryUserAccountControl> userAccountControls;

        public HashSet<ActiveDirectoryUserAccountControl> UserAccountControls
        {
            get
            {
                var set = userAccountControls;
                if (set == null)
                {
                    set = new HashSet<ActiveDirectoryUserAccountControl>();
                    var uacNullable = UserAccountControl;
                    if (uacNullable == null) return set;
                    var uac = uacNullable.Value;
                    foreach (var item in Util.GetEnumItems<ActiveDirectoryUserAccountControl>())
                    {
                        if ((uac & (int)item) == (int)item)
                        {
                            set.Add(item);
                        }
                    }
                    userAccountControls = set;
                }
                return set;
            }
        }

        /// <summary>
        /// Sets a flag in the user's User Account Control attribute.
        /// </summary>
        /// <param name="flag">A flag from the predefined UserAccountControl flags.</param>
        /// <returns>True if set, false otherwise.</returns>
        public bool UserAccountControlFlagAdd(ActiveDirectoryUserAccountControl flag)
        {
            if (UserAccountControl == null) return false;
            if (IsUserAccountControl(flag)) return false;

            var newUserAccountControl = UserAccountControl.Value | (int)flag;
            return AttributeSave("userAccountControl", newUserAccountControl.ToString());
        }

        /// <summary>
        /// Removes a flag from the user's User Account Control attribute.
        /// </summary>
        /// <param name="flag">A flag from the predefined UserAccountControl flags.</param>
        /// <returns>True if removed, false otherwise.</returns>
        public bool UserAccountControlFlagRemove(ActiveDirectoryUserAccountControl flag)
        {
            if (UserAccountControl == null) return false;
            if (!IsUserAccountControl(flag)) return false;

            var newUserAccountControl = UserAccountControl.Value & ~(int)flag;
            return AttributeSave("userAccountControl", newUserAccountControl.ToString());
        }

        #endregion UserAccountControls

        #region Methods Instance

        private object DirectoryEntryGet(string propertyName)
        {
            object o = null;
            var de = DirectoryEntry;
            if (de == null) return o;
            try
            {
                o = de.InvokeGet(propertyName);
            }
            catch (Exception e)
            {
                log.Debug("Error retrieving property [" + propertyName + "] from object: " + ToString(), e);
            }
            return o;
        }

        private bool? DirectoryEntryGetBool(string propertyName)
        {
            var o = DirectoryEntryGet(propertyName);
            if (o == null) return null;
            bool? b = null;
            try
            {
                b = Convert.ToBoolean(o);
            }
            catch (Exception e)
            {
                log.Debug("Error converting property [" + propertyName + "] of value [" + o + "] to bool from object: " + ToString(), e);
            }
            return b;
        }

        private DateTime? DirectoryEntryGetDateTime(string propertyName)
        {
            var o = DirectoryEntryGet(propertyName);
            if (o == null) return null;
            DateTime? b = null;
            try
            {
                b = (DateTime)o;
            }
            catch (Exception e)
            {
                log.Debug("Error converting property [" + propertyName + "] of value [" + o + "] to DateTime from object: " + ToString(), e);
            }
            return b;
        }

        private bool AttributeSave(string attributeName, object[] attributeValues)
        {
            var result = activeDirectory.Ldap.AttributeSave(DistinguishedName, attributeName, attributeValues);
            Refresh();
            return result;
        }

        private bool AttributeSave(string attributeName, string attributeValue) => AttributeSave(attributeName, new object[] { attributeValue });

        private IEnumerable<ActiveDirectoryObject> GetMemberOf(bool recursive = true)
        {
            var set = new HashSet<ActiveDirectoryObject>();

            var queue = new Queue<ActiveDirectoryObject>();
            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var m in current.MemberOf)
                {
                    var o = activeDirectory.GetObjectByDistinguishedName(m);
                    if (o == null) continue;
                    var setAdd = set.Add(o);
                    if (recursive && setAdd) queue.Enqueue(o);
                }
            }

            return set;
        }

        private IEnumerable<ActiveDirectoryObject> GetMembers(bool recursive = true)
        {
            var set = new HashSet<ActiveDirectoryObject>();

            var queue = new Queue<ActiveDirectoryObject>();
            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var m in current.Member)
                {
                    var o = activeDirectory.GetObjectByDistinguishedName(m);
                    if (o == null) continue;
                    var setAdd = set.Add(o);
                    if (recursive && setAdd) queue.Enqueue(o);
                }
            }

            return set;
        }

        public void Refresh()
        {
            var obj = activeDirectory.GetObjectByObjectGuid(ObjectGUID, useCache: false);
            Attributes = obj.Attributes;
            userAccountControls = null;
            userPrincipal = null;
            directoryEntry = null;
        }

        public bool RemoveMember(ActiveDirectoryObject activeDirectoryObject)
        {
            if (activeDirectoryObject == null) return false;
            var result = RemoveMember(activeDirectoryObject.DistinguishedName);
            activeDirectoryObject.Refresh();
            return result;
        }

        public bool RemoveMember(string distinguishedName)
        {
            var list = new List<object>();
            var found = false;
            foreach (var member in Member)
            {
                if (string.Equals(distinguishedName, member, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                }
                else
                {
                    list.Add(member);
                }
            }
            if (!found) return false;

            var result = activeDirectory.Ldap.AttributeSave(DistinguishedName, "member", list.ToArray());
            Refresh();
            return result;
        }

        public bool AddMember(ActiveDirectoryObject activeDirectoryObject)
        {
            if (activeDirectoryObject == null) return false;
            var result = AddMember(activeDirectoryObject.DistinguishedName);
            activeDirectoryObject.Refresh();
            return result;
        }

        public bool AddMember(string distinguishedName)
        {
            var list = new List<object>();

            foreach (var member in Member)
            {
                if (string.Equals(distinguishedName, member, StringComparison.OrdinalIgnoreCase))
                {
                    return false; // Already contains member
                }
                else
                {
                    list.Add(member);
                }
            }
            list.Add(distinguishedName);
            var result = activeDirectory.Ldap.AttributeSave(DistinguishedName, "member", list.ToArray());
            Refresh();
            return result;
        }

        public bool IsUserAccountControl(ActiveDirectoryUserAccountControl userAccountControl) => UserAccountControls.Contains(userAccountControl);

        public bool IsObjectClass(string objectClass) => ObjectClass.Any(o => string.Equals(o, objectClass, StringComparison.OrdinalIgnoreCase));

        public bool IsObjectCategory(string objectCategory) => ObjectCategory == null ? false : Util.ParseDistinguishedName(ObjectCategory).Any(o => string.Equals(o.Item2, objectCategory, StringComparison.OrdinalIgnoreCase));

        public IDictionary<string, object> GetProperties(bool includeExpensiveObjects = false)
        {
            var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var prop in GetPropertyInfos(includeExpensiveObjects: true))
            {
                if (!includeExpensiveObjects && ExpensiveProperties.Contains(prop.Name))
                {
                    d[prop.Name] = "[SKIPPED]";
                    continue;
                }
                if (prop.GetCustomAttributes(typeof(IgnoreInPropertiesListAttribute), false).OrEmpty().Any())
                {
                    d[prop.Name] = "[SKIPPED]";
                    continue;
                }
                try
                {
                    d[prop.Name] = Util.GetPropertyValue(this, prop.Name);
                }
                catch (Exception e)
                {
                    log.Warn("Error getting property [" + prop.Name + "] for object " + DistinguishedName);
                    log.Debug("Error getting property [" + prop.Name + "] for object " + DistinguishedName, e);
                }
            }

            return d;
        }

        public IDictionary<string, string> GetPropertiesStrings()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in GetProperties())
            {
                var propKey = kvp.Key;
                var propValue = kvp.Value;

                string val = propValue.ToStringGuessFormat().TrimOrNull();
                if (val == null) continue;
                if (val == "[]") continue;
                if (propValue.GetType() is IEnumerable<ActiveDirectoryObject> enumerableObjects)
                {
                    var list = enumerableObjects.ToList();
                    if (list.Count == 0) continue;
                    var multipleValues = list.Select(o => o.ObjectName).ToStringDelimited(",").TrimOrNull();
                    if (multipleValues == null) continue;
                    val = "[" + multipleValues + "]";
                }

                d[propKey] = val;
            }

            return d;
        }

        public void AddChildOU(string samAccountName, string description)
        {
            var childOU = DirectoryEntry.Children.Add("OU=" + samAccountName, "OrganizationalUnit");
            if (description != null) childOU.Properties["description"].Add(description);
            childOU.CommitChanges();
        }

        public void RemoveChildOU(ActiveDirectoryObject obj)
        {
            DirectoryEntry.Children.Remove(obj.DirectoryEntry);
        }

        #endregion Methods Instance

        #region Methods Static

        public static Table CreateTable(IEnumerable<ActiveDirectoryObject> objects, bool includeExpensiveObjects = false)
        {
            objects = objects ?? Enumerable.Empty<ActiveDirectoryObject>();
            var header = new string[] { "DistinguishedName", "ObjectGUID", "AttributeName", "AttributeValueIndex", "AttributeValue" };
            var data = new List<string[]>();
            foreach (var obj in objects)
            {
                foreach (var kvp in obj.GetProperties(includeExpensiveObjects).OrderBy(o => o.Key, StringComparer.OrdinalIgnoreCase))
                {
                    var attributeName = kvp.Key;
                    var attributeValue = kvp.Value;
                    var v = new List<(int attributeIndex, string attributeValue)>();
                    if (attributeValue != null)
                    {
                        if (attributeValue is string str) v.Add((0, str));
                        else if (attributeValue is byte[] bytes) v.Add((0, "0x" + Util.Base16(bytes)));
                        else if (attributeValue is IEnumerable enumerable)
                        {
                            var index = 0;
                            foreach (var attributeValueObject in enumerable)
                            {
                                if (attributeValueObject is string str2) v.Add((index, str2));
                                else if (attributeValueObject is byte[] bytes2) v.Add((index, "0x" + Util.Base16(bytes2)));
                                else v.Add((index, attributeValueObject.ToStringGuessFormat()));
                                index++;
                            }
                        }
                        else v.Add((0, attributeValue.ToStringGuessFormat()));
                    }

                    foreach (var item in v)
                    {
                        var rowValues = new List<string>
                        {
                            obj.DistinguishedName,
                            obj.ObjectGUID.ToString(),
                            attributeName,
                            item.attributeIndex.ToString(),
                            item.attributeValue
                        };
                        data.Add(rowValues.ToArray());
                    }
                }
            }

            log.Debug("Creating table with header " + header.ToStringDelimited(",") + " and " + data.Count + " rows");
            return Table.Create(data, header);
        }

        private static PropertyInfo[] GetPropertyInfos(bool includeExpensiveObjects = false)
        {
            var list = new List<PropertyInfo>();
            foreach (var prop in typeof(ActiveDirectoryObject).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (!prop.CanRead) continue;
                if (!includeExpensiveObjects && ExpensiveProperties.Contains(prop.Name)) continue;
                if (string.Equals(prop.Name, nameof(Attributes), StringComparison.OrdinalIgnoreCase)) continue;
                list.Add(prop);
            }
            return list.ToArray();
        }

        public static string[] GetPropertyNames(bool includeExpensiveObjects = false) => GetPropertyInfos(includeExpensiveObjects: includeExpensiveObjects).Select(o => o.Name).ToArray();

        #endregion Methods Static

        #region Object

        public override bool Equals(object obj) => Equals(obj as ActiveDirectoryObject);

        public override int GetHashCode() => Util.GenerateHashCode(DistinguishedName?.ToUpper(), ObjectGUID);

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(GetType().NameFormatted());
            sb.Append("[");
            var list = new List<string>();
            if (ObjectName != null) list.Add(ObjectName);
            if (DistinguishedName != null) list.Add(DistinguishedName);
            list.Add(ObjectGUID.ToString());
            sb.Append(list.ToStringDelimited(" "));
            sb.Append("]");
            return sb.ToString();
        }

        #endregion Object

        #region IEquatable

        public bool Equals(ActiveDirectoryObject other) => CompareTo(other) == 0;

        #endregion IEquatable

        #region IComparable

        public int CompareTo(ActiveDirectoryObject other)
        {
            if (other == null) return 1;
            int c = StringComparer.OrdinalIgnoreCase.Compare(DistinguishedName, other.DistinguishedName);
            if (c != 0) return c;
            c = ObjectGUID.CompareTo(other.ObjectGUID);
            if (c != 0) return c;
            return 0;
        }

        #endregion IComparable
    }
}
















