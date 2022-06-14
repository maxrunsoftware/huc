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
using System.Linq;
using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class ActiveDirectoryDisableUsers : ActiveDirectoryBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Disables users in ActiveDirectory");
        //help.AddDetail("Requires LDAPS configured on the server");
        help.AddParameter(nameof(lastLoginDays), "l", "Matches any users whose last login was days older then " + nameof(lastLoginDays) + " integer exclusive");
        help.AddParameter(nameof(execute), "exe", "Actually execute the disables (false)");
        help.AddParameter(nameof(excludeUsers), "eu", "User accounts to exclude seperated by comma or semicolon or pipe");
        help.AddParameter(nameof(excludeUsers), "eg", "Group accounts that users are members of to exclude seperated by comma or semicolon or pipe");
        help.AddExample(HelpExamplePrefix + " testuser");
    }

    private bool execute;
    private uint? lastLoginDays;
    private string[] excludeUsers;
    private string[] excludeGroups;

    protected override void ExecuteInternal(ActiveDirectory ad)
    {
        execute = GetArgParameterOrConfigBool(nameof(execute), "exe", false);
        excludeUsers =
            (GetArgParameterOrConfig(nameof(excludeUsers), "eu").TrimOrNull() ?? "")
            .Split(',', ';', '|')
            .TrimOrNull()
            .WhereNotNull()
            .ToArray();

        excludeGroups =
            (GetArgParameterOrConfig(nameof(excludeGroups), "eg").TrimOrNull() ?? "")
            .Split(',', ';', '|')
            .TrimOrNull()
            .WhereNotNull()
            .ToArray();


        var lastLoginDaysS = GetArgParameterOrConfig(nameof(lastLoginDays), "l").TrimOrNull();
        if (lastLoginDaysS != null) lastLoginDays = lastLoginDaysS.ToUInt();

        if (lastLoginDays == null) throw new ArgsException(nameof(lastLoginDays), "No criteria to disable user accounts specified");

        var users = ad.GetUsers();
        var usersToBeDisabled = new List<(ActiveDirectoryObject, string)>();

        if (lastLoginDays != null)
        {
            var daysAgo = (int)lastLoginDays.Value * -1;
            var dateDaysAgo = DateTime.UtcNow.AddDays(daysAgo);

            foreach (var user in users)
            {
                if (ShouldSkip(user)) continue;

                var lastLogon = DateTime.MinValue;
                if (user.LastLogon != null)
                    if (user.LastLogon.Value > lastLogon)
                        lastLogon = user.LastLogon.Value;

                if (user.LastLogonTimestamp != null)
                    if (user.LastLogonTimestamp.Value > lastLogon)
                        lastLogon = user.LastLogonTimestamp.Value;

                lastLogon = lastLogon.ToUniversalTime();

                if (lastLogon < dateDaysAgo)
                {
                    var msg = "lastLogin: " + lastLogon.ToLocalTime().ToStringYYYYMMDD();
                    if (lastLogon == DateTime.MinValue.ToUniversalTime()) msg = "never logged in";

                    usersToBeDisabled.Add((user, msg));
                }
            }
        }

        usersToBeDisabled = usersToBeDisabled
            .Where(o => !ShouldSkip(o.Item1))
            .OrderBy(o => o.Item1.ObjectName.ToUpper())
            .ToList();

        foreach (var item in usersToBeDisabled)
        {
            var userToBeDisabled = item.Item1;
            var msg = execute ? "Disabling User: " : "Execute Would Disable User: ";
            log.Info(msg + userToBeDisabled.ObjectName + " (" + item.Item2 + ")");

            if (execute)
                try
                {
                    var result = ad.DisableUser(userToBeDisabled.SAMAccountName);
                    log.Debug((result ? "User disabled: " : "User was already disabled: ") + userToBeDisabled.ObjectName);
                }
                catch (Exception e) { log.Error("Error disabling user '" + userToBeDisabled.SAMAccountName + "'", e); }
        }
    }


    private bool ShouldSkip(ActiveDirectoryObject ado)
    {
        var accountsToSkip = new List<string>
        {
            "Administrator",
            "Guest"
        };
        accountsToSkip.AddRange(excludeUsers.OrEmpty());


        var groupsToSkip = new List<string>
        {
            "Administrators",
            "Domain Admins",
            "Enterprise Admins"
        };
        groupsToSkip.AddRange(excludeGroups.OrEmpty());


        if (ado.SAMAccountName == null) return true;


        foreach (var accountToSkip in accountsToSkip)
        {
            if (ado.SAMAccountName.EqualsCaseInsensitive(accountToSkip)) return true;

            if (ado.ObjectName.EqualsCaseInsensitive(accountToSkip)) return true;
        }

        foreach (var groupToSkip in groupsToSkip)
        {
            foreach (var mem in ado.MemberOfNames)
                if (groupToSkip.EqualsCaseInsensitive(mem))
                    return true;
        }

        return false;
    }
}
