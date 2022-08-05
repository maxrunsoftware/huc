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

using System.Diagnostics;
using EmbedIO;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class WebServerUtilitySql : WebServerUtilityBase
{
    public override HttpVerbs Verbs => HttpVerbs.Any;


    public override string HandleJson()
    {
        var result = Handle();
        if (!result.success) return string.Empty;


        var objs = new List<List<string[]>>();
        foreach (var table in result.tables)
        {
            var l = new List<string[]>();
            l.Add(table.Columns.Select(o => o.Name).ToArray());
            foreach (var row in table) l.Add(row.ToArray());

            objs.Add(l);
        }

        return ToJson(objs);
    }


    public (bool success, string totalSeconds, Utilities.Table[] tables) Handle()
    {
        var connectionString = GetParameterString("connectionString");
        log.Debug(nameof(connectionString) + ": " + connectionString);

        var commandTimeout = GetParameterInt("commandTimeout");
        log.Debug(nameof(commandTimeout) + ": " + commandTimeout);

        var serverType = GetParameterString("serverType");
        log.Debug(nameof(serverType) + ": " + serverType);

        var sqlStatement = GetParameterString("sqlStatement");
        log.Debug(nameof(sqlStatement) + ": " + sqlStatement);


        /*
        var paramValues = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
            paramValues.Add("Product", product);
            paramValues.Add("PropertyNames", string.Join(";", props));
            uriBuilder.Query = paramValues.ToString();
        */

        if (connectionString == null || serverType == null || sqlStatement == null) return (false, "0", null);

        var d = new Dictionary<string, string>();
        d[nameof(connectionString)] = connectionString;
        if (commandTimeout != null) d[nameof(commandTimeout)] = commandTimeout.Value.ToString();

        d[nameof(serverType)] = serverType;
        d[nameof(sqlStatement)] = sqlStatement;

        var args = new List<string>();
        foreach (var kvp in d)
        {
            var s = $"-{kvp.Key}={kvp.Value}";
            log.Debug(s);
            args.Add(s);
        }

        var sql = new Sql();
        sql.Args = args.ToArray();

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var tables = sql.ExecuteTables();

        stopwatch.Stop();
        var stopwatchTime = stopwatch.Elapsed.TotalSeconds.ToString(MidpointRounding.AwayFromZero, 3);

        var tables2 = new List<Utilities.Table>();
        foreach (var table in tables)
        {
            if (!table.Columns.IsEmpty()) tables2.Add(table);
        }

        return (true, stopwatchTime, tables2.ToArray());
    }

    public override string HandleHtml()
    {
        var html = new HtmlWriter();
        html.Title = "SQL";
        html.CSS(HtmlWriter.CSS_TABLE);
        html.Javascript(HtmlWriter.JS_TABLE);
        try
        {
            var result = Handle();

            if (!result.success)
            {
                html.Form();
                html.P();
                html.InputText("connectionString", "Connection String ", 80, "Server=192.168.42.2;Database=NorthWind;User Id=testuser;Password=testpass;");
                html.Br(2);
                html.InputText("commandTimeout", "Command Timeout ", value: "60");
                html.Br(2);
                html.Select<SqlServerType>("serverType");
                html.Br(2);
                html.InputSubmit("Execute");
                html.Br(2);
                html.TextArea("sqlStatement", 24, 80, "SELECT * FROM Orders");
                html.PEnd();
                html.FormEnd();
            }
            else
            {
                foreach (var table in result.tables)
                {
                    html.Table(table);
                    html.Br(2);
                }
            }
        }
        catch (Exception e) { html.Exception(e); }

        return html.ToString();
    }
}
