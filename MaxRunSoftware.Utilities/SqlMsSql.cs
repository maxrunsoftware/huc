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
    public class SqlMsSql : Sql
    {

        public override Type DbTypesEnum => typeof(SqlMsSqlType);

        public SqlMsSql()
        {
            ExcludedDatabases.Add("master", "model", "msdb", "tempdb");
            DefaultDataTypeString = GetSqlDbType(SqlMsSqlType.NVarChar).SqlTypeName + "(MAX)";
            DefaultDataTypeInteger = GetSqlDbType(SqlMsSqlType.Int).SqlTypeName;
            DefaultDataTypeDateTime = GetSqlDbType(SqlMsSqlType.DateTime).SqlTypeName;
            EscapeLeft = '[';
            EscapeRight = ']';
            InsertBatchSizeMax = 2000;

            // https://docs.microsoft.com/en-us/sql/relational-databases/databases/database-identifiers?view=sql-server-ver16
            ValidIdentifierChars.AddRange(Constant.CHARS_ALPHANUMERIC + "@$#_");
            ReservedWords.AddRange(SqlMsSqlReservedWords.WORDS.SplitOnWhiteSpace().TrimOrNull().WhereNotNull());
        }

        public override string GetCurrentDatabaseName() => ExecuteScalarString("SELECT DB_NAME();").TrimOrNull();
        public override string GetCurrentSchemaName() => ExecuteScalarString("SELECT SCHEMA_NAME();").TrimOrNull();

        public override IEnumerable<SqlObjectDatabase> GetDatabases()
        {
            var sql = "SELECT DISTINCT [name] FROM sys.databases;";
            var t = ExecuteQuery(sql).First();
            foreach (var r in t)
            {
                var so = new SqlObjectDatabase(r[0]);
                if (ExcludedDatabases.Contains(so.DatabaseName)) continue;
                yield return so;
            }
        }

        public override IEnumerable<SqlObjectSchema> GetSchemas(string database = null)
        {
            var databaseNames = GetDatabaseNames(database);
            var sqls = new List<string>();
            foreach (var d in databaseNames)
            {
                var sql = new StringBuilder();
                sql.Append("SELECT DISTINCT [CATALOG_NAME],[SCHEMA_NAME]");
                sql.Append($" FROM {Escape(d)}.INFORMATION_SCHEMA.SCHEMATA");
                if (database != null) sql.Append($" WHERE CATALOG_NAME='{Unescape(database)}'");
                sql.Append(';');
                sqls.Add(sql.ToString());
            }

            var querySuccess = false;
            var exceptions = new List<Exception>();
            foreach (var sql in sqls)
            {
                Table t = Query(sql, exceptions);
                if (t == null) continue;
                querySuccess = true;
                foreach (var r in t)
                {
                    var so = new SqlObjectSchema(r[0], r[1]);
                    if (ExcludedSchemas.Contains(so.SchemaName)) continue;
                    yield return so;
                }
            }

            if (!querySuccess && exceptions.IsNotEmpty()) throw CreateExceptionErrorInSqls(sqls, exceptions);
        }

        public override IEnumerable<SqlObjectTable> GetTables(string database = null, string schema = null)
        {
            var databaseNames = GetDatabaseNames(database);
            var sqls = new List<string>();
            foreach (var d in databaseNames)
            {
                var sql = new StringBuilder();
                sql.Append("SELECT DISTINCT [TABLE_CATALOG],[TABLE_SCHEMA],[TABLE_NAME]");
                sql.Append($" FROM {Escape(d)}.INFORMATION_SCHEMA.TABLES");
                sql.Append($" WHERE TABLE_TYPE='BASE TABLE'");
                if (database != null) sql.Append($" AND TABLE_CATALOG='{Unescape(database)}'");
                if (schema != null) sql.Append($" AND TABLE_SCHEMA='{Unescape(schema)}'");
                sql.Append(';');
                sqls.Add(sql.ToString());
            }

            var querySuccess = false;
            var exceptions = new List<Exception>();
            foreach (var sql in sqls)
            {
                Table t = Query(sql, exceptions);
                if (t == null) continue;
                querySuccess = true;
                foreach (var r in t)
                {
                    var so = new SqlObjectTable(r[0], r[1], r[2]);

                    if (schema == null && ExcludedSchemas.Contains(so.SchemaName)) continue;
                    if (schema != null && !schema.EqualsCaseInsensitive(so.SchemaName)) continue;
                    yield return so;
                }
            }

            if (!querySuccess && exceptions.IsNotEmpty()) throw CreateExceptionErrorInSqls(sqls, exceptions);
        }

        public override IEnumerable<SqlObjectTableColumn> GetTableColumns(string database = null, string schema = null, string table = null)
        {
            var databaseNames = GetDatabaseNames(database);

            var cols = new string[]
            {
                "TABLE_CATALOG", // 0
                "TABLE_SCHEMA", // 1
                "TABLE_NAME", // 2
                "COLUMN_NAME", // 3
                "DATA_TYPE", // 4
                "IS_NULLABLE", // 5
                "ORDINAL_POSITION", // 6
                "CHARACTER_MAXIMUM_LENGTH", // 7
                "NUMERIC_PRECISION", // 8
                "NUMERIC_SCALE", // 9
                "COLUMN_DEFAULT", // 10
            };
            var sqls = new List<string>();
            foreach (var d in databaseNames)
            {
                var sql = new StringBuilder();
                sql.Append("SELECT DISTINCT " + cols.Select(o => $"c.{Escape(o)}").ToStringDelimited(","));
                sql.Append($" FROM {Escape(d)}.INFORMATION_SCHEMA.COLUMNS c");
                sql.Append($" INNER JOIN {Escape(d)}.INFORMATION_SCHEMA.TABLES t ON t.TABLE_CATALOG=c.TABLE_CATALOG AND t.TABLE_SCHEMA=c.TABLE_SCHEMA AND t.TABLE_NAME=c.TABLE_NAME");
                sql.Append($" WHERE t.TABLE_TYPE='BASE TABLE'");
                if (database != null) sql.Append($" AND c.TABLE_CATALOG='{Unescape(database)}'");
                if (schema != null) sql.Append($" AND c.TABLE_SCHEMA='{Unescape(schema)}'");
                if (table != null) sql.Append($" AND c.TABLE_NAME='{Unescape(table)}'");
                sql.Append(';');
                sqls.Add(sql.ToString());
            }

            var querySuccess = false;
            var exceptions = new List<Exception>();
            foreach (var sql in sqls)
            {
                Table t = Query(sql, exceptions);
                if (t == null) continue;
                querySuccess = true;
                foreach (var r in t)
                {
                    var dbTypeItem = GetSqlDbType(r[4]);
                    var dbType = dbTypeItem != null ? dbTypeItem.DbType : DbType.String;
                    var so = new SqlObjectTableColumn(
                        r[0],
                        r[1],
                        r[2],
                        r[3],
                        r[4],
                        dbType,
                        r[5].ToBool(),
                        r[6].ToInt(),
                        r[7].ToLongNullable(),
                        r[8].ToIntNullable(),
                        r[9].ToIntNullable(),
                        r[10]
                        );

                    if (schema == null && ExcludedSchemas.Contains(so.SchemaName)) continue;
                    if (schema != null && !schema.EqualsCaseInsensitive(so.SchemaName)) continue;
                    if (table != null && !table.EqualsCaseInsensitive(so.TableName)) continue;
                    yield return so;
                }
            }

            if (!querySuccess && exceptions.IsNotEmpty()) throw CreateExceptionErrorInSqls(sqls, exceptions);
        }

        public override bool GetTableExists(string database, string schema, string table)
        {
            database = database.TrimOrNull() ?? GetCurrentDatabaseName();
            if (database == null) throw new Exception("Could not determine current SQL database name");

            schema = schema.TrimOrNull() ?? GetCurrentSchemaName();
            if (schema == null) throw new Exception("Could not determine current SQL schema name");

            table = Unescape(table.TrimOrNull()).CheckNotNullTrimmed(nameof(table));

            return GetTables(database, schema).Where(o => o.TableName.EqualsCaseInsensitive(table)).Any();
        }

        public override bool DropTable(string database, string schema, string table)
        {
            database = database.TrimOrNull() ?? GetCurrentDatabaseName();
            if (database == null) throw new Exception("Could not determine current SQL database name");

            schema = schema.TrimOrNull() ?? GetCurrentSchemaName();
            if (schema == null) throw new Exception("Could not determine current SQL schema name");

            table = Unescape(table.TrimOrNull()).CheckNotNullTrimmed(nameof(table));

            if (!GetTableExists(database, schema, table)) return false;

            var dst = Escape(database) + "." + Escape(schema) + "." + Escape(table);
            ExecuteNonQuery($"DROP TABLE {dst};");
            return true;
        }



        public override string TextCreateTableColumn(TableColumn column)
        {
            var sql = new StringBuilder();

            sql.Append(Escape(column.Name));
            sql.Append(' ');

            var len = column.MaxLength;
            if (len < 1) len = 1;

            var dbType = column.DbType.ToSqlMsSqlType();

            if (dbType.In(SqlMsSqlType.NChar) && len > 4000) dbType = SqlMsSqlType.NVarChar; // NChar doesn't support more then 4000
            if (dbType.In(SqlMsSqlType.Char) && len > 8000) dbType = SqlMsSqlType.VarChar; // Char doesn't support more then 8000

            sql.Append(dbType);

            if (dbType.In(SqlMsSqlType.NChar, SqlMsSqlType.Char)) sql.Append("(" + len + ")");
            else if (dbType.In(SqlMsSqlType.NVarChar)) sql.Append("(" + (len <= 4000 ? len : "MAX") + ")");
            else if (dbType.In(SqlMsSqlType.VarChar)) sql.Append("(" + (len <= 8000 ? len : "MAX") + ")");
            else if (dbType.In(SqlMsSqlType.Float, SqlMsSqlType.Real, SqlMsSqlType.Decimal)) sql.Append("(" + column.NumericPrecision + "," + column.NumericScale + ")");

            if (!column.IsNullable) sql.Append(" NOT");
            sql.Append(" NULL");

            return sql.ToString();
        }

        private List<string> GetDatabaseNames(string database)
        {
            database = database.TrimOrNull();
            if (database != null) return new List<string> { database };
            return GetDatabases().Select(o => o.DatabaseName).ToList();
        }

        public override string Escape(string database, string schema, string table)
        {
            var sb = new StringBuilder();
            database = database.TrimOrNull();
            if (database != null) sb.Append(Escape(database));

            schema = schema.TrimOrNull();
            if (schema != null)
            {
                if (sb.Length > 0) sb.Append('.');
                sb.Append(Escape(schema));
            }

            table = table.TrimOrNull();
            if (table != null)
            {
                if (sb.Length > 0) sb.Append('.');
                sb.Append(Escape(table));
            }

            return sb.ToString();
        }
    }
}
