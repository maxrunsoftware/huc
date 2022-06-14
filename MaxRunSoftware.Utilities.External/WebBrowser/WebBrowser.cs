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
using System.IO;
using System.Linq;
using System.Reflection;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Support.UI;
using WebDriverManager;
using WebDriverManager.DriverConfigs;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

namespace MaxRunSoftware.Utilities.External;

public class WebBrowser : IDisposable
{
    public static OperatingSystem os;

    private static readonly ILogger log = Logging.LogFactory.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

    private readonly SingleUse isDisposed = new();

    public WebBrowserLocation Location { get; set; }
    public string DriverDirectory { get; set; }
    public string DriverDownloadDirectoryBase { get; set; }
    public static string DriverDownloadDirectoryBaseDefault { get; } = Path.Combine(Constant.CURRENT_EXE_DIRECTORY, "WebBrowserDrivers");
    public string Version { get; set; }
    public bool NativeEvents { get; set; } = true;

    public IList<string> OptionArguments { get; } = new List<string>
    {
        "ignore-certificate-errors",
        "disable-extensions",
        "headless",
        "disable-gpu",
        "silent",
        "silentOutput",
        "log-level=3",
        "no-sandbox"
    };

    public IList<string> OptionArgumentsExcluded { get; } = new List<string>
    {
        "enable-logging"
    };

    public WebDriver Browser { get; private set; }

    private IDriverConfig CreateDriverConfig(WebBrowserType type)
    {
        return type switch
        {
            WebBrowserType.Chrome => new ChromeConfig(),
            WebBrowserType.Edge => new EdgeConfig(),
            WebBrowserType.Firefox => new FirefoxConfig(),
            WebBrowserType.InternetExplorer => new InternetExplorerConfig(),
            WebBrowserType.Opera => new OperaConfig(),
            _ => throw new NotImplementedException("Unknown [BrowserType] " + type)
        };
    }

    private void SetupDriverManager()
    {
        // https://github.com/rosolko/WebDriverManager.Net/

        var driverConfig = CreateDriverConfig(Location.BrowserType);

        var browserVersion = Version;
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

        if (browserVersion == null)
        {
            throw new Exception("Unable to determine browser version for automatic driver download");
        }

        var browserArchitecture = Location.IsBrowser64Bit ? Architecture.X64 : Architecture.X32;

        var driverUrl = browserArchitecture == Architecture.X32 ? driverConfig.GetUrl32() : driverConfig.GetUrl64();
        driverUrl = UrlHelper.BuildUrl(driverUrl, browserVersion);

        DriverDownloadDirectoryBase ??= DriverDownloadDirectoryBaseDefault;

        //var binDestination = Path.Combine(currentDirectory, driverConfig.GetName(), browserVersion, browserArchitecture.ToString(), driverConfig.GetBinaryName());
        var browserDriverFile = Path.Combine(DriverDownloadDirectoryBase, driverConfig.GetName() + "_" + browserVersion + "_" + browserArchitecture, driverConfig.GetBinaryName());
        log.Debug("DriverManager binary location: " + browserDriverFile);


        var driverManager = new DriverManager();

        log.Debug($"DriverManager.SetUpDriver({driverUrl}, {browserDriverFile})");
        var driverManagerMsg = driverManager.SetUpDriver(driverUrl, browserDriverFile);
        log.Debug("DriverManager.SetUpDriver: " + driverManagerMsg);

        DriverDirectory = Path.GetDirectoryName(browserDriverFile);
    }

