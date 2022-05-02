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
using System.Diagnostics;
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
    public class WebBrowser : IDisposable
    {
        public static OperatingSystem os;

        private static readonly ILogger log = Logging.LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private SingleUse isDisposed = new();

        public WebBrowserLocation BrowserLocation { get; set; }
        public string BrowserDriverDirectory { get; set; }
        public string BrowserDriverDownloadDirectoryBase { get; set; }
        public static string BrowserDriverDownloadDirectoryBaseDefault { get; } = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(Constant.CURRENT_EXE)), "WebBrowserDrivers");
        public string BrowserVersion { get; set; }

        public WebDriver Browser { get; private set; }

        private IDriverConfig CreateDriverConfig(WebBrowserType type) => type switch
        {
            WebBrowserType.Chrome => new ChromeConfig(),
            WebBrowserType.Edge => new EdgeConfig(),
            WebBrowserType.Firefox => new FirefoxConfig(),
            WebBrowserType.InternetExplorer => new InternetExplorerConfig(),
            WebBrowserType.Opera => new OperaConfig(),
            _ => throw new NotImplementedException("Unknown [BrowserType] " + type),
        };

        private void SetupDriverManager()
        {
            // https://github.com/rosolko/WebDriverManager.Net/

            var driverConfig = CreateDriverConfig(BrowserLocation.BrowserType);

            var browserVersion = BrowserVersion;
            if (browserVersion == null)
            {
                try
                {
                    browserVersion = driverConfig.GetMatchingBrowserVersion();
                }
                catch (Exception) { }
            }
            if (browserVersion == null)
            {
                try
                {
                    browserVersion = driverConfig.GetLatestVersion();
                }
                catch (Exception) { }
            }
            if (browserVersion == null) throw new Exception("Unable to determine browser version for automatic driver download");

            var browserArchitecture = BrowserLocation.IsBrowser64Bit ? Architecture.X64 : Architecture.X32;

            var driverUrl = browserArchitecture == Architecture.X32 ? driverConfig.GetUrl32() : driverConfig.GetUrl64();
            driverUrl = UrlHelper.BuildUrl(driverUrl, browserVersion);

            if (BrowserDriverDownloadDirectoryBase == null) BrowserDriverDownloadDirectoryBase = BrowserDriverDownloadDirectoryBaseDefault;

            //var binDestination = Path.Combine(currentDirectory, driverConfig.GetName(), browserVersion, browserArchitecture.ToString(), driverConfig.GetBinaryName());
            var browserDriverFile = Path.Combine(BrowserDriverDownloadDirectoryBase, driverConfig.GetName() + "_" + browserVersion + "_" + browserArchitecture.ToString(), driverConfig.GetBinaryName());
            log.Debug($"DriverManager binary location: " + browserDriverFile);


            var driverManager = new DriverManager();

            log.Debug($"DriverManager.SetUpDriver({driverUrl}, {browserDriverFile})");
            var driverManagerMsg = driverManager.SetUpDriver(driverUrl, browserDriverFile);
            log.Debug("DriverManager.SetUpDriver: " + driverManagerMsg);

            BrowserDriverDirectory = Path.GetDirectoryName(browserDriverFile);
        }

        public void Start()
        {
            if (Browser != null) throw new Exception("Already Started");

            log.Debug("Starting Browser");

            if (BrowserLocation == null) throw new Exception($"Property [{nameof(BrowserLocation)}] is not defined");

            log.Debug($"{nameof(BrowserLocation)}: {BrowserLocation}");
            if (!BrowserLocation.IsExist) throw new Exception($"Browser executable does not exist {BrowserLocation.BrowserExecutable}");

            if (BrowserDriverDirectory == null) SetupDriverManager();

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

            var bt = BrowserLocation.BrowserType;
            if (bt == WebBrowserType.Chrome)
            {
                var options = new ChromeOptions();
                options.BinaryLocation = BrowserLocation.BrowserExecutable;
                foreach (var a in optionArguments) options.AddArgument(a);
                foreach (var a in optionArgumentsExcluded) options.AddExcludedArgument(a);

                var driverService = ChromeDriverService.CreateDefaultService(BrowserDriverDirectory);
                driverService.HideCommandPromptWindow = true;
                driverService.SuppressInitialDiagnosticInformation = true;
                driverService.EnableVerboseLogging = false;

                Browser = new ChromeDriver(driverService, options);
            }
            else if (bt == WebBrowserType.Edge)
            {
                var options = new EdgeOptions();
                options.BinaryLocation = BrowserLocation.BrowserExecutable;
                foreach (var a in optionArguments) options.AddArgument(a);
                foreach (var a in optionArgumentsExcluded) options.AddExcludedArgument(a);

                var driverService = EdgeDriverService.CreateDefaultService(BrowserDriverDirectory);
                driverService.HideCommandPromptWindow = true;
                driverService.SuppressInitialDiagnosticInformation = true;
                driverService.EnableVerboseLogging = false;

                Browser = new EdgeDriver(driverService, options);
            }
            else if (bt == WebBrowserType.Firefox)
            {
                var options = new FirefoxOptions();
                options.BrowserExecutableLocation = BrowserLocation.BrowserExecutable;
                foreach (var a in optionArguments) options.AddArgument(a);
                //foreach (var a in optionArgumentsExcluded) options.AddExcludedArgument(a);

                var driverService = FirefoxDriverService.CreateDefaultService(BrowserDriverDirectory);
                driverService.HideCommandPromptWindow = true;
                driverService.SuppressInitialDiagnosticInformation = true;
                //driverService.EnableVerboseLogging = false;

                Browser = new FirefoxDriver(driverService, options);
            }
            else if (bt == WebBrowserType.InternetExplorer)
            {
                var options = new InternetExplorerOptions();
                //options.BinaryLocation = BrowserExecutableFilePath;
                //foreach (var a in optionArguments) options.AddArgument(a);
                //foreach (var a in optionArgumentsExcluded) options.AddExcludedArgument(a);

                var driverService = InternetExplorerDriverService.CreateDefaultService(BrowserDriverDirectory);
                driverService.HideCommandPromptWindow = true;
                driverService.SuppressInitialDiagnosticInformation = true;
                //driverService.EnableVerboseLogging = false;

                Browser = new InternetExplorerDriver(driverService, options);
            }
            else if (bt == WebBrowserType.Opera)
            {
                var options = new OperaOptions();
                options.BinaryLocation = BrowserLocation.BrowserExecutable;
                foreach (var a in optionArguments) options.AddArgument(a);
                foreach (var a in optionArgumentsExcluded) options.AddExcludedArgument(a);

                var driverService = OperaDriverService.CreateDefaultService(BrowserDriverDirectory);
                driverService.HideCommandPromptWindow = true;
                driverService.SuppressInitialDiagnosticInformation = true;
                driverService.EnableVerboseLogging = false;

                Browser = new OperaDriver(driverService, options);
            }
            else
            {
                throw new NotImplementedException("Unknown [BrowserType] " + bt);
            }
        }

        public void GoTo(string url)
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

        public IDictionary<string, string> GetCookies()
        {
            var d = new Dictionary<string, string>();

            int cookieCount = 0;
            foreach (var c in Browser.Manage().Cookies.AllCookies)
            {
                log.Trace($"Cookie[{cookieCount}]  " + c);
                d[c.Name] = c.Value;
                cookieCount++;
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
