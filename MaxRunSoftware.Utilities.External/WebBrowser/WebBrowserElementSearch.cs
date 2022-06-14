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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using OpenQA.Selenium;

namespace MaxRunSoftware.Utilities.External;

public class WebBrowserElementSearch
{
    public string Id { get; set; }
    public string ClassName { get; set; }
    public string Name { get; set; }
    public string TagName { get; set; }
    public string XPath { get; set; }
    public string ValueEquals { get; set; }
    public string ValueContains { get; set; }

    public List<IWebElement> FindElements(WebDriver driver)
    {
        Id = Id.TrimOrNull();
        ClassName = ClassName.TrimOrNull();
        Name = Name.TrimOrNull();
        TagName = TagName.TrimOrNull();
        XPath = XPath.TrimOrNull();
        ValueEquals = ValueEquals.TrimOrNull();
        ValueContains = ValueContains.TrimOrNull();

        var searchedId = false;
        var searchedClassName = false;
        var searchedName = false;
        var searchedTagName = false;
        var searchedValueEquals = false;
        var searchedValueContains = false;

        var list = new List<IWebElement>();
        if (XPath != null) { list.AddRange(driver.FindElements(By.XPath(XPath))); }
        else if (Id != null)
        {
            searchedId = true;
            list.AddRange(driver.FindElements(By.Id(Id)));
        }
        else if (ClassName != null)
        {
            searchedClassName = true;
            if (ClassName.IndexOf(" ", StringComparison.Ordinal) < 0) { list.AddRange(driver.FindElements(By.ClassName(ClassName))); }
            else
            {
                var query = ClassName
                        .Split(' ')
                        .TrimOrNull()
                        .WhereNotNull()
                        .Select(o => $"contains(@class, '{o}')")
                        .ToStringDelimited(" and ")
                    ;

                var xpath = $"//*[{query}]";
                list.AddRange(driver.FindElements(By.XPath(xpath)));
            }
        }
        else if (Name != null)
        {
            searchedName = true;
            list.AddRange(driver.FindElements(By.Name(Name)));
        }
        else if (TagName != null)
        {
            searchedTagName = true;
            list.AddRange(driver.FindElements(By.TagName(TagName)));
        }
        else if (ValueEquals != null)
        {
            searchedValueEquals = true;

            // https://stackoverflow.com/a/5075279
            var xpath = $"//text()[. = '{ValueEquals}']";
            list.AddRange(driver.FindElements(By.XPath(xpath)));
        }
        else if (ValueContains != null)
        {
            searchedValueContains = true;

            // https://stackoverflow.com/a/5075279
            var xpath = $"//text()[contains(.,'{ValueContains}')]";
            list.AddRange(driver.FindElements(By.XPath(xpath)));
        }

        if (Id != null && !searchedId)
        {
            var list2 = new List<IWebElement>();
            foreach (var element in list)
            {
                var val = element.GetId();
                if (val == null) continue; // we are filtering on ID and this element doesn't have an ID so skip

                if (string.Equals(Id, val, StringComparison.OrdinalIgnoreCase)) list2.Add(element);
            }

            list = list2;
        }

        if (ClassName != null && !searchedClassName)
        {
            var list2 = new List<IWebElement>();
            foreach (var element in list)
            {
                var val = element.GetClassNames();
                if (val == null || val.Length == 0) continue; // we are filtering on ClassName and this element doesn't have a ClassName so skip

                var elementClassItems = new HashSet<string>(val.Select(o => o.ToLower()));

                var foundAll = true;
                foreach (var s in ClassName.Split(' ').TrimOrNull().WhereNotNull().Select(o => o.ToLower()))
                {
                    if (!elementClassItems.Contains(s))
                    {
                        foundAll = false;
                        break;
                    }
                }

                if (foundAll) list2.Add(element);
            }

            list = list2;
        }

        if (Name != null && !searchedName)
        {
            var list2 = new List<IWebElement>();
            foreach (var element in list)
            {
                var val = element.GetName();
                if (val == null) continue; // we are filtering on Name and this element doesn't have an Name so skip

                if (string.Equals(Name, val, StringComparison.OrdinalIgnoreCase)) list2.Add(element);
            }

            list = list2;
        }

        if (TagName != null && !searchedTagName)
        {
            var list2 = new List<IWebElement>();
            foreach (var element in list)
            {
                var val = element.TagName;
                if (val == null) continue; // we are filtering on TagName and this element doesn't have an TagName so skip

                if (string.Equals(TagName, val, StringComparison.OrdinalIgnoreCase)) list2.Add(element);
            }

            list = list2;
        }

        if (ValueEquals != null && !searchedValueEquals)
        {
            var list2 = new List<IWebElement>();
            foreach (var element in list)
            {
                var val = element.Text.TrimOrNull();
                if (val == null) continue; // we are filtering on Value and this element doesn't have a Value so skip

                if (string.Equals(ValueEquals, val, StringComparison.OrdinalIgnoreCase)) list2.Add(element);
            }

            list = list2;
        }

        if (ValueContains != null && !searchedValueContains)
        {
            var list2 = new List<IWebElement>();
            foreach (var element in list)
            {
                var val = element.Text.TrimOrNull();
                if (val == null) continue; // we are filtering on Value and this element doesn't have a Value so skip

                if (val.ToLower().Contains(ValueContains.ToLower())) list2.Add(element);
            }

            list = list2;
        }

        return list;
    }

