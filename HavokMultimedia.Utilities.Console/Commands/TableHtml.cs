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

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class TableHtml : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Converts a tab delimited data file to a nice html table file");
            help.AddParameter("cssFile", "css", "CSS file to embed in generated HTML file");
            help.AddParameter("javascriptFile", "js", "Javascript file to embed in generated HTML file");
            help.AddParameter("noJavascript", "nj", "Exclude the default javascript from the generated file (false)");
            help.AddParameter("noCSS", "nc", "Exclude the default CSS from the generated file (false)");
            help.AddValue("<tab delimited input file 1> <tab delimited input file 2> <etc>");
            help.AddExample("Orders.html");
            help.AddExample("css=MyStyleSheet.css js=MyJavascriptFile.js Orders.html");
        }

        private static readonly string JS = @"
window.onload=function(){

const getCellValue = (tr, idx) => tr.children[idx].innerText || tr.children[idx].textContent;

const comparer = (idx, asc) => (a, b) => ((v1, v2) => 
    v1 !== '' && v2 !== '' && !isNaN(v1) && !isNaN(v2) ? v1 - v2 : v1.toString().localeCompare(v2)
    )(getCellValue(asc ? a : b, idx), getCellValue(asc ? b : a, idx));

// do the work...
document.querySelectorAll('th').forEach(th => th.addEventListener('click', (() => {
    const table = th.closest('table');
    const tbody = table.querySelector('tbody');
    Array.from(tbody.querySelectorAll('tr'))
        .sort(comparer(Array.from(th.parentNode.children).indexOf(th), this.asc = !this.asc))
        .forEach(tr => tbody.appendChild(tr) );
})));

}
";

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
  cursor: pointer;
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

        private static string HtmlEscape(string unescaped) => WebUtility.HtmlEncode(unescaped);

        private string ReadFileContent(string fileName)
        {
            fileName = fileName.TrimOrNull();
            if (fileName == null) return null;
            var files = Util.ParseInputFiles(fileName.Yield());
            var content = new StringBuilder();
            foreach (var cssFil in files)
            {
                if (!File.Exists(cssFil)) throw new FileNotFoundException("CSS file " + cssFil + " does not exist", cssFil);
                content.AppendLine(ReadFile(cssFil));
            }
            var s = content.ToString().TrimOrNull();
            return s;
        }

        protected override void ExecuteInternal()
        {
            var includedItems = Util.ParseInputFiles(GetArgValuesTrimmed());
            if (includedItems.Count < 1) throw new ArgsException("inputFiles", "No input files supplied");
            for (int i = 0; i < includedItems.Count; i++) log.Debug($"inputFile[{i}]: {includedItems[i]}");
            foreach (var includedItem in includedItems)
            {
                if (!File.Exists(includedItem)) throw new FileNotFoundException("Input file " + includedItem + " does not exist", includedItem);
            }
            for (var i = 0; i < includedItems.Count; i++) log.Debug($"inputFile[{i}]: {includedItems[i]}");

            var noCss = GetArgParameterOrConfigBool("noCSS", "nc", false);
            var css = ReadFileContent(GetArgParameterOrConfig("cssFile", "css"));
            if (css == null && !noCss) css = CSS;

            var noJavascript = GetArgParameterOrConfigBool("noJavascript", "nj", false);
            var js = ReadFileContent(GetArgParameterOrConfig("javascriptFile", "js"));
            if (js == null && !noJavascript) js = JS;

            foreach (var includedItem in includedItems)
            {
                log.Debug($"Reading table file: {includedItem}");
                var table = ReadTableTab(includedItem);
                var outputFile = includedItem;
                log.Debug(nameof(outputFile) + ": " + outputFile);

                DeleteExistingFile(outputFile);

                log.Debug("Writing file with " + table.Columns.Count + " columns and " + table.Count + " rows to file " + outputFile);
                using (var stream = File.OpenWrite(outputFile))
                using (var sw = new StreamWriter(stream, Constant.ENCODING_UTF8_WITHOUT_BOM))
                {
                    sw.WriteLine($"<html>");
                    sw.WriteLine($"  <head>");
                    sw.WriteLine($"    <meta charset=\"utf - 8\">");
                    sw.WriteLine($"    <title>{HtmlEscape(Path.GetFileName(outputFile))}</title>");
                    if (js != null)
                    {
                        sw.WriteLine($"    <script type = \"text/javascript\">");
                        sw.WriteLine(js);
                        sw.WriteLine($"    </script>");
                    }
                    if (css != null)
                    {
                        sw.WriteLine($"    <style>");
                        sw.WriteLine(css);
                        sw.WriteLine($"    </style>");
                    }
                    sw.WriteLine($"  </head>");
                    sw.WriteLine($"  <body>");
                    sw.WriteLine($"    <table>");
                    sw.WriteLine($"      <thead>");
                    sw.Write($"        <tr>");
                    foreach (var c in table.Columns) sw.Write("<th>" + HtmlEscape(c.Name) + "</th>");
                    sw.WriteLine($"        </tr>");
                    sw.WriteLine($"      </thead>");
                    sw.WriteLine($"      <tbody>");
                    foreach (var r in table)
                    {
                        sw.Write("<tr>");
                        foreach (var cell in r) sw.Write("<td>" + HtmlEscape(cell) + "</td>");
                        sw.WriteLine("</tr>");
                    }
                    sw.WriteLine("      </tbody>");
                    sw.WriteLine("    </table>");
                    sw.WriteLine($"  </body>");
                    sw.WriteLine($"</html>");
                    sw.Flush();
                    stream.Flush(true);
                }

                log.Info("Delimited file with " + table.Columns.Count + " columns, " + table.Count + " rows created: " + outputFile);

            }

        }



    }
}
