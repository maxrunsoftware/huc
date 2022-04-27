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
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class SqlDownload : SqlQueryBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Execute a SQL statement and/or script with a list of files to download, which must contain the columns [FileName] and [FileURL] and any [cookie_<CookieName>] cookies to be used");
            help.AddParameter(nameof(showSuccess), "ss", "Show successfully downloaded file messages (false)");
            help.AddExample(HelpExamplePrefix + " -s=`SELECT file AS [FileName], myurl AS [FileURL] FROM MyDataTable`");
            help.AddExample(HelpExamplePrefix + " -f=`mssqlscript.sql`");

        }

        private bool showSuccess;

        protected void DownloadFile(string fileName, string fileUrl, Dictionary<string, string> cookies, int rowNum)
        {

            if (fileName == null) throw new Exception("[FileName] is NULL");
            if (fileUrl == null) throw new Exception("[FileURL] is NULL");

            log.Debug("Downloading [" + fileName + "] from " + fileUrl + "  (cookies: " + cookies.Count + ")");

            DeleteExistingFile(fileName);

            var response = Util.WebDownload(fileUrl, outFilename: fileName, cookies: cookies);
            var msg = "Successfully downloaded file " + fileName + "  (" + Util.FileGetSize(fileName) + ")";
            if (showSuccess) log.Info(msg);
            else log.Debug(msg);

            log.Debug(response.ToString());
            log.Trace(response.ToStringDetail());
        }

        protected override void ExecuteInternal()
        {
            base.ExecuteInternal();

            showSuccess = GetArgParameterOrConfigBool(nameof(showSuccess), "ss", false);
            var tables = ExecuteTables();

            var resultSetNum = 0;
            var resultSuccessNum = 0;
            var resultFailNum = 0;

            log.Info("Starting download of " + tables.Sum(o => o.Count) + " files");
            foreach (var table in tables)
            {
                resultSetNum++;
                log.Debug("Processing SQL Result: " + resultSetNum);

                if (!table.Columns.TryGetColumn("FileName", out var columnFileName))
                {
                    log.Warn("SQL Result " + resultSetNum + " does not contain a [FileName] column so skipping result");
                    continue;
                }

                if (!table.Columns.TryGetColumn("FileURL", out var columnFileURL))
                {
                    log.Warn("SQL Result " + resultSetNum + " does not contain a [FileURL] column so skipping result");
                    continue;
                }

                var cookieColumns = new List<TableColumn>();
                foreach (var c in table.Columns)
                {
                    if (c.Name.In(StringComparer.OrdinalIgnoreCase, "FileName", "FileURL")) continue;

                    if (c.Name.StartsWith("Cookie_", StringComparison.OrdinalIgnoreCase) || c.Name.StartsWith("Cookie.", StringComparison.OrdinalIgnoreCase))
                    {
                        if (c.Name.Length > 7)
                        {
                            cookieColumns.Add(c);
                        }
                    }

                }
                foreach (var c in cookieColumns) log.Debug("Found cookie column [" + c.Name + "]");

                int rowNum = 0;
                foreach (var row in table)
                {
                    rowNum++;
                    var fileName = row[columnFileName];
                    var fileUrl = row[columnFileURL];

                    var cookies = new Dictionary<string, string>();
                    foreach (var c in cookieColumns)
                    {
                        cookies[c.Name.Substring(7)] = row[c];
                    }

                    try
                    {
                        DownloadFile(fileName, fileUrl, cookies, rowNum);
                        resultSuccessNum++;
                    }
                    catch (Exception e)
                    {
                        resultFailNum++;
                        log.Warn($"[{resultSetNum}][{rowNum}] Error downloading file {fileName} from {fileUrl}   : " + e.Message);
                        log.Debug(e);
                    }
                }
            }

            log.Info($"Downloads complete  Success:{resultSuccessNum}  Failed:{resultFailNum}");


        }
    }
}