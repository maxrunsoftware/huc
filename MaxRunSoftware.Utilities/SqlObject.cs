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

public abstract class SqlObject
{

}

public class SqlObjectDatabase : SqlObject, IEquatable<SqlObjectDatabase>
{
    public string DatabaseName { get; }

    public SqlObjectDatabase(string databaseName)
    {
        DatabaseName = databaseName;
    }

    public override bool Equals(object obj) => Equals(obj as SqlObjectDatabase);
    public bool Equals(SqlObjectDatabase other)
    {
        if (other == null) return false;
        if (!string.Equals(DatabaseName, other.DatabaseName, StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }
    public override int GetHashCode() => Util.GenerateHashCode(DatabaseName?.ToUpper());
}

public class SqlObjectSchema : SqlObject, IEquatable<SqlObjectSchema>
{
    public string DatabaseName { get; }
    public string SchemaName { get; }

    public SqlObjectSchema(string databaseName, string schemaName)
    {
        DatabaseName = databaseName;
        SchemaName = schemaName;
    }

    public override bool Equals(object obj) => Equals(obj as SqlObjectSchema);
    public bool Equals(SqlObjectSchema other)
    {
        if (other == null) return false;
        if (!string.Equals(DatabaseName, other.DatabaseName, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(SchemaName, other.SchemaName, StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }
    public override int GetHashCode() => Util.GenerateHashCode(DatabaseName?.ToUpper(), SchemaName?.ToUpper());
}

public class SqlObjectTable : SqlObject, IEquatable<SqlObjectTable>
{
    public string DatabaseName { get; }
    public string SchemaName { get; }
    public string TableName { get; }

    public SqlObjectTable(string databaseName, string schemaName, string tableName)
    {
        DatabaseName = databaseName;
        SchemaName = schemaName;
        TableName = tableName;
    }

    public override bool Equals(object obj) => Equals(obj as SqlObjectTable);
    public bool Equals(SqlObjectTable other)
    {
        if (other == null) return false;
        if (!string.Equals(DatabaseName, other.DatabaseName, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(SchemaName, other.SchemaName, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(TableName, other.TableName, StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }
    public override int GetHashCode() => Util.GenerateHashCode(DatabaseName?.ToUpper(), SchemaName?.ToUpper(), TableName?.ToUpper());

}

public class SqlObjectTableColumn : SqlObject, IEquatable<SqlObjectTableColumn>
{
    public string DatabaseName { get; }
    public string SchemaName { get; }
    public string TableName { get; }
    public string ColumnName { get; }

    public string ColumnType { get; }
    public DbType ColumnDbType { get; }
    public bool IsNullable { get; }
    public int Ordinal { get; }
    public long? CharacterLengthMax { get; }
    public int? NumericPrecision { get; }
    public int? NumericScale { get; }
    public string ColumnDefault { get; }

    public SqlObjectTableColumn(
        string databaseName,
        string schemaName,
        string tableName,
        string columnName,

        string columnType,
        DbType columnDbType,
        bool isNullable,
        int ordinal,
        long? characterLengthMax,
        int? numericPrecision,
        int? numericScale,
        string columnDefault
        )
    {
        DatabaseName = databaseName;
        SchemaName = schemaName;
        TableName = tableName;
        ColumnName = columnName;

        ColumnType = columnType;
        ColumnDbType = columnDbType;
        IsNullable = isNullable;
        Ordinal = ordinal;
        CharacterLengthMax = characterLengthMax;
        NumericPrecision = numericPrecision;
        NumericScale = numericScale;
        ColumnDefault = columnDefault;
    }

    public override bool Equals(object obj) => Equals(obj as SqlObjectTableColumn);
    public bool Equals(SqlObjectTableColumn other)
    {
        if (other == null) return false;
        if (!string.Equals(DatabaseName, other.DatabaseName, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(SchemaName, other.SchemaName, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(TableName, other.TableName, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(ColumnName, other.ColumnName, StringComparison.OrdinalIgnoreCase)) return false;
        if (Ordinal != other.Ordinal) return false;
        return true;
    }
    public override int GetHashCode() => Util.GenerateHashCode(DatabaseName?.ToUpper(), SchemaName?.ToUpper(), TableName?.ToUpper(), ColumnName?.ToUpper(), Ordinal);

}
