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
using System.Text;

namespace HavokMultimedia.Utilities
{
    public class HtmlBuilder
    {
        public static readonly string JS_TABLE = @"
window.onload=function(){
const getCellValue = (tr, idx) => tr.children[idx].innerText || tr.children[idx].textContent;
const comparer = (idx, asc) => (a, b) => ((v1, v2) => 
    v1 !== '' && v2 !== '' && !isNaN(v1) && !isNaN(v2) ? v1 - v2 : v1.toString().localeCompare(v2)
    )(getCellValue(asc ? a : b, idx), getCellValue(asc ? b : a, idx));
document.querySelectorAll('th').forEach(th => th.addEventListener('click', (() => {
    const table = th.closest('table');
    const tbody = table.querySelector('tbody');
    Array.from(tbody.querySelectorAll('tr'))
        .sort(comparer(Array.from(th.parentNode.children).indexOf(th), this.asc = !this.asc))
        .forEach(tr => tbody.appendChild(tr) );
})));
}
";

        public static readonly string CSS_TABLE = @"
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

        public string Title { get; set; }
        private readonly List<string> javascripts = new List<string>();
        private readonly List<string> csss = new List<string>();
        private readonly StringBuilder body = new StringBuilder();
        public void Javascript(string javascript) => javascripts.Add(javascript);
        public void CSS(string css) => csss.Add(css);
        public void Table(Table table)
        {
            body.AppendLine($"<table>");
            body.AppendLine($"<thead>");
            body.Append($"<tr>");
            foreach (var c in table.Columns) body.Append("<th>" + c.Name.EscapeHtml() + "</th>");
            body.AppendLine($"</tr>");
            body.AppendLine($"</thead>");
            body.AppendLine($"<tbody>");
            foreach (var r in table)
            {
                body.Append("<tr>");
                foreach (var cell in r) body.Append("<td>" + cell.EscapeHtml() + "</td>");
                body.AppendLine("</tr>");
            }
            body.AppendLine("</tbody>");
            body.AppendLine("</table>");
        }

        public void BR() => BR(1);
        public void BR(int count) { for (int i = 0; i < count; i++) body.AppendLine("<br>"); }

        private class AttributeBuilder
        {
            private readonly string name;
            public AttributeBuilder(string name) => this.name = name;

            private readonly List<(string name, string value)> attributes = new List<(string name, string value)>();
            public AttributeBuilder Add(string name, object value)
            {
                attributes.Add((name, value?.ToString() ?? string.Empty));
                return this;
            }
            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append("<" + name);
                foreach (var attribute in attributes)
                {
                    sb.Append(" ");
                    sb.Append(attribute.name);
                    sb.Append("=");
                    sb.Append("\"");
                    sb.Append(attribute.value);
                    sb.Append("\"");
                }
                sb.Append(">");

                return sb.ToString();
            }
        }
        private AttributeBuilder Element(string name) => new AttributeBuilder(name);
        private AttributeBuilder Element(string name, string id) => Element(name).Add("id", id).Add("name", id);

        public void InputText(string id, string label = null, int size = 0, string value = null)
        {
            if (label != null) body.AppendLine(Element("label").Add("for", id).ToString() + label.EscapeHtml() + "</label>");
            var element = Element("input", id);
            element.Add("type", "text");
            if (size > 0) element.Add("size", size);
            if (value != null) element.Add("value", value);
            body.AppendLine(element.ToString());
        }
        public void InputPassword(string id, string label = null, int size = 0)
        {
            if (label != null) body.AppendLine(Element("label").Add("for", id).ToString() + label.EscapeHtml() + "</label>");
            var element = Element("input", id);
            element.Add("type", "password");
            if (size > 0) element.Add("size", size);
            body.AppendLine(element.ToString());
        }

        public void Select<TEnum>(string id, string label = null) where TEnum : struct, IConvertible, IComparable, IFormattable
        {
            if (label != null) body.AppendLine(Element("label").Add("for", id).ToString() + label.EscapeHtml() + "</label>");
            body.AppendLine(Element("select", id).ToString());
            foreach (var enumItem in Util.GetEnumItems<TEnum>())
            {
                body.Append(Element("option").Add("value", enumItem).ToString());
                body.Append(enumItem.ToString().EscapeHtml());
                body.AppendLine("</option>");
            }
            body.AppendLine("</select>");
        }

        public void InputSubmit(string value) => body.AppendLine(Element("input").Add("type", "submit").Add("value", value).ToString());
        public void InputFile(string id) => body.AppendLine(Element("input", id).Add("type", "file").ToString());
        public void Form() => body.AppendLine("<form>");
        public void FormEnd() => body.AppendLine("</form>");
        public void P() => body.AppendLine("<p>");
        public void P(string text)
        {
            P();
            Text(text);
            PEnd();
        }
        public void PEnd() => body.AppendLine("</p>");
        public void TextArea(string id, int rows = 0, int cols = 0, string text = null)
        {
            var element = Element("textarea", id);
            if (rows > 0) element.Add("rows", rows);
            if (cols > 0) element.Add("cols", cols);
            body.Append(element.ToString());
            if (text != null) body.Append(text.EscapeHtml());
            body.AppendLine("</textarea>");
        }
        public void Text(string text)
        {
            body.Append(text);
        }
        public void H1(string text)
        {
            body.AppendLine("<h1>" + text + "</h1>");
        }
        public void Pre(string text)
        {
            body.AppendLine("<pre>" + text + "</pre>");
        }
        public void Exception(Exception e)
        {
            H1(e.GetType().NameFormatted().EscapeHtml() + ": " + e.Message.EscapeHtml());
            Pre(e.ToString().EscapeHtml());
        }
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<html>");
            sb.AppendLine($"<head>");
            sb.AppendLine($"<meta charset=\"utf-8\">");
            var title = Title ?? "(Title)";
            sb.AppendLine($"<title>{title}</title>");
            foreach (var javascript in javascripts.TrimOrNull().WhereNotNull())
            {
                sb.AppendLine($"<script type=\"text/javascript\">");
                sb.AppendLine(javascript);
                sb.AppendLine($"</script>");
            }
            foreach (var css in csss.TrimOrNull().WhereNotNull())
            {
                sb.AppendLine($"<style>");
                sb.AppendLine(css);
                sb.AppendLine($"</style>");
            }
            sb.AppendLine($"</head>");
            sb.AppendLine($"<body>");
            sb.AppendLine(body.ToString());
            sb.AppendLine($"</body>");
            sb.AppendLine($"</html>");
            return sb.ToString();

        }
    }
}
