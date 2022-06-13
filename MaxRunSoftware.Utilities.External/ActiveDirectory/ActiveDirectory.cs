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
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;

namespace MaxRunSoftware.Utilities.External;

/// <summary>
/// ActiveDirectory is a class that allows for the query and manipulation
/// of Active Directory objects.
/// </summary>
public class ActiveDirectory : ActiveDirectoryCore
{
    private readonly PrincipalContext context;
    
    /// <summary>
    /// Constructs an Active Directory object with a base of the specified OU. Binds to Active Directory.
    /// </summary>
    /// <param name="server">The DNS style domain name of the Active Directory to connect to.</param>
    /// <param name="ldapPort">(Optional)The port to use to connect</param>
    /// <param name="userName">The username of the account in AD to use when making the connection.</param>
    /// <param name="password">The password of the account.</param>
    /// <param name="siteName">(Optional)The name of a site in Active Directory to use the domain controllers from. Defaults to DEFAULT_FIRST_SITE_NAME if not supplied.</param>
    /// <param name="ouDn">(Optional)The distinguished name of the OU to use as a base for operations or use DistinguishedName if null.</param>
    /// <param name="domainName">(Optional)The domain name of the connection.</param>
    public ActiveDirectory(
        string server = null,
        ushort ldapPort = Ldap.LDAP_PORT,
        string userName = null,
        string password = null,
        string siteName = DEFAULT_FIRST_SITE_NAME,
        string ouDn = null,
        string domainName = null) : base(
        server: server,
        ldapPort: ldapPort,
        userName: userName,
        password: password,
        siteName: siteName,
        ouDn: ouDn,
        domainName: domainName
    )
    {
        context = new PrincipalContext(
            ContextType.Domain,
            ipaddress,
            null,
            ContextOptions.Negotiate,
            username,
            password);
    }




    public void AddUserToGroup(string userSamAccountName, string groupSamAccountName)
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

    public void RemoveUserFromGroup(string userSamAccountName, string groupSamAccountName)
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
        using (var up = new UserPrincipal(context))
        {
            up.SamAccountName = samAccountName;
            up.UserPrincipalName = samAccountName + "@" + DomainName;
            up.DisplayName = displayName ?? samAccountName;
            up.GivenName = firstName ?? samAccountName;
            up.Surname = lastName ?? samAccountName;
            up.Name = name ?? samAccountName;
            if (emailAddress != null) up.EmailAddress = emailAddress;
            up.Enabled = true;
            //up.ExpirePasswordNow();
            up.Save();
        }

