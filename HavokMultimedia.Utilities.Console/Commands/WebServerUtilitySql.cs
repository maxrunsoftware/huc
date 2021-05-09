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
using System.Diagnostics;
using System.Linq;
using System.Text;
using EmbedIO;
using HavokMultimedia.Utilities.Console.External;

namespace HavokMultimedia.Utilities.Console.Commands
{
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

        private string HtmlPage()
        {
            var html = $@"
<form>
<p>
    <label for='connectionString'>Connection String </label>
    <input type='text' id='connectionString' name='connectionString' size='80' value='Server=192.168.42.2;Database=NorthWind;User Id=testuser;Password=testpass;'>
    <br><br>

    <label for='commandTimeout'>Command Timeout </label>
    <input type='text' id='commandTimeout' name='commandTimeout' value='60'>
    <br><br>

    <label for='serverType'>Server Type </label>
    <select id='serverType' name='serverType'>
    <option value='mssql'>MSSQL</option>
    <option value='mysql'>MySQL</option>
    </select>
    <br><br>

    <input type='submit' value='Execute'>
    <br><br>

    <textarea id='sqlStatement' name='sqlStatement' rows='24' cols='80'>SELECT * FROM Orders</textarea>
</p>
</form>
";
            return html.Replace("'", "\"");
        }

        private string HtmlResponse(Utilities.Table[] tables)
        {
            var body = new StringBuilder();

            int index = 0;
            if (tables.IsEmpty())
            {
                body.Append("<h2>No result sets</h2>");
            }
            foreach (var table in tables)
            {

                body.Append("<table>");
                body.Append("<thead>");
                if (tables.Length > 1) body.Append($"<tr><th colspan=\"{table.Columns.Count}\">Result {index + 1}</th></tr>");
                body.Append("<tr>");
                foreach (var c in table.Columns) body.Append("<th>" + c.Name + "</th>");
                body.Append("</tr>");
                body.Append("</thead>");
                body.Append("<tbody>");
                foreach (var r in table)
                {
                    body.Append("<tr>");
                    foreach (var cell in r)
                    {
                        body.Append("<td>" + cell + "</td>");
                    }
                    body.Append("</tr>");
                }
                body.Append("</tbody>");
                body.Append("</table>");

                if (index < tables.Length) body.Append("<br><br>");
                index++;

            }

            return body.ToString();
        }
        private static readonly string CSS = @"
table {
  font-family: Arial, Helvetica, sans-serif;
  border-collapse: collapse;
  width: 100%;
}
th {
  border: 1px solid #ddd;
  padding: 8px;
  width: 1px;
  white-space: nowrap;
  padding-top: 12px;
  padding-bottom: 12px;
  text-align: left;
  background-color: #4CAF50;
  color: white;
}
td {
  border: 1px solid #ddd;
  padding: 8px;
  width: 1px;
  white-space: nowrap;
}

tr:nth-child(even){background-color: #f2f2f2;}

tr:hover {background-color: #ddd;}
".Replace("'", "\"");

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

            if (connectionString == null || serverType == null || sqlStatement == null)
            {
                return (false, "0", null);
            }

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
            var stopwatchtime = stopwatch.Elapsed.TotalSeconds.ToString(MidpointRounding.AwayFromZero, 3);

            var tables2 = new List<Utilities.Table>();
            foreach (var table in tables)
            {
                if (!table.Columns.IsEmpty()) tables2.Add(table);
            }
            return (true, stopwatchtime, tables2.ToArray());
        }

        public override string HandleHtml()
        {

            try
            {
                var result = Handle();
                if (!result.success)
                {
                    return External.WebServer.HtmlMessage("SQL", HtmlPage(), css: CSS);
                }

                var body = HtmlResponse(result.tables);
                return External.WebServer.HtmlMessage("SQL executed in " + result.totalSeconds + " seconds", body.ToString(), css: CSS);
            }
            catch (Exception e)
            {
                return External.WebServer.HtmlMessage(e.GetType().FullNameFormatted(), e.ToString());
            }

        }
    }
}
