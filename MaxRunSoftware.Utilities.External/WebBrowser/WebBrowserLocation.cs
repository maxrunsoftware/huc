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
using System.Reflection;
using System.Runtime.InteropServices;

namespace MaxRunSoftware.Utilities.External;

public class WebBrowserLocation
{
    private static readonly ILogger log = Logging.LogFactory.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

    public WebBrowserType BrowserType { get; }
    public OSPlatform BrowserOS { get; }
    public string BrowserExecutable { get; }
    public bool IsBrowser64Bit { get; }

    public bool IsExist
    {
        get
        {
            try { return File.Exists(BrowserExecutable); }
            catch (Exception e) { log.Debug("Error on File.Exists(" + BrowserExecutable + ")", e); }

            return false;
        }
    }

    public WebBrowserLocation(string browserExecutable, WebBrowserType? browserType, OSPlatform? browserOS, bool? isBrowser64Bit)
    {
        BrowserExecutable = browserExecutable.CheckNotNullTrimmed(nameof(browserExecutable));
        if (browserType == null)
        {
            var name = Path.GetFileName(BrowserExecutable)!.ToLower();
            if (name.ContainsAny("chrome")) { browserType = WebBrowserType.Chrome; }
            else if (name.ContainsAny("edge")) { browserType = WebBrowserType.Edge; }
            else if (name.ContainsAny("firefox")) { browserType = WebBrowserType.Firefox; }
            else if (name.ContainsAny("opera")) { browserType = WebBrowserType.Opera; }
            else if (name.ContainsAny("explorer", "ie", "iexplore")) browserType = WebBrowserType.InternetExplorer;
        }

        if (browserType == null) throw new Exception("Could not determine browser type from executable " + BrowserExecutable);

        BrowserType = browserType.Value;

        BrowserOS = browserOS ?? Constant.OS;
        if (isBrowser64Bit == null)
        {
            if (BrowserOS == OSPlatform.Windows && BrowserExecutable!.ToLower().Contains("\\program files\\")) { isBrowser64Bit = true; }
            else if (BrowserOS == OSPlatform.Windows && BrowserExecutable!.ToLower().Contains("\\program files (x86)\\")) isBrowser64Bit = false;
        }

        IsBrowser64Bit = isBrowser64Bit ?? Constant.OS_X64;
    }

    public static IReadOnlyList<WebBrowserLocation> DefaultLocations { get; } = new List<WebBrowserLocation>
    {
        new("C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe", WebBrowserType.Chrome, OSPlatform.Windows, true),
        new("C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe", WebBrowserType.Chrome, OSPlatform.Windows, false),
        new("/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", WebBrowserType.Chrome, OSPlatform.OSX, true),
        new("/usr/bin/google-chrome", WebBrowserType.Chrome, OSPlatform.Linux, true),

        new("C:\\Program Files\\Mozilla Firefox\\firefox.exe", WebBrowserType.Firefox, OSPlatform.Windows, true),
        new("C:\\Program Files (x86)\\Mozilla Firefox\\firefox.exe", WebBrowserType.Firefox, OSPlatform.Windows, false),

        new("C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe", WebBrowserType.Edge, OSPlatform.Windows, true),
        new("C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe", WebBrowserType.Edge, OSPlatform.Windows, false),

        new("C:\\Program Files\\Internet Explorer\\iexplore.exe", WebBrowserType.InternetExplorer, OSPlatform.Windows, true),
        new("C:\\Program Files (x86)\\Internet Explorer\\iexplore.exe", WebBrowserType.InternetExplorer, OSPlatform.Windows, false)
    }.AsReadOnly();

    public static WebBrowserLocation FindBrowser(WebBrowserType? browserType = null)
    {
        foreach (var loc in DefaultLocations)
        {
            if (loc.BrowserOS != Constant.OS) continue;

            if (browserType != null && loc.BrowserType != browserType.Value) continue;

            if (!loc.IsExist) continue;

            return loc;
        }

        return null;
    }

    public WebBrowserLocation ChangeArchitecture(bool? isBrowser64Bit = null)
    {
        isBrowser64Bit ??= !IsBrowser64Bit;
        return new WebBrowserLocation(BrowserExecutable, BrowserType, BrowserOS, isBrowser64Bit);
    }

    public override string ToString() => this.ToStringGenerated();
}
