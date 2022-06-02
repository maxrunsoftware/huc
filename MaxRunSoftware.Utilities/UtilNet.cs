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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;

namespace MaxRunSoftware.Utilities
{
    public static partial class Util
    {
        /// <summary>
        /// Attempts to get the current local time from the internet
        /// </summary>
        /// <returns>The current local time</returns>
        public static DateTime NetGetInternetDateTime()
        {
            // http://stackoverflow.com/questions/6435099/how-to-get-datetime-from-the-internet

            ServicePointManager.SecurityProtocol = (SecurityProtocolType)(0xc0 | 0x300 | 0xc00);
            // SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

#pragma warning disable SYSLIB0014 // Type or member is obsolete
            var request = (HttpWebRequest)WebRequest.Create("http://worldtimeapi.org/api/timezone/Europe/London.txt");
#pragma warning restore SYSLIB0014 // Type or member is obsolete
            request.Method = "GET";
            request.Accept = "text/html, application/xhtml+xml, */*";
            request.UserAgent = "p2pcopy";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore); //No caching
            var response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode != HttpStatusCode.OK) throw new WebException(response.StatusCode + ":" + response.StatusDescription);
            var stream = new StreamReader(response.GetResponseStream());
            var html = stream.ReadToEnd(); //<timestamp time=\"1395772696469995\" delay=\"1395772696469995\"/>
            var time = Regex.Match(html, @"(?<=unixtime: )[^u]*").Value;
            var milliseconds = Convert.ToInt64(time) * 1000.0;
            var dateTime = new DateTime(1970, 1, 1).AddMilliseconds(milliseconds).ToLocalTime();

