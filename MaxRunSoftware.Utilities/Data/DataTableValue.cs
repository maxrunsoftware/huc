// /*
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
// */

namespace MaxRunSoftware.Utilities;

public class DataTableValue
{
    public DataTableValue(string columnName, int columnIndex, int rowIndex, object value)
    {
        ColumnName = columnName;
        ColumnIndex = columnIndex;
        RowIndex = rowIndex;
        if (value == DBNull.Value) Value = null;
        else Value = value;
    }

    public string ColumnName { get; }
    public int ColumnIndex { get; }
    public int RowIndex { get; }
    public object Value { get; }

    public override string ToString()
    {
        string valTypeString = "";
        if (Value != null)
        {
            if (Value is Type) valTypeString = nameof(Type);
            else valTypeString = Value.GetType().NameFormatted();
        }
        return $"[{RowIndex},{ColumnIndex}] {ColumnName}[{valTypeString}]: {Value.ToStringGuessFormat()}";
    }
    public static IEnumerable<DataTableValue> Parse(DataTable table)
    {
        var columnCount = table.Columns.Count;
        var columnNames = table.Columns.AsList().Select(o => o.ColumnName ?? ("Column" + o.Ordinal)).ToArray();
        var rowCount = table.Rows.Count;

        var rowCurrentIndex = 0;
        foreach (DataRow row in table.Rows)
        {
            for (int columnCurrentIndex = 0; columnCurrentIndex < columnCount; columnCurrentIndex++)
            {
                yield return new(columnNames[columnCurrentIndex], columnCurrentIndex, rowCurrentIndex, row[columnCurrentIndex]);
            }

            rowCurrentIndex++;

        }

    }

}

public static class DataTableValueExtensions
{
    public static IEnumerable<DataTableValue> GetDataTableValues(this DataTable table) => DataTableValue.Parse(table);
}

