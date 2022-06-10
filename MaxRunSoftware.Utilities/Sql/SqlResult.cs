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

namespace MaxRunSoftware.Utilities;

public class SqlResult
{
    public int ResultIndex { get; }
    public SqlResultColumnCollection Columns { get; }

    public SqlResult(int resultIndex, IDataReader reader)
    {
        ResultIndex = resultIndex;
        Columns = new SqlResultColumnCollection(reader);


    }

    public static IReadOnlyList<SqlResult> Create(IDataReader dataReader)
    {
        dataReader.CheckNotNull(nameof(dataReader));

        var list = new List<SqlResult>();
        int i = 0;
        do
        {
            var r = new SqlResult(i, dataReader);
            list.Add(r);
            i++;
        } while (dataReader.NextResult());

        return list.AsReadOnly();
    }
}

public class SqlResultColumnCollection : IReadOnlyList<SqlResultColumn>, IBucketReadOnly<string, SqlResultColumn>, IBucketReadOnly<int, SqlResultColumn>
{
    private readonly IReadOnlyList<SqlResultColumn> columns;
    private readonly IReadOnlyDictionary<string, SqlResultColumn> columnsByName;
    private readonly IReadOnlyList<string> columnNames;
    private readonly IReadOnlyList<int> columnIndexes;

    public SqlResultColumnCollection(IDataReader reader)
    {
        columns = reader.GetSchema().Select(o => new SqlResultColumn(o)).OrderBy(o => o.Index).ToList().AsReadOnly();
        columnsByName = DictionaryReadOnlyStringCaseInsensitive<SqlResultColumn>.Create(o => o.Name, columns);
        columnNames = columns.Select(o => o.Name).ToList().AsReadOnly();
        columnIndexes = columns.Select(o => o.Index).ToList().AsReadOnly();
    }

    public SqlResultColumn this[int index] => columns[index];
    public SqlResultColumn this[string name] => columnsByName[name];
    public IReadOnlyList<string> ColumnNames => columnNames;
    public bool Contains(int index) => index >= 0 && index < Count;
    public bool Contains(string name) => columnsByName.ContainsKey(name);
    public bool Contains(SqlResultColumn column) => columns.Contains(column);

    public int Count => columns.Count;

    public IEnumerator<SqlResultColumn> GetEnumerator() => columns.GetEnumerator();

    IEnumerable<string> IBucketReadOnly<string, SqlResultColumn>.Keys => columnNames;
    IEnumerable<int> IBucketReadOnly<int, SqlResultColumn>.Keys => columnIndexes;
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class SqlResultColumn
{
    public SqlDataReaderSchemaColumn SchemaColumn { get; }

    public int Index => SchemaColumn.Index;
    public string Name => SchemaColumn.Name;
    public int? Size => SchemaColumn.Size;
    public int? NumericPrecision => SchemaColumn.NumericPrecision;
    public int? NumericScale => SchemaColumn.NumericScale;
    public Type DataType => SchemaColumn.DataType;
    public string DataTypeName => SchemaColumn.DataTypeName;
    public bool? IsNullable => SchemaColumn.IsNullable;

    public SqlResultColumn(SqlDataReaderSchemaColumn schemaColumn) => SchemaColumn = schemaColumn.CheckNotNull(nameof(schemaColumn));
}

public class SqlResultRow
{

}



