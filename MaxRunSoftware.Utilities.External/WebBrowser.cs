// /*
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
// */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Opera;
using OpenQA.Selenium.Support.UI;
using WebDriverManager;
using WebDriverManager.DriverConfigs;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

namespace MaxRunSoftware.Utilities.External
{
    public enum WebBrowserType { Chrome, Edge, Firefox, InternetExplorer, Opera }
    public enum WebBrowserArchitecture { X32, X64 }

    public class WebBrowser : IDisposable
    {
        private static readonly ILogger log = Logging.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private SingleUse isDisposed = new();

        public string BrowserExecutableFilePath { get; set; }
        public string BrowserDriverFilePath { get; private set; }
        public WebBrowserType? BrowserType { get; set; }
        public WebBrowserArchitecture? BrowserArchitecture { get; set; }
        public string BrowserVersion { get; set; }

        public WebDriver Browser { get; private set; }

        public WebBrowser()
        {
            // https://github.com/rosolko/WebDriverManager.Net/
        }

        public void Start()
        {
            if (Browser != null) throw new Exception("Already Started");

            log.Debug("Starting Browser");

            if (BrowserExecutableFilePath == null) throw new Exception($"Property [{nameof(BrowserExecutableFilePath)}] is not defined");
            if (BrowserType == null) throw new Exception($"Property [{nameof(BrowserType)}] is not defined");
            //if (BrowserArchitecture == null) throw new Exception($"Property [{nameof(BrowserArchitecture)}] is not defined");

            var browserType = BrowserType.Value;
            var browserVersion = BrowserVersion;
            var browserArchitecture = Architecture.Auto;
            if (BrowserArchitecture != null)
            {
                if (BrowserArchitecture.Value == WebBrowserArchitecture.X32) browserArchitecture = Architecture.X32;
                else browserArchitecture = Architecture.X64;
            }

            if (browserVersion == null)
            {
                if (browserType == WebBrowserType.Chrome) browserVersion = VersionResolveStrategy.MatchingBrowser;
                else browserVersion = VersionResolveStrategy.Latest;
            }

            var driverManager = new DriverManager();
            IDriverConfig driverConfig;
            if (browserType == WebBrowserType.Chrome) driverConfig = new ChromeConfig();
            else if (browserType == WebBrowserType.Edge) driverConfig = new EdgeConfig();
            else if (browserType == WebBrowserType.Firefox) driverConfig = new FirefoxConfig();
            else if (browserType == WebBrowserType.InternetExplorer) driverConfig = new InternetExplorerConfig();
            else if (browserType == WebBrowserType.Opera) driverConfig = new OperaConfig();
            else throw new NotImplementedException("Unknown [BrowserType] " + browserType);

            if (BrowserDriverFilePath == null)
            {
                log.Debug($"Setting up DriverManager({driverConfig.GetType().Name}, {browserVersion}, {BrowserArchitecture}");
                var driverManagerMsg = driverManager.SetUpDriver(driverConfig, version: browserVersion, architecture: browserArchitecture);
                log.Debug("DriverManager.SetUpDriver: " + driverManagerMsg);
            }
            var browserDriverFilePath = BrowserDriverFilePath ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);


            var optionArguments = new List<string>
            {
                "--ignore-certificate-errors",
                "--disable-extensions",
                "headless",
                "--headless",
                "disable-gpu",
                "silent",
                "--silent",
                "--silentOutput",
                "log-level=3",
                "--log-level=3"
            };
            var optionArgumentsExcluded = new List<string>
            {
                "enable-logging"
            };


