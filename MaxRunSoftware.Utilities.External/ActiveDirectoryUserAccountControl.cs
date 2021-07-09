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

namespace MaxRunSoftware.Utilities.External
{
    /// <summary>
    /// Flags for use with the UserAccountControl and ms-DS-User-Account-Control-Computed properties of a user.
    /// </summary>
    [Flags()]
    public enum ActiveDirectoryUserAccountControl : int
    {
        /// <summary>
        /// The logon script will be run.
        /// </summary>
        Script = 0x0001,

        /// <summary>
        /// The user account is disabled.
        /// </summary>
        AccountDisable = 0x0002,

        /// <summary>
        /// The home folder is required.
        /// </summary>
        HomeDirRequired = 0x0008,

        /// <summary>
        /// Only available via ms-DS-User-Account-Control-Computed attribute.
        /// </summary>
        Lockout = 0x0010,

        /// <summary>
        /// No password is required.
        /// </summary>
        PasswordNotRequired = 0x0020,

        /// <summary>
        /// The user cannot change the password. This is a permission on the user's object.
        /// For information about how to set this permission, visit the following Web site:
        /// http://msdn2.microsoft.com/en-us/library/aa746398.aspx
        /// </summary>
        PasswordCannotChange = 0x0040,

        /// <summary>
        /// The user can send an encrypted password.
        /// </summary>
        EncryptedTextPasswordAllowed = 0x0080,

        /// <summary>
        /// This is an account for users whose primary account is in another domain. This
        /// account provides user access to this domain, but not to any domain that trusts
        /// this domain. This is sometimes referred to as a local user account.
        /// </summary>
        TempDuplicateAccount = 0x0100,

        /// <summary>
        /// This is a default account type that represents a typical user.
        /// </summary>
        NormalAccount = 0x0200,

        /// <summary>
        /// This is a permit to trust an account for a system domain that trusts other domains.
        /// </summary>
        InterdomainTrustAccount = 0x0800,

        /// <summary>
        /// This is a computer account for a computer that is running Microsoft Windows NT 4.0
        /// Workstation, Microsoft Windows NT 4.0 Server, Microsoft Windows 2000 Professional,
        /// or Windows 2000 Server and is a member of this domain.
        /// </summary>
        WorkstationTrustAccount = 0x1000,

        /// <summary>
        /// This is a computer account for a domain controller that is a member of this domain.
        /// </summary>
        ServerTrustAccount = 0x2000,

        /// <summary>
        /// Represents the password, which should never expire on the account.
        /// </summary>
        DoNotExpirePassword = 0x10000,

        /// <summary>
        /// This is an MNS logon account.
        /// </summary>
        MNSLogonAccount = 0x20000,

        /// <summary>
        /// When this flag is set, it forces the user to log on by using a smart card.
        /// </summary>
        SmartCardRequired = 0x40000,

        /// <summary>
        /// When this flag is set, the service account (the user or computer account) under which
        /// a service runs is trusted for Kerberos delegation. Any such service can impersonate
        /// a client requesting the service. To enable a service for Kerberos delegation, you must
        /// set this flag on the userAccountControl property of the service account.
        /// </summary>
        TrustedForDelgation = 0x80000,

        /// <summary>
        /// When this flag is set, the security context of the user is not delegated to a service
        /// even if the service account is set as trusted for Kerberos delegation.
        /// </summary>
        NotDelegated = 0x100000,

        /// <summary>
        /// (Windows 2000/Windows Server 2003) Restrict this principal to use only Data Encryption
        /// Standard (DES) encryption for keys.
        /// </summary>
        UseDESKeyOnly = 0x200000,

        /// <summary>
        /// (Windows 2000/Windows Server 2003) This account does not require Kerberos pre-authentication
        /// for logging on.
        /// </summary>
        DoNotRequirePreauth = 0x400000,

        /// <summary>
        /// Only available via ms-DS-User-Account-Control-Computed attribute.
        /// (Windows 2000/Windows Server 2003) The user's password has expired.
        /// </summary>
        PasswordExpired = 0x800000,

        /// <summary>
        /// (Windows 2000/Windows Server 2003) The account is enabled for delegation. This is a security-sensitive
        /// setting. Accounts that have this option enabled should be tightly controlled. This setting lets a service
        /// that runs under the account assume a client's identity and authenticate as that user to other remote servers
        /// on the network.
        /// </summary>
        TrustedToAuthForDelegation = 0x1000000,

        /// <summary>
        /// Only available via ms-DS-User-Account-Control-Computed attribute.
        /// (Windows Server 2008/Windows Server 2008 R2) The account is a read-only domain controller (RODC). This is a
        /// security-sensitive setting. Removing this setting from an RODC compromises security on that server.
        /// </summary>
        PartialSecretsAccount = 0x4000000,

        /// <summary>
        /// Only available via ms-DS-User-Account-Control-Computed attribute.
        /// </summary>
        UseAESKeys = 0x8000000
    }
}
