    public override string ToString()
    {
        var items = new List<string>();
        if (Id != null) items.Add(nameof(Id) + "=" + Id);

        if (ClassName != null) items.Add(nameof(ClassName) + "=" + ClassName);

        if (Name != null) items.Add(nameof(Name) + "=" + Name);

        if (TagName != null) items.Add(nameof(TagName) + "=" + TagName);

        if (XPath != null) items.Add(nameof(XPath) + "=" + XPath);

        return GetType().Name + "(" + items.ToStringDelimited(", ") + ")";
    }


    /// <summary>
    /// https://github.com/DotNetSeleniumTools/DotNetSeleniumExtras/blob/master/src/PageObjects/ByChained.cs
    /// Mechanism used to locate elements within a document using a series of other lookups.  This class will find all DOM
    /// elements that matches each of the locators in sequence
    /// </summary>
    /// <example>
    /// The following code will will find all elements that match by2 and appear under an element that matches by1.
    /// <code>
    /// driver.findElements(new ByChained(by1, by2))
    /// </code>
    /// </example>
    // ReSharper disable once UnusedType.Local
    private class ByChained : By
    {
        private readonly By[] bys;

        /// <summary>
        /// Initializes a new instance of the <see cref="ByChained" /> class with one or more <see cref="By" /> objects.
        /// </summary>
        /// <param name="bys">One or more <see cref="By" /> references</param>
        public ByChained(params By[] bys) { this.bys = bys; }

        /// <summary>
        /// Find a single element.
        /// </summary>
        /// <param name="context">Context used to find the element.</param>
        /// <returns>The element that matches</returns>
        public override IWebElement FindElement(ISearchContext context)
        {
            var elements = FindElements(context);
            if (elements.Count == 0) throw new NoSuchElementException("Cannot locate an element using " + ToString());

            return elements[0];
        }

        /// <summary>
        /// Finds many elements
        /// </summary>
        /// <param name="context">Context used to find the element.</param>
        /// <returns>A readonly collection of elements that match.</returns>
        public override ReadOnlyCollection<IWebElement> FindElements(ISearchContext context)
        {
            if (bys.Length == 0) return new List<IWebElement>().AsReadOnly();

            List<IWebElement> elems = null;
            foreach (var by in bys)
            {
                var newElems = new List<IWebElement>();

                if (elems == null)
                    newElems.AddRange(by.FindElements(context));
                else
                    foreach (var elem in elems)
                        newElems.AddRange(elem.FindElements(by));

                elems = newElems;
            }

            return elems.OrEmpty().ToList().AsReadOnly();
        }

        /// <summary>
        /// Writes out a comma separated list of the <see cref="By" /> objects used in the chain.
        /// </summary>
        /// <returns>Converts the value of this instance to a <see cref="string" /></returns>
        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            foreach (var by in bys)
            {
                if (stringBuilder.Length > 0) stringBuilder.Append(",");

                stringBuilder.Append(by);
            }

            return string.Format(CultureInfo.InvariantCulture, "By.Chained([{0}])", stringBuilder.ToString());
        }
    }
}
