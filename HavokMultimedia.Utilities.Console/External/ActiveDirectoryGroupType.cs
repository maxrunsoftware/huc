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

namespace HavokMultimedia.Utilities.Console.External
{
    /// <summary>
    /// GroupType enumerates the type of group objects in Active Directory.
    /// </summary>
    public enum ActiveDirectoryGroupType : int
    {
        /// <summary>
        /// Specifies a group that can contain accounts from the domain and other global
        /// groups from the same domain. This type of group can be exported to a different
        /// domain.
        /// </summary>
        GlobalDistributionGroup = 2,

        /// <summary>
        /// Specifies a group that can contain accounts from any domain, other domain
        /// local groups from the same domain, global groups from any domain, and
        /// universal groups. This type of group should not be included in access-control
        /// lists of resouces in other domains. This type of group is intended for use
        /// with the LDAP provider.
        /// </summary>
        LocalDistributionGroup = 4,

        /// <summary>
        /// Specifies a group that can contain accounts from any domain, global
        /// groups from any domain, and other universal groups. This type of group
        /// cannot contain domain local groups.
        /// </summary>
        UniversalDistributionGroup = 8,

        GlobalSecurityGroup = -2147483646,

        LocalSecurityGroup = -2147483644,

        BuiltInGroup = -2147483643,

        UniversalSecurityGroup = -2147483640
    }
}
















