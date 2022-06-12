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

using System.Numerics;

namespace MaxRunSoftware.Utilities;

public abstract class SqlResultBase
{
    protected readonly ILogger log;

    protected SqlResultBase() : this(true) { }

    protected SqlResultBase(bool createLogger)
    {
        log = createLogger ? LogFactory.LogFactoryImpl.GetLogger(GetType()) : LoggerBase.NULL_LOGGER;
    }
}

public class SqlResultCollection : SqlResultBase, IReadOnlyList<SqlResult>
{
    private readonly IReadOnlyList<SqlResult> results;

    public SqlResultCollection(IDataReader reader)
    {
        var list = new List<SqlResult>();
        var i = 0;
        do
        {
            var t = new SqlResult(i, reader);
            list.Add(t);
            i++;
        } while (reader.NextResult());

        results = list.AsReadOnly();
    }

    public IEnumerator<SqlResult> GetEnumerator()
    {
        return results.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => results.Count;

    public SqlResult this[int index] => results[index];
}

public class SqlResult : SqlResultBase
{
    public int Index { get; }
    public SqlResultColumnCollection Columns { get; }
    public SqlResultRowCollection Rows { get; }

    public SqlResult(int index, IDataReader reader)
    {
        log.Trace($"Reading {nameof(SqlResult)}[{index + 1}]");
        Index = index;
        Columns = new SqlResultColumnCollection(reader);
        Rows = new SqlResultRowCollection(reader, this);
    }
}

public static class SqlResultExtensions
{
    public static SqlResultCollection ReadSqlResults(this IDataReader reader)
    {
        return new SqlResultCollection(reader);
    }
}

public class SqlResultColumnCollection : SqlResultBase, IReadOnlyList<SqlResultColumn>, IBucketReadOnly<string, SqlResultColumn>, IBucketReadOnly<int, SqlResultColumn>
{
    private readonly IReadOnlyList<SqlResultColumn> columns;
    private readonly IReadOnlyDictionary<string, SqlResultColumn> columnsByName;
    private readonly IReadOnlyList<int> columnIndexes;

    public SqlResultColumnCollection(IDataReader reader, bool fullSchemaDetails = true)
    {
        columns = reader.GetSchema(fullSchemaDetails).Select(o => new SqlResultColumn(o)).OrderBy(o => o.Index).ToList().AsReadOnly();

        columnsByName = columns.ToDictionaryReadOnlyStringCaseInsensitive(o => o.Name);
        ColumnNames = columns.Select(o => o.Name).ToList().AsReadOnly();
        columnIndexes = columns.Select(o => o.Index).ToList().AsReadOnly();
    }

    public SqlResultColumn this[int index] => columns[index];
    public SqlResultColumn this[string name] => columnsByName[name];
    public IReadOnlyList<string> ColumnNames { get; }

    public bool Contains(int index)
    {
        return index >= 0 && index < Count;
    }

    public bool Contains(string name)
    {
        return columnsByName.ContainsKey(name);
    }

    public bool Contains(SqlResultColumn column)
    {
        return columns.Contains(column);
    }

    public int Count => columns.Count;

    public IEnumerator<SqlResultColumn> GetEnumerator()
    {
        return columns.GetEnumerator();
    }

    IEnumerable<string> IBucketReadOnly<string, SqlResultColumn>.Keys => ColumnNames;
    IEnumerable<int> IBucketReadOnly<int, SqlResultColumn>.Keys => columnIndexes;

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class SqlResultColumn : SqlResultBase
{
    private SqlDataReaderSchemaColumn SchemaColumn { get; }

    public int Index => SchemaColumn.Index;
    public string Name => SchemaColumn.Name;
    public int? Size => SchemaColumn.Size;
    public int? NumericPrecision => SchemaColumn.NumericPrecision;
    public int? NumericScale => SchemaColumn.NumericScale;
    public Type DataType => SchemaColumn.DataType;
    public string DataTypeName => SchemaColumn.DataTypeName;
    public bool? IsNullable => SchemaColumn.IsNullable;

    public SqlResultColumn(SqlDataReaderSchemaColumn schemaColumn)
    {
        SchemaColumn = schemaColumn.CheckNotNull(nameof(schemaColumn));
    }
}

public class SqlResultRowCollection : SqlResultBase, IReadOnlyList<SqlResultRow>
{
    public SqlResult Result { get; }

    private readonly IReadOnlyList<SqlResultRow> rows;

