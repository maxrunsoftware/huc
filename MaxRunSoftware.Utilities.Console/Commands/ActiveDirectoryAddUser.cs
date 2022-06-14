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

using MaxRunSoftware.Utilities.External;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo

namespace MaxRunSoftware.Utilities.Console.Commands;

public class ActiveDirectoryAddUser : ActiveDirectoryBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Adds a new user to ActiveDirectory");
        help.AddParameter(nameof(firstname), "fn", "The firstname of the new user");
        help.AddParameter(nameof(lastname), "ln", "The lastname of the new user");
        help.AddParameter(nameof(displayname), "dn", "The display name of the new user");
        help.AddParameter(nameof(emailaddress), "ea", "The email address of the new user");
        help.AddValue("<SAMAccountName>");
        help.AddExample(HelpExamplePrefix + " testuser");
        help.AddExample(HelpExamplePrefix + " -fn=First -ln=Last -dn=MyUser testuser");
    }

    private string firstname;
    private string lastname;
    private string displayname;
    private string emailaddress;

    protected override void ExecuteInternal(ActiveDirectory ad)
    {
        firstname = GetArgParameterOrConfig(nameof(firstname), "fn").TrimOrNull();
        lastname = GetArgParameterOrConfig(nameof(lastname), "ln").TrimOrNull();
        displayname = GetArgParameterOrConfig(nameof(displayname), "dn").TrimOrNull();
        emailaddress = GetArgParameterOrConfig(nameof(emailaddress), "ea").TrimOrNull();

        var samAccountName = GetArgValueTrimmed(0);
        samAccountName.CheckValueNotNull(nameof(samAccountName), log);

        log.Debug("Adding user: " + samAccountName);
        ad.AddUser(
            samAccountName,
            displayname,
            firstname,
            lastname,
            emailAddress: emailaddress
        );
        log.Info("Successfully added user " + samAccountName);

        //log.Debug("Changing password for user " + samAccountName + "   " + ou);
        //ado.Password = userpassword;
        //log.Info("Successfully set password for user " + samAccountName + "   " + ou);
    }
}
