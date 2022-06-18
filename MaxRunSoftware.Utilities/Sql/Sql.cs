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

// ReSharper disable PropertyCanBeMadeInitOnly.Global
public abstract class Sql
{
    protected readonly ILogger log;

    public virtual Func<IDbConnection> ConnectionFactory { get; set; }
    public bool ExceptionShowFullSql { get; set; }
    public char? EscapeLeft { get; set; } = '"';
    public char? EscapeRight { get; set; } = '"';
    public bool InsertCoerceValues { get; set; }
    public ushort InsertBatchSize { get; set; } = 1000;
    public ushort InsertBatchSizeMax { get; set; } = 2000; // MSSQL Limit
    public ISet<string> ExcludedDatabases { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // ReSharper disable once CollectionNeverUpdated.Global
    public ISet<string> ExcludedSchemas { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public int CommandTimeout { get; set; } = 60 * 60 * 24; // 24 hours
    public string DefaultDataTypeString { get; set; }
    public string DefaultDataTypeInteger { get; set; }
    public string DefaultDataTypeDateTime { get; set; }
    public abstract Type DbTypesEnum { get; }

    public ISet<string> ReservedWords { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public ISet<char> ValidIdentifierChars { get; } = new HashSet<char>();

    protected Sql() { log = LogFactory.LogFactoryImpl.GetLogger(GetType()); }

    public abstract string GetCurrentDatabaseName();
    public abstract string GetCurrentSchemaName();

    public abstract IEnumerable<SqlObjectDatabase> GetDatabases();
    public abstract IEnumerable<SqlObjectSchema> GetSchemas(string database = null);
    public abstract IEnumerable<SqlObjectTable> GetTables(string database = null, string schema = null);
    public abstract IEnumerable<SqlObjectTableColumn> GetTableColumns(string database = null, string schema = null, string table = null);

    public abstract bool GetTableExists(string database, string schema, string table);
    public abstract bool DropTable(string database, string schema, string table);

    public abstract string TextCreateTableColumn(TableColumn column);

    public virtual string TextCreateTableColumnText(string columnName, bool isNullable)
    {
        var sql = new StringBuilder();

        sql.Append(Escape(columnName));
        sql.Append(' ');
        sql.Append(DefaultDataTypeString);

        if (!isNullable) sql.Append(" NOT");

        sql.Append(" NULL");

        return sql.ToString();
    }

    #region Insert

    public virtual void Insert(IDbConnection connection, string database, string schema, string table, IDictionary<string, string> columnsAndValues)
    {
        var list = new List<(string columnName, string columnValue)>();
        foreach (var kvp in columnsAndValues) list.Add((Escape(kvp.Key), kvp.Value));

        var columnNames = list.Select(o => o.columnName).ToArray();
        var columnValues = list.Select(o => o.columnValue).ToArray();
        var columnParameterNames = new string[columnNames.Length];
        for (var i = 0; i < columnParameterNames.Length; i++) columnParameterNames[i] = "@p" + i;

        var sb = new StringBuilder();
        sb.Append($"INSERT INTO {Escape(database, schema, table)} (");

        sb.Append(string.Join(",", columnNames));
        sb.Append(") VALUES (");
        sb.Append(string.Join(",", columnParameterNames));
        sb.Append(')');
        //sb.Append(';');  // breaks Oracle
        
        using var cmd = CreateCommand(connection, sb.ToString());

        for (var i = 0; i < columnValues.Length; i++) cmd.AddParameter(parameterName: columnParameterNames[i], value: columnValues[i]);

        cmd.ExecuteNonQuery();
    }

    public virtual void Insert(string database, string schema, string table, Table tableData, ushort? batchSize = null)
    {
        if (table.IsEmpty()) return;

        var sqlObjectTableColumns = InsertGetTableColumns(database, schema, table);

        int bs = batchSize ?? InsertBatchSize;
        int bsm = InsertBatchSize;
        if (bs > bsm)
        {
            log.Warn($"Requested {nameof(InsertBatchSize)} of {bs} is greater then {nameof(InsertBatchSizeMax)} of {bsm} so using {bsm}");
            bs = bsm;
        }

        var countRows = tableData.Count;
        var countColumns = tableData.Columns.Count;

        var commandsByRow = new string[bs + 10];
        var commandVariableName = 0;
        for (var i = 0; i < commandsByRow.Length; i++)
        {
            var sbInsert = new StringBuilder();
            sbInsert.Append($"INSERT INTO {Escape(database, schema, table)} (");

            sbInsert.Append(string.Join(",", tableData.Columns.Select(o => Escape(o.Name))));
            sbInsert.Append(") VALUES (");
            for (var j = 0; j < countColumns; j++)
            {
                if (j > 0) sbInsert.Append(", ");

                sbInsert.Append("@v" + commandVariableName);
                commandVariableName++;
            }

            sbInsert.Append("); "); // TODO: Check Oracle
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
            foreach (var row in tableData)
            {
                command ??= CreateCommand(connection, null);

                currentRow++;
                currentRowInBatch++;
                var cmd = commandsByRow[currentRowInBatch - 1];
                sb.Append(cmd);
                for (var i = 0; i < countColumns; i++)
                {
                    var val = row[i];
                    if (InsertCoerceValues)
                    {
                        if (sqlObjectTableColumns != null && sqlObjectTableColumns.TryGetValue(tableData.Columns[i].Name, out var sqlObjectTableColumn)) { val = InsertCoerceValue(val, sqlObjectTableColumn); }
                    }

                    command.AddParameter(DbType.String, parameterName: "@v" + currentParameterCount, size: -1, value: val);

                    currentParameterCount++;
                }

                if (currentParameterCount + countColumns > bs || currentRow == countRows)
                {
                    // Commit tran
                    var sqlText = sb.ToString().TrimOrNull();
                    if (sqlText != null)
                    {
                        command.CommandText = sqlText;
                        log.Trace("ExecuteNonQuery: " + command.CommandText);
                        command.ExecuteNonQueryExceptionWrapped(ExceptionShowFullSql);
                        command.Dispose();
                        command = null;

                        currentParameterCount = 0;
                        currentRowInBatch = 0;
                        sbSize = Math.Max(sbSize, sb.Length);
                        sb = new StringBuilder(sbSize);
                    }
                }
            }

            if (command == null) return;

            log.Trace("ExecuteNonQuery: " + command.CommandText);
            command.ExecuteNonQueryExceptionWrapped(ExceptionShowFullSql);
            command.Dispose();
            //command = null;
        }
    }

    private string InsertCoerceValue(string value, SqlObjectTableColumn column)
    {
        if (column.ColumnDbType == DbType.Boolean)
        {
            if (value == null) return column.IsNullable ? null : "0";

            if (value.ToBoolTry(out var b)) return b ? "1" : "0";
        }

        return value; // Will probably fail
    }

    private Dictionary<string, SqlObjectTableColumn> InsertGetTableColumns(string database, string schema, string table)
    {
        if (!InsertCoerceValues) return null;

        var d = new Dictionary<string, SqlObjectTableColumn>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var c in GetTableColumns(database, schema, table)) d[c.ColumnName] = c;
        }
        catch (Exception e) { log.Debug($"Unable to obtain column list for database:{database}  schema:{schema}  table:{table}", e); }

        return d;
    }

    #endregion Insert

    protected IDbCommand CreateCommand(IDbConnection connection, string sql, CommandType commandType = CommandType.Text)
    {
        var c = connection.CreateCommand();
        c.CommandText = sql;
        c.CommandType = commandType;
        c.CommandTimeout = CommandTimeout;
        return c;
    }

    protected virtual IDbConnection OpenConnection()
    {
        var cf = ConnectionFactory;
        if (cf == null) throw new NullReferenceException($"{GetType().FullNameFormatted()}.{nameof(ConnectionFactory)} is null");

        var connection = cf();
        if (connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken) connection.Open();

        return connection;
    }

    protected IDataParameter[] AddParameters(IDbCommand command, SqlParameter[] parameters) => parameters.OrEmpty().Select(o => AddParameter(command, o)).ToArray();

    protected virtual IDataParameter AddParameter(IDbCommand command, SqlParameter parameter) => parameter == null ? null : command.AddParameter(parameter.Type, parameterName: CleanParameterName(parameter.Name), value: parameter.Value);

    protected virtual string CleanParameterName(string parameterName) => parameterName.CheckNotNullTrimmed(nameof(parameterName)).Replace(' ', '_');

    #region Execute

    public void ExecuteQuery(string sql, Action<IDataReader> action, params SqlParameter[] parameters)
    {
        using (var connection = OpenConnection())
        using (var command = CreateCommand(connection, sql))
        {
            AddParameters(command, parameters);
            log.Trace($"ExecuteQuery: {sql}");
            using (var reader = command.ExecuteReaderExceptionWrapped(ExceptionShowFullSql)) { action(reader); }
        }
    }

    public SqlResultCollection ExecuteQuery(string sql, params SqlParameter[] parameters)
    {
        using (var connection = OpenConnection())
        using (var command = CreateCommand(connection, sql))
        {
            AddParameters(command, parameters);
            log.Trace($"ExecuteQuery: {sql}");
            using (var reader = command.ExecuteReaderExceptionWrapped(ExceptionShowFullSql)) { return reader.ReadSqlResults(); }
        }
    }



    public int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
    {
        using (var connection = OpenConnection())
        using (var command = CreateCommand(connection, sql))
        {
            AddParameters(command, parameters);
            log.Trace($"ExecuteNonQuery: {sql}");
            return command.ExecuteNonQueryExceptionWrapped(ExceptionShowFullSql);
        }
    }

    public object ExecuteScalar(string sql, params SqlParameter[] parameters)
    {
        using (var connection = OpenConnection())
        using (var command = CreateCommand(connection, sql))
        {
            AddParameters(command, parameters);
            log.Trace($"ExecuteScalar: {sql}");
            return command.ExecuteScalarExceptionWrapped(ExceptionShowFullSql);
        }
    }

    public string ExecuteScalarString(string sql, params SqlParameter[] parameters)
    {
        var o = ExecuteScalar(sql, parameters);
        if (o == null || o == DBNull.Value) return null;

        return o.ToStringGuessFormat();
    }

    public Table[] ExecuteStoredProcedure(string schemaAndStoredProcedureEscaped, params SqlParameter[] parameters)
    {
        using (var connection = OpenConnection())
        using (var command = CreateCommand(connection, schemaAndStoredProcedureEscaped, CommandType.StoredProcedure))
        {
            AddParameters(command, parameters);
            log.Trace($"ExecuteStoredProcedure: {schemaAndStoredProcedureEscaped}");
            using (var reader = command.ExecuteReaderExceptionWrapped(ExceptionShowFullSql)) { return Table.Create(reader); }
        }
    }

    #endregion Execute

    #region Escape / Format

    public virtual bool NeedsEscaping(string objectThatMightNeedEscaping)
    {
        objectThatMightNeedEscaping = objectThatMightNeedEscaping.TrimOrNull();
        if (objectThatMightNeedEscaping == null) return false;

        if (ReservedWords.Contains(objectThatMightNeedEscaping)) return true;

        if (!objectThatMightNeedEscaping.ContainsOnly(ValidIdentifierChars)) return true;

        return false;
    }

    public virtual string Escape(string objectToEscape)
    {
        objectToEscape = objectToEscape.TrimOrNull();
        if (objectToEscape == null) return null;

        if (!NeedsEscaping(objectToEscape)) return objectToEscape;

        var el = EscapeLeft;
        if (el != null && !objectToEscape.StartsWith(el.Value)) objectToEscape = el.Value + objectToEscape;

        var er = EscapeRight;
        if (er != null && !objectToEscape.EndsWith(er.Value)) objectToEscape = objectToEscape + er.Value;

        return objectToEscape.TrimOrNull();
    }

    public virtual string Unescape(string objectToUnescape)
    {
        objectToUnescape = objectToUnescape.TrimOrNull();
        if (objectToUnescape == null) return null;

        var el = EscapeLeft;
        if (el != null)
        {
            while (!string.IsNullOrEmpty(objectToUnescape) && objectToUnescape.StartsWith(el.Value)) { objectToUnescape = objectToUnescape.RemoveLeft(1).TrimOrNull(); }
        }

        var er = EscapeRight;
        if (er != null)
        {
            while (!string.IsNullOrEmpty(objectToUnescape) && objectToUnescape.EndsWith(er.Value)) { objectToUnescape = objectToUnescape.RemoveRight(1).TrimOrNull(); }
        }

        return objectToUnescape.TrimOrNull();
    }

    public abstract string Escape(string database, string schema, string table);

    #endregion Escape / Format

    protected Table Query(string sql, List<Exception> exceptions)
    {
        try { return this.ExecuteQueryToTable(sql); }
        catch (Exception e)
        {
            log.Debug("Error Executing SQL: " + sql, e);
            exceptions.Add(e);
        }

        return null;
    }

    protected AggregateException CreateExceptionErrorInSqlStatements(IEnumerable<string> sqlStatements, IEnumerable<Exception> exceptions)
    {
        var sqlStatementsArray = sqlStatements.ToArray();
        return new AggregateException("Error executing " + sqlStatementsArray.Length + " SQL queries", exceptions);
    }

    public SqlType GetSqlDbType(object sqlDbTypeEnum) => sqlDbTypeEnum == null ? null : GetSqlDbType(sqlDbTypeEnum.ToString());

    public SqlType GetSqlDbType(string rawSqlDbType)
    {
        rawSqlDbType = rawSqlDbType.TrimOrNull();
        if (rawSqlDbType == null) return null;

        var dbTypesEnumType = DbTypesEnum;
        if (dbTypesEnumType == null) throw new NullReferenceException(GetType().FullNameFormatted() + "." + nameof(DbTypesEnum) + " is null");

        dbTypesEnumType.CheckIsEnum(nameof(DbTypesEnum));

        var item = SqlType.GetEnumItemBySqlName(dbTypesEnumType, rawSqlDbType);
        if (item == null) return null;

        if (!item.HasAttribute) throw MissingAttributeException.FieldMissingAttribute<SqlTypeAttribute>(dbTypesEnumType, rawSqlDbType);

        return item;
    }

    public IReadOnlyList<SqlType> GetSqlDbTypes()
    {
        var dbTypesEnumType = DbTypesEnum;
        if (dbTypesEnumType == null) throw new NullReferenceException(GetType().FullNameFormatted() + "." + nameof(DbTypesEnum) + " is null");

        dbTypesEnumType.CheckIsEnum(nameof(DbTypesEnum));

        return SqlType.GetEnumItems(dbTypesEnumType);
    }
}
