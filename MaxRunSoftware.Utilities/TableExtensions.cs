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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaxRunSoftware.Utilities
{
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