    public IEnumerator<SqlResultRow> GetEnumerator()
    {
        return rows.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => rows.Count;

    public SqlResultRow this[int index] => rows[index];

    public SqlResultRowCollection(IDataReader reader, SqlResult result)
    {
        Result = result.CheckNotNull(nameof(result));
        var valueRows = reader.GetValuesAll();
        var list = new List<SqlResultRow>(valueRows.Count);
        foreach (var valueRow in valueRows)
        {
            for (var i = 0; i < valueRow.Length; i++)
            {
                if (valueRow[i] == DBNull.Value)
                {
                    valueRow[i] = null;
                }
            }

            var row = new SqlResultRow(valueRow, this);
            list.Add(row);
        }

        rows = list.AsReadOnly();
    }
}

public class SqlResultRow : SqlResultBase, IReadOnlyList<object>
{
    private readonly object[] objs;
    private readonly SqlResultRowCollection rowCollection;

    public SqlResultRow(object[] data, SqlResultRowCollection rowCollection) : base(false)
    {
        objs = data;
        this.rowCollection = rowCollection;
    }

    public IEnumerator<object> GetEnumerator()
    {
        return ((IEnumerable<object>)objs).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => objs.Length;

    public object this[int index] => objs[index];
    public object this[string name] => rowCollection.Result.Columns[name].Index;
    public object this[SqlResultColumn column] => this[column.Index];

    public object GetObject(int index)
    {
        return this[index];
    }

    public object GetObject(string name)
    {
        return this[name];
    }

    public object GetObject(SqlResultColumn column)
    {
        return this[column];
    }

    public string GetString(int index)
    {
        return GetObject(index).ToStringGuessFormat();
    }

    public string GetString(string name)
    {
        return GetObject(name).ToStringGuessFormat();
    }

    public string GetString(SqlResultColumn column)
    {
        return GetObject(column).ToStringGuessFormat();
    }

    public T Get<T>(int index)
    {
        return GetConvert<T>(GetObject(index));
    }

    public T Get<T>(string name)
    {
        return GetConvert<T>(GetObject(name));
    }

    public T Get<T>(SqlResultColumn column)
    {
        return GetConvert<T>(GetObject(column));
    }

    private static readonly Dictionary<Type, Func<string, object>> CONVERTERS = CreateConverters();

    private static Dictionary<Type, Func<string, object>> CreateConverters()
    {
        var d = new Dictionary<Type, Func<string, object>>();

        d.AddRange(o => o.ToBool(), typeof(bool), typeof(bool?));

        d.AddRange(o => o.ToByte(), typeof(byte), typeof(byte?));
        d.AddRange(o => o.ToSByte(), typeof(sbyte), typeof(sbyte?));

        d.AddRange(o => o.ToShort(), typeof(short), typeof(short?));
        d.AddRange(o => o.ToUShort(), typeof(ushort), typeof(ushort?));

        d.AddRange(o => o.ToInt(), typeof(int), typeof(int?));
        d.AddRange(o => o.ToUInt(), typeof(uint), typeof(uint?));

        d.AddRange(o => o.ToLong(), typeof(long), typeof(long?));
        d.AddRange(o => o.ToULong(), typeof(ulong), typeof(ulong?));

        d.AddRange(o => o.ToFloat(), typeof(float), typeof(float?));
        d.AddRange(o => o.ToDouble(), typeof(double), typeof(double?));
        d.AddRange(o => o.ToDecimal(), typeof(decimal), typeof(decimal?));

        d.AddRange(o => BigInteger.Parse(o), typeof(BigInteger), typeof(BigInteger?));

        d.AddRange(o => o[0], typeof(char), typeof(char?));

        d.AddRange(o => o.ToGuid(), typeof(Guid), typeof(Guid?));

        return new Dictionary<Type, Func<string, object>>(d);
    }

    private static T GetConvert<T>(object o)
    {
        if (o == null)
        {
            return default;
        }

        var returnType = typeof(T);

        if (returnType == typeof(object))
        {
            return (T)o;
        }

        if (returnType == typeof(byte[]))
        {
            return (T)o;
        }

        if (returnType == typeof(string))
        {
            return (T)(object)o.ToStringGuessFormat();
        }

        if (returnType == typeof(char[]))
        {
            return (T)(object)o.ToStringGuessFormat();
        }

        var os = o.ToString().TrimOrNull();
        if (os == null)
        {
            return default;
        }

        if (CONVERTERS.TryGetValue(returnType, out var converter))
        {
            return (T)converter(os);
        }

        return (T)o;
    }
}
