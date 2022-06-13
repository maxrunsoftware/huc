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

public static class ExtensionsData
{
    private static List<string[]> ToCsvSplit(DataTable dataTable)
    {
        var rows = new List<string[]>();

        var columns = dataTable.Columns.AsList().ToArray();
        var width = columns.Length;

        var columnStrings = new string[width];
        for (var i = 0; i < width; i++)
        {
            columnStrings[i] = columns[i].ColumnName;
        }

        rows.Add(columnStrings);

        foreach (DataRow dataRow in dataTable.Rows)
        {
            var array = dataRow.ItemArray;
            var arrayWidth = array.Length;
            var arrayString = new string[arrayWidth];
            for (var i = 0; i < arrayWidth; i++)
            {
                arrayString[i] = array[i].ToStringGuessFormat();
            }

            rows.Add(arrayString);
        }

        return rows;
    }

    public static void DisposeSafely(this IDbConnection connection, Action<string, Exception> errorLog = null)
    {
        if (connection == null)
        {
            return;
        }

        try
        {
            if (connection.State != ConnectionState.Closed)
            {
                connection.Close();
            }
        }
        catch (Exception e)
        {
            errorLog?.Invoke("Error closing database connection [" + connection.GetType().FullNameFormatted() + "]", e);
        }

        try
        {
            connection.Dispose();
        }
        catch (Exception e)
        {
            errorLog?.Invoke("Error disposing database connection [" + connection.GetType().FullNameFormatted() + "]", e);
        }
    }

    public static IDataParameter SetParameterValue(this IDbCommand command, int parameterIndex, object value)
    {
        var o = command.Parameters[parameterIndex];
        if (o is not IDbDataParameter p)
        {
            return null;
        }

        p.Value = value ?? DBNull.Value;
        return p;
    }

    public static IDataParameter AddParameter(this IDbCommand command,
        DbType? dbType = null,
        ParameterDirection? direction = null,
        string parameterName = null,
        byte? precision = null,
        byte? scale = null,
        int? size = null,
        string sourceColumn = null,
        DataRowVersion? sourceVersion = null,
        object value = null)
    {
        var p = command.CreateParameter();
        if (dbType.HasValue)
        {
            p.DbType = dbType.Value;
        }

        if (direction.HasValue)
        {
            p.Direction = direction.Value;
        }

        if (parameterName != null)
        {
            p.ParameterName = parameterName;
        }

        if (precision.HasValue)
        {
            p.Precision = precision.Value;
        }

        if (scale.HasValue)
        {
            p.Scale = scale.Value;
        }

        if (size.HasValue)
        {
            p.Size = size.Value;
        }

        if (sourceColumn != null)
        {
            p.SourceColumn = sourceColumn;
        }

        if (sourceVersion.HasValue)
        {
            p.SourceVersion = sourceVersion.Value;
        }

        p.Value = value ?? DBNull.Value;
        command.Parameters.Add(p);
        return p;
    }

    public static List<string> GetNames(this IDataReader dataReader)
    {
        var list = new List<string>();
        var count = dataReader.FieldCount;
        for (var i = 0; i < count; i++)
        {
            list.Add(dataReader.GetName(i));
        }

        return list;
    }

    public static int GetNameIndex(this IDataReader dataReader, string columnName)
    {
        var columnNames = dataReader.GetNames();

        foreach (var sc in Constant.LIST_StringComparison)
        {
            for (var i = 0; i < columnNames.Count; i++)
            {
                var c = columnNames[i].TrimOrNull();
                if (c == null)
                {
                    continue;
                }

                if (string.Equals(columnName, c, sc))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    public static object[] GetValues(this IDataReader dataReader)
    {
        return GetValues(dataReader, dataReader.FieldCount);
    }

    public static object[] GetValues(this IDataReader dataReader, int fieldCount)
    {
        var objs = new object[fieldCount];
        dataReader.GetValues(objs);
        return objs;
    }

    public static List<object[]> GetValuesAll(this IDataReader dataReader)
    {
        var fieldCount = dataReader.FieldCount;
        var list = new List<object[]>();
        while (dataReader.Read())
        {
            list.Add(GetValues(dataReader, fieldCount));
        }

        return list;
    }

    public static List<DataColumn> AsList(this DataColumnCollection dataColumnCollection)
    {
        var list = new List<DataColumn>();
        if (dataColumnCollection == null)
        {
            return list;
        }

        list.Capacity = dataColumnCollection.Count + 1;
        foreach (DataColumn c in dataColumnCollection)
        {
            list.Add(c);
        }

        return list;
    }

    public static List<object[]> AsList(this DataRowCollection dataRowCollection)
    {
        var list = new List<object[]>();
        if (dataRowCollection == null)
        {
            return list;
        }

        list.Capacity = dataRowCollection.Count + 1;
        foreach (DataRow row in dataRowCollection)
        {
            list.Add(row.ItemArray.Copy());
        }

        return list;
    }

    public static IDbCommand CreateCommand(this IDbConnection connection, string commandText, CommandType? commandType = null, int? commandTimeout = null)
    {
        var command = connection.CreateCommand();
        if (commandText != null)
        {
            command.CommandText = commandText;
        }

        if (commandType != null)
        {
            command.CommandType = commandType.Value;
        }

        if (commandTimeout != null)
        {
            command.CommandTimeout = commandTimeout.Value;
        }

        return command;
    }

    public static T GetValueOrDefault<T>(this IDataRecord row, string fieldName)
    {
        // http://stackoverflow.com/a/2610220
        var ordinal = row.GetOrdinal(fieldName);
        return row.GetValueOrDefault<T>(ordinal);
    }

    public static T GetValueOrDefault<T>(this IDataRecord row, int ordinal)
    {
        // http://stackoverflow.com/a/2610220
        return (T)(row.IsDBNull(ordinal) ? default(T) : row.GetValue(ordinal));
    }

    public static string GetStringNullable(this IDataReader reader, int i)
    {
        return reader.IsDBNull(i) ? null : reader.GetString(i);
    }

    public static string ToCsv(this DataTable dataTable, string delimiter, string escape)
    {
        var list = ToCsvSplit(dataTable);

        var sb = new StringBuilder();

        foreach (var array in list)
        {
            for (var i = 0; i < array.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(delimiter);
                }

                sb.Append(escape);
                var cell = array[i];
                if (cell != null && cell.IndexOf("\"", StringComparison.Ordinal) >= 0)
                {
                    cell = cell.Replace("\"", "\"\"");
                }

                sb.Append(cell);
                sb.Append(escape);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static IReadOnlyList<SqlDataReaderSchemaColumn> GetSchema(this IDataReader reader, bool fullSchemaDetails = true)
    {
        return SqlDataReaderSchemaColumn.Create(reader, fullSchemaDetails);
    }
}
