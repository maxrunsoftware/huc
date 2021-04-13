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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;

namespace HavokMultimedia.Utilities
{
    /// <summary>
    /// Encapsulates a 2-diminsional array of string data into columns and rows. The table will never be jagged, all rows will have all columns.
    /// Fields can be null. Columns must have names or they will be generated. Row limit is limited to int.MaxValue for all general purposes.
    /// </summary>
    [Serializable]
    public sealed class Table : ISerializable, IReadOnlyList<TableRow>
    {
        private readonly IReadOnlyList<TableRow> rows;
        public TableColumnCollection Columns { get; }

        private Table(IEnumerable<string[]> data, bool firstRowIsHeader)
        {
            data.CheckNotNull(nameof(data));

            var rowData = new List<string[]>();
            var columnNames = new List<string>();

            var columnsCount = 0;
            var parsedHeader = false;
            if (!firstRowIsHeader) parsedHeader = true;

            foreach (var dataArray in data)
            {
                if (dataArray == null) continue;
                var ss = dataArray.TrimOrNull();
                columnsCount = Math.Max(columnsCount, ss.Length);
                if (!parsedHeader)
                {
                    // Header row
                    columnNames.AddRange(ss);
                    parsedHeader = true;
                }
                else
                {
                    // Data row
                    rowData.Add(ss);
                }
            }

            var rows = new List<TableRow>(rowData.Count);
            for (var i = 0; i < rowData.Count; i++)
            {
                var row = rowData[i].Resize(columnsCount);
                rows.Add(new TableRow(this, row, i));
            }
            this.rows = rows.AsReadOnly();

            var columns = new List<TableColumn>(columnsCount);
            for (var i = 0; i < columnsCount; i++)
            {
                columns.Add(new TableColumn(this, i, columnNames.GetAtIndexOrDefault(i)));
            }
            Columns = new TableColumnCollection(columns);
        }

        #region ISerializable

        private Table(SerializationInfo info, StreamingContext context) : this(info.GetValue<string[][]>(typeof(Table).Name), true)
        {
        }


        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            var list = new List<string[]>(rows.Count + 1)
            {
                Columns.ColumnNames.ToArray()
            };
            list.AddRange(this.Select(o => o.ToArray()));

            info.AddValue(typeof(Table).Name, list.ToArray());
        }

        #endregion ISerializable

        #region IReadOnlyList<TableRow>

        /// <summary>
        /// Number of rows in Table
        /// </summary>
        public int Count => rows.Count;

        /// <summary>
        /// Gets a specific row
        /// </summary>
        /// <param name="rowIndex">The row index</param>
        /// <returns>The row</returns>
        public TableRow this[int rowIndex] => rows[rowIndex];

        public IEnumerator<TableRow> GetEnumerator() => rows.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IReadOnlyList<TableRow>

        #region Create

        public static Table Create<TEnumerable>(IEnumerable<TEnumerable> data, bool firstRowIsHeader) where TEnumerable : class, IEnumerable<string> => new Table(data.CheckNotNull(nameof(data)).Where(o => o != null).Select(o => (o as string[]) ?? o.ToArray()), firstRowIsHeader);

        // public static Table Create<TEnumerable>(IEnumerable<TEnumerable> data, TEnumerable header) where TEnumerable : class, IEnumerable<string> => Create(new List<TEnumerable> { header.CheckNotNull(nameof(header)) }.Concat(data.CheckNotNull(nameof(data))), true);

        public static Table Create<TEnumerableRow, TEnumerableHeader>(IEnumerable<TEnumerableRow> data, TEnumerableHeader header) where TEnumerableRow : class, IEnumerable<string> where TEnumerableHeader : class, IEnumerable<string> => Create(new List<IEnumerable<string>> { header.CheckNotNull(nameof(header)) }.Concat(data.CheckNotNull(nameof(data))), true);

        public static Table[] Create(IDataReader dataReader, TypeConverter converter)
        {
            converter.CheckNotNull(nameof(converter));
            return Create(dataReader, o => (string)converter(o, typeof(string)));
        }

        public static Table[] Create(IDataReader dataReader) => Create(dataReader, Util.ChangeType<string>);

