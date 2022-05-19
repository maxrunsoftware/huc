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
using System.Text;

namespace MaxRunSoftware.Utilities
{
    public class SqlMySQL : Sql
    {
        public override IEnumerable<string> GetDatabases() => ExecuteQueryToList("SELECT schema_name FROM information_schema.schemata;");
        public override IEnumerable<string> GetTables(string database, string schema) => ExecuteQueryToList($"SELECT TABLE_NAME FROM information_schema.tables WHERE TABLE_SCHEMA='{Unescape(database)}';");
        public override void DropTable(string database, string schema, string table)
        {
            var dst = Escape(database) + "." + Escape(table);
            var sql = $"DROP TABLE IF EXISTS {dst};";
            ExecuteNonQuery(sql);
        }
        public override IEnumerable<string> GetSchemas(string database) => new List<string>();
        public override IEnumerable<string> GetColumns(string database, string schema, string table) => ExecuteQueryToList($"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='{Unescape(database)}' AND TABLE_NAME = '{Unescape(table)}' ORDER BY ORDINAL_POSITION;");

        public override string GetCurrentDatabase() => ExecuteQueryToList("SELECT DATABASE();").FirstOrDefault();
        public override string GetCurrentSchema() => null;

        public override string TextCreateTableColumn(TableColumn column)
        {
            throw new NotImplementedException();
        }

        public static readonly Func<string, string> ESCAPE_MYSQL = (o =>
        {
            if (o == null) return o;
            if (!o.StartsWith("`")) o = "`" + o;
            if (!o.EndsWith("`")) o = o + "`";
            return o;
        });

        public static readonly Func<string, string> UNESCAPE_MYSQL = (o =>
        {
            if (o == null) return o;
            if (o.StartsWith("`")) o = o.RemoveLeft(1);
            if (o.EndsWith("`")) o = o.RemoveRight(1);
            return o;
        });

        protected override string TextCreateTableColumnTextDataType => "LONGTEXT";

        public SqlMySQL(Func<IDbConnection> connectionFactory) : base(connectionFactory)
        {
            EscapeObject = ESCAPE_MYSQL;
            UnescapeObject = UNESCAPE_MYSQL;
        }


    }
}
