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
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public enum SqlServerType { MSSQL, MySQL, Oracle }

    public abstract class SqlBase : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddParameter("connectionString", "c", "SQL connection string");
            help.AddParameter("commandTimeout", "ct", "Length of time in seconds to wait for SQL command to execute before erroring out (" + (60 * 60 * 24) + ")");
            help.AddParameter("serverType", "st", "The SQL server type [ MSSQL | MySQL ] (MSSQL)");
            help.AddDetail("Example connection strings:");
            help.AddDetail("  Server=192.168.0.5;Database=myDatabase;User Id=myUsername;Password=myPassword;");
            help.AddDetail("  Server=192.168.0.5\\instanceName;Database=myDataBase;User Id=myUsername;Password=myPassword;");
            help.AddDetail("  Server=192.168.0.5;Database=myDataBase;Trusted_Connection=True;");
        }

        private string connectionString;
        private int commandTimeout;
        protected SqlServerType SqlServerType { get; private set; }

        protected override void Execute()
        {
            connectionString = GetArgParameterOrConfigRequired("connectionString", "c");
            commandTimeout = GetArgParameterOrConfigInt("commandTimeout", "ct", 60 * 60 * 24);
            SqlServerType = GetArgParameterOrConfigEnum("serverType", "st", SqlServerType.MSSQL);

        }

        protected Utilities.Sql GetSqlHelper()
        {
            if (connectionString == null) throw new Exception("base.Execute() never called for class " + GetType().FullNameFormatted());

            switch (SqlServerType)
            {
                case SqlServerType.MSSQL: return new SqlMSSQL(CreateConnectionMSSQL) { CommandTimeout = commandTimeout };
                case SqlServerType.MySQL: return new SqlMySQL(CreateConnectionMySQL) { CommandTimeout = commandTimeout };
                case SqlServerType.Oracle: return new SqlOracle(CreateConnectionMySQL) { CommandTimeout = commandTimeout };
                default: throw new NotImplementedException($"sqlServerType {SqlServerType} has not been implemented yet");
            }
        }


        private IDbConnection CreateConnectionMSSQL()
        {
            var conn = new SqlConnection(connectionString);

            conn.InfoMessage += delegate (object sender, SqlInfoMessageEventArgs e)
            {
                foreach (SqlError info in e.Errors)
                {
                    var msg = info.Message.TrimOrNull();
                    if (msg == null) continue;
                    if (info.Class > 10) log.Warn(msg);
                    else log.Info(msg);
                }
            };
            return conn;
        }

        private IDbConnection CreateConnectionMySQL()
        {
            var conn = new MySqlConnection(connectionString);

            conn.InfoMessage += delegate (object sender, MySqlInfoMessageEventArgs e)
            {
                foreach (var info in e.errors)
                {
                    var msg = info.Message.TrimOrNull();
                    if (msg == null) continue;
                    if (info.Code > 10) log.Warn(msg);
                    else log.Info(msg);
                }
            };
            return conn;
        }

        private IDbConnection CreateConnectionOracle()
        {
            var conn = new OracleConnection(connectionString);

            conn.InfoMessage += delegate (object sender, OracleInfoMessageEventArgs e)
            {
                foreach (var info in e.Errors)
                {
                    var msg = info.ToString().TrimOrNull();
                    if (msg == null) continue;
                    else log.Info(msg);
                }
            };
            return conn;
        }

    }
}