        public static Table[] Create(IDataReader dataReader, Converter<object, string> converter)
        {
            dataReader.CheckNotNull(nameof(dataReader));
            converter.CheckNotNull(nameof(converter));

            var list = new List<Table>();
            do
            {
                var header = dataReader.GetNames();
                var rowsObject = dataReader.GetValuesAll();
                var rows = rowsObject.Select(o => o.Select(oo => converter(oo).TrimOrNull()));
                var t = Create(rows, header);
                list.Add(t);
            } while (dataReader.NextResult());

            return list.ToArray();
        }

        #endregion Create

        #region RemoveColumn

        /// <summary>
        /// Returns a new Table with the specified TableColumn removed
        /// </summary>
        /// <param name="column">The column to remove</param>
        /// <returns>A new Table without the specified column</returns>
        public Table RemoveColumn(TableColumn column) => RemoveColumn(column.CheckNotNull(nameof(column)).Index);

        public Table RemoveColumn(string columnName) => RemoveColumn(Columns[columnName.CheckNotNullTrimmed(nameof(columnName))]);

        public Table RemoveColumn(int columnIndex)
        {
            var c = Columns[columnIndex]; // be sure column actually exists
            columnIndex = c.Index;

            var len = Columns.Count;
            var h = new List<string>();
            for (var i = 0; i < Columns.ColumnNames.Count; i++)
            {
                if (i != columnIndex) h.Add(Columns.ColumnNames[i]);
            }
            var lenMinusOne = h.Count;

            var rs = new List<List<string>>();
            foreach (var row in this)
            {
                var list = new List<string>(lenMinusOne);
                for (var i = 0; i < len; i++)
                {
                    if (i != columnIndex) list.Add(row[i]);
                }
                rs.Add(list);
            }
            return Create(rs, h);
        }

        #endregion RemoveColumn

        #region RenameColumn

        public Table RenameColumn(TableColumn column, string newColumnName) => RenameColumn(column.CheckNotNull(nameof(column)).Index, newColumnName);

        public Table RenameColumn(string columnName, string newColumnName) => RenameColumn(Columns[columnName.CheckNotNullTrimmed(nameof(columnName))], newColumnName);

        public Table RenameColumn(int columnIndex, string newColumnName)
        {
            var c = Columns[columnIndex]; // be sure column actually exists
            columnIndex = c.Index;

            newColumnName = newColumnName.CheckNotNullTrimmed(nameof(newColumnName));

            var len = Columns.Count;
            var h = new List<string>(len);
            for (var i = 0; i < len; i++)
            {
                h.Add(i == columnIndex ? newColumnName : Columns.ColumnNames[i]);
            }
            return Create(this, h);
        }

        #endregion RenameColumn

        #region AddColumn

        public Table AddColumn(string newColumnName, Func<TableRow, string> newColumnDataGenerator, int newColumnIndex = int.MaxValue)
        {
            var newData = new string[Count];
            foreach (var row in this)
            {
                newData[row.RowIndex] = newColumnDataGenerator(row);
            }
            return AddColumn(newColumnName, newData, newColumnIndex);
        }

        public Table AddColumn(string newColumnName, params string[] newColumnDataValues) => AddColumn(newColumnName, (IEnumerable<string>)newColumnDataValues);

        public Table AddColumn(string newColumnName, IEnumerable<string> newColumnData, int newColumnIndex = int.MaxValue)
        {
            newColumnName = newColumnName.CheckNotNullTrimmed(nameof(newColumnName));
            if (!(newColumnData is string[] newColumnDataArray))
            {
                newColumnDataArray = (newColumnData ?? Enumerable.Empty<string>()).ToArray();
            }

            var oldlen = Columns.Count;
            var newlen = oldlen + 1;
            if (newColumnIndex > oldlen) newColumnIndex = oldlen;
            if (newColumnIndex < 0) newColumnIndex = 0;

            var h = new List<string>(newlen);
            h.AddRange(Columns.ColumnNames);
            h.Insert(newColumnIndex, newColumnName);

            var oldRowCount = Count;
            var newRowCount = Math.Max(oldRowCount, newColumnDataArray.Length);
            var oldRowEmpty = new string[oldlen];

            var newRows = new List<string[]>();
            for (var i = 0; i < newRowCount; i++)
            {
                var newRow = new string[newlen];
                var oldRow = i < oldRowCount ? this[i].ToArray() : oldRowEmpty;
                var newRowItem = newColumnDataArray.GetAtIndexOrDefault(i);

                Array.Copy(oldRow, 0, newRow, 0, oldRow.Length);
                newRow[newRow.Length - 1] = newRowItem;

                var array = newRow;
                var source = newRow.Length - 1;
                var dest = newColumnIndex;
                Array.Copy(newRow, dest, array, dest + 1, source - dest);
                array[dest] = newRowItem;

                newRows.Add(array);
            }
            return Create(newRows, h);
        }

