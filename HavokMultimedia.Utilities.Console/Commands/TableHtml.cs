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

        private string ReadFileContent(string fileName)
        {
            fileName = fileName.TrimOrNull();
            if (fileName == null) return null;
            var files = Util.ParseInputFiles(fileName.Yield());
            var content = new StringBuilder();
            foreach (var file in files)
            {
                content.AppendLine(ReadFile(file));
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
            if (css == null && !noCss) css = HtmlBuilder.CSS_TABLE;

            var noJavascript = GetArgParameterOrConfigBool("noJavascript", "nj", false);
            var js = ReadFileContent(GetArgParameterOrConfig("javascriptFile", "js"));
            if (js == null && !noJavascript) js = HtmlBuilder.JS_TABLE;

            foreach (var includedItem in includedItems)
            {
                log.Debug($"Reading table file: {includedItem}");
                var table = ReadTableTab(includedItem);
                var outputFile = includedItem;
                log.Debug(nameof(outputFile) + ": " + outputFile);

                DeleteExistingFile(outputFile);

                log.Debug("Writing file with " + table.Columns.Count + " columns and " + table.Count + " rows to file " + outputFile);

                var html = new HtmlBuilder();
                html.Title = Path.GetFileName(outputFile).EscapeHtml();
                html.Javascript(js);
                html.CSS(css);
                html.Table(table);
                Util.FileWrite(outputFile, html.ToString(), Constant.ENCODING_UTF8_WITHOUT_BOM);

                log.Info("Delimited file with " + table.Columns.Count + " columns, " + table.Count + " rows created: " + outputFile);

            }

        }



    }
}
