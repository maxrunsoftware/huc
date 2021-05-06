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

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class Sql : SqlBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Execute a SQL statement and/or script and optionally save the result(s) to a tab delimited file(s)");
            help.AddParameter("sqlStatement", "s", "SQL statement to execute");
            help.AddParameter("sqlScriptFile", "f", "SQL script file to execute");
            help.AddValue("<result file 1> <result file 2> <etc>");
            help.AddExample("-c=`Server=192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;` -s=`SELECT TOP 100 * FROM Orders` Orders100.txt");
            help.AddExample("-c=`Server=192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;` -s=`SELECT * FROM Orders; SELECT * FROM Employees` Orders.txt Employees.txt");
            help.AddExample("-c=`Server=192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;` -f=`mssqlscript.sql` OrdersFromScript.txt");
        }

        public Utilities.Table[] ExecuteTables()
        {
            base.ExecuteInternal();

            var s = GetArgParameterOrConfig("sqlStatement", "s");

            var f = GetArgParameterOrConfig("sqlScriptFile", "f").TrimOrNull();

            string fData = null;
            if (f != null) fData = ReadFile(f);
            if (fData.TrimOrNull() != null) log.Debug($"sqlScriptFileData: {fData.Length}");

            if (s.TrimOrNull() == null && fData.TrimOrNull() == null) throw new Exception($"No SQL provided to execute");
            var sql = (s ?? string.Empty) + Constant.NEWLINE_WINDOWS + (fData ?? string.Empty);
            sql = sql.TrimOrNull();
            if (sql == null) throw new ArgsException("sqlStatement", "No SQL provided to execute");
            log.Debug($"sql: {sql}");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var c = GetSqlHelper();
            var tables = c.ExecuteQuery(sql);

            stopwatch.Stop();
            var stopwatchtime = stopwatch.Elapsed.TotalSeconds.ToString(MidpointRounding.AwayFromZero, 3);
            log.Info($"Completed SQL execution in {stopwatchtime} seconds");

            log.Debug($"Successfully retrieved {tables.Length} results from SQL");

            return tables;
        }

        protected override void ExecuteInternal()
        {
            var resultFiles = GetArgValuesTrimmed();
            for (var i = 0; i < resultFiles.Count; i++)
            {
                if (resultFiles[i] == null) continue;
                resultFiles[i] = Path.GetFullPath(resultFiles[i]);
                DeleteExistingFile(resultFiles[i]);
            }
            for (var i = 0; i < resultFiles.Count; i++) log.Debug($"resultFiles[{i}]: {resultFiles[i]}");

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
}
