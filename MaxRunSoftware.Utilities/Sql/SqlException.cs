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

public class SqlException : Exception
{
    #region Constructors

    public SqlException() { }

    public SqlException(string message) : base(message) { }

    public SqlException(string message, Exception innerException) : base(message, innerException) { }

    public SqlException(Exception innerException, IDbCommand command, bool showFullSql) : base(ParseMessage(command, showFullSql), innerException) { }

    #endregion Constructors

    private static string ParseMessage(IDbCommand command, bool showFullSql)
    {
        var defaultMsg = "Error Executing SQL";
        if (!showFullSql)
        {
            return defaultMsg;
        }

        if (command == null)
        {
            return defaultMsg;
        }

        var commandText = command.CommandText.TrimOrNull();
        if (commandText == null)
        {
            return defaultMsg;
        }

        try
        {
            var sql = new StringBuilder(commandText);

            sql = sql.Replace(";", ";" + Environment.NewLine);

            var parameters = new List<IDbDataParameter>();
            foreach (IDbDataParameter p in command.Parameters)
            {
                parameters.Add(p);
            }

            foreach (var p in parameters.OrderByDescending(p => p.ParameterName.Length).ThenByDescending(p => p.ParameterName))
            {
                string val;
                if (p.Value == null || p.Value.Equals(DBNull.Value))
                {
                    val = "NULL";
                }
                else if (p.DbType == DbType.Binary && p.Value is byte[] bytes)
                {
                    val = "byte[" + bytes.Length + "]";
                }
                else if (Constant.DBTYPES_NUMERIC.Contains(p.DbType))
                {
                    val = p.Value.ToString();
                }
                else
                {
                    val = "'" + p.Value + "'";
                }

                sql = sql.Replace(p.ParameterName, val);
            }

            return defaultMsg + ":" + Environment.NewLine + sql;
        }
        catch (Exception e)
        {
            var msgPart = new StringBuilder(": [Unable to parse parameters");
            var parseParametersMsg = e?.Message?.TrimOrNull();
            if (parseParametersMsg != null)
            {
                parseParametersMsg = parseParametersMsg.SplitOnNewline().TrimOrNull().WhereNotNull().ToStringDelimited(" ").TrimOrNull();
                if (parseParametersMsg != null)
                {
                    msgPart.Append(": ");
                    msgPart.Append(parseParametersMsg);
                }
            }

            msgPart.Append(']');
            return defaultMsg + parseParametersMsg + Environment.NewLine + commandText;
        }
    }
}
