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
using System.Threading;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class SqlDownload : SqlQueryBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Execute a SQL statement and/or script with a list of files to download, which must contain the columns [FileName] and [FileURL] and any [cookie_<CookieName>] cookies to be used");
        help.AddParameter(nameof(threads), "t", "Number of threads (1)");
        help.AddParameter(nameof(showSuccess), "ss", "Show successfully downloaded file messages (false)");
        help.AddExample(HelpExamplePrefix + " -s=`SELECT file AS [FileName], myurl AS [FileURL] FROM MyDataTable`");
        help.AddExample(HelpExamplePrefix + " -f=`mssqlscript.sql`");
    }

    private int threads;
    private bool showSuccess;

    private void ProcessFileDownload(FileDownload fileDownload)
    {
        var msgPrefix = $"[{fileDownload.ResultNum}][{fileDownload.RowNum}]";
        log.Debug($"{msgPrefix} Downloading [{fileDownload.FileName}] from {fileDownload.FileUrl}  (cookies: {fileDownload.Cookies.Count})");

        try
        {
            DeleteExistingFile(fileDownload.FileName);
            var response = Util.WebDownload(fileDownload.FileUrl, fileDownload.FileName, cookies: fileDownload.Cookies);
            var msg = $"{msgPrefix} Successfully downloaded file {fileDownload.FileName}  ({Util.FileGetSize(fileDownload.FileName)})";
            if (showSuccess) { log.Info(msg); }
            else { log.Debug(msg); }

            log.Debug(response.ToString());
            log.Trace(response.ToStringDetail());
        }
        catch (Exception e)
        {
            log.Warn($"{msgPrefix} Error downloading file {fileDownload.FileName} from {fileDownload.FileUrl}   : " + e.Message);
            log.Debug(e);
        }
    }
    
    private class FileDownload
    {
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public Dictionary<string, string> Cookies { get; } = new();
        public int ResultNum { get; set; }
        public int RowNum { get; set; }
        
    }

    private List<FileDownload> ParseTable(Utilities.Table table, int resultSetNum)
    {
        var fileDownloads = new List<FileDownload>();

        log.Debug("Processing SQL Result: " + resultSetNum);

        if (!table.Columns.TryGetColumn("FileName", out var columnFileName))
        {
            log.Warn("SQL Result " + resultSetNum + " does not contain a [FileName] column so skipping result");
            return fileDownloads;
        }

        if (!table.Columns.TryGetColumn("FileURL", out var columnFileUrl))
        {
            log.Warn("SQL Result " + resultSetNum + " does not contain a [FileURL] column so skipping result");
            return fileDownloads;
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

        var rowNum = 0;
        foreach (var row in table)
        {
            rowNum++;
            var fileDownload = new FileDownload();
            fileDownload.FileName = row[columnFileName];
            fileDownload.FileUrl = row[columnFileUrl];
            fileDownload.RowNum = rowNum;
            fileDownload.ResultNum = resultSetNum;
            foreach (var c in cookieColumns) fileDownload.Cookies[c.Name.Substring(7)] = row[c];
                
            if (fileDownload.FileName == null) continue; // TODO: Log message
            if (fileDownload.FileUrl == null) continue; // TODO: Log message
                
            fileDownloads.Add(fileDownload);
        }

        
        return fileDownloads;
    }

    protected override void ExecuteInternal()
    {
        base.ExecuteInternal();

        threads = GetArgParameterOrConfigInt(nameof(threads), "t", 1);
        showSuccess = GetArgParameterOrConfigBool(nameof(showSuccess), "ss", false);
        var tables = ExecuteTables();

        var fileDownloads = new List<FileDownload>();
        
        var resultSetNum = 0;
        foreach (var table in tables)
        {
            resultSetNum++;
            fileDownloads.AddRange(ParseTable(table, resultSetNum));
        }

        log.Info($"Downloading {fileDownloads.Count} files");

        var pool = new ConsumerThreadPool<FileDownload>(ProcessFileDownload);
        pool.NumberOfThreads = threads;
        foreach (var fileDownload in fileDownloads)
        {
            pool.AddWorkItem(fileDownload);
        }
        
        pool.FinishedAddingWorkItems();

        while (!pool.IsComplete)
        {
            Thread.Sleep(1000);
        }
        
        log.Info($"Downloads complete " + fileDownloads.Count);
    }
}
