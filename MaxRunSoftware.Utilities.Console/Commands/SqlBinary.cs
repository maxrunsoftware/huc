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
using System.Data;
using System.Diagnostics;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class SqlBinary : SqlQueryBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Execute a SQL statement and/or script querying binary data from a SQL database, which must contain the columns [FileName] and [FileData]");
        // ReSharper disable StringLiteralTypo
        help.AddExample(HelpExamplePrefix + " -s=`SELECT file AS [FileName], columndata AS [FileData] FROM MyDataTable`");
        help.AddExample(HelpExamplePrefix + " -f=`mssqlscript.sql`");
        // ReSharper restore StringLiteralTypo
    }

    private void ProcessResults(IDataReader dataReader)
    {
        var numResultSet = 0;

        do
        {
            numResultSet++;
            var fileNameIndex = dataReader.GetNameIndex("FileName");
            if (fileNameIndex < 0)
            {
                log.Warn("Column [FileName] not specified in result set " + numResultSet);
                continue;
            }

            var fileDataIndex = dataReader.GetNameIndex("FileData");
            if (fileDataIndex < 0) throw new Exception("Column [FileData] not specified in result set " + numResultSet);

            var fieldCount = dataReader.FieldCount;
            var numRow = 0;
            var numRowSuccess = 0;

            while (dataReader.Read())
            {
                numRow++;
                var objs = new object[fieldCount];
                dataReader.GetValues(objs);
                var fileName = objs.GetAtIndexOrDefault(fileNameIndex).ToStringGuessFormat().TrimOrNull();
                var fileData = objs.GetAtIndexOrDefault(fileDataIndex);

                if (fileName == null)
                {
                    log.Warn((numResultSet > 1 ? "[" + numResultSet + "]" : "") + "[" + numRow + "]  " + "Skipping because [FileName] is null");
                    continue;
                }

                if (fileData == null)
                {
                    log.Warn((numResultSet > 1 ? "[" + numResultSet + "]" : "") + "[" + numRow + "]  " + "Skipping because [FileData] is null");
                    continue;
                }

                if (fileData is not byte[] fileDataBinary)
                {
                    log.Warn((numResultSet > 1 ? "[" + numResultSet + "]" : "") + "[" + numRow + "]  " + "Skipping because could not convert [FileData] to byte[]");
                    continue;
                }

                WriteFileBinary(fileName, fileDataBinary);
                numRowSuccess++;
            }

            log.Info("Successfully wrote " + numRowSuccess + " files from result set " + numResultSet);
        } while (dataReader.NextResult());
    }


    protected override void ExecuteInternal()
    {
        base.ExecuteInternal();

        var sql = GetSql();

        if (serverType.NotIn(SqlServerType.MsSql)) throw new Exception("SQL server type [" + serverType + "] is currently unsupported");

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        if (serverType == SqlServerType.MsSql)
            using (var connection = CreateConnectionMsSql())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = commandTimeout;
                    command.CommandType = CommandType.Text;
                    command.CommandText = sql;
                    log.Trace($"ExecuteQuery: {sql}");

                    using (var dataReader = command.ExecuteReader()) { ProcessResults(dataReader); }
                }
            }

        stopwatch.Stop();
        var stopwatchTime = stopwatch.Elapsed.TotalSeconds.ToString(MidpointRounding.AwayFromZero, 3);
        log.Info($"Completed SQL execution in {stopwatchTime} seconds");

        log.Debug("SQL completed");
    }
}
