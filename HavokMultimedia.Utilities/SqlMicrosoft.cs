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
using System.Data;
using System.Linq;
using System.Text;

namespace HavokMultimedia.Utilities
{
    public abstract class SqlMicrosoftInformationSchema
    {
        private static readonly IBucketReadOnly<string, string> CACHE_CAMEL_TO_UPPER = new BucketCacheCopyOnWrite<string, string>(columnName =>
        {
            var sb = new StringBuilder();
            var first = true;
            foreach (var c in columnName)
            {
                if (char.IsLower(c)) sb.Append(char.ToUpper(c));
                else
                {
                    if (!first) sb.Append("_");
                    first = false;
                    sb.Append(c);
                }
            }
            return sb.ToString();
        });

        protected string GetSqlColumnName(string columnNameCamel) => CACHE_CAMEL_TO_UPPER[columnNameCamel];

        protected string GetValue(TableRow row, string columnNameCamel) => row.Table.Columns.TryGetColumn(GetSqlColumnName(columnNameCamel), out var c) ? row[c] : null;
    }

    public sealed class SqlMicrosoftInformationSchemaTable : SqlMicrosoftInformationSchema
    {
        public string TableCatalog { get; }
        public string TableSchema { get; }
        public string TableName { get; }
        public string TableType { get; }

        internal SqlMicrosoftInformationSchemaTable(TableRow row)
        {
            TableCatalog = GetValue(row, nameof(TableCatalog));
            TableSchema = GetValue(row, nameof(TableSchema));
            TableName = GetValue(row, nameof(TableName));
            TableType = GetValue(row, nameof(TableType));
        }
    }

    public sealed class SqlMicrosoftInformationSchemaColumn : SqlMicrosoftInformationSchema
    {
        public string TableCatalog { get; }
        public string TableSchema { get; }
        public string TableName { get; }
        public string ColumnName { get; }
        public string OrdinalPosition { get; }
        public string ColumnDefault { get; }
        public string IsNullable { get; }
        public string DataType { get; }
        public string CharacterMaximumLength { get; }
        public string CharacterOctetLength { get; }
        public string NumericPrecision { get; }
        public string NumericPrecisionRadix { get; }
        public string NumericScale { get; }
        public string DatetimePrecision { get; }
        public string CharacterSetCatalog { get; }
        public string CharacterSetSchema { get; }
        public string CharacterSetName { get; }
        public string CollationCatalog { get; }
        public string CollationSchema { get; }
        public string CollationName { get; }
        public string DomainCatalog { get; }
        public string DomainSchema { get; }
        public string DomainName { get; }

        internal SqlMicrosoftInformationSchemaColumn(TableRow row)
        {
            TableCatalog = GetValue(row, nameof(TableCatalog));
            TableSchema = GetValue(row, nameof(TableSchema));
            TableName = GetValue(row, nameof(TableName));
            ColumnName = GetValue(row, nameof(ColumnName));
            OrdinalPosition = GetValue(row, nameof(OrdinalPosition));
            ColumnDefault = GetValue(row, nameof(ColumnDefault));
            IsNullable = GetValue(row, nameof(IsNullable));
            DataType = GetValue(row, nameof(DataType));
            CharacterMaximumLength = GetValue(row, nameof(CharacterMaximumLength));
            CharacterOctetLength = GetValue(row, nameof(CharacterOctetLength));
            NumericPrecision = GetValue(row, nameof(NumericPrecision));
            NumericPrecisionRadix = GetValue(row, nameof(NumericPrecisionRadix));
            NumericScale = GetValue(row, nameof(NumericScale));
            DatetimePrecision = GetValue(row, nameof(DatetimePrecision));
            CharacterSetCatalog = GetValue(row, nameof(CharacterSetCatalog));
            CharacterSetSchema = GetValue(row, nameof(CharacterSetSchema));
            CharacterSetName = GetValue(row, nameof(CharacterSetName));
            CollationCatalog = GetValue(row, nameof(CollationCatalog));
            CollationSchema = GetValue(row, nameof(CollationSchema));
            CollationName = GetValue(row, nameof(CollationName));
            DomainCatalog = GetValue(row, nameof(DomainCatalog));
            DomainSchema = GetValue(row, nameof(DomainSchema));
            DomainName = GetValue(row, nameof(DomainName));
        }
    }

