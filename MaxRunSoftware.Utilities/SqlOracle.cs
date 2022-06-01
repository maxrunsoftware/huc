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
    public class SqlOracle : Sql
    {
        public override Type DbTypesEnum => typeof(SqlOracleType);

        public SqlOracle()
        {
            //ExcludedDatabases.Add("master", "model", "msdb", "tempdb");
            DefaultDataTypeString = GetSqlDbType(SqlOracleType.NClob).SqlTypeName;
            DefaultDataTypeInteger = GetSqlDbType(SqlOracleType.Int32).SqlTypeName;
            DefaultDataTypeDateTime = GetSqlDbType(SqlOracleType.DateTime).SqlTypeName;
            EscapeLeft = '"';
            EscapeRight = '"';
            //InsertBatchSizeMax = 2000;

            // https://docs.oracle.com/cd/B19306_01/server.102/b14200/sql_elements008.htm
            ValidIdentifierChars.AddRange(Constant.CHARS_ALPHANUMERIC + "$_#");
            ReservedWords.AddRange(SqlOracleReservedWords.WORDS.SplitOnWhiteSpace().TrimOrNull().WhereNotNull());
        }

        public bool DropTableCascadeConstraints { get; set; }
        public bool DropTablePurge { get; set; }

        private string currentDatabaseName;
        public override string GetCurrentDatabaseName()
        {
            if (currentDatabaseName != null) return currentDatabaseName;

            var sqls = new string[]
            {
                "select SYS_CONTEXT('USERENV','DB_NAME') from dual;",
                "select global_name from global_name;",
                "select ora_database_name from dual;",
                "select pdb_name FROM DBA_PDBS;",
                "select name from v$database;",
            };

            var exceptions = new List<Exception>();
            foreach (var sql in sqls)
            {
                Table t = Query(sql, exceptions);
                if (t == null) continue;
                foreach (var r in t)
                {
                    var v = r[0].TrimOrNull();
                    if (v == null) continue;
                    currentDatabaseName = v;
                    return v;
                }
            }

            if (exceptions.IsNotEmpty()) throw CreateExceptionErrorInSqls(sqls, exceptions);
            return null;
        }

        public override string GetCurrentSchemaName()
        {
            var sqls = new string[]
            {
                "select SYS_CONTEXT('USERENV','CURRENT_SCHEMA') from dual;",
                "select user from dual;",
                "select SYS_CONTEXT('USERENV','SESSION_USER') from dual;",
            };

            var exceptions = new List<Exception>();
            foreach (var sql in sqls)
            {
                Table t = Query(sql, exceptions);
                if (t == null) continue;
                foreach (var r in t)
                {
                    var v = r[0].TrimOrNull();
                    if (v != null) return v;
                }
            }

            if (exceptions.IsNotEmpty()) throw CreateExceptionErrorInSqls(sqls, exceptions);
            return null;
        }

        public override IEnumerable<SqlObjectDatabase> GetDatabases()
        {
            var empty = Enumerable.Empty<SqlObjectDatabase>();
            var dbName = GetCurrentDatabaseName();
            if (dbName == null) return empty;
            if (ExcludedDatabases.Contains(dbName)) return empty;
            return (new SqlObjectDatabase(dbName)).Yield();
        }



        public override IEnumerable<SqlObjectSchema> GetSchemas(string database = null)
        {
            if (ShouldStop(database, out var dbName)) yield break;

            // TODO: Expensive operation
            var sqls = new string[]
            {
                "SELECT DISTINCT username FROM dba_users;",
                "SELECT DISTINCT username FROM all_users;",
                "SELECT DISTINCT owner FROM dba_objects;",
                "SELECT DISTINCT owner FROM dba_segments;",
                "SELECT DISTINCT OWNER FROM dba_tables;",
                "SELECT DISTINCT OWNER FROM all_tables;",
            };

            var exceptions = new List<Exception>();
            var alreadyUsed = new HashSet<SqlObjectSchema>();
            foreach (var sql in sqls)
            {
                var t = Query(sql, exceptions);
                if (t == null) continue;

                foreach (var r in t)
                {
                    var valSchema = r[0].TrimOrNull();
                    if (valSchema == null) continue;
                    var so = new SqlObjectSchema(dbName, valSchema);
                    if (!alreadyUsed.Add(so)) continue;
                    if (so.SchemaName != null && ExcludedSchemas.Contains(so.SchemaName)) continue;
                    yield return so;
                }
            }

            if (alreadyUsed.IsEmpty() && exceptions.IsNotEmpty()) throw CreateExceptionErrorInSqls(sqls, exceptions);
        }

        public override IEnumerable<SqlObjectTable> GetTables(string database = null, string schema = null)
        {
            if (ShouldStop(database, out var dbName)) yield break;
            var currentSchema = GetCurrentSchemaName();

            // TODO: Expensive operation
            var sqls = new string[]
            {
                "SELECT DISTINCT OWNER,TABLE_NAME FROM dba_tables;",
                "SELECT DISTINCT OWNER,TABLE_NAME FROM all_tables;",
                "SELECT DISTINCT NULL AS OWNER,TABLE_NAME FROM user_tables;",
            };

            var exceptions = new List<Exception>();
            var alreadyUsed = new HashSet<SqlObjectTable>();
            foreach (var sql in sqls)
            {
                var t = Query(sql, exceptions);
                if (t == null) continue;

                foreach (var r in t)
                {
                    var valSchema = r[0].TrimOrNull() ?? currentSchema;
                    var valTable = r[1].TrimOrNull();
                    if (valTable == null) continue;
                    var so = new SqlObjectTable(dbName, valSchema, valTable);

                    if (!alreadyUsed.Add(so)) continue;
                    if (so.SchemaName != null && ExcludedSchemas.Contains(so.SchemaName)) continue;
                    yield return so;
                }
            }

            if (alreadyUsed.IsEmpty() && exceptions.IsNotEmpty()) throw CreateExceptionErrorInSqls(sqls, exceptions);

        }

        public override IEnumerable<SqlObjectTableColumn> GetTableColumns(string database = null, string schema = null, string table = null)
        {
            if (ShouldStop(database, out var dbName)) yield break;
            var currentSchema = GetCurrentSchemaName();

            var cols = new string[]
            {
                "OWNER",
                "TABLE_NAME",
                "COLUMN_NAME",
                "DATA_TYPE",
                "DATA_LENGTH",
                "DATA_PRECISION",
                "DATA_SCALE",
                "NULLABLE",
                "COLUMN_ID",
                "DATA_DEFAULT",
                "CHAR_LENGTH",
            };
            var sqls = new string[]
            {
                "SELECT DISTINCT      " + cols.ToStringDelimited(",") + " FROM dba_tab_columns;",
                "SELECT DISTINCT      " + cols.ToStringDelimited(",") + " FROM all_tab_columns;",
                "SELECT DISTINCT NULL " + cols.ToStringDelimited(",") + " FROM user_tab_columns;",
            };

            var exceptions = new List<Exception>();
            var alreadyUsed = new HashSet<SqlObjectTableColumn>();
            foreach (var sql in sqls)
            {
                var t = Query(sql, exceptions);
                if (t == null) continue;

                foreach (var r in t)
                {
                    var valSchema = r["OWNER"].TrimOrNull() ?? currentSchema;
                    var valTable = r["TABLE_NAME"].TrimOrNull();
                    if (valTable == null) continue;
                    var valColumn = r["COLUMN_NAME"].TrimOrNull();
                    if (valColumn == null) continue;

                    var valCharacterLengthMax = (r["CHAR_LENGTH"] ?? "0").ToInt();
                    if (valCharacterLengthMax < 1) valCharacterLengthMax = (r["DATA_LENGTH"] ?? "0").ToInt();
                    var valColumnDefault = r["DATA_DEFAULT"].TrimOrNull();
                    if (valColumnDefault != null && valColumnDefault.EqualsCaseInsensitive("null")) valColumnDefault = null;

                    var dbTypeItem = GetSqlDbType(r["DATA_TYPE"]);
                    var dbType = dbTypeItem != null ? dbTypeItem.DbType : DbType.String;

                    var so = new SqlObjectTableColumn(
                        dbName,
                        valSchema,
                        valTable,
                        valColumn,
                        r["DATA_TYPE"],
                        dbType,
                        r["NULLABLE"].ToBool(),
                        r["COLUMN_ID"].ToInt(),
                        valCharacterLengthMax,
                        r["DATA_PRECISION"].ToIntNullable(),
                        r["DATA_SCALE"].ToIntNullable(),
                        valColumnDefault
                        );

                    if (!alreadyUsed.Add(so)) continue;
                    if (so.SchemaName != null && ExcludedSchemas.Contains(so.SchemaName)) continue;
                    yield return so;
                }
            }

            if (alreadyUsed.IsEmpty() && exceptions.IsNotEmpty()) throw CreateExceptionErrorInSqls(sqls, exceptions);
        }

        public override bool GetTableExists(string database, string schema, string table)
        {
            schema = schema.TrimOrNull() ?? GetCurrentSchemaName();
            if (schema == null) throw new Exception("Could not determine current SQL schema name");

            table = Unescape(table.TrimOrNull()).CheckNotNullTrimmed(nameof(table));

            foreach (var t in GetTables(database, schema))
            {
                if (string.Equals(t.TableName, table, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public override bool DropTable(string database, string schema, string table)
        {
            if (!GetTableExists(database, schema, table)) return false;

            var sql = new StringBuilder();
            sql.Append("DROP TABLE ");
            if (schema != null) sql.Append(" " + schema + ".");
            sql.Append(table);
            if (DropTableCascadeConstraints) sql.Append(" CASCADE CONSTRAINTS");
            if (DropTablePurge) sql.Append(" PURGE");
            sql.Append(';');

            ExecuteNonQuery(sql.ToString());
            return true;
        }

        public override string TextCreateTableColumn(TableColumn column)
        {
            throw new NotImplementedException();
        }

        private bool ShouldStop(string database, out string dbName)
        {
            database = database.TrimOrNull();
            dbName = GetCurrentDatabaseName();
            if (database != null)
            {
                if (dbName == null)
                {
                    log.Debug($"Requested database name '{database}' does not match our connected database because could not determine current database name, proceeding anyway");
                    return false;
                }
                if (!dbName.EqualsCaseInsensitive(database))
                {
                    log.Debug($"Requested database name '{database}' does not match our connected database '{dbName}' so we are not continuing request");
                    return true;
                }
            }
            if (dbName != null)
            {
                if (ExcludedDatabases.Contains(dbName))
                {
                    log.Debug($"Requesred database name '{database}' is in our list of {nameof(ExcludedDatabases)} so we are not continuing request");
                    return true;
                }
            }
            return false;
        }

        public override string Escape(string database, string schema, string table)
        {
            var sb = new StringBuilder();

            schema = schema.TrimOrNull();
            if (schema != null)
            {
                sb.Append(schema.ToUpper());
            }

            table = table.TrimOrNull();
            if (table != null)
            {
                if (sb.Length > 0) sb.Append('.');
                sb.Append(table.ToUpper());
            }

            return sb.ToString();
        }

    }
}
