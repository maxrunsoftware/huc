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
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class Sql : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Execute a SQL statement and/or script and optionally save the result(s) to a tab delimited file(s)");
            help.AddParameter("connectionString", "c", "SQL connection string");
            help.AddParameter("sqlStatement", "s", "SQL statement to execute");
            help.AddParameter("sqlScriptFile", "f", "SQL script file to execute");
            help.AddParameter("commandTimeout", "t", "Length of time in seconds to wait for SQL command to execute before erroring out (" + (60 * 60 * 24) + ")");
            help.AddDetail("Example connection strings:");
            help.AddDetail("  Server=192.168.0.5;Database=myDatabase;User Id=myUsername;Password=myPassword;");
            help.AddDetail("  Server=192.168.0.5\\instanceName;Database=myDataBase;User Id=myUsername;Password=myPassword;");
            help.AddDetail("  Server=192.168.0.5;Database=myDataBase;Trusted_Connection=True;");
            help.AddValue("<result file 1> <result file 2> <etc>");
        }

        protected override void Execute()
        {
            var cs = GetArgParameterOrConfigRequired("connectionString", "c");

            var s = GetArgParameterOrConfig("sqlStatement", "s");

            var f = GetArgParameterOrConfig("sqlScriptFile", "f").TrimOrNull();

            string fData = null;
            if (f != null) fData = ReadFile(f);
            if (fData.TrimOrNull() != null) log.Debug($"sqlScriptFileData: {fData.Length}");

            if (s.TrimOrNull() == null && fData.TrimOrNull() == null) throw new Exception($"No SQL provided to execute");
            var sql = (s ?? string.Empty) + Constant.NEWLINE_WINDOWS + (fData ?? string.Empty);
            sql = sql.TrimOrNull();
            if (sql == null) throw new ArgsException("sqlStatement", "No SQL provided to execute");
            log.Debug($"sql: {sql}");

            var t = GetArgParameterOrConfigInt("commandTimeout", "t", 60 * 60 * 24);

            var resultFiles = GetArgValues().OrEmpty().TrimOrNull().ToList();
            for (var i = 0; i < resultFiles.Count; i++)
            {
                if (resultFiles[i] == null) continue;
                resultFiles[i] = Path.GetFullPath(resultFiles[i]);
                DeleteExistingFile(resultFiles[i]);
            }
            for (var i = 0; i < resultFiles.Count; i++) log.Debug($"resultFiles[{i}]: {resultFiles[i]}");


            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Utilities.Table[] tables = Array.Empty<Utilities.Table>();
            using (var conn = new SqlConnection(cs))
            {
                conn.Open();
                conn.InfoMessage += delegate (object sender, SqlInfoMessageEventArgs e)
                {
                    var msg = e?.Message.TrimOrNull();
                    if (msg != null) log.Info(msg);
                };

                using (var cmd = conn.CreateCommand(sql, commandType: System.Data.CommandType.Text, commandTimeout: t))
                {
                    if (resultFiles.WhereNotNull().IsEmpty())
                    {
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        tables = cmd.ExecuteQuery();
                    }
                }
            }

            stopwatch.Stop();
            var stopwatchtime = stopwatch.Elapsed.TotalSeconds.ToString(MidpointRounding.AwayFromZero, 3);
            log.Info($"Completed SQL execution in {stopwatchtime} seconds");

            log.Debug($"Successfully retrieved {tables.Length} results from SQL");
            for (var i = 0; i < tables.Length; i++)
            {
                var table = tables[i];
                var rf = resultFiles.ElementAtOrDefault(i);

                if (rf != null)
                {
                    WriteTabDelimitedFile(rf, table);
                }
            }

            log.Debug("SQL completed");
        }

        protected void WriteTabDelimitedFile(string fileName, Utilities.Table table, string suffix = null)
        {
            fileName = Path.GetFullPath(fileName);
            table.CheckNotNull(nameof(table));

            log.Debug("Writing TAB delimited Table to file " + fileName);
            using (var stream = Util.FileOpenWrite(fileName))
            using (var streamWriter = new StreamWriter(stream, Utilities.Constant.ENCODING_UTF8_WITHOUT_BOM))
            {
                table.ToDelimited(
                    o => streamWriter.Write(o),
                    headerDelimiter: "\t",
                    headerQuoting: null,
                    includeHeader: true,
                    dataDelimiter: "\t",
                    dataQuoting: null,
                    includeRows: true,
                    newLine: Utilities.Constant.NEWLINE_WINDOWS
                    );
                streamWriter.Flush();
                stream.Flush(true);
            }

            log.Info("Successfully wrote " + table.ToString() + " to file " + fileName + (suffix ?? string.Empty));
        }

    }
}
