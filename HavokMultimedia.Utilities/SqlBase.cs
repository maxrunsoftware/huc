// /*
// Copyright (c) 2021 Steven Foster (steven.d.foster@gmail.com)
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
// */
using System;
using System.Data;
using System.Linq;

namespace HavokMultimedia.Utilities
{
    public abstract class SqlBase
    {
        protected readonly ILogger log;
        private readonly Func<IDbConnection> connectionFactory;

        public bool IsDisposed { get; private set; }

        public Func<string, string> EscapeObject { get; set; }

        public int CommandTimeout { get; set; } = 60 * 60 * 24;

        public SqlBase(Func<IDbConnection> connectionFactory)
        {
            this.connectionFactory = connectionFactory.CheckNotNull(nameof(connectionFactory));
            log = LogFactory.LogFactoryImpl.GetLogger(GetType());
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
    }
}
