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
    public class SqlMSSQL : Sql
    {
        public override IEnumerable<string> GetDatabases() => ExecuteQueryToList("SELECT name FROM sys.databases;");
        public override IEnumerable<string> GetTables(string database, string schema) => ExecuteQueryToList($"SELECT DISTINCT [TABLE_NAME] FROM {Escape(database)}.INFORMATION_SCHEMA.TABLES WHERE [TABLE_TYPE]='BASE TABLE'" + (schema == null ? "" : $" AND [TABLE_SCHEMA]='{Unescape(schema)}'") + ";");
        public override void DropTable(string database, string schema, string table)
        {
            schema = schema ?? "dbo";
            var dst = Escape(database) + "." + Escape(schema) + "." + Escape(table);

            var sql = $"if exists (select * from {Escape(database)}.INFORMATION_SCHEMA.TABLES where TABLE_NAME = '{Unescape(table)}' AND TABLE_SCHEMA = '{Unescape(schema)}') DROP TABLE {dst};";
            ExecuteNonQuery(sql);
        }
        public override IEnumerable<string> GetSchemas(string database) => ExecuteQueryToList($"SELECT DISTINCT [SCHEMA_NAME] FROM {Escape(database)}.INFORMATION_SCHEMA.SCHEMATA WHERE [CATALOG_NAME]='{Unescape(database)}';");
        public override IEnumerable<string> GetColumns(string database, string schema, string table) => ExecuteQueryToList($"SELECT [COLUMN_NAME] FROM {Escape(database)}.INFORMATION_SCHEMA.COLUMNS WHERE [TABLE_CATALOG]='{Unescape(database)}' AND [TABLE_SCHEMA]='{Unescape(schema)}' AND [TABLE_NAME]='{Unescape(table)}' ORDER BY [ORDINAL_POSITION];");

        public override string GetCurrentDatabase() => ExecuteQueryToList("SELECT DB_NAME();").FirstOrDefault();
        public override string GetCurrentSchema() => ExecuteQueryToList("SELECT SCHEMA_NAME();").FirstOrDefault();

        public static readonly Func<string, string> ESCAPE_MSSQL = (o =>
        {
            if (o == null) return o;
            if (!o.StartsWith("[")) o = "[" + o;
            if (!o.EndsWith("]")) o = o + "]";
            return o;
        });

        public static readonly Func<string, string> UNESCAPE_MSSQL = (o =>
        {
            if (o == null) return o;
            if (o.StartsWith("[")) o = o.RemoveLeft(1);
            if (o.EndsWith("]")) o = o.RemoveRight(1);
            return o;
        });

        protected override string TextCreateTableColumnTextDataType => SqlDbType.NVarChar.ToString() + "(MAX)";

        public SqlMSSQL(Func<IDbConnection> connectionFactory) : base(connectionFactory)
        {
            EscapeObject = ESCAPE_MSSQL;
            UnescapeObject = UNESCAPE_MSSQL;
        }

        public override string TextCreateTableColumn(TableColumn column)
        {
            var sql = new StringBuilder();

            sql.Append(Escape(column.Name));
            sql.Append(' ');

            var len = column.MaxLength;
            if (len < 1) len = 1;

            if (!Constant.MAP_Type_SqlDbType.TryGetValue(column.Type, out var sqlDbType)) sqlDbType = SqlDbType.NVarChar;

            if (sqlDbType.In(SqlDbType.NChar) && len > 4000) sqlDbType = SqlDbType.NVarChar; // NChar doesn't support more then 4000
            if (sqlDbType.In(SqlDbType.Char) && len > 8000) sqlDbType = SqlDbType.VarChar; // Char doesn't support more then 8000

            sql.Append(sqlDbType);

            if (sqlDbType.In(SqlDbType.NChar, SqlDbType.Char)) sql.Append("(" + len + ")");
            else if (sqlDbType.In(SqlDbType.NVarChar)) sql.Append("(" + (len <= 4000 ? len : "MAX") + ")");
            else if (sqlDbType.In(SqlDbType.VarChar)) sql.Append("(" + (len <= 8000 ? len : "MAX") + ")");

            // TODO: fix this to not use 37 and instead calculate actual values
            else if (sqlDbType.In(SqlDbType.Decimal)) sql.Append("(" + (37 - column.NumberOfDecimalDigits) + "," + column.NumberOfDecimalDigits + ")");

            if (!column.IsNullable) sql.Append(" NOT");
            sql.Append(" NULL");

            return sql.ToString();
        }
    }
}