            return dateTime;
        }

        /// <summary>
        /// Get all of the IP addresses on the current machine
        /// </summary>
        /// <returns>The IP addresses</returns>
        public static IEnumerable<IPAddress> NetGetIPAddresses() => NetworkInterface.GetAllNetworkInterfaces()
                .Where(o => o.OperationalStatus == OperationalStatus.Up)
                .Select(o => o.GetIPProperties())
                .WhereNotNull()
                .SelectMany(o => o.UnicastAddresses)
                .Select(o => o.Address)
                .WhereNotNull();

        private static bool[] NetGetPortStatusInternal()
        {
            var array = new bool[65536]; // array[0] should not ever be populated
            array.Populate(true);

            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

            foreach (var tcpConnectionInformation in ipGlobalProperties.GetActiveTcpConnections())
            {
                array[tcpConnectionInformation.LocalEndPoint.Port] = false;
            }

            foreach (var ipEndPoint in ipGlobalProperties.GetActiveTcpListeners())
            {
                array[ipEndPoint.Port] = false;
            }

            return array;
        }

        public static IEnumerable<(int port, bool isOpen)> NetGetPortStatus()
        {
            var portStatus = NetGetPortStatusInternal();
            for (int i = 1; i < portStatus.Length; i++)
            {
                yield return (i, portStatus[i]);
            }
        }

        public static IEnumerable<int> NetGetOpenPorts() => NetGetPortStatus().Where(o => o.isOpen).Select(o => o.port);

        public static IEnumerable<int> NetGetClosedPorts() => NetGetPortStatus().Where(o => !o.isOpen).Select(o => o.port);

        public static bool NetIsPortAvailable(int port) => NetGetPortStatus().Where(o => o.port == port).Select(o => o.isOpen).FirstOrDefault();

        /// <summary>
        /// Tries to find an open port in a range or if none is found a -1 is returned
        /// </summary>
        /// <param name="startInclusive">The inclusive starting port to search for an open port</param>
        /// <param name="endInclusive">The inclusive ending port to search for an open port</param>
        /// <returns>An open port or -1 if none were found</returns>
        public static int NetFindOpenPort(int startInclusive, int endInclusive = 65535)
        {
            if (startInclusive < 1) startInclusive = 1;
            if (endInclusive > 65535) endInclusive = 65535;
            foreach (var portStatus in NetGetPortStatus())
            {
                if (portStatus.port < startInclusive) continue;
                if (portStatus.port > endInclusive) continue;
                if (!portStatus.isOpen) continue;
                return portStatus.port;
            }
            return -1;
        }

        #region Web

        public static string WebParseFilename(string url)
        {
            var outputFile = url.Split('/').TrimOrNull().WhereNotNull().LastOrDefault();
            outputFile = FilenameSanitize(outputFile, "_");
            return outputFile;
        }

        public sealed class WebResponse
        {
            public string Url { get; }
            public byte[] Data { get; }
            public IDictionary<string, List<string>> Headers { get; }
            public string ContentType
            {
                get
                {
                    if (Headers.TryGetValue("Content-Type", out var list))
                    {
                        if (list.Count > 0)
                        {
                            return list[0];
                        }
                    }

                    return null;
                }
            }

            public WebResponse(string url, byte[] data, WebHeaderCollection headers)
            {
                this.Url = url;
                Data = data;
                var d = new SortedDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headers.Count; i++)
                {
                    var key = headers.GetKey(i).TrimOrNull();
                    var val = headers.Get(i);
                    if (key != null && val.TrimOrNull() != null)
                    {
                        d.AddToList(key, val);
                    }
                }

                Headers = d;
            }

            public override string ToString()
            {
                return GetType().Name + $"[{Url}] Headers:{Headers.Count} Data:" + (Data == null ? "null" : Data.Length);
            }
            public string ToStringDetail()
            {
                var sb = new StringBuilder();
                sb.AppendLine(GetType().Name);
                sb.AppendLine("\tUrl: " + Url);
                sb.AppendLine("\tData: " + (Data == null ? "null" : Data.Length));
                foreach (var kvp in Headers)
                {
                    var key = kvp.Key;
                    var valList = kvp.Value;
                    if (key == null) continue;
                    if (valList == null) continue;
                    if (valList.IsEmpty()) continue;
                    if (valList.Count == 1)
                    {
                        sb.AppendLine("\t" + key + ": " + valList[0]);
                    }
                    else
                    {
                        for (int i = 0; i < valList.Count; i++)
                        {
                            sb.AppendLine("\t" + key + "[" + i + "]: " + valList[i]);
                        }
                    }
                }

                return sb.ToString().TrimOrNull(); // remove trailing newline
            }
        }

        public static WebResponse WebDownload(string url, string outFilename = null, string username = null, string password = null, IDictionary<string, string> cookies = null)
        {
#pragma warning disable SYSLIB0014 // Type or member is obsolete
            var cli = new WebClient();
#pragma warning restore SYSLIB0014 // Type or member is obsolete

            using (cli)
            {
                // https://stackoverflow.com/a/7861726
                var sb = new StringBuilder();
                foreach (var kvp in cookies)
                {
                    var name = kvp.Key.TrimOrNull();
                    var val = kvp.Value.TrimOrNull();
                    if (name == null || val == null) continue;
                    if (sb.Length > 0) sb.Append(";");
                    sb.Append(kvp.Key + "=" + kvp.Value);
                }
                cli.Headers.Add(HttpRequestHeader.Cookie, sb.ToString());

                if (outFilename != null)
                {
                    if (outFilename.ContainsAny(Path.DirectorySeparatorChar.ToString(), Path.AltDirectorySeparatorChar.ToString()))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(outFilename));
                    }
                }

                bool unauthorized = false;
                try
                {
                    byte[] data = null;
                    if (outFilename == null)
                    {
                        data = cli.DownloadData(url);
                    }
                    else
                    {
                        cli.DownloadFile(url, outFilename);
                    }

                    return new WebResponse(url, data, cli.ResponseHeaders);
                }
                catch (WebException we)
                {
                    if (username != null && password != null && we.Message != null && we.Message.Contains("(401)"))
                    {
                        unauthorized = true;
                    }
                    else
                    {
                        throw;
                    }

                }
                if (unauthorized)
                {
                    // https://stackoverflow.com/a/26016919
                    string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                    cli.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", credentials);
                    byte[] data = null;
                    if (outFilename == null)
                    {
                        data = cli.DownloadData(url);
                    }
                    else
                    {
                        cli.DownloadFile(url, outFilename);
                    }

                    return new WebResponse(url, data, cli.ResponseHeaders);
                }

                throw new NotImplementedException("Should not happen");
            }
        }

        #endregion Web

        public static IEnumerable<Tuple<string, string>> ParseDistinguishedName(this string value)
        {
            // https://stackoverflow.com/a/53337546

            const int COMPONENT = 1;
            const int QUOTED_STRING = 2;
            const int ESCAPED_CHARACTER = 3;

            var previousState = COMPONENT;
            var currentState = COMPONENT;
            var currentComponent = new StringBuilder();
            var previousChar = char.MinValue;
            var position = 0;

            Tuple<string, string> parseComponent(StringBuilder sb)
            {
                var s = sb.ToString();
                sb.Clear();

                var index = s.IndexOf('=');
                if (index == -1)
                {
                    return null;
                }

                var item1 = s.Substring(0, index).Trim().ToUpper();
                var item2 = s.Substring(index + 1).Trim();

                return Tuple.Create(item1, item2);
            }

            while (position < value.Length)
            {
                var currentChar = value[position];

                switch (currentState)
                {
                    case COMPONENT:
                        switch (currentChar)
                        {
                            case ',':
                            case ';':
                                // Separator found, yield parsed component
                                var component = parseComponent(currentComponent);
                                if (component != null)
                                {
                                    yield return component;
                                }
                                break;

                            case '\\':
                                // Escape character found
                                previousState = currentState;
                                currentState = ESCAPED_CHARACTER;
                                break;

                            case '"':
                                // Quotation mark found
                                if (previousChar == currentChar)
                                {
                                    // Double quotes inside quoted string produce single quote
                                    currentComponent.Append(currentChar);
                                }
                                currentState = QUOTED_STRING;
                                break;

                            default:
                                currentComponent.Append(currentChar);
                                break;
                        }
                        break;

                    case QUOTED_STRING:
                        switch (currentChar)
                        {
                            case '\\':
                                // Escape character found
                                previousState = currentState;
                                currentState = ESCAPED_CHARACTER;
                                break;

                            case '"':
                                // Quotation mark found
                                currentState = COMPONENT;
                                break;

                            default:
                                currentComponent.Append(currentChar);
                                break;
                        }
                        break;

                    case ESCAPED_CHARACTER:
                        currentComponent.Append(currentChar);
                        currentState = previousState;
                        currentChar = char.MinValue;
                        break;
                }

                previousChar = currentChar;
                position++;
            }

            // Yield last parsed component, if any
            if (currentComponent.Length > 0)
            {
                var component = parseComponent(currentComponent);
                if (component != null)
                {
                    yield return component;
                }
            }
        }

    }
}
