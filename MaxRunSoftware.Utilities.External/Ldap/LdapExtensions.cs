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
using System.Text;

namespace MaxRunSoftware.Utilities.External;

public static class LdapExtensions
{
    private static readonly ILogger log = Logging.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

    private static string ToStringDebugOutput(object o)
    {
        var sb = new StringBuilder();
        foreach (var prop in ClassReaderWriter.GetProperties(o.GetType(), canGet: true, isInstance: true))
        {
            object val = null;
            try
            {
                val = prop.GetValue(o);
            }
            catch (Exception e)
            {
                log.Debug("Error retrieving property " + o.GetType().FullNameFormatted() + "." + prop.Name, e);
            }

            sb.AppendLine("    " + prop.Name + ": " + val.ToStringGuessFormat());
        }
        return sb.ToString();
    }

    public static string ToStringDebug(this System.DirectoryServices.DirectoryEntry entry) => ToStringDebugOutput(entry);

    public static string ToStringDebug(this System.DirectoryServices.DirectorySearcher searcher) => ToStringDebugOutput(searcher);

    public static string ToStringDebug(this System.DirectoryServices.AccountManagement.PrincipalContext context) => ToStringDebugOutput(context);
}
