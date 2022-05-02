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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class WebBrowser : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Executes scripted browser actions");
            help.AddParameter(nameof(browserExecutableFile), "b", "Path to the browser executable");
            help.AddParameter(nameof(browserDriverDirectory), "d", "Path to the pre-downloaded Selenium browser driver directory");
            help.AddParameter(nameof(browserDriverDownloadDirectory), "dd", "Path to the base directory to download and use Selenium drivers to (" + External.WebBrowser.BrowserDriverDownloadDirectoryBaseDefault + ")");

            help.AddParameter(nameof(browserType), "t", "Browser type " + DisplayEnumOptions<WebBrowserType>());
            help.AddParameter(nameof(browserVersion), "v", "Browser version");
            help.AddValue("<XML script file>");
            help.AddExample("myBrowserScript.xml");

            using (var w = new XmlWriter(true))
            {
                using (var ele = w.Element("browser"))
                {
                    var s = new List<(string name, object value)>()
                    {
                        ("id", "someID"),
                        ("class", "some class"),
                        ("name", "someName"),
                        ("tagName", "input"),
                        ("xpath", "//button[contains(@class, 'btn') and contains(@class, 'button-primary')]")
                    };

                    using (var act = w.Element("sleep", ("seconds", 5))) { }
                    using (var act = w.Element("text", "Hello World", attributes: s.ToArray())) { }
                    using (var act = w.Element("click", attributes: s.ToArray())) { }
                    using (var act = w.Element("select", "Blue", attributes: s.ToArray())) { }
                    using (var act = w.Element("goto", "http://Some-Url.com", ("url", "https://Or-Some-Other-Url.com"))) { }
                    using (var act = w.Element("cookieSave")) { }
                    using (var act = w.Element("cookieSave", ("name", "myCookie"), ("file", "C:\\myCookieValue.txt"))) { }
                    using (var act = w.Element("cookieSave", ("file", "C:\\allCookieValues.txt"))) { }


                }
                help.AddDetail(w.ToString());
            }


        }

        private string browserExecutableFile;
        private string browserDriverDirectory;
        private string browserDriverDownloadDirectory;
        private WebBrowserType? browserType;
        private string browserVersion;

        private WebBrowserLocation browserLocation;

        private string scriptFile;

        private External.WebBrowser browser;

        protected override void ExecuteInternal()
        {
            browserDriverDirectory = GetArgParameterOrConfig(nameof(browserDriverDirectory), "d");
            browserDriverDownloadDirectory = GetArgParameterOrConfig(nameof(browserDriverDownloadDirectory), "dd", External.WebBrowser.BrowserDriverDownloadDirectoryBaseDefault);
            browserVersion = GetArgParameterOrConfig(nameof(browserVersion), "v");

            browserType = GetArgParameterOrConfigEnum<WebBrowserType>(nameof(browserType), "t");
            browserExecutableFile = GetArgParameterOrConfig(nameof(browserExecutableFile), "b");

            if (browserExecutableFile == null && browserType == null)
            {
                browserLocation = WebBrowserLocation.FindBrowser();
                if (browserLocation == null) throw new CommandException($"Arguments [{nameof(browserType)}] and [{nameof(browserExecutableFile)}] cannot both be empty, specify one or the other");
            }
            else if (browserExecutableFile == null && browserType != null)
            {
                browserLocation = WebBrowserLocation.FindBrowser(browserType);
                if (browserLocation == null) throw new CommandException($"Could not locate browser executable for {browserType.Value}, please specify the {nameof(browserExecutableFile)} argument");
            }
            else if (browserExecutableFile != null && browserType == null)
            {
                browserLocation = new WebBrowserLocation(browserExecutableFile, browserType, Constant.OS, null);
            }
            else if (browserExecutableFile != null && browserType != null)
            {
                browserLocation = new WebBrowserLocation(browserExecutableFile, browserType, Constant.OS, null);
            }
            log.Debug(browserLocation.ToString());

            if (browserType == null) browserType = browserLocation.BrowserType;
            if (browserExecutableFile == null) browserExecutableFile = browserLocation.BrowserExecutable;

            scriptFile = GetArgValueFile(0, valueName: "XML script file");
            var scriptFileData = ReadFile(scriptFile);

            var root = XmlReader.Read(scriptFileData);
            if (!root.Name.EqualsCaseInsensitive("browser")) throw new Exception("No <browser> root element is defined");
            var children = root.Children;
            if (children.IsEmpty()) throw new Exception("No action elements defined");

            using (browser = new External.WebBrowser())
            {
                browser.BrowserLocation = browserLocation;
                browser.BrowserDriverDirectory = browserDriverDirectory;
                browser.BrowserDriverDownloadDirectoryBase = browserDriverDownloadDirectory;
                browser.BrowserVersion = browserVersion;

                browser.Start();

                int actionNum = 0;
                foreach (var element in children)
                {
                    actionNum++;
                    log.Debug($"Executing action {actionNum} {element.Name}");

                    if (element.Name.EqualsCaseInsensitive("sleep")) ActionSleep(element);
                    else if (element.Name.EqualsCaseInsensitive("text")) ActionText(element);
                    else if (element.Name.EqualsCaseInsensitive("click")) ActionClick(element);
                    else if (element.Name.EqualsCaseInsensitive("select")) ActionSelect(element);
                    else if (element.Name.EqualsCaseInsensitive("goto")) ActionGoTo(element);
                    else if (element.Name.EqualsCaseInsensitive("cookieSave")) ActionCookieSave(element);
                    else log.Warn($"Unknown action <{element.Name}>");
                }



            }


        }


        private void ActionSleep(XmlElement element)
        {
            var seconds = element["seconds"].TrimOrNull();
            if (seconds == null) throw ExceptionMissingAttribute(element, nameof(seconds));

            decimal msd = 1000;
            msd = msd * seconds.ToDecimal();
            int ms = (int)msd;
            log.Debug($"Sleeping for {ms} milliseconds");
            Thread.Sleep(ms);
        }

        private void ActionText(XmlElement element)
        {
            var s = ParseSearch(element);
            var t = element.Value.TrimOrNull() ?? element["value"].TrimOrNull();
            browser.SendKeys(s, t);
        }

        private void ActionSelect(XmlElement element)
        {
            var s = ParseSearch(element);
            var t = element.Value.TrimOrNull() ?? element["value"].TrimOrNull();
            browser.Select(s, t);
        }

        private void ActionClick(XmlElement element)
        {
            var s = ParseSearch(element);
            browser.Click(s);
        }

        private void ActionGoTo(XmlElement element)
        {
            var url = element.Value.TrimOrNull() ?? element["url"].TrimOrNull();
            if (url == null) throw new CommandException($"Action <{element.Name}> does not define attribute [{nameof(url)}] or have a Value defined");
            browser.GoTo(url);
        }

        private void ActionCookieSave(XmlElement element)
        {
            var name = element["name"].TrimOrNull();
            if (name == null) log.Debug($"Action <{element.Name}> does not define attribute [{nameof(name)}] so returning all cookies");
            else log.Debug($"Action <{element.Name}> defines attribute [{nameof(name)}] so looking for cookie named {name}");

            var file = element["file"].TrimOrNull();
            if (file == null) log.Debug($"Action <{element.Name}> does not define attribute [{nameof(file)}] so returning results to console");
            else log.Debug($"Action <{element.Name}> defines attribute [{nameof(file)}] so writing results to {file}");

            var cookies = browser.GetCookies();
            if (name == null)
            {
                var sb = new StringBuilder();
                foreach (var kvp in cookies.OrderBy(o => o.Key.ToLower()))
                {
                    sb.AppendLine(kvp.Key + "=" + kvp.Value);
                }
                if (file == null) log.Info(sb.ToString());
                else WriteFile(file, sb.ToString());
            }
            else
            {
                var val = cookies.GetValueCaseInsensitive(name);
                if (val == null) throw new CommandException($"Could not find cookie named [{name}]");

                if (file == null) log.Info(val);
                else WriteFile(file, val);
            }
        }

        private WebBrowserElementSearch ParseSearch(XmlElement element)
        {
            var s = new WebBrowserElementSearch
            {
                Id = element[nameof(WebBrowserElementSearch.Id)].TrimOrNull(),
                ClassName = element[nameof(WebBrowserElementSearch.ClassName)].TrimOrNull() ?? element["class"].TrimOrNull(),
                Name = element[nameof(WebBrowserElementSearch.Name)].TrimOrNull(),
                TagName = element[nameof(WebBrowserElementSearch.TagName)].TrimOrNull(),
                XPath = element[nameof(WebBrowserElementSearch.XPath)].TrimOrNull(),
                ValueEquals = element[nameof(WebBrowserElementSearch.ValueEquals)].TrimOrNull(),
                ValueContains = element[nameof(WebBrowserElementSearch.ValueContains)].TrimOrNull(),
            };

            if (s.Id == null && s.ClassName == null && s.Name == null && s.TagName == null && s.XPath == null && s.ValueEquals == null && s.ValueContains == null)
            {
                var l = new List<string>
                {
                    nameof(WebBrowserElementSearch.Id),
                    nameof(WebBrowserElementSearch.ClassName),
                    nameof(WebBrowserElementSearch.Name),
                    nameof(WebBrowserElementSearch.TagName),
                    nameof(WebBrowserElementSearch.XPath),
                    nameof(WebBrowserElementSearch.ValueEquals),
                    nameof(WebBrowserElementSearch.ValueContains),
                }.Select(o => "[" + o + "]").ToList();

                throw new CommandException($"Action <{element.Name}> requires search parameters, please specify " + l.ToStringDelimited(", "));
            }

            return s;
        }

        private CommandException ExceptionMissingAttribute(XmlElement element, string attributeName)
        {
            return new CommandException($"Action <{element.Name}> does not define required attribute [{attributeName}]");
        }

    }
}
