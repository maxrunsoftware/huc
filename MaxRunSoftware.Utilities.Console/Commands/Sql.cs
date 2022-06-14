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

using System.IO;
using System.Linq;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class Sql : SqlQueryBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Execute a SQL statement and/or script and optionally save the result(s) to a tab delimited file(s)");
        help.AddValue("<result file 1> <result file 2> <etc>");
        help.AddExample(HelpExamplePrefix + " -s=`SELECT TOP 100 * FROM Orders` Orders100.txt");
        help.AddExample(HelpExamplePrefix + " -s=`SELECT * FROM Orders; SELECT * FROM Employees` Orders.txt Employees.txt");
        // ReSharper disable StringLiteralTypo
        help.AddExample(HelpExamplePrefix + " -f=`mssqlscript.sql` OrdersFromScript.txt");
        // ReSharper restore StringLiteralTypo
    }

    protected override void ExecuteInternal()
    {
        var resultFiles = GetArgValuesTrimmed();
        for (var i = 0; i < resultFiles.Count; i++)
        {
            if (resultFiles[i] == null)
            {
                continue;
            }

            resultFiles[i] = Path.GetFullPath(resultFiles[i]);
            DeleteExistingFile(resultFiles[i]);
        }

        for (var i = 0; i < resultFiles.Count; i++)
        {
            log.Debug($"resultFiles[{i}]: {resultFiles[i]}");
        }

        var tables = ExecuteTables();

        for (var i = 0; i < tables.Length; i++)
        {
            var table = tables[i];
            var rf = resultFiles.ElementAtOrDefault(i);

            if (rf != null)
            {
                WriteTableTab(rf, table);
            }
        }

        log.Debug("SQL completed");
    }
}