    public class SqlMicrosoft
    {
        private static readonly ILogger log = LogFactory.LogFactoryImpl.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Func<IDbConnection> connectionFactory;

        private readonly object locker = new object();

        public static readonly Func<string, string> ESCAPE_MSSQL = (o =>
        {
            if (o == null) return o;
            if (!o.StartsWith("["))
            {
                if (o.EndsWith("]")) return ("[" + o);
                return ("[" + o + "]");
            }
            if (!o.EndsWith("]")) return o + "]";
            return o;
        });

        public bool IsDisposed { get; private set; }

        public Func<string, string> EscapeObject { get; set; } = ESCAPE_MSSQL;

        public int CommandTimeout { get; set; } = 60 * 60 * 24;

        public SqlMicrosoft(Func<IDbConnection> connectionFactory) => this.connectionFactory = connectionFactory.CheckNotNull(nameof(connectionFactory));

        #region CRUD

        private object Insert(string schemaAndTableEscaped, string pkColumnName, params SqlParameter[] parameters)
        {
            var sb = new StringBuilder(255);
            pkColumnName = pkColumnName.TrimOrNull();

            sb.Append("INSERT INTO " + schemaAndTableEscaped + " (");

            var comma = false;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (comma) sb.Append(",");
                sb.Append(Escape(parameters[i].Name));
                comma = true;
            }
            sb.Append(")");
            if (pkColumnName != null)
            {
                sb.Append(" OUTPUT INSERTED." + pkColumnName);
            }
            sb.Append(" VALUES (");
            comma = false;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (comma) sb.Append(",");
                sb.Append("@" + parameters[i].Name.Replace(' ', '_'));
                comma = true;
            }
            sb.Append(")");

