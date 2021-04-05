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
using System.Data;
using System.Text;

namespace HavokMultimedia.Utilities
{
    public class SqlOracle : Sql
    {
        public override IEnumerable<string> GetDatabases() => ExecuteQueryToList("SELECT NAME FROM v$database;");
        public override IEnumerable<string> GetTables(string database, string schema) => ExecuteQueryToList($"SELECT table_name FROM all_tables;");
        public override void DropTable(string database, string schema, string table)
        {
            var sql = new StringBuilder();
            sql.AppendLine($"declare c int; ");
            sql.AppendLine($"begin ");
            sql.AppendLine($"   select count(*) into c from user_tables where table_name = upper('{table}'); ");
            sql.AppendLine($"   if c = 1 then ");
            sql.AppendLine($"      execute immediate 'drop table {table}'; ");
            sql.AppendLine($"   end if; ");
            sql.AppendLine($"end;");

            ExecuteNonQuery(sql.ToString());
        }
        public override IEnumerable<string> GetSchemas(string database) => ExecuteQueryToList($"SELECT username FROM dba_users");
        public override IEnumerable<string> GetColumns(string database, string schema, string table) => ExecuteQueryToList($"SELECT column_name FROM USER_TAB_COLUMNS WHERE table_name = '{table}';");

        public static readonly Func<string, string> ESCAPE_ORACLE = (o =>
        {
            if (o == null) return o;
            if (!o.StartsWith("\"")) o = "\"" + o;
            if (!o.EndsWith("\"")) o = o + "\"";
            return o;
        });

        public SqlOracle(Func<IDbConnection> connectionFactory) : base(connectionFactory)
        {
            EscapeObject = ESCAPE_ORACLE;
        }


    }
}
