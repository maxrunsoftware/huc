﻿/*
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
using System.Text;

namespace MaxRunSoftware.Utilities
{
    /// <summary>
    /// Encapsulates a 2-diminsional array of string data into immutable columns and rows. The table will never be jagged, all rows will have all columns.
    /// Fields can be null. Columns must have names or they will be generated. Row limit is limited to int.MaxValue.
    /// </summary>
    [Serializable]
    public sealed class Table : ISerializable, IReadOnlyList<TableRow>
    {
        /// <summary>
        /// A unique ID for this table
        /// </summary>
        public Guid Id { get; init; }
        private readonly IReadOnlyList<TableRow> rows;

        /// <summary>
        /// The columns in this table
        /// </summary>
        public TableColumnCollection Columns { get; }

        private Table(IEnumerable<string[]> data, bool firstRowIsHeader)
        {
            Id = Guid.NewGuid();
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
        /// Gets a specific row, or throws out of bounds exception
        /// </summary>
        /// <param name="rowIndex">The row index</param>
        /// <returns>The row</returns>
        public TableRow this[int rowIndex] => rows[rowIndex];

        public IEnumerator<TableRow> GetEnumerator() => rows.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IReadOnlyList<TableRow>

        #region Create

        public static Table Create<TEnumerable>(IEnumerable<TEnumerable> data, bool firstRowIsHeader) where TEnumerable : class, IEnumerable<string>
        {
            data.CheckNotNull(nameof(data));
            var list = new List<string[]>();
            foreach (var row in data)
            {
                var rowList = new List<string>();
                foreach (var cell in row.OrEmpty())
                {
                    rowList.Add(cell);
                }
                list.Add(rowList.ToArray());
            }
            return new Table(list, firstRowIsHeader);
        }

        public static Table Create<TEnumerableRow, TEnumerableHeader>(IEnumerable<TEnumerableRow> data, TEnumerableHeader header) where TEnumerableRow : class, IEnumerable<string> where TEnumerableHeader : class, IEnumerable<string>
        {
            data.CheckNotNull(nameof(data));
            header.CheckNotNull(nameof(header));

            var list = new List<string[]>();
            var headerList = new List<string>();
            foreach (var item in header) headerList.Add(item);
            list.Add(headerList.ToArray());

            foreach (var row in data)
            {
                var rowList = new List<string>();
                foreach (var cell in row)
                {
                    rowList.Add(cell);
                }
                list.Add(rowList.ToArray());
            }

            return new Table(list, true);
        }

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

        #region RemoveColumns

        public Table RemoveColumns(params TableColumn[] columns)
        {
            var columnIndexesToRemove = new HashSet<int>();
            foreach (var column in columns)
            {
                if (!Columns.ContainsColumn(column)) throw new Exception("Column [" + column.Index + ":" + column.Name + "] does not exist on table");
                columnIndexesToRemove.Add(column.Index);
            }

            // All of the columns exist, let's build up our new table...
            var header = new List<string>();
            foreach (var column in Columns)
            {
                if (!columnIndexesToRemove.Contains(column.Index)) header.Add(column.Name);
            }

            var rows = new List<string[]>();
            var newWidth = Columns.Count - columnIndexesToRemove.Count + 1;
            foreach (var row in this)
            {
                var rowData = new List<string>(newWidth);
                for (int i = 0; i < Columns.Count; i++)
                {
                    if (!columnIndexesToRemove.Contains(i)) rowData.Add(row[i]);
                }
                rows.Add(rowData.ToArray());
            }

            return Create(rows, header);
        }

        public Table RemoveColumns(params string[] columnNames)
        {
            var list = new List<TableColumn>();
            foreach (var columnName in columnNames)
            {
                var column = Columns[columnName];
                list.Add(column);
            }
            return RemoveColumns(list.ToArray());
        }

        public Table RemoveColumns(params int[] columnIndexes)
        {
            var list = new List<TableColumn>();
            foreach (var columnIndex in columnIndexes)
            {
                var column = Columns[columnIndex];
                list.Add(column);
            }
            return RemoveColumns(list.ToArray());
        }

        #endregion RemoveColumns

        #region RenameColumn

        public Table RenameColumn(TableColumn column, string newColumnName)
        {
            if (!Columns.ContainsColumn(column.CheckNotNull(nameof(column)))) throw new Exception($"No column [{column.Index}:{column.Name}] is attached to this table {Id}");
            var columnIndex = column.Index;

            newColumnName = newColumnName.CheckNotNullTrimmed(nameof(newColumnName));

            var len = Columns.Count;
            var h = new List<string>(len);
            for (var i = 0; i < len; i++)
            {
                h.Add(i == columnIndex ? newColumnName : Columns.ColumnNames[i]);
            }
            return Create(this, h);
        }
        public Table RenameColumn(string columnName, string newColumnName) => RenameColumn(Columns[columnName.CheckNotNullTrimmed(nameof(columnName))], newColumnName);

        public Table RenameColumn(int columnIndex, string newColumnName) => RenameColumn(Columns[columnIndex], newColumnName);

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

        /// <summary>
        /// Gets a sub-set of the current table rows by a predicate
        /// </summary>
        /// <param name="predicate">Whether to include the row or not</param>
        /// <returns>A new table with a subset of the rows</returns>
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
        private readonly HashSet<TableColumn> columnsSet;
        public IReadOnlyList<string> ColumnNames { get; }

        public int Count => columns.Count;

        IEnumerable<string> IBucketReadOnly<string, TableColumn>.Keys => ColumnNames;

        internal TableColumnCollection(IEnumerable<TableColumn> columns)
        {
            this.columns = columns.CheckNotNull(nameof(columns)).WhereNotNull().ToList().AsReadOnly();
            ColumnNames = columns.Select(o => o.Name).ToList().AsReadOnly();
            columnsSet = new HashSet<TableColumn>(this.columns);
            // Use a fast cache for column name lookups by any case formatting
            columnNameCache = new BucketCacheThreadSafeCopyOnWrite<string, TableColumn>(columnName =>
            {
                foreach (var sc in Constant.LIST_StringComparison)
                    foreach (var item in columns)
                        if (string.Equals(item.Name, columnName, sc))
                            return item;
                return null;
            });
        }

        public TableColumn this[int columnIndex] => columns[columnIndex];

        /// <summary>
        /// Attempts to get a column with the specified name, or throws an ArgumentException if column was not found
        /// </summary>
        /// <param name="columnName">The name of the column to get</param>
        /// <returns>The found column or throws an exception</returns>
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

        public bool ContainsColumn(TableColumn column) => columnsSet.Contains(column);

        public IEnumerator<TableColumn> GetEnumerator() => columns.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            return string.Join(", ", this);
        }
    }

    public sealed class TableColumn : IEquatable<TableColumn>
    {
        private readonly Lazy<Type> type;
        private readonly Lazy<int> maxLength;
        private readonly Lazy<bool> isNullable;
        private readonly Lazy<int> numberOfDecimalDigits;

        /// <summary>
        /// The Table this column is attached to
        /// </summary>
        public Table Table { get; }

        /// <summary>
        /// The zero-based index of this column
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The name of this column
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// (lazy) The maximum string length for rows in this column
        /// </summary>
        public int MaxLength => maxLength.Value;

        /// <summary>
        /// (lazy) If there is any null values in this column
        /// </summary>
        public bool IsNullable => isNullable.Value;

        /// <summary>
        /// (lazy) Attempts to determine the type of this column by testing all values against various types
        /// </summary>
        public Type Type => type.Value;

        /// <summary>
        /// (lazy) If this column is a decimal, float, or double and has a single decimal place then this is the max number of decimal digits
        /// </summary>
        public int NumberOfDecimalDigits => numberOfDecimalDigits.Value;

        internal TableColumn(Table table, int index, string name)
        {
            Table = table;
            Index = index;
            Name = name ?? ("Column" + (index + 1));
            maxLength = new Lazy<int>(() => table.Max(row => row[Index] == null ? 0 : row[Index].Length));
            isNullable = new Lazy<bool>(() => table.Any(row => row[Index] == null));
            type = new Lazy<Type>(() => Util.GuessType(table.Select(o => o[Index])));
            numberOfDecimalDigits = new Lazy<int>(() => GetNumberOfDecimalPlaces(table, Index));
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

        public override bool Equals(object obj) => Equals(obj as TableColumn);

        public bool Equals(TableColumn other)
        {
            if (other == null) return false;
            if (!other.Table.Id.Equals(Table.Id)) return false;
            if (!other.Index.Equals(Index)) return false;
            if (!string.Equals(other.Name, Name, StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        public override int GetHashCode() => Util.GenerateHashCode(Table.Id, Index, Name.ToUpper());
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

        public int GetNumberOfCharacters(int lengthOfNull) => data.GetNumberOfCharacters(lengthOfNull);

        #region IBucketReadOnly<string, string>

        public IEnumerable<string> Keys => Table.Columns.ColumnNames;
        IEnumerable<string> IBucketReadOnly<string, string>.Keys => Keys;

        public string this[string columnName] => this[Table.Columns[columnName].Index];

        #endregion IBucketReadOnly<string, string>

        #region IBucketReadOnly<TableColumn, string>

        IEnumerable<TableColumn> IBucketReadOnly<TableColumn, string>.Keys => Table.Columns;
        public string this[TableColumn column] => this[column.Index];

        #endregion IBucketReadOnly<TableColumn, string>

        #region IReadOnlyList<string>

        public int Count => Table.Columns.Count;
        public string this[int columnIndex] => data[columnIndex];

        public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)data).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IReadOnlyList<string>

        public string[] ToArray() => data.Copy();
    }

    public static class TableExtensions
    {
        public static string ToXml(this Table table, bool format = true)
        {
            using (var w = new XmlWriter(formatted: format))
            {
                using (w.Element("table"))
                {
                    foreach (var c in table.Columns)
                    {
                        using (w.Element("column", ("index", c.Index)))
                        {
                            w.Value(c.Name);
                        }
                    }

                    foreach (var r in table)
                    {
                        using (w.Element("row", ("index", r.RowIndex)))
                        {
                            for (int j = 0; j < r.Count; j++)
                            {
                                using (w.Element("cell", ("index", j)))
                                {
                                    w.Value(r[j] ?? string.Empty);
                                }
                            }
                        }
                    }

                }

                return w.ToString();
            }


        }

        public static string ToJson(this Table table, bool format = true)
        {
            using (var w = new JsonWriter(formatted: format))
            {
                using (w.Object())
                {
                    using (w.Array("columns"))
                    {
                        foreach (var c in table.Columns) w.Value(c.Name);
                    }
                    using (w.Array("rows"))
                    {
                        foreach (var r in table)
                        {
                            using (w.Array())
                            {
                                foreach (var cell in r) w.Value(cell ?? string.Empty);
                            }
                        }
                    }
                }
                return w.ToString();
            }

        }

        /// <summary>
        /// Generates a new table with the specified column list
        /// </summary>
        /// <param name="table">The source table</param>
        /// <param name="columnNames">The existing columns to keep</param>
        /// <returns>A new table with the specified columns if they exist</returns>
        public static Table SetColumnsListTo(this Table table, params string[] columnNames)
        {
            var columnsToKeep = new HashSet<TableColumn>();
            foreach (var columnName in columnNames)
            {
                columnsToKeep.Add(table.Columns[columnName]);
            }
            var columnsToRemove = new HashSet<TableColumn>();
            foreach (var column in table.Columns)
            {
                if (!columnsToKeep.Contains(column)) columnsToRemove.Add(column);
            }
            return table.RemoveColumns(columnsToRemove.ToArray());
        }

        /// <summary>
        /// Returns a chunk of rows. The chunk is based on the number of characters in the rows.
        /// </summary>
        /// <param name="table">Table</param>
        /// <param name="maxNumberOfCharacters">The number of characters to chunk by</param>
        /// <param name="lengthOfNull">How many characters is a NULL value worth</param>
        /// <returns>Iterator of row chunks</returns>
        public static IEnumerable<TableRow[]> GetRowsChunkedByNumberOfCharacters(this Table table, int maxNumberOfCharacters, int lengthOfNull)
        {
            var list = new List<TableRow>();
            int currentSize = 0;
            foreach (var row in table)
            {
                if (list.Count > 0 && currentSize + row.GetNumberOfCharacters(lengthOfNull) >= maxNumberOfCharacters)
                {
                    // adding the current row will result in us being too big so return what we have so far
                    yield return list.ToArray();
                    list = new();
                    currentSize = 0;
                }
                list.Add(row);
                currentSize += row.GetNumberOfCharacters(lengthOfNull);

            }
            if (list.Count > 0) yield return list.ToArray();
        }

        /// <summary>
        /// Returns a chunk of rows. The chunk is based on the number of rows specified.
        /// </summary>
        /// <param name="table">Table</param>
        /// <param name="numberOfRows">The number of rows in each chunk</param>
        /// <returns>Iterator of row chunks</returns>
        public static IEnumerable<TableRow[]> GetRowsChunkedByNumber(this Table table, int numberOfRows)
        {
            var list = new List<TableRow>();
            foreach (var row in table)
            {

                var currentSize = list.Count;
                if (list.Count == 0) list.Add(row);
                else if (1 + currentSize < numberOfRows) list.Add(row);
                else
                {
                    yield return list.ToArray();
                    list = new();
                    list.Add(row);
                }
            }
            if (list.Count > 0) yield return list.ToArray();
        }

        /// <summary>
        /// The number of cells in this table including the header row
        /// </summary>
        /// <param name="table">Table</param>
        /// <returns>The number of cells</returns>
        public static int GetNumberOfCells(this Table table)
        {
            return (table.Count * table.Columns.Count) + table.Columns.Count;
        }

        /// <summary>
        /// The number of characters in this table including the header row
        /// </summary>
        /// <param name="table">Table</param>
        /// <param name="lengthOfNull">How many characters is a NULL value</param>
        /// <returns>The total number of characters in this table</returns>
        public static int GetNumberOfCharacters(this Table table, int lengthOfNull)
        {
            int size = 0;
            foreach (var column in table.Columns)
            {
                size += column.Name.Length;
            }
            foreach (var row in table)
            {
                size += row.GetNumberOfCharacters(lengthOfNull);
            }
            return size;
        }

        private static string ToDelimitedReplacements(string str, string delimiter, string replacement)
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
                var sb = new StringBuilder();
                bool first = true;
                foreach (var col in table.Columns.ColumnNames)
                {
                    if (first) first = false;
                    else sb.Append(headerDelimiter);
                    sb.Append(headerQuoting);
                    var colText = ToDelimitedReplacements(col, headerDelimiter, headerDelimiterReplacement);
                    sb.Append(colText);
                    sb.Append(headerQuoting);
                }
                writer(sb.ToString() + newLine);
            }
            if (includeRows)
            {
                foreach (var row in table)
                {
                    var sb = new StringBuilder();
                    var first = true;
                    foreach (var cell in row)
                    {
                        if (first) first = false;
                        else sb.Append(dataDelimiter);
                        sb.Append(dataQuoting);
                        var cellText = ToDelimitedReplacements(cell, dataDelimiter, dataDelimiterReplacement);
                        sb.Append(cellText);
                        sb.Append(dataQuoting);
                    }
                    writer(sb.ToString() + newLine);
                }
            }
        }
    }
}