            using (var connection = OpenConnection())
            using (var command = CreateCommand(connection, sb.ToString()))
            {
                AddParameters(command, parameters);

                if (pkColumnName != null)
                {
                    return command.ExecuteScalar();
                }
                else
                {
                    return command.ExecuteNonQuery();
                }
            }
        }

        private void InsertOld(string schemaAndTableEscaped, Table table)
        {
            if (table.IsEmpty()) return;
            var sb = new StringBuilder(255);

            sb.Append("INSERT INTO " + schemaAndTableEscaped + " (");
            sb.Append(string.Join(",", table.Columns.Select(o => Escape(o.Name))));
            sb.Append(") VALUES (");
            sb.Append(string.Join(",", table.Columns.Select(o => "@" + CleanParameterName(o.Name))));
            sb.Append(")");

            using (var connection = OpenConnection())
            using (var command = CreateCommand(connection, sb.ToString()))
            {
                foreach (var col in table.Columns)
                {
                    command.AddParameter(dbType: DbType.String, parameterName: CleanParameterName(col.Name), size: -1, value: table.First()[col]);
                }

                command.Prepare();
                command.ExecuteNonQuery();

                var columnCount = table.Columns.Count;
                var first = true;
                foreach (var row in table)
                {
                    if (first)
                    {
                        first = false;
                        continue;
                    }

                    for (var i = 0; i < columnCount; i++)
                    {
                        command.SetParameterValue(i, row[i]);
                    }

                    command.ExecuteNonQuery();
                }
            }
        }

        public Table GetFromTable(string schemaAndTableEscaped, params SqlParameter[] parametersWhere)
        {
            var sb = new StringBuilder(255);

            sb.Append("SELECT * FROM " + schemaAndTableEscaped);
            WhereCreateSql(sb, parametersWhere);

            return GetWhere(sb.ToString(), parametersWhere).First();
        }

        public Table[] GetWhere(string sql, params SqlParameter[] parametersWhere)
        {
            using (var connection = OpenConnection())
            using (var command = CreateCommand(connection, sql))
            {
                WhereAddParameters(command, parametersWhere);

                using (var reader = command.ExecuteReader())
                {
                    return Table.Create(reader);
                }
            }
        }

        public int InsertPrimaryKeyInt(string schemaAndTableEscaped, string pkColumnName, params SqlParameter[] parameters) => Convert.ToInt32(Insert(schemaAndTableEscaped, pkColumnName.CheckNotNullTrimmed("pkColumnName"), parameters));

        public long InsertPrimaryKeyLong(string schemaAndTableEscaped, string pkColumnName, params SqlParameter[] parameters) => Convert.ToInt64(Insert(schemaAndTableEscaped, pkColumnName.CheckNotNullTrimmed("pkColumnName"), parameters));

        public Guid InsertPrimaryKeyGuid(string schemaAndTableEscaped, string pkColumnName, params SqlParameter[] parameters) => Guid.Parse(Insert(schemaAndTableEscaped, pkColumnName.CheckNotNullTrimmed("pkColumnName"), parameters).ToStringGuessFormat());

        public bool Insert(string schemaAndTableEscaped, params SqlParameter[] parameters) => Convert.ToInt32(Insert(schemaAndTableEscaped, null, parameters)) > 0;

        public void Insert(string schemaAndTableEscaped, Table table, int batchSize = 1000)
        {
            if (table.IsEmpty()) return;
            if (batchSize < 2)
            {
                InsertOld(schemaAndTableEscaped, table);
                return;
            }

            if (batchSize > 2000) batchSize = 2000;

            var countRows = table.Count;
            var countColumns = table.Columns.Count;

            var commandsByRow = new string[batchSize + 10];
            var commandVariableName = 0;
            for (var i = 0; i < commandsByRow.Length; i++)
            {
                var sbInsert = new StringBuilder();
                sbInsert.Append($"INSERT INTO {schemaAndTableEscaped} (");
                sbInsert.Append(string.Join(",", table.Columns.Select(o => Escape(o.Name))));
                sbInsert.Append(") VALUES (");
                for (var j = 0; j < countColumns; j++)
                {
                    if (j > 0) sbInsert.Append(", ");
                    sbInsert.Append("@v" + commandVariableName);
                    commandVariableName++;
                }
                sbInsert.Append("); ");
                commandsByRow[i] = sbInsert.ToString();
            }

            var sbSize = 100;
            var sb = new StringBuilder(sbSize);
            var currentRow = 0;
            var currentRowInBatch = 0;
            var currentParameterCount = 0;

            using (var connection = OpenConnection())
            {
                IDbCommand command = null;
                foreach (var row in table)
                {
                    if (command == null) command = CreateCommand(connection, null);
                    currentRow++;
                    currentRowInBatch++;
                    var cmd = commandsByRow[currentRowInBatch - 1];
                    sb.Append(cmd);
                    for (var i = 0; i < countColumns; i++)
                    {
                        var p = command.AddParameter(dbType: DbType.String, parameterName: "@v" + currentParameterCount, size: -1, value: row[i]);
                        currentParameterCount++;
                    }

                    if ((currentParameterCount + countColumns) > batchSize || currentRow == countRows)
                    {
                        // Commit tran
                        command.CommandText = sb.ToString().TrimOrNull();
                        if (command.CommandText != null)
                        {
                            command.ExecuteNonQuery();
                            command.Dispose();
                            command = null;

                            currentParameterCount = 0;
                            currentRowInBatch = 0;
                            sbSize = Math.Max(sbSize, sb.Length);
                            sb = new StringBuilder(sbSize);
                        }
                    }
                }

                if (command != null)
                {
                    command.ExecuteNonQuery();
                    command.Dispose();
                    command = null;
                }
            }
        }

        public int Update(string schemaAndTableEscaped, SqlParameter[] parametersToUpdate, SqlParameter[] parametersWhere)
        {
            var sb = new StringBuilder(255);
            sb.Append("UPDATE " + schemaAndTableEscaped + " SET ");

            var comma = false;
            for (var i = 0; i < parametersToUpdate.Length; i++)
            {
                if (comma) sb.Append(",");
                sb.Append(Escape(parametersToUpdate[i].Name) + " = @" + CleanParameterName(parametersToUpdate[i].Name));
                comma = true;
            }

            WhereCreateSql(sb, parametersWhere);

            using (var connection = OpenConnection())
            using (var command = CreateCommand(connection, sb.ToString()))
            {
                AddParameters(command, parametersToUpdate);
                WhereAddParameters(command, parametersWhere);
                return command.ExecuteNonQuery();
            }
        }

        public int Delete(string schemaAndTableEscaped, params SqlParameter[] parametersWhere)
        {
            var sb = new StringBuilder(255);
            sb.Append("DELETE " + schemaAndTableEscaped);

            WhereCreateSql(sb, parametersWhere);

            using (var connection = OpenConnection())
            using (var command = CreateCommand(connection, sb.ToString()))
            {
                WhereAddParameters(command, parametersWhere);
                return command.ExecuteNonQuery();
            }
        }

        public int Count(string schemaAndTableEscaped, params SqlParameter[] parametersWhere)
        {
            var sb = new StringBuilder(255);
            sb.Append("SELECT COUNT(*) FROM " + schemaAndTableEscaped);

            WhereCreateSql(sb, parametersWhere);

            using (var connection = OpenConnection())
            using (var command = CreateCommand(connection, sb.ToString()))
            {
                WhereAddParameters(command, parametersWhere);
                return int.Parse(command.ExecuteScalar().ToString());
            }
        }

        #endregion CRUD

        #region Information

        public string GetDefaultSchema() => ExecuteScalar("SELECT SCHEMA_NAME();")?.ToString().TrimOrNull();

        public string GetDefaultDatabase() => ExecuteScalar("SELECT [default_database_name] FROM sys.server_principals WHERE sid = SUSER_SID();")?.ToString().TrimOrNull();

        public string GetCurrentUsername() => ExecuteScalar("SELECT SUSER_SNAME();")?.ToString().TrimOrNull();

        public string GetCurrentDatabase() => ExecuteScalar("SELECT DB_NAME();")?.ToString().TrimOrNull();

        public bool GetDatabaseExists(string databaseName) => ExecuteScalar($"SELECT DB_ID('{databaseName}');")?.ToString().TrimOrNull() != null;

        public bool GetTableExists(string table, string schema = null, string database = null)
        {
            var s = Escape("INFORMATION_SCHEMA") + "." + Escape("TABLES");

            if (database != null)
            {
                if (!GetDatabaseExists(database)) return false;
                s = Escape(database) + "." + s;
            }

            if (schema == null) schema = GetDefaultSchema();

            return Count(
                s,
                new SqlParameter("TABLE_SCHEMA", schema),
                new SqlParameter("TABLE_NAME", table),
                new SqlParameter("TABLE_TYPE", "BASE TABLE")
                ) > 0;
        }

        public SqlMicrosoftInformationSchemaTable[] GetTables(string database = null)
        {
            var s = Escape("INFORMATION_SCHEMA") + "." + Escape("TABLES");
            if (database != null) s = Escape(database) + "." + s;
            return GetFromTable(s).Select(o => new SqlMicrosoftInformationSchemaTable(o)).ToArray();
        }

        public SqlMicrosoftInformationSchemaColumn[] GetColumns(string database = null)
        {
            var s = Escape("INFORMATION_SCHEMA") + "." + Escape("COLUMNS");
            if (database != null) s = Escape(database) + "." + s;
            return GetFromTable(s).Select(o => new SqlMicrosoftInformationSchemaColumn(o)).ToArray();
        }

        #endregion Information

        #region Tables

        public void DropTable(string table, string schema = null, string database = null)
        {
            var s = Escape(table);
            if (schema == null) schema = GetDefaultSchema();
            s = Escape(schema) + "." + s;
            if (database != null) s = Escape(database) + "." + s;
            ExecuteNonQuery("DROP TABLE " + s + ";");
        }

        #endregion Tables

        #region General

        public Table[] ExecuteQuery(string sql, params SqlParameter[] parameters)
        {
            using (var connection = OpenConnection())
            using (var command = CreateCommand(connection, sql))
            {
                AddParameters(command, parameters);
                using (var reader = command.ExecuteReader())
                {
                    return Table.Create(reader);
                }
            }
        }

        public int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using (var connection = OpenConnection())
            using (var command = CreateCommand(connection, sql))
            {
                AddParameters(command, parameters);
                return command.ExecuteNonQuery();
            }
        }

        public object ExecuteScalar(string sql, params SqlParameter[] parameters)
        {
            using (var connection = OpenConnection())
            using (var command = CreateCommand(connection, sql))
            {
                AddParameters(command, parameters);
                return command.ExecuteScalar();
            }
        }

        public Table[] ExecuteStoredProcedure(string schemaAndStoredProcedureEscaped, params SqlParameter[] parameters)
        {
            using (var connection = OpenConnection())
            using (var command = CreateCommand(connection, schemaAndStoredProcedureEscaped, commandType: CommandType.StoredProcedure))
            {
                AddParameters(command, parameters);
                using (var reader = command.ExecuteReader())
                {
                    return Table.Create(reader);
                }
            }
        }

        #endregion General

        private IDbCommand CreateCommand(IDbConnection connection, string sql, CommandType commandType = CommandType.Text)
        {
            var c = connection.CreateCommand();

            c.CommandText = sql;
            c.CommandType = commandType;
            c.CommandTimeout = CommandTimeout;
            return c;
        }

        private IDbConnection OpenConnection()
        {
            var connection = connectionFactory();
            if (connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken) connection.Open();
            return connection;
        }

        private string CleanParameterName(string parameterName) => parameterName.CheckNotNullTrimmed(nameof(parameterName)).Replace(' ', '_');

        private IDataParameter AddParameter(IDbCommand command, SqlParameter parameter) => parameter == null ? null : command.AddParameter(dbType: parameter.Type, parameterName: CleanParameterName(parameter.Name), value: parameter.Value);

        private IDataParameter[] AddParameters(IDbCommand command, SqlParameter[] parameters) => parameters.OrEmpty().Select(o => AddParameter(command, o)).ToArray();

        private void WhereCreateSql(StringBuilder sb, SqlParameter[] parametersWhere)
        {
            if (parametersWhere == null) return;
            for (var i = 0; i < parametersWhere.Length; i++)
            {
                sb.Append(i == 0 ? " WHERE " : " AND ");
                sb.Append(Escape(parametersWhere[i].Name));
                if (parametersWhere[i].Value == null || parametersWhere[i].Value == DBNull.Value)
                {
                    sb.Append(" IS NULL");
                }
                else
                {
                    sb.Append("=@" + CleanParameterName(parametersWhere[i].Name));
                }
            }
        }

        private void WhereAddParameters(IDbCommand command, SqlParameter[] parametersWhere)
        {
            if (parametersWhere == null) return;
            for (var i = 0; i < parametersWhere.Length; i++)
            {
                if (parametersWhere[i].Value != null && parametersWhere[i].Value != DBNull.Value)
                {
                    AddParameter(command, parametersWhere[i]);
                }
            }
        }

        public string Escape(string objectToEscape)
        {
            var f = EscapeObject;
            return f != null ? f(objectToEscape) : objectToEscape;
        }
    }

}
