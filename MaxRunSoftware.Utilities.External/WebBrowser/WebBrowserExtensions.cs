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

using OpenQA.Selenium;

namespace MaxRunSoftware.Utilities.External;

public static class WebBrowserExtensions
{
    public static string GetId(this IWebElement element)
    {
        // TODO: All kinds of potential performance issues with this.
        var namesId = new[] { "id", "Id", "ID", "iD" };
        for (var i = 0; i < namesId.Length; i++)
        {
            var val = element.GetDomAttribute(namesId[i]).TrimOrNull();
            if (val != null) return val;
        }

        for (var i = 0; i < namesId.Length; i++)
        {
            var val = element.GetAttribute(namesId[i]).TrimOrNull();
            if (val != null) return val;
        }

        return null;
    }

    public static string GetName(this IWebElement element)
    {
        // TODO: All kinds of potential performance issues with this.
        var namesId = new[] { "name", "Name", "NAME" };
        for (var i = 0; i < namesId.Length; i++)
        {
            var val = element.GetDomAttribute(namesId[i]).TrimOrNull();
            if (val != null) return val;
        }

        for (var i = 0; i < namesId.Length; i++)
        {
            var val = element.GetAttribute(namesId[i]).TrimOrNull();
            if (val != null) return val;
        }

        return null;
    }

    public static string GetClassName(this IWebElement element)
    {
        // TODO: All kinds of potential performance issues with this.
        var namesClass = new[] { "class", "Class", "CLASS", "classname", "className", "Classname", "ClassName", "CLASSNAME" };
        for (var i = 0; i < namesClass.Length; i++)
        {
            var val = element.GetDomAttribute(namesClass[i]).TrimOrNull();
            if (val != null) return val;
        }

        for (var i = 0; i < namesClass.Length; i++)
        {
            var val = element.GetAttribute(namesClass[i]).TrimOrNull();
            if (val != null) return val;
        }

        return null;
    }

    public static string[] GetClassNames(this IWebElement element)
    {
        var v = GetClassName(element);
        if (v == null) return Array.Empty<string>();

        return v.Split(' ').TrimOrNull().WhereNotNull();
    }
}