        var activeDirectoryObject = GetObjectBySAMAccountName(samAccountName);
        MoveObject(activeDirectoryObject, DomainUsersDN);
    }

    private List<ActiveDirectoryObject> FindOU(string ouName)
    {
        if (ouName.EqualsCaseInsensitive(DistinguishedName))
        {
            return GetObjectByDistinguishedName(DistinguishedName).Yield().WhereNotNull().ToList();
        }

        var d = new Dictionary<string, ActiveDirectoryObject>(StringComparer.OrdinalIgnoreCase);

        var ous = this.GetOUs();
        if (ouName.Contains(","))
        {
            // full distinguished name
            foreach (var ou in ous) if (ou.DistinguishedName.EqualsCaseInsensitive(ouName)) d[ou.DistinguishedName] = ou;
        }
        else
        {
            // SAM account name of OU
            foreach (var ou in ous) if (ou.SAMAccountName != null && ou.SAMAccountName.EqualsCaseInsensitive(ouName)) d[ou.DistinguishedName] = ou;
            foreach (var ou in ous) if (ou.Name != null && ou.Name.EqualsCaseInsensitive(ouName)) d[ou.DistinguishedName] = ou;
            foreach (var ou in ous) if (ou.DisplayName != null && ou.DisplayName.EqualsCaseInsensitive(ouName)) d[ou.DistinguishedName] = ou;
            foreach (var ou in ous) if (ou.ObjectName != null && ou.ObjectName.EqualsCaseInsensitive(ouName)) d[ou.DistinguishedName] = ou;
        }
        return d.Values.ToList();
    }

    public void AddOU(string samAccountName, string parentOUName = null, string description = null)
    {
        parentOUName ??= DistinguishedName.TrimOrNull();
        if (parentOUName == null) throw new Exception("Could not determine top level OU");

        var parentOUs = FindOU(parentOUName);
        if (parentOUs.IsEmpty()) throw new Exception("Parent OU \"" + parentOUName + "\" not found");
        if (parentOUs.Count > 1) throw new Exception("Multiple potential parents found, use DistinguishedName...  " + parentOUs.Select(o => o.DistinguishedName).ToStringDelimited("  "));
        var parentOU = parentOUs.First();

        parentOU.AddChildOU(samAccountName, description);
    }

    public void RemoveOU(string samAccountNameOrDn)
    {
        var ous = FindOU(samAccountNameOrDn);
        if (ous.IsEmpty()) throw new Exception("OU \"" + samAccountNameOrDn + "\" not found");
        if (ous.Count > 1) throw new Exception("Multiple OUs found, use DistinguishedName...  " + ous.Select(o => o.DistinguishedName).ToStringDelimited("  "));
        var ou = ous.First();
        var ouChildren = ou.Children.ToList();
        log.Debug("OU " + ou.DistinguishedName + " contains " + ouChildren.Count + " children objects");
        if (!ouChildren.IsEmpty()) throw new Exception("Cannot remove OU " + ou.DistinguishedName + " because it contains " + ouChildren.Count + " objects...  " + ouChildren.Select(o => o.DistinguishedName).ToStringDelimited("  "));
        var ouParent = ou.Parent;
        if (ouParent == null) throw new Exception("Could not determine parent OU of object " + ou.DistinguishedName);

        ouParent.RemoveChildOU(ou);
    }

    public void AddGroup(string samAccountName, ActiveDirectoryGroupType groupType = ActiveDirectoryGroupType.GlobalSecurityGroup)
    {
        if (!IsGroupNameValid(samAccountName)) throw new Exception($"Group name {samAccountName} is an invalid name");

        using (var up = new GroupPrincipal(context))
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

    public void RemoveUser(string samAccountName)
    {
        var p = context.FindUserBySamAccountNameRequired(samAccountName);
        p.Delete();
    }

    public void RemoveGroup(string samAccountName)
    {
        var p = context.FindGroupBySamAccountNameRequired(samAccountName);
        p.Delete();
    }

    public bool DisableUser(string samAccountName)
    {
        var p = context.FindUserBySamAccountNameRequired(samAccountName);
        var enabledN = p.Enabled;
        if (enabledN == null || enabledN.Value)
        {
            p.Enabled = false;
            p.Save();
            return true;
        }
        else
        {
            return false;
        }

    }
    public bool EnableUser(string samAccountName)
    {
        var p = context.FindUserBySamAccountNameRequired(samAccountName);
        var enabledN = p.Enabled;
        if (enabledN == null || !enabledN.Value)
        {
            p.Enabled = true;
            p.Save();
            return true;
        }
        else
        {
            return false;
        }
    }

    public string GetUserOU(string samAccountName)
    {
        var user = context.FindUserBySamAccountNameRequired(samAccountName);
        if (user.GetUnderlyingObject() is not DirectoryEntry de) return null;
     
        // ReSharper disable ConstantConditionalAccessQualifier
        return de.Parent?.Properties["distinguishedName"]?.Value?.ToString();
        // ReSharper restore ConstantConditionalAccessQualifier
    }

    public void ChangePassword(string samAccountName, string newPassword)
    {
        var user = context.FindUserBySamAccountNameRequired(samAccountName);

        user.SetPassword(newPassword);
        user.Enabled = true;
        user.UnlockAccount();
        user.Save();
    }

    public void MoveUser(string samAccountName, string newOuSamAccountName)
    {
        var user = context.FindUserBySamAccountNameRequired(samAccountName);
        var userDn = user.DistinguishedName;

        var userObject = GetObjectBySAMAccountName(samAccountName);
        if (userObject == null) throw new Exception($"Could not locate user object for {userDn}");

        if (newOuSamAccountName.EqualsCaseInsensitive("Users"))
        {
            MoveObject(userObject, DomainUsersDN);
        }
        else
        {
            var ou = this.GetOUByName(newOuSamAccountName);
            if (ou == null) throw new Exception($"Could not find OU named {newOuSamAccountName}");

            log.Debug($"Moving user {userObject.DistinguishedName} to {ou.DistinguishedName}");
            MoveObject(userObject, ou.DistinguishedName);
        }
    }

    public void MoveGroup(string samAccountName, string newOuSamAccountName)
    {
        var group = context.FindGroupBySamAccountNameRequired(samAccountName);
        var groupDn = group.DistinguishedName;

        var groupObject = GetObjectBySAMAccountName(samAccountName);
        if (groupObject == null) throw new Exception($"Could not locate group object for {groupDn}");

        if (newOuSamAccountName.EqualsCaseInsensitive("Users"))
        {
            MoveObject(groupObject, DomainUsersDN);
        }
        else
        {
            var ou = this.GetOUByName(newOuSamAccountName);
            if (ou == null) throw new Exception($"Could not find OU named {newOuSamAccountName}");

            log.Debug($"Moving group {groupObject.DistinguishedName} to {ou.DistinguishedName}");
            MoveObject(groupObject, ou.DistinguishedName);
        }
    }

    #region IDisposable

    /// <summary>
    /// Releases underlying resources associated with the Active Directory connection.
    /// </summary>
    public override void Dispose()
    {
        if (context != null)
        {
            try
            {
                context.Dispose();
            }
            catch (Exception e)
            {
                log.Warn("Error disposing of " + context.GetType().FullName, e);
            }
        }
        base.Dispose();
    }

    #endregion IDisposable
}