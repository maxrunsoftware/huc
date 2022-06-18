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

namespace MaxRunSoftware.Utilities;

public class SqlMySql : Sql
{
    // https://dev.mysql.com/doc/refman/8.0/en/information-schema-columns-table.html

    public override Type DbTypesEnum => typeof(SqlMySqlType);

    public SqlMySql()
    {
        DefaultDataTypeString = GetSqlDbType(SqlMySqlType.LongText).SqlTypeName;
        DefaultDataTypeInteger = GetSqlDbType(SqlMySqlType.Int32).SqlTypeName;
        DefaultDataTypeDateTime = GetSqlDbType(SqlMySqlType.DateTime).SqlTypeName;
        EscapeLeft = '`';
        EscapeRight = '`';
        //InsertBatchSizeMax = 2000;

        // https://dev.mysql.com/doc/refman/8.0/en/identifiers.html
        ValidIdentifierChars.AddRange(Constant.CHARS_ALPHANUMERIC + "$_");
        ReservedWords.AddRange(SqlMySqlReservedWords.WORDS.SplitOnWhiteSpace().TrimOrNull().WhereNotNull());
    }

    public override string GetCurrentDatabaseName() => ExecuteScalarString("SELECT DATABASE();").TrimOrNull();

    public override string GetCurrentSchemaName() => GetCurrentDatabaseName();

    public override IEnumerable<SqlObjectDatabase> GetDatabases()
    {
        var t = this.ExecuteQueryToTable("SELECT DISTINCT schema_name FROM information_schema.schemata;");
        foreach (var r in t)
        {
            var so = new SqlObjectDatabase(r[0]);
            if (ExcludedDatabases.Contains(so.DatabaseName)) continue;

            if (ExcludedSchemas.Contains(so.DatabaseName)) continue;

            yield return so;
        }
    }

    public override IEnumerable<SqlObjectSchema> GetSchemas(string database = null) => GetDatabases().Select(o => new SqlObjectSchema(o.DatabaseName, o.DatabaseName));

    public override IEnumerable<SqlObjectTable> GetTables(string database = null, string schema = null)
    {
        database = database.TrimOrNull();
        schema = schema.TrimOrNull();
        if (database != null && schema != null && !database.EqualsCaseInsensitive(schema)) throw new ArgumentException($"Arguments {nameof(database)} '{database}' and {nameof(schema)} '{schema}' cannot both be specified with different values");

        var dbName = database ?? schema;

        var sql = new StringBuilder();
        sql.Append("SELECT DISTINCT TABLE_SCHEMA,TABLE_NAME");
        sql.Append(" FROM information_schema.tables");
        sql.Append(" WHERE TABLE_TYPE='BASE TABLE'");
        if (dbName != null) sql.Append($" AND TABLE_SCHEMA='{Unescape(dbName)}'");

        sql.Append(';');

        var t = this.ExecuteQueryToTable(sql.ToString());

        foreach (var r in t)
        {
            var so = new SqlObjectTable(r[0], r[0], r[1]);
            if (dbName == null && ExcludedDatabases.Contains(so.DatabaseName)) continue;

            if (dbName == null && ExcludedSchemas.Contains(so.SchemaName)) continue;

            if (dbName != null && !dbName.EqualsCaseInsensitive(so.DatabaseName)) continue;

            yield return so;
        }
    }

