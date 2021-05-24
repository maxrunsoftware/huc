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

namespace HavokMultimedia.Utilities.Console.External
{
    public static class ActiveDirectoryExtensions
    {
        public static void Add(this List<DirectoryAttribute> list, string name, params string[] values)
        {
            var attrValues = new List<string>();
            foreach (var value in values.OrEmpty()) attrValues.Add(value);
            attrValues = attrValues.TrimOrNull().WhereNotNull().ToList();
            DirectoryAttribute a;
            if (attrValues.Count < 2) a = new DirectoryAttribute(name, attrValues.First());
            else a = new DirectoryAttribute(name, attrValues.ToArray());
            list.Add(a);
        }

        public static List<DirectoryAttribute> ToDirectoryAttributes(this IDictionary<string, List<string>> directoryAttributes)
        {
            var list = new List<DirectoryAttribute>();
            foreach (var kvp in directoryAttributes)
            {
                list.Add(kvp.Key, kvp.Value.ToArray());
            }
            return list;
        }

        private static T FindBySamAccountName<T>(T principal, string samAccountName) where T : Principal
        {
            PrincipalSearcher s = new PrincipalSearcher();
            principal.SamAccountName = samAccountName;
            s.QueryFilter = principal;
            foreach (Principal p in s.FindAll())
            {
                if (p is T pp) return pp;
            }
            return null;
        }

        public static UserPrincipal FindUserBySamAccountName(this PrincipalContext context, string samAccountName) => FindBySamAccountName(new UserPrincipal(context), samAccountName);
        public static UserPrincipal FindUserBySamAccountNameRequired(this PrincipalContext context, string samAccountName)
        {
            var p = FindUserBySamAccountName(context, samAccountName);
            if (p == null) throw new Exception($"User {samAccountName} not found");
            return p;
        }

        public static GroupPrincipal FindGroupBySamAccountName(this PrincipalContext context, string samAccountName) => FindBySamAccountName(new GroupPrincipal(context), samAccountName);
        public static GroupPrincipal FindGroupBySamAccountNameRequired(this PrincipalContext context, string samAccountName)
        {
            var p = FindGroupBySamAccountName(context, samAccountName);
            if (p == null) throw new Exception($"Group {samAccountName} not found");
            return p;
        }

        public static ComputerPrincipal FindComputerBySamAccountName(this PrincipalContext context, string samAccountName) => FindBySamAccountName(new ComputerPrincipal(context), samAccountName);
        public static ComputerPrincipal FindComputerBySamAccountNameRequired(this PrincipalContext context, string samAccountName)
        {
            var p = FindComputerBySamAccountName(context, samAccountName);
            if (p == null) throw new Exception($"Computer {samAccountName} not found");
            return p;
        }



    }



}
















