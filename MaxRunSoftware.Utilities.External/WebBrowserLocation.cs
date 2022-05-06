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
using System.Runtime.InteropServices;
using System.Text;

namespace MaxRunSoftware.Utilities.External
{
    public class WebBrowserLocation
    {
        public WebBrowserType BrowserType { get; }
        public OSPlatform BrowserOS { get; }
        public string BrowserExecutable { get; }
        public bool IsBrowser64Bit { get; }
        public bool IsExist => File.Exists(BrowserExecutable);

        public WebBrowserLocation(string browserExecutable, WebBrowserType? browserType, OSPlatform? browserOS, bool? isBrowser64Bit)
        {
            BrowserExecutable = browserExecutable.CheckNotNullTrimmed(nameof(browserExecutable));
            if (browserType == null)
            {
                // Chrome, Edge, Firefox, InternetExplorer, Opera 
                var name = Path.GetFileName(BrowserExecutable).ToLower();
                if (name.Contains("chrome")) browserType = WebBrowserType.Chrome;
                else if (name.Contains("edge")) browserType = WebBrowserType.Edge;
                else if (name.Contains("firefox")) browserType = WebBrowserType.Firefox;
                else if (name.Contains("opera")) browserType = WebBrowserType.Opera;
                else if (name.Contains("explorer")) browserType = WebBrowserType.InternetExplorer;
                else if (name.Contains("ie")) browserType = WebBrowserType.InternetExplorer;
                else if (name.Contains("iexplore")) browserType = WebBrowserType.InternetExplorer;
            }
            if (browserType == null) throw new Exception("Could not determine browser type from executable " + BrowserExecutable);
            BrowserType = browserType.Value;

            BrowserOS = browserOS == null ? Constant.OS : browserOS.Value;
            if (isBrowser64Bit == null)
            {
                if (BrowserOS == OSPlatform.Windows && BrowserExecutable.ToLower().Contains("\\program files\\")) isBrowser64Bit = true;
                else if (BrowserOS == OSPlatform.Windows && BrowserExecutable.ToLower().Contains("\\program files (x86)\\")) isBrowser64Bit = false;
            }
            IsBrowser64Bit = isBrowser64Bit ?? Constant.OS_X64;
        }

        public static IReadOnlyList<WebBrowserLocation> DEFAULT_LOCATIONS { get; } = new List<WebBrowserLocation>()
        {
            new WebBrowserLocation("C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe", browserType: WebBrowserType.Chrome, browserOS: OSPlatform.Windows, isBrowser64Bit: true),
            new WebBrowserLocation("C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe", browserType: WebBrowserType.Chrome, browserOS: OSPlatform.Windows, isBrowser64Bit: false),
            new WebBrowserLocation("/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", browserType: WebBrowserType.Chrome, browserOS: OSPlatform.OSX, isBrowser64Bit: true),
            new WebBrowserLocation("/usr/bin/google-chrome", browserType: WebBrowserType.Chrome, browserOS: OSPlatform.Linux, isBrowser64Bit: true),

            new WebBrowserLocation("C:\\Program Files\\Mozilla Firefox\\firefox.exe", browserType: WebBrowserType.Firefox, browserOS: OSPlatform.Windows, isBrowser64Bit: true),
            new WebBrowserLocation("C:\\Program Files (x86)\\Mozilla Firefox\\firefox.exe", browserType: WebBrowserType.Firefox, browserOS: OSPlatform.Windows, isBrowser64Bit: false),

            new WebBrowserLocation("C:\\Program Files\\Internet Explorer\\iexplore.exe", browserType: WebBrowserType.InternetExplorer, browserOS: OSPlatform.Windows, isBrowser64Bit: true),
            new WebBrowserLocation("C:\\Program Files (x86)\\Internet Explorer\\iexplore.exe", browserType: WebBrowserType.InternetExplorer, browserOS: OSPlatform.Windows, isBrowser64Bit: false),



        }.AsReadOnly();

        public static WebBrowserLocation FindBrowser(WebBrowserType? browserType = null)
        {
            foreach (var loc in DEFAULT_LOCATIONS)
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
            if (isBrowser64Bit == null) isBrowser64Bit = !IsBrowser64Bit;
            return new WebBrowserLocation(BrowserExecutable, BrowserType, BrowserOS, isBrowser64Bit);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(GetType().Name + "(");
            sb.Append(nameof(BrowserType) + "=" + BrowserType + ", ");
            sb.Append(nameof(BrowserOS) + "=" + BrowserOS + ", ");
            sb.Append(nameof(IsBrowser64Bit) + "=" + IsBrowser64Bit + ", ");
            sb.Append(nameof(IsExist) + "=" + IsExist + ", ");
            sb.Append(nameof(BrowserExecutable) + "=" + BrowserExecutable);
            sb.Append(")");
            return sb.ToString();
        }
    }
}