            if (browserType == WebBrowserType.Chrome)
            {
                var options = new ChromeOptions();
                options.BinaryLocation = BrowserExecutableFilePath;
                foreach (var a in optionArguments) options.AddArgument(a);
                foreach (var a in optionArgumentsExcluded) options.AddExcludedArgument(a);

                var driverService = ChromeDriverService.CreateDefaultService(browserDriverFilePath);
                driverService.HideCommandPromptWindow = true;
                driverService.SuppressInitialDiagnosticInformation = true;
                driverService.EnableVerboseLogging = false;

                Browser = new ChromeDriver(driverService, options);
            }
            else if (browserType == WebBrowserType.Edge)
            {
                var options = new EdgeOptions();
                options.BinaryLocation = BrowserExecutableFilePath;
                foreach (var a in optionArguments) options.AddArgument(a);
                foreach (var a in optionArgumentsExcluded) options.AddExcludedArgument(a);

                var driverService = EdgeDriverService.CreateDefaultService(browserDriverFilePath);
                driverService.HideCommandPromptWindow = true;
                driverService.SuppressInitialDiagnosticInformation = true;
                driverService.EnableVerboseLogging = false;

                Browser = new EdgeDriver(driverService, options);
            }
            else if (browserType == WebBrowserType.Firefox)
            {
                var options = new FirefoxOptions();
                options.BrowserExecutableLocation = BrowserExecutableFilePath;
                foreach (var a in optionArguments) options.AddArgument(a);
                //foreach (var a in optionArgumentsExcluded) options.AddExcludedArgument(a);

                var driverService = FirefoxDriverService.CreateDefaultService(browserDriverFilePath);
                driverService.HideCommandPromptWindow = true;
                driverService.SuppressInitialDiagnosticInformation = true;
                //driverService.EnableVerboseLogging = false;

                Browser = new FirefoxDriver(driverService, options);
            }
            else if (browserType == WebBrowserType.InternetExplorer)
            {
                var options = new InternetExplorerOptions();
                //options.BinaryLocation = BrowserExecutableFilePath;
                //foreach (var a in optionArguments) options.AddArgument(a);
                //foreach (var a in optionArgumentsExcluded) options.AddExcludedArgument(a);

                var driverService = InternetExplorerDriverService.CreateDefaultService(browserDriverFilePath);
                driverService.HideCommandPromptWindow = true;
                driverService.SuppressInitialDiagnosticInformation = true;
                //driverService.EnableVerboseLogging = false;

                Browser = new InternetExplorerDriver(driverService, options);
            }
            else if (browserType == WebBrowserType.Opera)
            {
                var options = new OperaOptions();
                options.BinaryLocation = BrowserExecutableFilePath;
                foreach (var a in optionArguments) options.AddArgument(a);
                foreach (var a in optionArgumentsExcluded) options.AddExcludedArgument(a);

                var driverService = OperaDriverService.CreateDefaultService(browserDriverFilePath);
                driverService.HideCommandPromptWindow = true;
                driverService.SuppressInitialDiagnosticInformation = true;
                driverService.EnableVerboseLogging = false;

                Browser = new OperaDriver(driverService, options);
            }
            else
            {
                throw new NotImplementedException("Unknown [BrowserType] " + browserType);
            }
        }

        public void GoToUrl(string url)
        {
            Browser.Navigate().GoToUrl(url);
        }

        public void SendKeys(WebBrowserElementSearch search, string text)
        {
            SearchRequiredOne(search).SendKeys(text);
        }

        public void Click(WebBrowserElementSearch search)
        {
            SearchRequiredOne(search).Click();
        }

        public void Select(WebBrowserElementSearch search, params string[] names)
        {
            var element = SearchRequiredOne(search);
            var se = new SelectElement(element);
            if (names.Length > 1 && !se.IsMultiple) throw new Exception("Select element does not allow multiple selections but trying to select multiple values: " + names.ToStringDelimited(", "));

            foreach (var name in names)
            {
                try
                {
                    se.SelectByValue(name);
                }
                catch (NoSuchElementException)
                {
                    se.SelectByText(name);
                }
            }
        }

        public IDictionary<string, string> GetCookies(IEqualityComparer<string> comparer = null)
        {
            var d = comparer == null ? new Dictionary<string, string>() : new Dictionary<string, string>(comparer);

            foreach (var c in Browser.Manage().Cookies.AllCookies)
            {
                d[c.Name] = c.Value;
            }

            return d;
        }

        private IWebElement SearchRequiredOne(WebBrowserElementSearch search)
        {
            var elements = search.FindElements(Browser);
            if (elements.IsEmpty()) throw new Exception("No element found matching " + search.ToString());
            if (elements.Count > 1) throw new Exception($"Multiple elements ({elements.Count}) found matching " + search.ToString());
            return elements[0];
        }


        public void Dispose()
        {
            if (!isDisposed.TryUse()) return;

            var b = Browser;
            Browser = null;

            if (b == null) return;
            try
            {
                b.Quit();
            }
            catch (Exception e)
            {
                log.Warn($"Error calling {b.GetType().Name}.{nameof(WebDriver.Quit)}()", e);
            }

            b.DisposeSafely(log.Warn);
        }
    }
}
