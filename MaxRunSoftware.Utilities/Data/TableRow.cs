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

    public int GetNumberOfCharacters(int lengthOfNull)
    {
        return data.GetNumberOfCharacters(lengthOfNull);
    }

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

    public IEnumerator<string> GetEnumerator()
    {
        return ((IEnumerable<string>)data).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion IReadOnlyList<string>

    public string[] ToArray()
    {
        return data.Copy();
    }
}
