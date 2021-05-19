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

using System.IO;
using System.Text;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class TableHtml : TableBase
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            base.CreateHelp(help);
            help.AddSummary("Converts a tab delimited data file to a nice html table file");
            help.AddParameter("cssFile", "css", "CSS file to embed in generated HTML file");
            help.AddParameter("javascriptFile", "js", "Javascript file to embed in generated HTML file");
            help.AddParameter("noJavascript", "nj", "Exclude the default javascript from the generated file (false)");
            help.AddParameter("noCSS", "nc", "Exclude the default CSS from the generated file (false)");
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

        private string css;
        private string js;

        protected override string Convert(Utilities.Table table)
        {
            var html = new HtmlBuilder();
            html.Title = Path.GetFileName(CurrentOutputFile).EscapeHtml();
            html.Javascript(js);
            html.CSS(css);
            html.Table(table);
            return html.ToString();
        }

        protected override void ExecuteInternal()
        {
            var noCss = GetArgParameterOrConfigBool("noCSS", "nc", false);
            css = ReadFileContent(GetArgParameterOrConfig("cssFile", "css"));
            if (css == null && !noCss) css = HtmlBuilder.CSS_TABLE;

            var noJavascript = GetArgParameterOrConfigBool("noJavascript", "nj", false);
            js = ReadFileContent(GetArgParameterOrConfig("javascriptFile", "js"));
            if (js == null && !noJavascript) js = HtmlBuilder.JS_TABLE;

            base.ExecuteInternal();
        }



    }
}
