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

namespace MaxRunSoftware.Utilities;

public class HtmlWriter
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
    private readonly List<string> javascriptItems = new();
    private readonly List<string> cssItems = new();
    private readonly StringBuilder body = new();

    public void Javascript(string javascript)
    {
        javascriptItems.Add(javascript);
    }

    public void CSS(string css)
    {
        cssItems.Add(css);
    }

    public void Table(Table table)
    {
        var s = new StringBuilder();
        s.AppendLine("<table>");
        s.AppendLine("<thead>");
        s.Append("<tr>");
        foreach (var c in table.Columns)
        {
            s.Append("<th>" + c.Name + "</th>");
        }

        s.AppendLine("</tr>");
        s.AppendLine("</thead>");
        s.AppendLine("<tbody>");
        foreach (var r in table)
        {
            s.Append("<tr>");
            foreach (var cell in r)
            {
                s.Append("<td>" + cell + "</td>");
            }

            s.AppendLine("</tr>");
        }

        s.AppendLine("</tbody>");
        s.AppendLine("</table>");

        Body(s);
    }

    public void Br(int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            body.AppendLine("<br>");
        }
    }

    private class AttributeBuilder
    {
        private readonly string name;

        public AttributeBuilder(string name)
        {
            this.name = name;
        }

        private readonly List<(string name, string value)> attributes = new();

        public AttributeBuilder Add(string attributeName, object attributeValue)
        {
            attributes.Add((attributeName, attributeValue?.ToString() ?? string.Empty));
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

    private AttributeBuilder Element(string name)
    {
        return new AttributeBuilder(name);
    }

    private AttributeBuilder Element(string name, string id)
    {
        return Element(name).Add("id", id).Add("name", id);
    }

    public void InputText(string id, string label = null, int size = 0, string value = null)
    {
        BodyLabel(id, label);
        var element = Element("input", id);
        element.Add("type", "text");
        if (size > 0)
        {
            element.Add("size", size);
        }

        if (value != null)
        {
            element.Add("value", value);
        }

        Body(element);
    }

    public void InputPassword(string id, string label = null, int size = 0)
    {
        BodyLabel(id, label);
        var element = Element("input", id).Add("type", "password");
        if (size > 0)
        {
            element.Add("size", size);
        }

        Body(element);
    }

    public void BodyLabel(string forId, string labelText)
    {
        if (forId == null)
        {
            return;
        }

        if (labelText == null)
        {
            return;
        }

        Body(Element("label").Add("for", forId) + labelText + "</label>");
    }

    public void Select<TEnum>(string id, string label = null) where TEnum : struct, IConvertible, IComparable, IFormattable
    {
        BodyLabel(id, label);
        Body(Element("select", id));
        foreach (var enumItem in Util.GetEnumItems<TEnum>())
        {
            Body(Element("option").Add("value", enumItem).ToString() + enumItem + "</option>");
        }

        Body("</select>");
    }

    public void InputSubmit(string value)
    {
        Body(Element("input").Add("type", "submit").Add("value", value));
    }

    public void InputFile(string id)
    {
        Body(Element("input", id).Add("type", "file"));
    }

    public void Form(string action = null)
    {
        var element = Element("form");
        if (action != null)
        {
            element.Add("action", action);
        }

        Body(element);
    }

    public void FormEnd()
    {
        Body("</form>");
    }

    public void P()
    {
        Body("<p>");
    }

    public void P(string text)
    {
        P();
        Body(text);
        PEnd();
    }

    public void PEnd()
    {
        Body("</p>");
    }

    public void TextArea(string id, int rows = 0, int cols = 0, string text = null)
    {
        var element = Element("textarea", id);
        if (rows > 0)
        {
            element.Add("rows", rows);
        }

        if (cols > 0)
        {
            element.Add("cols", cols);
        }

        Body(element);
        Body(text);
        Body("</textarea>");
    }

    public void Body(object text)
    {
        Body(text?.ToString());
    }

    public void Body(string text)
    {
        if (text == null)
        {
            return;
        }

        if (text.Length == 0)
        {
            return;
        }

        body.AppendLine(text);
    }

    public void H1(string text)
    {
        Body("<h1>" + text + "</h1>");
    }

    public void Pre(string text)
    {
        Body("<pre>" + text + "</pre>");
    }

    public void Exception(Exception e)
    {
        H1(e.GetType().NameFormatted().EscapeHtml() + ": " + e.Message.EscapeHtml());
        Pre(e.ToString().EscapeHtml());
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        var title = Title ?? "(Title)";
        sb.AppendLine($"<title>{title}</title>");
        foreach (var str in javascriptItems.TrimOrNull().WhereNotNull())
        {
            sb.AppendLine("<script type=\"text/javascript\">");
            sb.AppendLine(str);
            sb.AppendLine("</script>");
        }

        foreach (var str in cssItems.TrimOrNull().WhereNotNull())
        {
            sb.AppendLine("<style>");
            sb.AppendLine(str);
            sb.AppendLine("</style>");
        }

        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine(body.ToString());
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }
}
