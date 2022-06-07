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

using System.IO;
using System.Text;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class TableHtml : TableBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Converts a tab delimited data file to a nice html table file");
            help.AddParameter(nameof(cssFile), "css", "CSS file to embed in generated HTML file");
            help.AddParameter(nameof(javascriptFile), "js", "Javascript file to embed in generated HTML file");
            help.AddParameter(nameof(noJavascript), "nj", "Exclude the default javascript from the generated file (false)");
            help.AddParameter(nameof(noCSS), "nc", "Exclude the default CSS from the generated file (false)");
            help.AddExample("Orders.html");
            help.AddExample("css=MyStyleSheet.css js=MyJavascriptFile.js Orders.html");
        }

        private string cssFile;
        private string javascriptFile;
        private bool noJavascript;
        private bool noCSS;

        private string ReadFileContent(string fileName)
        {
            fileName = fileName.TrimOrNull();
            if (fileName == null) return null;
            var files = ParseInputFiles(fileName.Yield());
            var content = new StringBuilder();
            foreach (var file in files)
            {
                content.AppendLine(ReadFile(file));
            }
            var s = content.ToString().TrimOrNull();
            return s;
        }

        private string css;
        private string js;

        protected override string Convert(Utilities.Table table)
        {
            var html = new HtmlWriter();
            html.Title = Path.GetFileName(CurrentOutputFile).EscapeHtml();
            html.Javascript(js);
            html.CSS(css);
            html.Table(table);
            return html.ToString();
        }

        protected override void ExecuteInternal()
        {
            noCSS = GetArgParameterOrConfigBool(nameof(noCSS), "nc", false);
            cssFile = GetArgParameterOrConfig(nameof(cssFile), "css");
            css = ReadFileContent(cssFile);
            if (css == null && !noCSS) css = HtmlWriter.CSS_TABLE;

            noJavascript = GetArgParameterOrConfigBool(nameof(noJavascript), "nj", false);
            javascriptFile = GetArgParameterOrConfig(nameof(javascriptFile), "js");
            js = ReadFileContent(javascriptFile);
            if (js == null && !noJavascript) js = HtmlWriter.JS_TABLE;

            base.ExecuteInternal();
        }
    }
}
