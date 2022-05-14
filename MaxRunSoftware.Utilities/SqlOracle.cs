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
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MaxRunSoftware.Utilities
{
    public class SqlOracle : Sql
    {
        public override IEnumerable<string> GetDatabases() => ExecuteQueryToList("SELECT NAME FROM v$database;");
        public override IEnumerable<string> GetTables(string database, string schema) => ExecuteQueryToList($"SELECT table_name FROM all_tables;");
        public override void DropTable(string database, string schema, string table)
        {
            // TODO: Check if the DROP TABLE part needs escaping or not
            var sql = $@"
                declare c int;
                begin 
                   select count(*) into c from user_tables where table_name = upper('{Unescape(table)}'); 
                   if c = 1 then
                      execute immediate 'drop table {table}'; 
                   end if;
                end;
            ";
            ExecuteNonQuery(sql);
        }
        public override IEnumerable<string> GetSchemas(string database) => ExecuteQueryToList($"SELECT username FROM dba_users");
        public override IEnumerable<string> GetColumns(string database, string schema, string table) => ExecuteQueryToList($"SELECT column_name FROM USER_TAB_COLUMNS WHERE table_name = '{Unescape(table)}';");

        public override string GetCurrentDatabase() => ExecuteQueryToSingle("select name from v$database;", "select ora_database_name from dual;", "select global_name from global_name;");
        public override string GetCurrentSchema() => ExecuteQueryToSingle("select SYS_CONTEXT('USERENV','CURRENT_SCHEMA') from dual;");

        private string ExecuteQueryToSingle(params string[] sqls)
        {
            foreach (var sql in sqls)
            {
                try
                {
                    var val = ExecuteQueryToList(sql).FirstOrDefault();
                    if (val != null) return val;
                }
                catch (Exception e)
                {
                    log.Debug("Error Executing SQL: " + sql, e);
                }
            }
            return null;
        }

        public static readonly Func<string, string> ESCAPE_ORACLE = (o =>
        {
            if (o == null) return o;
            if (!o.StartsWith("\"")) o = "\"" + o;
            if (!o.EndsWith("\"")) o = o + "\"";
            return o;
        });
        public static readonly Func<string, string> UNESCAPE_ORACLE = (o =>
        {
            if (o == null) return o;
            if (o.StartsWith("\"")) o = o.RemoveLeft(1);
            if (o.EndsWith("\"")) o = o.RemoveRight(1);
            return o;
        });

        public SqlOracle(Func<IDbConnection> connectionFactory) : base(connectionFactory)
        {
            EscapeObject = ESCAPE_ORACLE;
            UnescapeObject = UNESCAPE_ORACLE;
        }


    }
}
