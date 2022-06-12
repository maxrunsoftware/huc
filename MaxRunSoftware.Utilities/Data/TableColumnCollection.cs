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
        var tableColumns = columns.ToList();
        ColumnNames = tableColumns.Select(o => o.Name).ToList().AsReadOnly();
        columnsSet = new HashSet<TableColumn>(this.columns);
        // Use a fast cache for column name lookups by any case formatting
        columnNameCache = new BucketCacheThreadSafeCopyOnWrite<string, TableColumn>(columnName =>
        {
            foreach (var sc in Constant.LIST_StringComparison)
            {
                foreach (var item in tableColumns)
                {
                    if (string.Equals(item.Name, columnName, sc))
                    {
                        return item;
                    }
                }
            }

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
            if (c == null)
            {
                throw new ArgumentException("Column '" + columnName + "' not found. Valid columns are: " + string.Join(", ", ColumnNames), nameof(columnName));
            }

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

    public bool ContainsColumn(string columnName) => TryGetColumn(columnName, out _);

    public bool ContainsColumn(int columnIndex) => TryGetColumn(columnIndex, out _);

    public bool ContainsColumn(TableColumn column) => columnsSet.Contains(column);

    public IEnumerator<TableColumn> GetEnumerator() => columns.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => this.ToStringDelimited(", ");
}
