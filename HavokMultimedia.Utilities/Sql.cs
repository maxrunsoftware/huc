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
    public abstract class Sql
    {
        protected readonly ILogger log;
        private readonly Func<IDbConnection> connectionFactory;

        public bool IsDisposed { get; private set; }

        public Func<string, string> EscapeObject { get; set; }

        public int CommandTimeout { get; set; } = 60 * 60 * 24;

        public Sql(Func<IDbConnection> connectionFactory)
        {
            this.connectionFactory = connectionFactory.CheckNotNull(nameof(connectionFactory));
            log = LogFactory.LogFactoryImpl.GetLogger(GetType());
        }

        public abstract IEnumerable<string> GetDatabases();
        public abstract IEnumerable<string> GetTables(string database, string schema);
        public abstract void DropTable(string database, string schema, string table);
        public abstract IEnumerable<string> GetSchemas(string database);
        public abstract IEnumerable<string> GetColumns(string database, string schema, string table);
        public bool GetTableExists(string database, string schema, string table) => GetTables(database, schema).Where(o => string.Equals(table, o, StringComparison.OrdinalIgnoreCase)).Any();

        public virtual void Insert(IDbConnection connection, string database, string schema, string table, IDictionary<string, string> columnsAndValues)
        {
            var list = new List<(string columnName, string columnValue)>();
            foreach (var kvp in columnsAndValues) list.Add((Escape(kvp.Key), kvp.Value));
            var columnNames = list.Select(o => o.columnName).ToArray();
            var columnValues = list.Select(o => o.columnValue).ToArray();
            var columnParameterNames = new string[columnNames.Length];
            for (int i = 0; i < columnParameterNames.Length; i++) columnParameterNames[i] = "@p" + i;

            var sb = new StringBuilder();
            if (schema == null) sb.Append($"INSERT INTO {Escape(database)}.{Escape(table)} (");
            else sb.Append($"INSERT INTO {Escape(database)}.{Escape(schema)}.{Escape(table)} (");
            sb.Append(string.Join(",", columnNames));
            sb.Append(") VALUES (");
            sb.Append(string.Join(",", columnParameterNames));
            sb.Append(");");
            using (var cmd = CreateCommand(connection, sb.ToString()))
            {
                for (int i = 0; i < columnValues.Length; i++)
                {
                    cmd.AddParameter(parameterName: columnParameterNames[i], value: columnValues[i]);
                }
                cmd.ExecuteNonQuery();
            }
        }

        public virtual void Insert(string database, string schema, string table, Table tableData, int batchSize = 1000)
        {
            if (table.IsEmpty()) return;

            if (batchSize > 2000) batchSize = 2000; // MSSQL character limit

            var countRows = tableData.Count;
            var countColumns = tableData.Columns.Count;

            var commandsByRow = new string[batchSize + 10];
            var commandVariableName = 0;
            for (var i = 0; i < commandsByRow.Length; i++)
            {
                var sbInsert = new StringBuilder();
                if (schema == null) sbInsert.Append($"INSERT INTO {Escape(database)}.{Escape(table)} (");
                else sbInsert.Append($"INSERT INTO {Escape(database)}.{Escape(schema)}.{Escape(table)} (");

                sbInsert.Append(string.Join(",", tableData.Columns.Select(o => Escape(o.Name))));
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
                foreach (var row in tableData)
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

        protected IDbCommand CreateCommand(IDbConnection connection, string sql, CommandType commandType = CommandType.Text)
        {
            var c = connection.CreateCommand();
            c.CommandText = sql;
            c.CommandType = commandType;
            c.CommandTimeout = CommandTimeout;
            return c;
        }

        protected IDbConnection OpenConnection()
        {
            var connection = connectionFactory();
            if (connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken) connection.Open();
            return connection;
        }
        protected IDataParameter[] AddParameters(IDbCommand command, SqlParameter[] parameters) => parameters.OrEmpty().Select(o => AddParameter(command, o)).ToArray();

        public string Escape(string objectToEscape)
        {
            var f = EscapeObject;
            return f != null ? f(objectToEscape) : objectToEscape;
        }

        protected virtual IDataParameter AddParameter(IDbCommand command, SqlParameter parameter) => parameter == null ? null : command.AddParameter(dbType: parameter.Type, parameterName: CleanParameterName(parameter.Name), value: parameter.Value);

        protected virtual string CleanParameterName(string parameterName) => parameterName.CheckNotNullTrimmed(nameof(parameterName)).Replace(' ', '_');

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

        public List<string> ExecuteQueryToList(string sql, params SqlParameter[] parameters)
        {
            var tables = ExecuteQuery(sql, parameters);
            var list = new List<string>();
            if (tables.Length < 1) return list;
            var table = tables[0];
            if (table.Columns.Count < 1) return list;
            foreach (var row in table)
            {
                list.Add(row[0]);
            }
            return list;
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

        public string ExecuteScalarString(string sql, params SqlParameter[] parameters)
        {
            var o = ExecuteScalar(sql, parameters);
            if (o == null || o == DBNull.Value) return null;
            return o.ToStringGuessFormat();
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
    }
}
