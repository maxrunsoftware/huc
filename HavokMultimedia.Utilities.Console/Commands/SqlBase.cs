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
using System.Data.SqlClient;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public abstract class SqlBase : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddParameter("connectionString", "c", "SQL connection string");
            help.AddParameter("commandTimeout", "t", "Length of time in seconds to wait for SQL command to execute before erroring out (" + (60 * 60 * 24) + ")");
        }

        private string connectionString;
        private int commandTimeout;
        protected override void Execute()
        {
            connectionString = GetArgParameterOrConfigRequired("connectionString", "c");
            commandTimeout = GetArgParameterOrConfigInt("commandTimeout", "ct", 60 * 60 * 24);

        }

        protected SqlMicrosoft GetSqlHelper()
        {
            var h = new SqlMicrosoft(CreateConnection);
            h.CommandTimeout = commandTimeout;
            return h;
        }

        private IDbConnection CreateConnection()
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
    }
}
