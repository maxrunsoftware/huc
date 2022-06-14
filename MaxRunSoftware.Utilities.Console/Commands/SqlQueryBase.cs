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
using System.Diagnostics;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public abstract class SqlQueryBase : SqlBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddParameter(nameof(sqlStatement), "s", "SQL statement to execute");
            help.AddParameter(nameof(sqlScriptFile), "f", "SQL script file to execute");

            help.AddDetail("If both -" + nameof(sqlStatement) + " and -" + nameof(sqlScriptFile) + " are specified then the SQL of both is combined with the -" + nameof(sqlStatement) + " executing first");
        }

        private string sqlStatement;
        private string sqlScriptFile;

        public string GetSql()
        {
            sqlStatement = GetArgParameterOrConfig(nameof(sqlStatement), "s");
            sqlScriptFile = GetArgParameterOrConfig(nameof(sqlScriptFile), "f").TrimOrNull();

            string sqlScriptFileData = null;
            if (sqlScriptFile != null) sqlScriptFileData = ReadFile(sqlScriptFile);
            if (sqlScriptFileData.TrimOrNull() != null) log.DebugParameter(nameof(sqlScriptFileData), sqlScriptFileData.Length);

            if (sqlStatement.TrimOrNull() == null && sqlScriptFileData.TrimOrNull() == null) throw new ArgsException(nameof(sqlStatement), "No SQL provided to execute");
            var sql = (sqlStatement ?? string.Empty) + Constant.NEWLINE_WINDOWS + (sqlScriptFileData ?? string.Empty);
            sql = sql.TrimOrNull();
            if (sql == null) throw new ArgsException(nameof(sqlStatement), "No SQL provided to execute");
            log.DebugParameter(nameof(sql), sql);

            return sql;
        }

        public Utilities.Table[] ExecuteTables()
        {
            base.ExecuteInternal();

            var sql = GetSql();

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





    }
}
