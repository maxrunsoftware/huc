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
using System.Linq;
using System.Text;

namespace HavokMultimedia.Utilities
{
    public class SqlMSSQL : Sql
    {
        public override IEnumerable<string> GetDatabases() => ExecuteQueryToList("SELECT name FROM master.sys.databases;");
        public override IEnumerable<string> GetTables(string database, string schema) => ExecuteQueryToList($"SELECT DISTINCT [TABLE_NAME] FROM {Escape(database)}.INFORMATION_SCHEMA.TABLES WHERE [TABLE_TYPE]='BASE TABLE' AND [TABLE_SCHEMA]='{schema}';");
        public override void DropTable(string database, string schema, string table)
        {

            schema = schema ?? "dbo";
            var dst = Escape(database) + "." + Escape(schema) + "." + Escape(table);

            var sql = $"if exists (select * from {Escape(database)}.INFORMATION_SCHEMA.TABLES where TABLE_NAME = '{table}' AND TABLE_SCHEMA = '{schema}') DROP TABLE {dst};";
            ExecuteNonQuery(sql);
        }
        public override IEnumerable<string> GetSchemas(string database) => ExecuteQueryToList($"SELECT DISTINCT [SCHEMA_NAME] FROM {Escape(database)}.INFORMATION_SCHEMA.SCHEMATA WHERE [CATALOG_NAME]='{database}';");
        public override IEnumerable<string> GetColumns(string database, string schema, string table) => ExecuteQueryToList($"SELECT [COLUMN_NAME] FROM {Escape(database)}.INFORMATION_SCHEMA.COLUMNS WHERE [TABLE_CATALOG]='{database}' AND [TABLE_SCHEMA]='{schema}' AND [TABLE_NAME]='{table}' ORDER BY [ORDINAL_POSITION];");



        public static readonly Func<string, string> ESCAPE_MSSQL = (o =>
        {
            if (o == null) return o;
            if (!o.StartsWith("[")) o = "[" + o;
            if (!o.EndsWith("]")) o = o + "]";
            return o;
        });

        public SqlMSSQL(Func<IDbConnection> connectionFactory) : base(connectionFactory)
        {
            EscapeObject = ESCAPE_MSSQL;
        }


    }

}
