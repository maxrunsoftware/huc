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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

// ReSharper disable StringLiteralTypo

namespace MaxRunSoftware.Utilities.Console.Commands;

public class SqlLoad : SqlBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Loads a tab delimited data file into a SQL server table");
        help.AddDetail("It is expected that the table being imported is in Tab delimited format, and that there is a header row. If there is no header row set the -noHeader option");
        help.AddParameter(nameof(drop), "dp", "Drop existing table if it exists (false)");
        help.AddParameter(nameof(detectColumnTypes), "dct", "Attempts to detect column types and lengths (false)");
        help.AddParameter(nameof(coerceValues), "cv", "Attempts to convert values to fit into the columns (false)");
        help.AddParameter(nameof(errorOnNonexistentColumns), "ec", "When inserting to an existing table, whether to error if columns in the data file don't have cooresponding columns in the existing SQL table (false)");
        help.AddParameter(nameof(batchSize), "bs", "Number of insert commands to batch. Set to zero if having issues (1000)");
        help.AddParameter(nameof(rowNumberColumnName), "rncn", "If provided, an extra column is inserted with the line number, -1 because excluding header");
        help.AddParameter(nameof(currentUtcDateTimeColumnName), "ctcn", "If provided, an extra column containing the UTC datetime is inserted");
        help.AddParameter(nameof(noHeader), "nh", "Set if your importing tab delimited file does not have a header row (false)");
        help.AddParameter(nameof(database), "d", "Database name to load table (current database)");
        help.AddParameter(nameof(schema), "s", "Schema to load table");
        help.AddParameter(nameof(table), "t", "Table name (file name)");
        help.AddValue("<tab delimited file to load>");
        help.AddExample(HelpExamplePrefix + " Orders.txt");
        help.AddExample(HelpExamplePrefix + " -d=NorthWind -s=dbo -t=TempOrders Orders.txt");
        help.AddExample(HelpExamplePrefix + " -drop -rowNumberColumnName=RowNumber -currentUtcDateTimeColumnName=UploadTime -d=NorthWind -s=dbo -t=TempOrders Orders.txt");
    }

    private bool drop;
    private bool detectColumnTypes;
    private bool coerceValues;
    private bool errorOnNonexistentColumns;
    private int batchSize;
    private string rowNumberColumnName;
    private string currentUtcDateTimeColumnName;
    private bool noHeader;
    private string database;
    private string schema;
    private string table;

    protected override void ExecuteInternal()
    {
        base.ExecuteInternal();

        drop = GetArgParameterOrConfigBool(nameof(drop), "dp", false);
        detectColumnTypes = GetArgParameterOrConfigBool(nameof(detectColumnTypes), "dct", false);
        coerceValues = GetArgParameterOrConfigBool(nameof(coerceValues), "cv", false);
        errorOnNonexistentColumns = GetArgParameterOrConfigBool(nameof(errorOnNonexistentColumns), "ec", false);
        batchSize = GetArgParameterOrConfigInt(nameof(batchSize), "bs", 1000);
        rowNumberColumnName = GetArgParameterOrConfig(nameof(rowNumberColumnName), "rncn").TrimOrNull();
        currentUtcDateTimeColumnName = GetArgParameterOrConfig(nameof(currentUtcDateTimeColumnName), "ctcn").TrimOrNull();
        noHeader = GetArgParameterOrConfigBool(nameof(noHeader), "nh", false);

        var inputFile = GetArgValueTrimmed(0);
        inputFile.CheckValueNotNull(nameof(inputFile), log);
        inputFile = ParseInputFiles(inputFile.Yield()).FirstOrDefault();
        CheckFileExists(inputFile);

        var t = ReadTableTab(inputFile, headerRow: !noHeader);
        if (t.Columns.IsEmpty())
        {
            throw new Exception("No columns in import file");
        }

        var c = GetSqlHelper();
        log.Debug("Created SQL Helper of type " + c.GetType().NameFormatted());

        database = GetArgParameterOrConfig(nameof(database), "d").TrimOrNull() ?? c.GetCurrentDatabaseName();

        schema = GetArgParameterOrConfig(nameof(schema), "s").TrimOrNull() ?? c.GetCurrentSchemaName();

        table = (GetArgParameterOrConfig(nameof(table), "t").TrimOrNull() ?? Path.GetFileNameWithoutExtension(inputFile).TrimOrNull()) ?? GetArgParameterOrConfigRequired(nameof(table), "t");

        var databaseSchemaTable = c.Escape(database, schema, table);
        log.DebugParameter(nameof(databaseSchemaTable), databaseSchemaTable);

        c.InsertCoerceValues = coerceValues;

        // Drop table
        var tableExists = c.GetTableExists(database, schema, table);
        if (tableExists && drop)
        {
            log.Info($"Dropping existing table: {databaseSchemaTable}");
            c.DropTable(database, schema, table);
        }

        // Create table
        tableExists = c.GetTableExists(database, schema, table);
        if (!tableExists)
        {
            var columnList = new List<string>();
            if (rowNumberColumnName != null)
            {
                columnList.Add(c.Escape(rowNumberColumnName) + " INTEGER NULL");
            }

            if (currentUtcDateTimeColumnName != null)
            {
                columnList.Add(c.Escape(currentUtcDateTimeColumnName) + " DATETIME NULL");
            }

            foreach (var col in t.Columns)
            {
                columnList.Add(detectColumnTypes ? c.TextCreateTableColumn(col) : c.TextCreateTableColumnText(col.Name, true));
            }

            log.Debug("Executing Create Table...");

            var sql = $"CREATE TABLE {databaseSchemaTable} (" + columnList.ToStringDelimited(",") + ");";
            log.Trace(sql);

            var sqlLog = Environment.NewLine +
                         $"CREATE TABLE {databaseSchemaTable} (" + Environment.NewLine +
                         "\t" + columnList.ToStringDelimited(Environment.NewLine + ",\t") +
                         Environment.NewLine + ");";
            log.Debug(sqlLog);

            c.ExecuteNonQuery(sql);

            log.Debug("Created Table " + databaseSchemaTable);
        }


        var sqlColumnsList = c.GetTableColumns(database, schema, table).Select(o => o.ColumnName).ToList();
        log.Debug("Found " + sqlColumnsList.Count + " columns in table " + databaseSchemaTable + "  " + sqlColumnsList.ToStringDelimited(", "));

        // TODO: Oracle is weird. Oracle column names are case-sensitive when escaping column name, which we always do. See https://seeq.atlassian.net/wiki/spaces/KB/pages/443088907/SQL+Column+Names+and+Case+Sensitivity#Oracle 
        var sqlColumns = sqlColumnsList.ToHashSet(serverType.In(SqlServerType.Oracle) ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

        // Add RowNumber and Timestamp columns if specified
        if (rowNumberColumnName != null)
        {
            log.Debug("Adding RowNumber column: " + rowNumberColumnName);
            var i = 1;
            t = t.AddColumn(rowNumberColumnName, _ => (i++).ToString(), 0);
        }

        if (currentUtcDateTimeColumnName != null)
        {
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            log.Debug("Adding UTC Timestemp column: " + currentUtcDateTimeColumnName + "  (" + now + ")");
            t = t.AddColumn(currentUtcDateTimeColumnName, _ => now, 1);
        }

        // Compare file table columns to SQL table columns
        var columnsToInsert = new List<TableColumn>();
        foreach (var tableColumn in t.Columns)
        {
            if (sqlColumns.Contains(tableColumn.Name))
            {
                // Found column in SQL table
                columnsToInsert.Add(tableColumn);
            }
            else if (!errorOnNonexistentColumns)
            {
                // Didn't find column in SQL table, but that is OK we'll just not import it
                log.Info($"Ignoring column {tableColumn.Name} because it does not exist as a column in table {table}");
            }
            else
            {
                // Didn't find column in SQL table, and it is required, so we should fail here
                throw new Exception($"DataFile contains column {tableColumn.Name} but existing SQL table {table} does not contain this column.");
            }
        }

        // Remove columns from file table that do not exist in SQL table
        var columnsToRemove = t.Columns.Select(o => o.Name).Except(columnsToInsert.Select(o => o.Name), StringComparer.OrdinalIgnoreCase);
        t = t.RemoveColumns(columnsToRemove.ToArray());

        log.Info($"Writing {t.Count} rows to database in columns " + columnsToInsert.Select(o => o.Name).ToStringDelimited(", "));

        var stopwatch = Stopwatch.StartNew();
        c.Insert(database, schema, table, t);
        stopwatch.Stop();
        log.Info($"Completed writing {t.Count} rows to database in {stopwatch.Elapsed.ToStringTotalSeconds(3)} seconds");
    }
}