    public override IEnumerable<SqlObjectTableColumn> GetTableColumns(string database = null, string schema = null, string table = null)
    {
        database = database.TrimOrNull();
        schema = schema.TrimOrNull();
        table = table.TrimOrNull();
        if (database != null && schema != null && !database.EqualsCaseInsensitive(schema)) throw new ArgumentException($"Arguments {nameof(database)} '{database}' and {nameof(schema)} '{schema}' cannot both be specified with different values");

        var dbName = database ?? schema;

        var cols = new[]
        {
            "TABLE_SCHEMA", // 0
            "TABLE_NAME", // 1
            "COLUMN_NAME", // 2
            "DATA_TYPE", // 3
            "IS_NULLABLE", // 4
            "ORDINAL_POSITION", // 5
            "CHARACTER_MAXIMUM_LENGTH", // 6
            "NUMERIC_PRECISION", // 7
            "NUMERIC_SCALE", // 8
            "COLUMN_DEFAULT" // 9
        };

        var sb = new StringBuilder();
        sb.Append("SELECT DISTINCT " + cols.Select(o => $"c.{o}").ToStringDelimited(","));
        sb.Append(" FROM information_schema.columns c");
        sb.Append(" INNER JOIN information_schema.tables t ON t.TABLE_CATALOG=c.TABLE_CATALOG AND t.TABLE_SCHEMA=c.TABLE_SCHEMA AND t.TABLE_NAME=c.TABLE_NAME");
        sb.Append(" WHERE t.TABLE_TYPE='BASE TABLE'");
        if (dbName != null) sb.Append($" AND c.TABLE_SCHEMA='{Unescape(dbName)}'");

        if (table != null) sb.Append($" AND c.TABLE_NAME='{Unescape(table)}'");

        sb.Append(';');

        var t = this.ExecuteQueryToTable(sb.ToString());
        foreach (var r in t)
        {
            var dbTypeItem = GetSqlDbType(r[3]);
            var dbType = dbTypeItem?.DbType ?? DbType.String;

            var so = new SqlObjectTableColumn(
                r[0],
                r[0],
                r[1],
                r[2],
                r[3],
                dbType,
                r[4].ToBool(),
                r[5].ToInt(),
                r[6].ToLongNullable(),
                r[7].ToIntNullable(),
                r[8].ToIntNullable(),
                r[9]
            );

            if (dbName == null && ExcludedDatabases.Contains(so.DatabaseName)) continue;

            if (dbName == null && ExcludedSchemas.Contains(so.SchemaName)) continue;

            if (dbName != null && !dbName.EqualsCaseInsensitive(so.DatabaseName)) continue;

            if (table != null && !table.EqualsCaseInsensitive(so.TableName)) continue;

            yield return so;
        }
    }

    public override bool GetTableExists(string database, string schema, string table)
    {
        var dbName = database.TrimOrNull() ?? schema.TrimOrNull() ?? GetCurrentDatabaseName();
        if (dbName == null) throw new Exception("Could not determine current SQL database/schema name");

        table = Unescape(table.TrimOrNull()).CheckNotNullTrimmed(nameof(table));

        return GetTables(dbName, dbName).Any(o => o.TableName.EqualsCaseInsensitive(table));
    }

    public override bool DropTable(string database, string schema, string table)
    {
        var dbName = database.TrimOrNull() ?? schema.TrimOrNull() ?? GetCurrentDatabaseName();
        if (dbName == null) throw new Exception("Could not determine current SQL database/schema name");

        table = Unescape(table.TrimOrNull()).CheckNotNullTrimmed(nameof(table));

        if (!GetTableExists(dbName, dbName, table)) return false;

        var dst = Escape(dbName) + "." + Escape(table);
        ExecuteNonQuery($"DROP TABLE {dst};");
        return true;
    }

    public override string TextCreateTableColumn(TableColumn column) => throw new NotImplementedException();

    public override string Escape(string database, string schema, string table)
    {
        database = database.TrimOrNull();
        schema = schema.TrimOrNull();
        table = table.TrimOrNull();
        if (database != null && schema != null && !database.EqualsCaseInsensitive(schema)) throw new ArgumentException($"Arguments {nameof(database)} '{database}' and {nameof(schema)} '{schema}' cannot both be specified with different values");

        var db = database ?? schema;

        var sb = new StringBuilder();
        if (db != null) sb.Append(Escape(db));

        if (table != null)
        {
            if (sb.Length > 0) sb.Append('.');

            sb.Append(Escape(table));
        }

        return sb.ToString();
    }
}
