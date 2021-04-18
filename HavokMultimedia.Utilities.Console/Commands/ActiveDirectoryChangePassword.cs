﻿/*
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

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class ActiveDirectoryChangePassword : ActiveDirectoryBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Changes the password of a user in ActiveDirectory");
            help.AddValue("<SAMAccountName> <new password>");
        }

        protected override void Execute()
        {
            base.Execute();
            var values = GetArgValues().TrimOrNull().WhereNotNull();

            var samAccountName = values.GetAtIndexOrDefault(0);
            log.Debug(nameof(samAccountName) + ": " + samAccountName);
            if (samAccountName == null) throw new ArgsException(nameof(samAccountName), $"No {nameof(samAccountName)} specified");

            var newPassword = values.GetAtIndexOrDefault(1);
            log.Debug(nameof(newPassword) + ": " + newPassword);
            if (newPassword == null) throw new ArgsException(nameof(newPassword), $"No {nameof(newPassword)} specified");

            using (var ad = GetActiveDirectory())
            {
                var ado = ad.GetObjectBySAMAccountName(samAccountName);
                if (ado == null)
                {
                    log.Info($"User not found.");
                }
                else
                {
                    ado.Password = newPassword;
                    log.Info($"Password changed successfully");
                }
            }


        }
    }
}