    public void Start()
    {
        if (Browser != null)
        {
            throw new Exception("Already Started");
        }

        log.Debug("Starting Browser");

        if (Location == null)
        {
            throw new Exception($"Property [{nameof(Location)}] is not defined");
        }

        log.Debug($"{nameof(Location)}: {Location}");
        if (!Location.IsExist)
        {
            throw new Exception($"Browser executable does not exist {Location.BrowserExecutable}");
        }

        if (DriverDirectory == null)
        {
            SetupDriverManager();
        }

        var optionArguments = OptionArguments.ToList();
        optionArguments.AddRange(optionArguments.Where(o => !o.StartsWith("-")).Select(o => "--" + o).ToList());

        var optionArgumentsExcluded = OptionArgumentsExcluded.ToList();

        var bt = Location.BrowserType;
        if (bt == WebBrowserType.Chrome)
        {
            var options = new ChromeOptions();
            options.BinaryLocation = Location.BrowserExecutable;
            foreach (var a in optionArguments)
            {
                options.AddArgument(a);
            }

            foreach (var a in optionArgumentsExcluded)
            {
                options.AddExcludedArgument(a);
            }

            var driverService = ChromeDriverService.CreateDefaultService(DriverDirectory);
            driverService.HideCommandPromptWindow = true;
            driverService.SuppressInitialDiagnosticInformation = true;
            driverService.EnableVerboseLogging = false;

            Browser = new ChromeDriver(driverService, options);
        }
        else if (bt == WebBrowserType.Edge)
        {
            var options = new EdgeOptions();
            options.BinaryLocation = Location.BrowserExecutable;
            foreach (var a in optionArguments)
            {
                options.AddArgument(a);
            }

            foreach (var a in optionArgumentsExcluded)
            {
                options.AddExcludedArgument(a);
            }

            var driverService = EdgeDriverService.CreateDefaultService(DriverDirectory);
            driverService.HideCommandPromptWindow = true;
            driverService.SuppressInitialDiagnosticInformation = true;
            driverService.EnableVerboseLogging = false;

            Browser = new EdgeDriver(driverService, options);
        }
        else if (bt == WebBrowserType.Firefox)
        {
            var options = new FirefoxOptions();
            options.BrowserExecutableLocation = Location.BrowserExecutable;
            foreach (var a in optionArguments)
            {
                options.AddArgument(a);
            }
            //foreach (var a in optionArgumentsExcluded) options.AddExcludedArgument(a);

            var driverService = FirefoxDriverService.CreateDefaultService(DriverDirectory);
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
            options.EnableNativeEvents = NativeEvents;

            var driverService = InternetExplorerDriverService.CreateDefaultService(DriverDirectory);
            driverService.HideCommandPromptWindow = true;
            driverService.SuppressInitialDiagnosticInformation = true;
            //driverService.EnableVerboseLogging = false;

            Browser = new InternetExplorerDriver(driverService, options);
        }
        else if (bt == WebBrowserType.Opera)
        {
            //var options = new OperaOptions();
            var options = new ChromeOptions();
            options.BinaryLocation = Location.BrowserExecutable;
            foreach (var a in optionArguments)
            {
                options.AddArgument(a);
            }

            foreach (var a in optionArgumentsExcluded)
            {
                options.AddExcludedArgument(a);
            }

            //var driverService = OperaDriverService.CreateDefaultService(DriverDirectory);
            var driverService = ChromeDriverService.CreateDefaultService(DriverDirectory);
            driverService.HideCommandPromptWindow = true;
            driverService.SuppressInitialDiagnosticInformation = true;
            driverService.EnableVerboseLogging = false;

            // Browser = new OperaDriver(driverService, options);
            Browser = new ChromeDriver(driverService, options);

            options.BinaryLocation = Location.BrowserExecutable;
            foreach (var a in optionArguments)
            {
                options.AddArgument(a);
            }

            foreach (var a in optionArgumentsExcluded)
            {
                options.AddExcludedArgument(a);
            }

            driverService.HideCommandPromptWindow = true;
            driverService.SuppressInitialDiagnosticInformation = true;
            driverService.EnableVerboseLogging = false;
        }
        else
        {
            throw new NotImplementedException("Unknown [BrowserType] " + bt);
        }

        Browser.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
        Browser.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(60);
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
        if (names.Length > 1 && !se.IsMultiple)
        {
            throw new Exception("Select element does not allow multiple selections but trying to select multiple values: " + names.ToStringDelimited(", "));
        }

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

        var cookieCount = 0;
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
        if (elements.IsEmpty())
        {
            throw new Exception("No element found matching " + search);
        }

        if (elements.Count > 1)
        {
            throw new Exception($"Multiple elements ({elements.Count}) found matching " + search);
        }

        return elements[0];
    }


    public void Dispose()
    {
        if (!isDisposed.TryUse())
        {
            return;
        }

        var b = Browser;
        Browser = null;

        if (b == null)
        {
            return;
        }

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