        #endregion AddColumn



        public Table Subset(Func<TableRow, bool> predicate)
        {
            var list = new List<IEnumerable<string>>(Count);

            foreach (var row in this)
            {
                if (predicate(row)) list.Add(row);
            }

            return Create(list, Columns.ColumnNames);
        }

        public override string ToString() => "Table[columns:" + Columns.Count + "][rows:" + Count + "]";
    }

    public sealed class TableColumnCollection : IReadOnlyList<TableColumn>, IBucketReadOnly<string, TableColumn>
    {
        private readonly IBucketReadOnly<string, TableColumn> columnNameCache;
        private readonly IReadOnlyList<TableColumn> columns;
        public IReadOnlyList<string> ColumnNames { get; }

        public int Count => columns.Count;

        IEnumerable<string> IBucketReadOnly<string, TableColumn>.Keys => ColumnNames;

        internal TableColumnCollection(IEnumerable<TableColumn> columns)
        {
            this.columns = columns.CheckNotNull(nameof(columns)).WhereNotNull().ToList().AsReadOnly();
            ColumnNames = columns.Select(o => o.Name).ToList().AsReadOnly();

            columnNameCache = new BucketCacheCopyOnWrite<string, TableColumn>(columnName =>
            {
                foreach (var sc in Constant.LIST_StringComparison)
                    foreach (var item in columns)
                        if (string.Equals(item.Name, columnName, sc))
                            return item;
                return null;
            });
        }

        public TableColumn this[int columnIndex] => columns[columnIndex];

        public TableColumn this[string columnName]
        {
            get
            {
                columnName = columnName.CheckNotNullTrimmed(nameof(columnName));
                var c = columnNameCache[columnName];
                if (c == null) throw new ArgumentException("Column '" + columnName + "' not found. Valid columns are: " + string.Join(", ", ColumnNames), nameof(columnName));
                return c;
            }
        }

        public bool TryGetColumn(string columnName, out TableColumn column)
        {
            columnName = columnName.TrimOrNull();
            if (columnName == null)
            {
                column = null;
                return false;
            }

            var c = columnNameCache[columnName];
            if (c == null)
            {
                column = null;
                return false;
            }

            column = c;
            return true;
        }

        public bool TryGetColumn(int columnIndex, out TableColumn column)
        {
            if (columnIndex < 0)
            {
                column = null;
                return false;
            }

            if (columnIndex > Count)
            {
                column = null;
                return false;
            }

            var c = columns[columnIndex];
            if (c == null)
            {
                column = null;
                return false;
            }

            column = c;
            return true;
        }

        public bool ContainsColumn(string columnName) => TryGetColumn(columnName, out var column);

        public bool ContainsColumn(int columnIndex) => TryGetColumn(columnIndex, out var column);

        public IEnumerator<TableColumn> GetEnumerator() => columns.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class TableColumn
    {
        private readonly Lazy<Type> type;
        private readonly Lazy<int> maxLength;
        private readonly Lazy<bool> isNullable;
        private readonly Lazy<int> numberOfDecimalPlaces;
        public Table Table { get; }

        public int Index { get; }
        public string Name { get; }
        public int MaxLength => maxLength.Value;
        public bool IsNullable => isNullable.Value;
        public Type Type => type.Value;
        public int NumberOfDecimalPlaces => numberOfDecimalPlaces.Value;

        internal TableColumn(Table table, int index, string name)
        {
            Table = table;
            Index = index;
            Name = name ?? ("Column" + (index + 1));
            maxLength = new Lazy<int>(() => table.Max(row => row[Index] == null ? 0 : row[Index].Length));
            isNullable = new Lazy<bool>(() => table.Any(row => row[Index] == null));
            type = new Lazy<Type>(() => Util.GuessType(table.Select(o => o[Index])));
            numberOfDecimalPlaces = new Lazy<int>(() => GetNumberOfDecimalPlaces(table, Index));
        }

        private int GetNumberOfDecimalPlaces(Table table, int index)
        {
            var type = table.Columns[index].Type;
            if (type.NotIn(typeof(decimal), typeof(float), typeof(double))) return 0;
            int decimalPlaces = 0;
            foreach (var row in table)
            {
                var val = row[index];
                if (val == null) continue;
                var split = val.Split('.').TrimOrNull().WhereNotNull().ToList();
                if (split.Count < 2) continue; // no decimal place
                if (split.Count > 2) continue; // should not happen
                decimalPlaces = Math.Max(decimalPlaces, split[1].Length);
            }
            return decimalPlaces;
        }

        public override string ToString() => Name;
    }

    public sealed class TableRow : IReadOnlyList<string>, IBucketReadOnly<string, string>, IBucketReadOnly<TableColumn, string>
    {
        private readonly string[] data;
        public Table Table { get; }
        public int RowIndex { get; }

        internal TableRow(Table table, string[] data, int rowIndex)
        {
            Table = table;
            this.data = data;
            RowIndex = rowIndex;
        }

        #region IBucketReadOnly<string, string>

        public IEnumerable<string> Keys => Table.Columns.ColumnNames;
        IEnumerable<string> IBucketReadOnly<string, string>.Keys => Keys;

        public string this[string key] => this[Table.Columns[key].Index];

        #endregion IBucketReadOnly<string, string>

        #region IBucketReadOnly<TableColumn, string>

        IEnumerable<TableColumn> IBucketReadOnly<TableColumn, string>.Keys => Table.Columns;
        public string this[TableColumn key] => this[key.Index];

        #endregion IBucketReadOnly<TableColumn, string>

        #region IReadOnlyList<string>

        public int Count => Table.Columns.Count;
        public string this[int index] => data[index];

        public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)data).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IReadOnlyList<string>

        public string[] ToArray() => data.Copy();
    }

    public static class TableExtensions
    {
        public static Table[] ExecuteQuery(this IDbCommand command)
        {
            Table[] ts = new Table[0];

            using (var reader = command.ExecuteReader())
            {
                ts = Table.Create(reader);
            }

            return ts;
        }

        private static string Replacements(string str, string delimiter, string replacement)
        {
            if (str == null) return null;
            str = str.Replace(Constant.NEWLINE_WINDOWS, " ");
            str = str.Replace(Constant.NEWLINE_MAC, " ");
            str = str.Replace(Constant.NEWLINE_UNIX, " ");
            if (replacement != null) str = str.Replace(delimiter, replacement);
            return str;
        }
        public static void ToDelimited(
            this Table table,
             Action<string> writer,
             string headerDelimiter = "\t",
             string headerQuoting = null,
             string dataDelimiter = "\t",
             string dataQuoting = null,
             string newLine = Constant.NEWLINE_WINDOWS,
             bool includeHeader = true,
             bool includeRows = true,
             string headerDelimiterReplacement = null,
             string dataDelimiterReplacement = null
             )
        {
            headerDelimiter = headerDelimiter ?? string.Empty;
            dataDelimiter = dataDelimiter ?? string.Empty;
            headerQuoting = headerQuoting ?? string.Empty;
            dataQuoting = dataQuoting ?? string.Empty;
            newLine = newLine ?? string.Empty;

            if (includeHeader)
            {
                var itemsHeader = table.Columns.ColumnNames.Select(o => headerQuoting + (Replacements(o, headerDelimiter, headerDelimiterReplacement) ?? string.Empty) + headerQuoting);
                writer(string.Join(headerDelimiter, itemsHeader + newLine));
            }
            if (includeRows)
            {
                foreach (var row in table)
                {
                    var itemsData = row.Select(o => dataQuoting + (Replacements(o, dataDelimiter, dataDelimiterReplacement) ?? string.Empty) + dataQuoting);
                    writer(string.Join(dataDelimiter, itemsData + newLine));
                }
            }
        }
    }
}
