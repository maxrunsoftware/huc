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

public class SqlDataReaderSchemaColumn
{
    public SqlDataReaderSchemaColumnBasic Basic { get; }
    public SqlDataReaderSchemaColumnExtended Extended { get; }

    public SqlDataReaderSchemaColumn(SqlDataReaderSchemaColumnBasic basic, SqlDataReaderSchemaColumnExtended extended)
    {
        Basic = basic.CheckNotNull(nameof(basic));
        Extended = extended;

        Index = Basic.Index;
        Name = Basic.ColumnName.TrimOrNull() ?? Extended?.ColumnName?.TrimOrNull() ?? Extended?.BaseColumnName?.TrimOrNull();
        Size = Extended?.ColumnSize;
        NumericPrecision = Extended?.NumericPrecision;
        NumericScale = Extended?.NumericScale;
        DataType = Extended?.DataType ?? Basic.FieldType;
        DataTypeName = Extended?.DataTypeName?.TrimOrNull() ?? Basic.DataTypeName.TrimOrNull();
        IsNullable = Extended?.AllowDbNull;
    }

    private SqlDataReaderSchemaColumn(Pair pair) : this(pair.basic.basic, pair.extended?.extended) { }

    public int Index { get; }
    public string Name { get; }
    public int? Size { get; }
    public int? NumericPrecision { get; }
    public int? NumericScale { get; }
    public Type DataType { get; }
    public string DataTypeName { get; }
    public bool? IsNullable { get; }

    private class Pair
    {
        public readonly WrapperBasic basic;
        public WrapperExtended extended;

        public Pair(WrapperBasic b)
        {
            basic = b;
        }
    }

    private class WrapperBasic
    {
        public readonly SqlDataReaderSchemaColumnBasic basic;
        public readonly int index;
        public readonly string name;
        public readonly Type type;

        public WrapperBasic(SqlDataReaderSchemaColumnBasic o)
        {
            basic = o;
            index = o.Index;
            name = o.ColumnName.TrimOrNull();
            type = o.FieldType;
        }
    }

    private class WrapperExtended
    {
        public readonly SqlDataReaderSchemaColumnExtended extended;
        public int index;
        public readonly string name;
        public readonly Type type;

        public WrapperExtended(SqlDataReaderSchemaColumnExtended o)
        {
            extended = o;
            index = o.ColumnOrdinal ?? 1_673_452_543; // some random value that should never be used
            name = o.ColumnName.TrimOrNull() ?? o.BaseColumnName.TrimOrNull();
            type = o.DataType ?? o.ProviderSpecificDataType;
        }
    }

    public static IReadOnlyList<SqlDataReaderSchemaColumn> Create(IDataReader reader, bool fullSchemaDetails = true)
    {
        var pairs = SqlDataReaderSchemaColumnBasic.Create(reader).Select(o => new Pair(new WrapperBasic(o))).ToList();
        var wrapperItems = new List<WrapperExtended>();
        if (fullSchemaDetails)
        {
            wrapperItems = SqlDataReaderSchemaColumnExtended.Create(reader).Select(o => new WrapperExtended(o)).ToList();
        }

        // Short circuit if no items
        if (wrapperItems.IsEmpty())
        {
            return pairs.Select(o => new SqlDataReaderSchemaColumn(o)).ToList();
        }


        // Fix for MySql adding +1 to column ordinal in schema
        var columnOrdinalOffset = wrapperItems.Select(o => o.index).Min();
        foreach (var e in wrapperItems)
        {
            e.index -= columnOrdinalOffset;
        }


        // Try to match every basic with an extended
        var matchers = new List<Func<WrapperBasic, WrapperExtended, bool>>
        {
            (b, e) => b.index == e.index && b.name.EqualsCaseSensitive(e.name) && b.type == e.type,
            (b, e) => b.index == e.index && b.name.EqualsCaseSensitive(e.name),
            (b, e) => b.index == e.index && b.type == e.type,
            (b, e) => b.index == e.index,
            (b, e) => b.name.EqualsCaseSensitive(e.name),
            (b, e) => b.name.EqualsCaseInsensitive(e.name),
            (b, e) => b.type == e.type
        };
        foreach (var matcher in matchers)
        {
            foreach (var pair in pairs.Where(o => o.extended == null))
            {
                foreach (var e in wrapperItems)
                {
                    if (matcher(pair.basic, e))
                    {
                        pair.extended = e;
                    }

                    if (pair.extended != null)
                    {
                        break;
                    }
                }

                if (pair.extended != null)
                {
                    wrapperItems.Remove(pair.extended);
                }

                if (wrapperItems.IsEmpty())
                {
                    break;
                }
            }

            if (wrapperItems.IsEmpty())
            {
                break;
            }
        }

        if (wrapperItems.IsNotEmpty())
        {
            throw new SqlException($"Had extra {nameof(SqlDataReaderSchemaColumnExtended)} values that we could not match: " + wrapperItems.Select(o => o.name).ToStringDelimited(", "));
        }

        return pairs.Select(o => new SqlDataReaderSchemaColumn(o)).ToList();
    }
}

