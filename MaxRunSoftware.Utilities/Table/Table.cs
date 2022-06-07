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

using System.Runtime.Serialization;

namespace MaxRunSoftware.Utilities;

public delegate string TableCellModificationHandler(Table table, TableColumn column, TableRow row, int rowIndex, string value);

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

    public Table Modify(TableCellModificationHandler handler)
    {
        var width = Columns.Count;

        var cols = Columns.ToArray();
        var newTable = new List<string[]>();
        var newColumns = new string[width];
        for (int i = 0; i < width; i++) newColumns[i] = Columns[i].Name;
        newTable.Add(newColumns);

        int rowIndex = 0;
        foreach (var row in this)
        {
            var newRow = new string[width];
            for (int i = 0; i < width; i++)
            {
                var val = row[i];
                val = handler(this, cols[i], row, rowIndex, val);
                newRow[i] = val;
            }
            newTable.Add(newRow);
            rowIndex++;
        }

        return Create(newTable, true);
    }

    public Table Transpose()
    {
        var rowsOldCount = Count;
        var colsOldCount = Columns.Count;
        var rowsNewCount = Columns.Count;
        var colsNewCount = Count + 1;

        var list = new List<string[]>(rowsNewCount + 1);
        for (int i = 0; i < rowsNewCount; i++) list.Add(new string[colsNewCount]);

        for (int i = 0; i < colsOldCount; i++)
        {
            list[i][0] = Columns.ColumnNames[i];
        }

        for (int newRowIndex = 0; newRowIndex < rowsNewCount; newRowIndex++)
        {
            for (int newColIndex = 1; newColIndex < colsNewCount; newColIndex++)
            {
                list[newRowIndex][newColIndex] = this[newColIndex - 1][newRowIndex];
            }
        }

        return Create(list, true);
    }

    public override string ToString() => "Table[columns:" + Columns.Count + "][rows:" + Count + "]";

}
