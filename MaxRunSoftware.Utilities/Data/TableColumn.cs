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

using System.Globalization;

namespace MaxRunSoftware.Utilities;

public sealed class TableColumn : IEquatable<TableColumn>
{
    private readonly Lazy<Type> type;
    private readonly Lazy<DbType> dbType;
    private readonly Lazy<int> maxLength;
    private readonly Lazy<bool> isNullable;
    private readonly Lazy<int> numericPrecision;
    private readonly Lazy<int> numericScale;
    private readonly Lazy<int> hashCode;

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
    /// (lazy) Attempts to determine the DbType of this column by testing all values against various types
    /// </summary>
    public DbType DbType => dbType.Value;

    /// <summary>
    /// (lazy) If this column is a numeric then this is the max number of digits
    /// </summary>
    public int NumericPrecision => numericPrecision.Value;

    /// <summary>
    /// (lazy) If this column is a decimal, float, or double and has a decimal place then this is the max number of decimal
    /// digits
    /// </summary>
    public int NumericScale => numericScale.Value;

    internal TableColumn(Table table, int index, string name)
    {
        Table = table;
        Index = index;
        Name = name ?? "Column" + (index + 1);
        maxLength = new Lazy<int>(() => table.Count == 0 ? 0 : table.Max(row => row[Index] == null ? 0 : row[Index].Length));
        isNullable = new Lazy<bool>(() => table.Count != 0 && table.Any(row => row[Index] == null));
        type = new Lazy<Type>(() => table.Count == 0 ? typeof(string) : Util.GuessType(table.Select(o => o[Index])));
        dbType = new Lazy<DbType>(() => table.Count == 0 ? DbType.String : Util.GuessDbType(table.Select(o => o[Index])));
        numericPrecision = new Lazy<int>(() => table.Count == 0 ? 0 : GetNumericPrecision(table, Index));
        numericScale = new Lazy<int>(() => table.Count == 0 ? 0 : GetNumericScale(table, Index));
        hashCode = new Lazy<int>(() => Util.Hash(Table.Id, Index, Name.ToUpper()));
    }

    private int GetNumericPrecision(Table table, int index)
    {
        var columnType = table.Columns[index].Type;
        columnType = Nullable.GetUnderlyingType(columnType) ?? columnType;
        if (columnType.NotIn(Constant.Types_Numeric)) return 0;

        var result = 0;
        foreach (var row in table)
        {
            var val = row[index].TrimOrNull();
            if (val == null) continue;

            var digits = val.Count(char.IsDigit);

            result = Math.Max(result, digits);
        }

        return result;
    }

    private int GetNumericScale(Table table, int index)
    {
        var decimalChar = (NumberFormatInfo.CurrentInfo.NumberDecimalSeparator.TrimOrNull() ?? ".")[0];

        var columnType = table.Columns[index].Type;
        if (columnType.NotIn(typeof(decimal), typeof(float), typeof(double))) return 0;

        var result = 0;
        foreach (var row in table)
        {
            var val = row[index].TrimOrNull();
            if (val == null) continue;

            var foundDecimal = false;
            var decimalDigits = 0;
            foreach (var c in val)
            {
                if (c == decimalChar) { foundDecimal = true; }
                else if (char.IsDigit(c) && foundDecimal) decimalDigits++;
            }

            result = Math.Max(result, decimalDigits);
        }

        return result;
    }

    public override string ToString() => Name;

    public override bool Equals(object obj) => Equals(obj as TableColumn);

    public bool Equals(TableColumn other)
    {
        if (other == null) return false;

        if (other.GetHashCode() != GetHashCode()) return false;

        if (!other.Table.Id.Equals(Table.Id)) return false;

        if (!other.Index.Equals(Index)) return false;

        if (!string.Equals(other.Name, Name, StringComparison.OrdinalIgnoreCase)) return false;

        return true;
    }

    public override int GetHashCode() => hashCode.Value;
}