public class SqlDataReaderSchemaColumnBasic
{
    public int Index { get; set; }
    public string ColumnName { get; set; }
    public Type FieldType { get; set; }
    public string DataTypeName { get; set; }

    public static IReadOnlyList<SqlDataReaderSchemaColumnBasic> Create(IDataReader reader)
    {
        var columns = new List<SqlDataReaderSchemaColumnBasic>();

        var colCount = reader.FieldCount;
        for (var colIndex = 0; colIndex < colCount; colIndex++)
        {
            var columnBasic = new SqlDataReaderSchemaColumnBasic();
            columnBasic.Index = colIndex;
            columnBasic.ColumnName = reader.GetName(colIndex);
            columnBasic.FieldType = reader.GetFieldType(colIndex);
            columnBasic.DataTypeName = reader.GetDataTypeName(colIndex);
            columns.Add(columnBasic);
        }

        return columns;
    }
}

public class SqlDataReaderSchemaColumnExtended
{
    public string ColumnName { get; set; }
    public int? ColumnOrdinal { get; set; }
    public int? ColumnSize { get; set; }
    public int? NumericPrecision { get; set; }
    public int? NumericScale { get; set; }
    public bool? IsUnique { get; set; }
    public bool? IsKey { get; set; }
    public bool? IsRowId { get; set; }
    public string BaseServerName { get; set; }
    public string BaseCatalogName { get; set; }
    public string BaseColumnName { get; set; }
    public string BaseSchemaName { get; set; }
    public string BaseTableName { get; set; }
    public Type DataType { get; set; }
    public bool? AllowDbNull { get; set; }
    public int? ProviderType { get; set; }
    public bool? IsAliased { get; set; }
    public bool? IsByteSemantic { get; set; }
    public bool? IsExpression { get; set; }
    public bool? IsIdentity { get; set; }
    public bool? IsAutoIncrement { get; set; }
    public bool? IsRowVersion { get; set; }
    public bool? IsHidden { get; set; }
    public bool? IsLong { get; set; }
    public bool? IsReadOnly { get; set; }
    public bool? IsValueLob { get; set; }
    public Type ProviderSpecificDataType { get; set; }
    public string IdentityType { get; set; }
    public string DataTypeName { get; set; }
    public string XmlSchemaCollectionDatabase { get; set; }
    public string XmlSchemaCollectionOwningSchema { get; set; }
    public string XmlSchemaCollectionName { get; set; }
    public string UdtAssemblyQualifiedName { get; set; }
    public string UdtTypeName { get; set; }
    public int? NonVersionedProviderType { get; set; }
    public bool? IsColumnSet { get; set; }

    public static IReadOnlyList<SqlDataReaderSchemaColumnExtended> Create(IDataReader reader)
    {
        reader.CheckNotNull(nameof(reader));

        var columns = new List<SqlDataReaderSchemaColumnExtended>();

        var dataTable = reader.GetSchemaTable();

        if (dataTable == null)
        {
            return columns.AsReadOnly();
        }

        var d = dataTable.Columns.Cast<DataColumn>().ToDictionary(col => col.ColumnName);

        var cols = new DictionaryReadOnlyStringCaseInsensitive<DataColumn>(d);

        var props = ClassReaderWriter.GetProperties(typeof(SqlDataReaderSchemaColumnExtended), canSet: true, isInstance: true).ToList();
        foreach (DataRow dataRow in dataTable.Rows)
        {
            var columnExtended = new SqlDataReaderSchemaColumnExtended();
            foreach (var prop in props)
            {
                if (!cols.TryGetValue(prop.Name, out var dataColumn))
                {
                    continue; // DataTable does not contain column
                }

                var dataValue = dataRow[dataColumn];
                prop.SetValue(columnExtended, dataValue, Util.ChangeType);
            }

            columns.Add(columnExtended);
        }


        return columns;
    }


    public override string ToString()
    {
        var props = ClassReaderWriter.GetPropertiesValues(this).Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.ToStringGuessFormat() ?? "")).ToList();

        var maxColumnLen = props.Select(o => o.Key.Length).Max() + 2;

        var sb = new StringBuilder();
        sb.AppendLine(GetType().FullNameFormatted() + " {");
        foreach (var p in props)
        {
            sb.AppendLine("  " + (p.Key + ":").PadRight(maxColumnLen) + p.Value);
        }

        sb.AppendLine("} ");

        return sb.ToString();
    }
}
