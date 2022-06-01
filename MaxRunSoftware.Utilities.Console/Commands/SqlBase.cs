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
using System.Data;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public enum SqlServerType { MsSql, MySql, Oracle }

    public abstract class SqlBase : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddParameter(nameof(connectionString), "c", "SQL connection string");
            help.AddParameter(nameof(commandTimeout), "ct", "Length of time in seconds to wait for SQL command to execute before erroring out (" + (60 * 60 * 24) + ")");
            help.AddParameter(nameof(serverType), "st", "The SQL server type (" + SqlServerType.MsSql + ")  " + DisplayEnumOptions<SqlServerType>());
            help.AddParameter(nameof(showSqlInExceptions), "ssie", "Show full SQL in exceptions (false)");

            help.AddDetail("Example connection strings:");
            help.AddDetail("  Server=192.168.0.5;Database=myDatabase;User Id=myUsername;Password=myPassword;");
            help.AddDetail("  Server=192.168.0.5\\instanceName;Database=myDataBase;User Id=myUsername;Password=myPassword;");
            help.AddDetail("  Server=192.168.0.5;Database=myDataBase;Trusted_Connection=True;");
        }

        protected string HelpExamplePrefix => "-c=`Server=192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;`";

        private string connectionString;
        protected int commandTimeout { get; private set; }
        protected SqlServerType serverType { get; private set; }
        private bool showSqlInExceptions;

        protected override void ExecuteInternal()
        {
            connectionString = GetArgParameterOrConfigRequired(nameof(connectionString), "c");
            commandTimeout = GetArgParameterOrConfigInt(nameof(commandTimeout), "ct", 60 * 60 * 24);
            serverType = GetArgParameterOrConfigEnum(nameof(serverType), "st", SqlServerType.MsSql);
            showSqlInExceptions = GetArgParameterOrConfigBool(nameof(showSqlInExceptions), "ssie", false);
        }

        protected Utilities.Sql GetSqlHelper()
        {
            if (connectionString == null) throw new Exception("base.Execute() never called for class " + GetType().FullNameFormatted());

            Utilities.Sql sql;
            if (serverType == SqlServerType.MsSql) sql = new SqlMsSql() { ConnectionFactory = CreateConnectionMsSql };
            else if (serverType == SqlServerType.MySql) sql = new SqlMySql() { ConnectionFactory = CreateConnectionMySql };
            else if (serverType == SqlServerType.Oracle) sql = new SqlOracle() { ConnectionFactory = CreateConnectionOracle };
            else throw new NotImplementedException($"sqlServerType {serverType} has not been implemented yet");

            sql.CommandTimeout = commandTimeout;
            sql.ExceptionShowFullSql = showSqlInExceptions;

            return sql;
        }


        protected IDbConnection CreateConnectionMsSql()
        {
            var conn = new SqlConnection(connectionString);

            conn.InfoMessage += delegate (object sender, SqlInfoMessageEventArgs e)
            {
                if (e.Errors != null)
                {
                    foreach (SqlError info in e.Errors)
                    {
                        var msg = info.Message.TrimOrNull();
                        if (msg == null) continue;
                        if (info.Class > 10) log.Warn(msg);
                        else log.Info(msg);
                    }
                }
            };
            return conn;
        }

        private IDbConnection CreateConnectionMySql()
        {
            var conn = new MySqlConnection(connectionString);

            conn.InfoMessage += delegate (object sender, MySqlInfoMessageEventArgs e)
            {
                if (e.errors != null)
                {
                    foreach (var info in e.errors.OrEmpty())
                    {
                        var msg = info?.Message.TrimOrNull();
                        if (msg == null) continue;
                        if (info.Code > 10) log.Warn(msg);
                        else log.Info(msg);
                    }
                }
            };
            return conn;
        }

        private IDbConnection CreateConnectionOracle()
        {
            var conn = new OracleConnection(connectionString);

            conn.InfoMessage += delegate (object sender, OracleInfoMessageEventArgs e)
            {
                if (e.Errors != null)
                {
                    foreach (var info in e.Errors)
                    {
                        var msg = info?.ToString().TrimOrNull();
                        if (msg == null) continue;
                        else log.Info(msg);
                    }
                }
            };
            return conn;
        }

    }
}
