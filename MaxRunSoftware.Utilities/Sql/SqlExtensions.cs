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

public static class SqlExtensions
{
    public static IDataReader ExecuteReaderExceptionWrapped(this IDbCommand command, bool exceptionShowFullSql)
    {
        try { return command.ExecuteReader(); }
        catch (Exception e) { throw new SqlException(e, command, exceptionShowFullSql); }
    }

    public static object ExecuteScalarExceptionWrapped(this IDbCommand command, bool exceptionShowFullSql)
    {
        try { return command.ExecuteScalar(); }
        catch (Exception e) { throw new SqlException(e, command, exceptionShowFullSql); }
    }

    public static int ExecuteNonQueryExceptionWrapped(this IDbCommand command, bool exceptionShowFullSql)
    {
        try { return command.ExecuteNonQuery(); }
        catch (Exception e) { throw new SqlException(e, command, exceptionShowFullSql); }
    }
    
    public static List<string> ExecuteQueryToList(this Sql sql, string sqlQuery, params SqlParameter[] parameters)
    {
        var list = new List<string>();
        var table = ExecuteQueryToTable(sql, sqlQuery, parameters);
        if (table == null) return list;
        if (table.Columns.Count < 1) return list;

        foreach (var row in table) list.Add(row[0]);

        return list;
    }

    public static Table ExecuteQueryToTable(this Sql sql, string sqlQuery, params SqlParameter[] parameters) => ExecuteQueryToTables(sql, sqlQuery, parameters).FirstOrDefault();
    
    public static Table[] ExecuteQueryToTables(this Sql sql, string sqlQuery, params SqlParameter[] parameters) => Table.Create(sql.ExecuteQuery(sqlQuery, parameters));
}
