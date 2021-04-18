/*
Copyright (c) 2021 Steven Foster (steven.d.foster@gmail.com)

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
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace HavokMultimedia.Utilities
{
    /// <summary>
    /// Simple atomic boolean value
    /// </summary>
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public struct AtomicBoolean : IComparable, IComparable<bool>, IEquatable<bool>, IComparable<AtomicBoolean>, IEquatable<AtomicBoolean>
    {
        private int m_value;

        public bool Value => m_value == 1;

        public AtomicBoolean(bool startingValue) => m_value = startingValue ? 1 : 0;

        public static implicit operator bool(AtomicBoolean atomicBoolean) => atomicBoolean.Value;

        public static implicit operator AtomicBoolean(bool boolean) => new AtomicBoolean(boolean);

        /// <summary>
        /// Sets this value to true
        /// </summary>
        /// <returns>True if the current value was changed, else false</returns>
        public bool SetTrue() => Set(true);

        /// <summary>
        /// Sets this value to false
        /// </summary>
        /// <returns>True if the current value was changed, else false</returns>
        public bool SetFalse() => Set(false);

        /// <summary>
        /// Sets the value of this object
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>True if the current value was changed, else false</returns>
        public bool Set(bool value) => value ? 0 == System.Threading.Interlocked.Exchange(ref m_value, 1) : 1 == System.Threading.Interlocked.Exchange(ref m_value, 0);

        public override int GetHashCode() => ((bool)this).GetHashCode();

        public int CompareTo(object obj) => ((bool)this).CompareTo(obj);

        public int CompareTo(bool other) => ((bool)this).CompareTo(other);

        public int CompareTo(AtomicBoolean other) => ((bool)this).CompareTo(other);

        public bool Equals(bool other) => ((bool)this).Equals(other);

        public bool Equals(AtomicBoolean other) => ((bool)this).Equals(other);

        public override bool Equals(object obj) => ((bool)this).Equals(obj);

        public override string ToString() => ((bool)this).ToString();
    }

    public class SingleUse
    {
        private readonly AtomicBoolean boolean = false;

        public bool IsUsed => boolean;

        /// <summary>
        /// Attempts to 'use' this instance. If this is the first time using it, we will return
        /// true. Otherwise we return false if we have already been used.
        /// </summary>
        /// <returns>true if we have never used before, false if we have already been used</returns>
        public bool TryUse() => boolean.SetTrue();
    }

    /// <summary>
    /// System-wide Mutex lock. Good for locking on files using the file name.
    /// </summary>
    public sealed class MutexLock : IDisposable
    {
        private readonly System.Threading.Mutex mutex;
        private readonly SingleUse su = new SingleUse();

        public string MutexName { get; }

        public MutexLock(string mutexName, TimeSpan timeout)
        {
            MutexName = mutexName;
            mutex = new System.Threading.Mutex(false, mutexName);
            if (!mutex.WaitOne(timeout)) throw new MutexLockTimeoutException(mutexName, timeout);
        }

        public void Dispose()
        {
            if (!su.TryUse()) return;
            mutex.ReleaseMutex();
        }
    }

    public sealed class MutexLockTimeoutException : System.Threading.WaitHandleCannotBeOpenedException
    {
        public string MutexName { get; }
        public TimeSpan Timeout { get; }

        public MutexLockTimeoutException(string mutexName, TimeSpan timeout) : base("Failed to aquire mutex [" + mutexName + "] after waiting " + timeout.ToStringTotalSeconds(numberOfDecimalDigits: 3) + "s")
        {
            MutexName = mutexName;
            Timeout = timeout;
        }
    }

    public static class Util
    {
        #region ConsoleColorChanger

        private readonly struct ConsoleColorChanger : IDisposable
        {
            private readonly ConsoleColor foreground;
            private readonly bool foregroundSwitched;
            private readonly ConsoleColor background;
            private readonly bool backgroundSwitched;

            public ConsoleColorChanger(ConsoleColor? foreground, ConsoleColor? background)
            {
                this.foreground = System.Console.ForegroundColor;
                this.background = System.Console.BackgroundColor;

                var fswitch = false;
                if (foreground != null)
                {
                    if (this.foreground != foreground.Value)
                    {
                        fswitch = true;
                        System.Console.ForegroundColor = foreground.Value;
                    }
                }
                foregroundSwitched = fswitch;

                var bswitch = false;
                if (background != null)
                {
                    if (this.background != background.Value)
                    {
                        bswitch = true;
                        System.Console.BackgroundColor = background.Value;
                    }
                }
                backgroundSwitched = bswitch;
            }

            public void Dispose()
            {
                if (foregroundSwitched) System.Console.ForegroundColor = foreground;
                if (backgroundSwitched) System.Console.BackgroundColor = background;
            }
        }

        /// <summary>
        /// Changes the console color, and then changes it back when it is disposed
        /// </summary>
        /// <param name="foreground">The foreground color</param>
        /// <param name="background">The background color</param>
        /// <returns>A disposable that will change the colors back once disposed</returns>
        public static IDisposable ChangeConsoleColor(ConsoleColor? foreground = null, ConsoleColor? background = null) => new ConsoleColorChanger(foreground, background);

        #endregion ConsoleColorChanger

        public static char FindMagicCharacter(string str)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));
            const int threshold = 1000; // TODO: Fine tune this. It determines whether to use a simple loop or hashset.

            var c = 33;
            if (str.Length < threshold)
            {
                while (str.IndexOf((char)c) >= 0) c++;
            }
            else
            {
                var hash = new HashSet<char>();
                var chars = str.ToCharArray();
                for (var i = 0; i < chars.Length; i++) hash.Add(chars[i]);
                while (hash.Contains((char)c)) c++;
            }

            return (char)c;
        }

        public static int Compare<TEnumerble1, TEnumerable2, TItem>(TEnumerble1 enumerable1, TEnumerable2 enumerable2, IComparer<TItem> comparer) where TEnumerble1 : IEnumerable<TItem> where TEnumerable2 : IEnumerable<TItem>
        {
            if (enumerable1 == null) return enumerable2 == null ? 0 : -1;
            if (enumerable2 == null) return 1;

            using (var e1 = enumerable1.GetEnumerator())
            {
                using (var e2 = enumerable2.GetEnumerator())
                {
                    while (true)
                    {
                        var mn1 = e1.MoveNext();
                        var mn2 = e2.MoveNext();
                        if (!mn1) return mn2 ? -1 : 0;
                        if (!mn2) return 1;

                        var o1 = e1.Current;
                        var o2 = e2.Current;

                        var r = comparer.Compare(o1, o2);

                        if (r != 0) return r;
                    }
                }
            }
        }

        /// <summary>Attempts to identity which NewLine character a string uses.</summary>
        /// <param name="str">String to search</param>
        /// <returns>The newline identified</returns>
        public static string IdentifyNewLine(string str)
        {
            var nl = Environment.NewLine;
            if (str == null) return nl;
            if (str.Length == 0) return nl;

            str = str.Remove(Constant.NEWLINE_WINDOWS, out var cWin);
            str = str.Remove(Constant.NEWLINE_UNIX, out var cUnix);
            str = str.Remove(Constant.NEWLINE_MAC, out var cMac);

            var d = new SortedDictionary<int, List<string>>();
            d.AddToList(cWin, Constant.NEWLINE_WINDOWS);
            d.AddToList(cUnix, Constant.NEWLINE_UNIX);
            d.AddToList(cMac, Constant.NEWLINE_MAC);

            var list = d.ToListReversed().First().Value;

            if (list.Count == 1) return list.First();
            if (list.Count == 3) return nl;
            if (list.Count == 2)
            {
                if (nl.In(list[0], list[1])) return nl;
                if (Constant.NEWLINE_WINDOWS.In(list[0], list[1])) return Constant.NEWLINE_WINDOWS;
                if (Constant.NEWLINE_UNIX.In(list[0], list[1])) return Constant.NEWLINE_UNIX;
                if (Constant.NEWLINE_MAC.In(list[0], list[1])) return Constant.NEWLINE_MAC;
                return nl;
            }

            throw new NotImplementedException("Should never make it here");
        }

        public static TOutput ChangeType<TInput, TOutput>(TInput obj) => (TOutput)ChangeType(obj, typeof(TOutput));

        public static TOutput ChangeType<TOutput>(object obj) => (TOutput)ChangeType(obj, typeof(TOutput));

        public static object ChangeType(object obj, Type outputType)
        {
            if (obj == null || obj == DBNull.Value)
            {
                if (!outputType.IsValueType) return null;
                if (outputType.IsNullable()) return null;
                return Convert.ChangeType(obj, outputType); // Should throw exception
            }

            if (outputType.IsNullable(out var underlyingTypeOutput))
            {
                return ChangeType(obj, underlyingTypeOutput);
            }

            var inputType = obj.GetType();
            if (inputType.IsNullable(out var underlyingTypeInput)) inputType = underlyingTypeInput;

            if (inputType == typeof(string))
            {
                var o = obj as string;
                if (outputType == typeof(bool)) return o.ToBool();
                if (outputType == typeof(DateTime)) return o.ToDateTime();
                if (outputType == typeof(Guid)) return o.ToGuid();
                if (outputType == typeof(MailAddress)) return o.ToMailAddress();
                if (outputType == typeof(Uri)) return o.ToUri();
                if (outputType == typeof(IPAddress)) return o.ToIPAddress();

                if (outputType.IsEnum) return Util.GetEnumItem(outputType, o);
            }

            if (inputType.IsEnum) return ChangeType(obj.ToString(), outputType);

            if (outputType == typeof(string)) return obj.ToStringGuessFormat();

            return Convert.ChangeType(obj, outputType);
        }

        /// <summary>
        /// Gets a 001/100 format for a running count
        /// </summary>
        /// <param name="index">The zero based index, +1 will be added automatically</param>
        /// <param name="total">The total number of items</param>
        /// <returns>001/100 formatted string</returns>
        public static string FormatRunningCount(int index, int total) => (index + 1).ToStringPadded().Right(total.ToString().Length) + "/" + total;

        public static string FormatRunningCountPercent(int index, int total, int decimalPlaces)
        {
            int len = 3;
            if (decimalPlaces > 0) len += 1; // decimal
            len += decimalPlaces;

            decimal dindex = index + 1;
            var dtotal = total;
            var dmultipler = 100;

            return (dindex / dtotal * dmultipler).ToString(MidpointRounding.AwayFromZero, decimalPlaces).PadLeft(len) + " %";
        }

        public static bool IsValidUTF8(byte[] buffer) => IsValidUTF8(buffer, buffer.Length);

        /// <summary></summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static bool IsValidUTF8(byte[] buffer, int length)
        {
            var position = 0;
            var bytes = 0;
            while (position < length)
            {
                if (!IsValidUTF8(buffer, position, length, ref bytes))
                {
                    return false;
                }
                position += bytes;
            }
            return true;
        }

        /// <summary></summary>
        /// <param name="buffer"></param>
        /// <param name="position"></param>
        /// <param name="length"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static bool IsValidUTF8(byte[] buffer, int position, int length, ref int bytes)
        {
            if (length > buffer.Length)
            {
                throw new ArgumentException("Invalid length");
            }

            if (position > length - 1)
            {
                bytes = 0;
                return true;
            }

            var ch = buffer[position];

            if (ch <= 0x7F)
            {
                bytes = 1;
                return true;
            }

            if (ch >= 0xc2 && ch <= 0xdf)
            {
                if (position >= length - 2)
                {
                    bytes = 0;
                    return false;
                }
                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }
                bytes = 2;
                return true;
            }

            if (ch == 0xe0)
            {
                if (position >= length - 3)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0xa0 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }
                bytes = 3;
                return true;
            }

            if (ch >= 0xe1 && ch <= 0xef)
            {
                if (position >= length - 3)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }

                bytes = 3;
                return true;
            }

            if (ch == 0xf0)
            {
                if (position >= length - 4)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0x90 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
                    buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }

                bytes = 4;
                return true;
            }

            if (ch == 0xf4)
            {
                if (position >= length - 4)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0x8f ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
                    buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }

                bytes = 4;
                return true;
            }

            if (ch >= 0xf1 && ch <= 0xf3)
            {
                if (position >= length - 4)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
                    buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }

                bytes = 4;
                return true;
            }

            return false;
        }

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

        #region Networking

        /// <summary>
        /// Attempts to get the current local time from the internet
        /// </summary>
        /// <returns>The current local time</returns>
        public static DateTime NetGetInternetDateTime()
        {
            // http://stackoverflow.com/questions/6435099/how-to-get-datetime-from-the-internet

            ServicePointManager.SecurityProtocol = (SecurityProtocolType)(0xc0 | 0x300 | 0xc00);
            // SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var request = (HttpWebRequest)WebRequest.Create("http://worldtimeapi.org/api/timezone/Europe/London.txt");
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

        #endregion Networking

        #region Type and Assembly Scanning

        /// <summary>Gets all non-system types in all visible assemblies.</summary>
        /// <returns>All non-system types in all visible assemblies</returns>
        public static Type[] GetTypes()
        {
            var d = new Dictionary<string, HashSet<Type>>();
            try
            {
                foreach (var asm in GetAssemblies())
                {
                    try
                    {
                        var n = asm.FullName;
                        if (n == null) continue;
                        if (!d.TryGetValue(n, out var set)) d.Add(n, set = new HashSet<Type>());
                        foreach (var t in asm.GetTypes())
                        {
                            if (t != null) set.Add(t);
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }

            return d.Values.SelectMany(o => o).WhereNotNull().ToArray();
        }

        /// <summary>Gets all non-system assemblies currently visible.</summary>
        /// <returns>All non-system assemblies currently visible</returns>
        public static Assembly[] GetAssemblies()
        {
            var items = new Stack<Assembly>();
            try
            {
                items.Push(Assembly.GetEntryAssembly());
            }
            catch (Exception) { }

            try
            {
                items.Push(Assembly.GetCallingAssembly());
            }
            catch (Exception) { }

            try
            {
                items.Push(Assembly.GetExecutingAssembly());
            }
            catch (Exception) { }

            try
            {
                items.Push(MethodBase.GetCurrentMethod().DeclaringType?.Assembly);
            }
            catch (Exception) { }

            try
            {
                var stackTrace = new StackTrace();        // get call stack
                var stackFrames = stackTrace.GetFrames(); // get method calls (frames)
                foreach (var stackFrame in stackFrames)
                {
                    try
                    {
                        items.Push(stackFrame?.GetMethod()?.GetType()?.Assembly);
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }

            var asms = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
            while (items.Count > 0)
            {
                var a = items.Pop();
                if (a == null) continue;
                try
                {
                    var name = a.FullName;
                    if (name == null) continue;
                    if (name.StartsWith("System.", StringComparison.OrdinalIgnoreCase)) continue;
                    if (name.StartsWith("System,", StringComparison.OrdinalIgnoreCase)) continue;
                    if (name.StartsWith("mscorlib,", StringComparison.OrdinalIgnoreCase)) continue;
                    if (asms.ContainsKey(name)) continue;
                    asms.Add(name, a);
                    var asmsNames = a.GetReferencedAssemblies();
                    if (asmsNames != null)
                    {
                        foreach (var asmsName in asmsNames)
                        {
                            try
                            {
                                var aa = Assembly.Load(asmsName);
                                if (aa != null)
                                {
                                    var aaName = aa.FullName;
                                    if (aaName != null)
                                    {
                                        if (!asms.ContainsKey(aaName)) items.Push(aa);
                                    }
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                }
                catch (Exception) { }
            }

            return asms.Values.WhereNotNull().ToArray();
        }

        #endregion Type and Assembly Scanning

        #region Attributes

        public static TAttribute GetAssemblyAttribute<TClassInAssembly, TAttribute>() where TClassInAssembly : class where TAttribute : class => typeof(TClassInAssembly).GetTypeInfo().Assembly.GetCustomAttributes(typeof(TAttribute)).SingleOrDefault() as TAttribute;

        #endregion Attributes

        #region Diagnostics

        /// <summary>
        /// Diagnostic disposable token
        /// </summary>
        public sealed class DiagnosticToken : IDisposable
        {
            private static long idCounter;
            private readonly Action<string> log;
            private readonly Stopwatch stopwatch;
            private readonly SingleUse isDisposed = new SingleUse();

            public long Id { get; }
            public string MemberName { get; }
            public string SourceFilePath { get; }
            public string SourceFileName { get; }
            public int SourceLineNumber { get; }
            public long MemoryStart { get; }
            public int MemoryStartMB => (((double)MemoryStart) / ((double)Constant.BYTES_MEGA)).ToString(MidpointRounding.AwayFromZero, 0).ToInt();

            public long MemoryEnd { get; private set; }
            public int MemoryEndMB => (((double)MemoryStart) / ((double)Constant.BYTES_MEGA)).ToString(MidpointRounding.AwayFromZero, 0).ToInt();

            public TimeSpan Time { get; private set; }

            internal DiagnosticToken(Action<string> log, string memberName, string sourceFilePath, int sourceLineNumber)
            {
                Id = System.Threading.Interlocked.Increment(ref idCounter);
                this.log = log.CheckNotNull(nameof(log));
                MemberName = memberName.TrimOrNull();
                SourceFilePath = sourceFilePath.TrimOrNull();
                if (SourceFilePath != null)
                {
                    try
                    {
                        SourceFileName = Path.GetFileName(SourceFilePath).TrimOrNull();
                    }
                    catch (Exception) { }
                }
                SourceLineNumber = sourceLineNumber;
                MemoryStart = Environment.WorkingSet;
                stopwatch = Stopwatch.StartNew();
                var mn = MemberName ?? "?";
                var sfn = SourceFileName ?? "?";
                log($"+TRACE[{Id}]: {mn} ({sfn}:{SourceLineNumber})  {MemoryStartMB.ToStringCommas()} MB");
            }

            public void Dispose()
            {
                if (!isDisposed.TryUse()) return;
                MemoryEnd = Environment.WorkingSet;
                stopwatch.Stop();
                Time = stopwatch.Elapsed;
                var mn = MemberName ?? "?";
                var sfn = SourceFileName ?? "?";

                var memDif = MemoryEndMB - MemoryStartMB;
                var memDifString = memDif > 0 ? "(+" + memDif + ")" : memDif < 0 ? "(-" + memDif + ")" : "";
                var time = Time.TotalSeconds.ToString(MidpointRounding.AwayFromZero, 3);

                log($"-TRACE[{Id}]: {mn} ({sfn}:{SourceLineNumber})  {MemoryEndMB.ToStringCommas()} MB {memDifString}  {time}s");
            }
        }

        /// <summary>
        /// With a using statement, logs start and stop time, memory difference, and souce line number. Only the log argument should be supplied
        /// </summary>
        /// <param name="log">Only provide this argument</param>
        /// <param name="memberName">No not provide this argument</param>
        /// <param name="sourceFilePath">No not provide this argument</param>
        /// <param name="sourceLineNumber">No not provide this argument</param>
        /// <returns>Disposable token when logging should end</returns>
        public static IDisposable Diagnostic(Action<string> log, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "", [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0) => new DiagnosticToken(log, memberName, sourceFilePath, sourceLineNumber);

        #endregion Diagnostics

        #region GuessType

        /// <summary>
        /// Tries to guess the best type that is valid to convert a string to
        /// </summary>
        /// <param name="s">The string to guess</param>
        /// <returns>The best found Type or string type if a best guess couldn't be found</returns>
        public static Type GuessType(string s) => GuessType(s.CheckNotNull(nameof(s)).Yield());

        /// <summary>
        /// Tries to guess the best type for a group of strings. All strings provided must be convertable for the match to be made
        /// </summary>
        /// <param name="strs">The strings to guess on</param>
        /// <returns>The best found Type or string type if a best guess couldn't be found</returns>
        public static Type GuessType(IEnumerable<string> strs)
        {
            strs.CheckNotNull(nameof(strs));

            var list = strs.TrimOrNull().ToList();
            var listcount = list.Count;

            list = list.WhereNotNull().ToList();
            var nullable = (listcount != list.Count);
            if (list.Count == 0) return typeof(string);

            if (list.All(o => Guid.TryParse(o, out var v))) return nullable ? typeof(Guid?) : typeof(Guid);
            if (list.All(o => o.CountOccurances(".") == 3))
            {
                if (list.All(o => IPAddress.TryParse(o, out var v))) return typeof(IPAddress);
            }

            if (list.All(o => o.ToBoolTry(out var v))) return nullable ? typeof(bool?) : typeof(bool);
            if (list.All(o => o.ToByteTry(out var v))) return nullable ? typeof(byte?) : typeof(byte);
            if (list.All(o => o.ToSByteTry(out var v))) return nullable ? typeof(sbyte?) : typeof(sbyte);
            if (list.All(o => o.ToShortTry(out var v))) return nullable ? typeof(short?) : typeof(short);
            if (list.All(o => o.ToUShortTry(out var v))) return nullable ? typeof(ushort?) : typeof(ushort);
            if (list.All(o => o.ToIntTry(out var v))) return nullable ? typeof(int?) : typeof(int);
            if (list.All(o => o.ToUIntTry(out var v))) return nullable ? typeof(uint?) : typeof(uint);
            if (list.All(o => o.ToLongTry(out var v))) return nullable ? typeof(long?) : typeof(long);
            if (list.All(o => o.ToULongTry(out var v))) return nullable ? typeof(ulong?) : typeof(ulong);
            if (list.All(o => o.ToDecimalTry(out var v))) return nullable ? typeof(decimal?) : typeof(decimal);
            if (list.All(o => o.ToFloatTry(out var v))) return nullable ? typeof(float?) : typeof(float);
            if (list.All(o => o.ToDoubleTry(out var v))) return nullable ? typeof(double?) : typeof(double);
            if (list.All(o => BigInteger.TryParse(o, out var v))) return nullable ? typeof(BigInteger?) : typeof(BigInteger);

            if (list.All(o => o.Length == 1)) return nullable ? typeof(char?) : typeof(char);

            if (list.All(o => DateTime.TryParse(o, out var v))) return nullable ? typeof(DateTime?) : typeof(DateTime);
            if (list.All(o => Uri.TryCreate(o, UriKind.Absolute, out var vUri))) return typeof(Uri);



            return typeof(string);
        }

        #endregion GuessType

        #region Serialization

        /// <summary>
        /// Deserializes an object from binary data
        /// </summary>
        /// <param name="data">The serialized binary data</param>
        /// <returns>The deserialized object</returns>
        public static object DeserializeBinary(byte[] data)
        {
            IFormatter formatter = new BinaryFormatter();

            using (var stream = new MemoryStream(data))
            {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                return formatter.Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            }
        }

        /// <summary>
        /// Serializes an object to binary data
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>The serialized binary data</returns>
        public static byte[] SerializeBinary(object obj)
        {
            IFormatter formatter = new BinaryFormatter();

            using (var stream = new MemoryStream())
            {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                formatter.Serialize(stream, obj);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                stream.Flush();
                stream.Close();
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Deep clones a serializable object
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="obj">The object to clonse</param>
        /// <returns>The clone</returns>
        public static T Clone<T>(T obj)
        {
            // http://stackoverflow.com/a/78612
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", "obj");
            }

            // Don't serialize a null object, simply return the default for that object
            if (ReferenceEquals(obj, null))
            {
                return default;
            }

            IFormatter formatter = new BinaryFormatter();
            using (Stream stream = new MemoryStream())
            {
                using (stream)
                {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                    formatter.Serialize(stream, obj);
                    stream.Seek(0, SeekOrigin.Begin);
                    return (T)formatter.Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                }
            }
        }

        #endregion Serialization

        #region Compression

        /// <summary>
        /// Compresses binary data
        /// </summary>
        /// <param name="data">Data to compress</param>
        /// <param name="compressionLevel">The level of compression</param>
        /// <returns>The compressed data</returns>
        public static byte[] CompressGZip(byte[] data, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            using (var stream = new MemoryStream())
            {
                using (var gstream = new GZipStream(stream, compressionLevel))
                {
                    gstream.Write(data, 0, data.Length);

                    gstream.Flush();
                    stream.Flush();

                    gstream.Close();
                    stream.Close();

                    return stream.ToArray();
                }
            }
        }

        /// <summary>
        /// Decompresses binary data
        /// </summary>
        /// <param name="data">The data to decompress</param>
        /// <returns>The decompressed data</returns>
        public static byte[] DecompressGZip(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                using (var gstream = new GZipStream(stream, CompressionMode.Decompress))
                {
                    using (var stream2 = new MemoryStream())
                    {
                        gstream.CopyTo(stream2);
                        stream2.Flush();
                        stream2.Close();
                        return stream2.ToArray();
                    }
                }
            }
        }

        #endregion Compression

        #region HashCode

        public static int GenerateHashCode<T1>(T1 item1) => (EqualityComparer<T1>.Default.Equals(item1, default) ? 0 : item1.GetHashCode());

        public static int GenerateHashCode<T1, T2>(T1 item1, T2 item2) => GenerateHashCode(item1, item2, false, false, false, false, false, false);

        public static int GenerateHashCode<T1, T2, T3>(T1 item1, T2 item2, T3 item3) => GenerateHashCode(item1, item2, item3, false, false, false, false, false);

        public static int GenerateHashCode<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4) => GenerateHashCode(item1, item2, item3, item4, false, false, false, false);

        public static int GenerateHashCode<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) => GenerateHashCode(item1, item2, item3, item4, item5, false, false, false);

        public static int GenerateHashCode<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) => GenerateHashCode(item1, item2, item3, item4, item5, item6, false, false);

        public static int GenerateHashCode<T1, T2, T3, T4, T5, T6, T7>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) => GenerateHashCode(item1, item2, item3, item4, item5, item6, item7, false);

        public static int GenerateHashCode<T1, T2, T3, T4, T5, T6, T7, T8>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
        {
            // http://stackoverflow.com/a/263416
            unchecked
            {
                const int START = 17;
                const int PRIME = 16777619;

                var hash = START;
                hash = hash * PRIME + (EqualityComparer<T1>.Default.Equals(item1, default) ? 0 : item1.GetHashCode());
                hash = hash * PRIME + (EqualityComparer<T2>.Default.Equals(item2, default) ? 0 : item2.GetHashCode());
                hash = hash * PRIME + (EqualityComparer<T3>.Default.Equals(item3, default) ? 0 : item3.GetHashCode());
                hash = hash * PRIME + (EqualityComparer<T4>.Default.Equals(item4, default) ? 0 : item4.GetHashCode());
                hash = hash * PRIME + (EqualityComparer<T5>.Default.Equals(item5, default) ? 0 : item5.GetHashCode());
                hash = hash * PRIME + (EqualityComparer<T6>.Default.Equals(item6, default) ? 0 : item6.GetHashCode());
                hash = hash * PRIME + (EqualityComparer<T7>.Default.Equals(item7, default) ? 0 : item7.GetHashCode());
                hash = hash * PRIME + (EqualityComparer<T8>.Default.Equals(item8, default) ? 0 : item8.GetHashCode());
                return hash;
            }
        }

        public static int GenerateHashCodeFromCollection<T>(IEnumerable<T> items)
        {
            // http://stackoverflow.com/a/263416
            unchecked
            {
                const int START = 17;
                const int PRIME = 16777619;

                var hash = START;

                foreach (var item in items)
                {
                    hash = hash * PRIME + (EqualityComparer<T>.Default.Equals(item, default) ? 0 : item.GetHashCode());
                }

                return hash;
            }
        }

        #endregion HashCode

        #region Hashing

        private static string GenerateHashInternal(HashAlgorithm hashAlgorithm, Stream stream)
        {
            using (hashAlgorithm)
            {
                var hash = hashAlgorithm.ComputeHash(stream);
                var str = BitConverter.ToString(hash);
                return str.Replace("-", "").ToLower();
            }
        }

        private static string GenerateHashInternal(HashAlgorithm hashAlgorithm, byte[] bytes)
        {
            using (hashAlgorithm)
            {
                var hash = hashAlgorithm.ComputeHash(bytes);
                var str = BitConverter.ToString(hash);
                return str.Replace("-", "").ToLower();
            }
        }

        private static string GenerateHashInternal(HashAlgorithm hashAlgorithm, string file)
        {
            using (var fs = FileOpenRead(file))
            {
                return GenerateHashInternal(hashAlgorithm, fs);
            }
        }

        public static string GenerateHashMD5(Stream stream) => GenerateHashInternal(MD5.Create(), stream);

        public static string GenerateHashMD5(byte[] bytes) => GenerateHashInternal(MD5.Create(), bytes);

        public static string GenerateHashMD5(string file) => GenerateHashInternal(MD5.Create(), file);

        public static string GenerateHashSHA1(Stream stream) => GenerateHashInternal(SHA1Managed.Create(), stream);

        public static string GenerateHashSHA1(byte[] bytes) => GenerateHashInternal(SHA1Managed.Create(), bytes);

        public static string GenerateHashSHA1(string file) => GenerateHashInternal(SHA1Managed.Create(), file);

        public static string GenerateHashSHA256(Stream stream) => GenerateHashInternal(SHA256Managed.Create(), stream);

        public static string GenerateHashSHA256(byte[] bytes) => GenerateHashInternal(SHA256Managed.Create(), bytes);

        public static string GenerateHashSHA256(string file) => GenerateHashInternal(SHA256Managed.Create(), file);

        public static string GenerateHashSHA384(Stream stream) => GenerateHashInternal(SHA384Managed.Create(), stream);

        public static string GenerateHashSHA384(byte[] bytes) => GenerateHashInternal(SHA384Managed.Create(), bytes);

        public static string GenerateHashSHA384(string file) => GenerateHashInternal(SHA384Managed.Create(), file);

        public static string GenerateHashSHA512(Stream stream) => GenerateHashInternal(SHA512Managed.Create(), stream);

        public static string GenerateHashSHA512(byte[] bytes) => GenerateHashInternal(SHA512Managed.Create(), bytes);

        public static string GenerateHashSHA512(string file) => GenerateHashInternal(SHA512Managed.Create(), file);

        #endregion Hashing

        #region IsEqual

        public static bool IsEqual<T1>(T1 o11, T1 o21) => Object.Equals(o11, o21);

        public static bool IsEqual<T1, T2>(T1 o11, T1 o21, T2 o12, T2 o22) => Object.Equals(o11, o21) && Object.Equals(o12, o22);

        public static bool IsEqual<T1, T2, T3>(T1 o11, T1 o21, T2 o12, T2 o22, T3 o13, T3 o23) => Object.Equals(o11, o21) && Object.Equals(o12, o22) && Object.Equals(o13, o23);

        public static bool IsEqual<T1, T2, T3, T4>(T1 o11, T1 o21, T2 o12, T2 o22, T3 o13, T3 o23, T4 o14, T4 o24) => Object.Equals(o11, o21) && Object.Equals(o12, o22) && Object.Equals(o13, o23) && Object.Equals(o14, o24);

        public static bool IsEqualBytes(byte[] b1, byte[] b2)
        {
            /* TODO: Calling natively is FAST but not crossplatform compliant. Figure out solution that compiles in mono but uses natively if available.
                [System.Runtime.InteropServices.DllImport("msvcrt.dll", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
                private static extern int memcmp(byte[] b1, byte[] b2, UIntPtr count);
                public static bool Equals(this byte[] b1, byte[] b2)
                {
                   // http://stackoverflow.com/a/1445405 http://www.pinvoke.net/default.aspx/msvcrt.memcmp
                   if (b1 == b2) return true; //reference equality check
                   if (b1 == null || b2 == null || b1.Length != b2.Length) return false;
                   return memcmp(b1, b2, new UIntPtr((uint)b1.Length)) == 0;
                }
            */

            if (b1 == b2) return true; //reference equality check
            if (b1 == null || b2 == null || b1.Length != b2.Length) return false;

            var len = b1.Length;
            for (var i = 0; i < len; i++)
            {
                if (b1[i] != b2[i]) return false;
            }
            return true;
        }

        #endregion IsEqual

        #region File

        public readonly struct FileListResult
        {
            public string Path { get; }

            public bool IsDirectory { get; }

            public Exception Exception { get; }

            public FileListResult(string path, bool isDirectory, Exception exception) : this()
            {
                Path = path;
                IsDirectory = isDirectory;
                Exception = exception;
            }

            public override string ToString() => Path;

            public FileSystemInfo GetFileSystemInfo() => IsDirectory ? new DirectoryInfo(Path) : (FileSystemInfo)new FileInfo(Path);
        }

        internal static DirectoryNotFoundException CreateExceptionDirectoryNotFound(string directoryPath) => new DirectoryNotFoundException("Directory does not exist " + directoryPath);

        internal static FileNotFoundException CreateExceptionFileNotFound(string filePath) => new FileNotFoundException("File does not exist " + filePath, filePath);

        internal static DirectoryNotFoundException CreateExceptionIsNotDirectory(string directoryPath) => new DirectoryNotFoundException("Path is a file not a directory " + directoryPath);

        public static (string directoryName, string fileName, string extension) SplitFileName(string file)
        {
            file = Path.GetFullPath(file);
            var d = Path.GetFullPath(Path.GetDirectoryName(file));
            var f = Path.GetFileNameWithoutExtension(file);
            var e = Path.GetExtension(file).TrimOrNull();
            if (e != null && e.Length > 0 && e[0] == '.') e = e.Remove(0, 1).TrimOrNull();
            return (d.TrimOrNull() ?? string.Empty, f ?? string.Empty, e.TrimOrNull() ?? string.Empty);
        }

        public static string FileGetMD5(string file)
        {
            using (var stream = FileOpenRead(file)) return Util.GenerateHashMD5(stream);
        }

        public static string FileGetSHA1(string file)
        {
            using (var stream = FileOpenRead(file)) return Util.GenerateHashSHA1(stream);
        }

        public static string FileGetSHA256(string file)
        {
            using (var stream = FileOpenRead(file)) return Util.GenerateHashSHA256(stream);
        }

        public static string FileGetSHA384(string file)
        {
            using (var stream = FileOpenRead(file)) return Util.GenerateHashSHA384(stream);
        }

        public static string FileGetSHA512(string file)
        {
            using (var stream = FileOpenRead(file)) return Util.GenerateHashSHA512(stream);
        }

        public static long FileGetSize(string file) => (new System.IO.FileInfo(file)).Length;

        public static string FileChangeName(string file, string newFileName)
        {
            file = Path.GetFullPath(file);
            var dir = Path.GetDirectoryName(file);
            var name = newFileName;
            var ext = string.Empty;
            if (Path.HasExtension(file)) ext = Path.GetExtension(file);
            if (!ext.StartsWith(".")) ext = "." + ext;
            var f = Path.Combine(dir, name + ext);
            f = Path.GetFullPath(f);
            return f;
        }

        public static string FileChangeNameRandomized(string file) => FileChangeNameAppendRight(file, "_" + Guid.NewGuid().ToString().Replace("-", ""));

        public static string FileChangeNameAppendRight(string file, string stringToAppend) => FileChangeName(file, Path.GetFileNameWithoutExtension(Path.GetFullPath(file)) + stringToAppend);

        public static string FileChangeNameAppendLeft(string file, string stringToAppend) => FileChangeName(file, stringToAppend + Path.GetFileNameWithoutExtension(Path.GetFullPath(file)));

        public static string FileChangeExtension(string file, string newExtension)
        {
            file = Path.GetFullPath(file);
            var dir = Path.GetDirectoryName(file);
            var name = Path.GetFileNameWithoutExtension(file);
            var ext = newExtension ?? string.Empty;
            if (!ext.StartsWith(".")) ext = "." + ext;
            var f = Path.Combine(dir, name + ext);
            f = Path.GetFullPath(f);
            return f;
        }

        public static bool IsDirectory(string path)
        {
            path = path.CheckNotNullTrimmed(path);
            if (File.Exists(path)) return false;
            if (Directory.Exists(path)) return true;
            return false;
        }

        public static bool IsEqualFile(string file1, string file2, bool buffered)
        {
            file1.CheckFileExists("file1");
            file2.CheckFileExists("file2");

            // TODO: Check if file system is case sensitive or not then do a case-insensitive comparison since Windows uses case-insensitive filesystem.
            if (string.Equals(file1, file2)) return true;

            var file1Size = FileGetSize(file1);
            var file2Size = FileGetSize(file2);

            if (file1Size != file2Size) return false;

            if (buffered)
            {
                var file1Bytes = FileRead(file1);
                var file2Bytes = FileRead(file2);

                return Util.IsEqualBytes(file1Bytes, file2Bytes);
            }
            else
            {
                // No buffer needed http://stackoverflow.com/a/2069317 http://blogs.msdn.com/b/brada/archive/2004/04/15/114329.aspx

                // Compare method from http://stackoverflow.com/a/1359947
                const int BYTES_TO_READ = sizeof(long);

                var iterations = (int)Math.Ceiling((double)file1Size / BYTES_TO_READ);

                using (var fs1 = FileOpenRead(file1))
                using (var fs2 = FileOpenRead(file2))
                {
                    var one = new byte[BYTES_TO_READ];
                    var two = new byte[BYTES_TO_READ];

                    for (var i = 0; i < iterations; i++)
                    {
                        fs1.Read(one, 0, BYTES_TO_READ);
                        fs2.Read(two, 0, BYTES_TO_READ);

                        if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        public static bool IsFile(string path)
        {
            path = path.CheckNotNullTrimmed(path);
            if (Directory.Exists(path)) return false;
            if (File.Exists(path)) return true;
            return false;
        }

        public static IEnumerable<FileListResult> FileList(string directoryPath, bool recursive = false)
        {
            directoryPath = directoryPath.CheckNotNullTrimmed(nameof(directoryPath));
            if (IsFile(directoryPath)) throw CreateExceptionIsNotDirectory(directoryPath);
            if (!IsDirectory(directoryPath)) throw CreateExceptionDirectoryNotFound(directoryPath);

            var queue = new Queue<string>();
            queue.Enqueue(directoryPath);
            while (queue.Count > 0)
            {
                var currentDirectory = Path.GetFullPath(queue.Dequeue());
                Exception exception = null;
                string[] files = null;
                try
                {
                    var subdirs = Directory.GetDirectories(currentDirectory).OrEmpty();
                    for (var i = 0; i < subdirs.Length; i++) queue.Enqueue(subdirs[i]);
                    files = Directory.GetFiles(currentDirectory).OrEmpty();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                yield return new FileListResult(currentDirectory, true, exception);

                if (exception == null && files != null)
                {
                    for (var i = 0; i < files.Length; i++)
                    {
                        yield return new FileListResult(Path.GetFullPath(files[i]), false, exception);
                    }
                }

                if (!recursive) queue.Clear();
            }
        }

        public static IEnumerable<string> FileListFiles(string directoryPath, bool recursive = false)
        {
            foreach (var o in FileList(directoryPath, recursive))
            {
                if (o.Exception == null && !o.IsDirectory)
                {
                    yield return o.Path;
                }
            }
        }

        public static IEnumerable<string> FileListDirectories(string directoryPath, bool recursive = false)
        {
            foreach (var o in FileList(directoryPath, recursive))
            {
                if (o.Exception == null && o.IsDirectory)
                {
                    yield return o.Path;
                }
            }
        }

        public static bool FileIsAbsolutePath(string path)
        {
            if (path.TrimOrNull() == null) return false;
            if (path.IndexOfAny(Path.GetInvalidPathChars()) != -1) return false;
            if (!Path.IsPathRooted(path)) return false;
            if (Path.GetPathRoot(path).Equals(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)) return false;
            return true;
        }

        public static FileStream FileOpenRead(string filename) => File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        public static FileStream FileOpenWrite(string filename) => File.Open(filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);

        public static byte[] FileRead(string path)
        {
            // https://github.com/mosa/Mono-Class-Libraries/blob/master/mcs/class/corlib/System.IO/File.cs

            using (var stream = FileOpenRead(path))
            {
                var size = stream.Length;
                // limited to 2GB according to http://msdn.microsoft.com/en-us/library/system.io.file.readallbytes.aspx
                if (size > int.MaxValue) throw new IOException("Reading more than 2GB with this call is not supported");
                var pos = 0;
                var count = (int)size;
                var result = new byte[size];
                while (count > 0)
                {
                    var n = stream.Read(result, pos, count);
                    if (n == 0) throw new IOException("Unexpected end of stream");
                    pos += n;
                    count -= n;
                }
                return result;
            }
        }

        public static string FileRead(string path, Encoding encoding) => encoding.GetString(FileRead(path));

        public static void FileWrite(string path, byte[] data, bool append = false)
        {
            if (File.Exists(path) && !append) File.Delete(path);
            using (var stream = File.OpenWrite(path))
            {
                stream.Write(data, 0, data.Length);
                stream.Flush(true);
            }
        }

        public static void FileWrite(string path, string data, Encoding encoding, bool append = false) => FileWrite(path, encoding.GetBytes(data), append);

        public static string FilenameSanitize(string path, string replacement)
        {
            if (path == null) return null;
            //if (replacement == null) throw new ArgumentNullException("replacement");

            var illegalChars = new HashSet<char>(Path.GetInvalidFileNameChars());

            var pathChars = path.ToCharArray();
            var sb = new StringBuilder();

            for (var i = 0; i < pathChars.Length; i++)
            {
                var c = pathChars[i];
                if (illegalChars.Contains(c))
                {
                    if (replacement != null) sb.Append(replacement);
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        #region Temp

        private static readonly object LOCK_TEMP = new object();

        private sealed class TempDirectory : IDisposable
        {
            private static readonly ILogger log = LogFactory.LogFactoryImpl.GetLogger<TempDirectory>();
            private readonly string path;

            public TempDirectory(string path)
            {
                log.Debug($"Creating temporary directory {path}");
                Directory.CreateDirectory(path);
                this.path = path;
            }

            public void Dispose()
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        log.Debug($"Deleting temporary directory {path}");
                        Directory.Delete(path, true);
                        log.Debug($"Successfully deleted temporary directory {path}");
                    }
                }
                catch (Exception e)
                {
                    log.Warn($"Error deleting temporary directory {path}", e);
                }
            }
        }

        private sealed class TempFile : IDisposable
        {
            private static readonly ILogger log = LogFactory.LogFactoryImpl.GetLogger<TempFile>();
            private readonly string path;

            public TempFile(string path)
            {
                log.Debug($"Creating temporary file {path}");
                File.WriteAllBytes(path, Array.Empty<byte>());
                this.path = path;
            }

            public void Dispose()
            {
                try
                {
                    if (File.Exists(path))
                    {
                        log.Debug($"Deleting temporary file {path}");
                        File.Delete(path);
                        log.Debug($"Successfully deleted temporary file {path}");
                    }
                }
                catch (Exception e)
                {
                    log.Warn($"Error deleting temporary file {path}", e);
                }
            }
        }

        public static IDisposable CreateTempDirectory(out string path)
        {
            lock (LOCK_TEMP)
            {
                var parentDir = Path.GetTempPath();
                string p;
                do
                {
                    p = Path.GetFullPath(Path.Combine(parentDir, Path.GetRandomFileName()));
                } while (Directory.Exists(p));
                var t = new TempDirectory(p);
                path = p;
                return t;
            }
        }

        public static IDisposable CreateTempFile(out string path)
        {
            lock (LOCK_TEMP)
            {
                var parentDir = Path.GetTempPath();
                string p;
                do
                {
                    p = Path.GetFullPath(Path.Combine(parentDir, Path.GetRandomFileName()));
                } while (File.Exists(p));

                var t = new TempFile(p);
                path = p;
                return t;
            }
        }

        #endregion Temp

        #region Exceptions

        #endregion Exceptions

        #endregion File

        #region SplitDelimited

        /// <summary>http://stackoverflow.com/a/3776617</summary>
        private static readonly Regex SplitDelimitedCommaRegex = new Regex("(?:^|,)(\"(?:[^\"]+|\"\")*\"|[^,]*)", RegexOptions.Compiled);

        private static readonly string[] SplitDelimitedTabArray = new string[] { "\t" };

        public static List<string[]> SplitDelimitedComma(string text)
        {
            var result = new List<string[]>();
            if (text == null) return result;

            var lines = text.SplitOnNewline().TrimOrNull().WhereNotNull().ToList();

            foreach (var line in lines)
            {
                var matches = SplitDelimitedCommaRegex.Matches(line);
                var items = new List<string>(matches.Count);
                foreach (Match match in matches)
                {
                    var item = match.Value.TrimStart(',').Replace("\"", "").TrimOrNull();
                    items.Add(item);
                }

                result.Add(items.ToArray());
            }

            return result;
        }

        public static List<string[]> SplitDelimitedTab(string text)
        {
            var result = new List<string[]>();
            if (text == null) return result;

            var lines = text.SplitOnNewline().Where(o => o.TrimOrNull() != null).ToList();

            foreach (var line in lines)
            {
                var matches = line.Split(SplitDelimitedTabArray, StringSplitOptions.None);
                var items = new List<string>(matches.Length);
                foreach (var match in matches)
                {
                    var item = match.TrimOrNull();
                    items.Add(item);
                }

                result.Add(items.ToArray());
            }

            return result;
        }

        #endregion SplitDelimited

        #region Enum

        private static readonly EnumCache enumCache = new EnumCache();

        private sealed class EnumCache
        {
            private static readonly IBucketReadOnly<Type, IReadOnlyDictionary<string, object>> enums = new BucketCacheCopyOnWrite<Type, IReadOnlyDictionary<string, object>>(type => Enum.GetNames(type).ToDictionary(o => o, o => Enum.Parse(type, o)).AsReadOnly());

            private readonly object locker = new object();
            private volatile IReadOnlyDictionary<DicKey, object> cache = (new Dictionary<DicKey, object>()).AsReadOnly();

            private readonly struct DicKey : IEquatable<DicKey>
            {
                private readonly int hashCode;
                public readonly Type enumType;
                public readonly string enumItemName;

                public DicKey(Type enumType, string enumItemName)
                {
                    this.enumType = enumType;
                    this.enumItemName = enumItemName;
                    hashCode = GenerateHashCode(enumType, enumItemName);
                }

                public override int GetHashCode() => hashCode;

                public override bool Equals(object obj) => obj is DicKey ? Equals((DicKey)obj) : false;

                public bool Equals(DicKey other) => hashCode == other.hashCode && enumType.Equals(other.enumType) && enumItemName.Equals(other.enumItemName);
            }

            public bool TryGetEnumObject(Type enumType, string enumItemName, out object enumObject, bool throwExceptions)
            {
                enumItemName = enumItemName.TrimOrNull();
                if (throwExceptions)
                {
                    enumType.CheckNotNull(nameof(enumType));
                    enumItemName.CheckNotNull(nameof(enumItemName));
                }
                else
                {
                    if (enumType == null || enumItemName == null)
                    {
                        enumObject = null;
                        return false;
                    }
                }

                if (!enumType.IsEnum)
                {
                    var ut = Nullable.GetUnderlyingType(enumType);
                    if (ut == null || !ut.IsEnum)
                    {
                        if (throwExceptions)
                        {
                            throw new ArgumentException("Type [" + enumType.FullNameFormatted() + "] is not an enum", nameof(enumType));
                        }
                        else
                        {
                            enumObject = null;
                            return false;
                        }
                    }
                    enumType = ut;
                }

                var key = new DicKey(enumType, enumItemName);

                if (cache.TryGetValue(key, out var eo))
                {
                    enumObject = eo;
                    return true;
                }

                lock (locker)
                {
                    if (cache.TryGetValue(key, out eo))
                    {
                        enumObject = eo;
                        return true;
                    }

                    var d = enums[enumType];
                    foreach (var sc in Constant.LIST_StringComparison)
                    {
                        foreach (var kvp in d)
                        {
                            if (string.Equals(kvp.Key, enumItemName, sc))
                            {
                                eo = kvp.Value;
                                var c = new Dictionary<DicKey, object>();
                                foreach (var kvp2 in cache) c.Add(kvp2.Key, kvp2.Value);
                                c.Add(key, eo);
                                cache = c.AsReadOnly();

                                enumObject = eo;
                                return true;
                            }
                        }
                    }

                    if (throwExceptions)
                    {
                        var itemNames = string.Join(", ", d.Keys.ToArray());
                        throw new ArgumentException("Type Enum [" + enumType.FullNameFormatted() + "] does not contain a member named '" + enumItemName + "', valid values are... " + itemNames, nameof(enumItemName));
                    }
                    else
                    {
                        enumObject = null;
                        return false;
                    }
                }
            }
        }

        public static TEnum GetEnumItem<TEnum>(string name) where TEnum : struct, IConvertible, IComparable, IFormattable => (TEnum)GetEnumItem(typeof(TEnum), name);

        public static object GetEnumItem(Type enumType, string name) => enumCache.TryGetEnumObject(enumType, name, out var o, true) ? o : null;

        public static TEnum? GetEnumItemNullable<TEnum>(string name) where TEnum : struct, IConvertible, IComparable, IFormattable
        {
            var o = GetEnumItemNullable(typeof(TEnum), name);
            if (o == null) return null;
            return (TEnum)o;
        }

        public static object GetEnumItemNullable(Type enumType, string name) => enumCache.TryGetEnumObject(enumType, name, out var o, false) ? o : null;

        public static IReadOnlyList<TEnum> GetEnumItems<TEnum>() where TEnum : struct, IConvertible, IComparable, IFormattable => (TEnum[])Enum.GetValues(typeof(TEnum));

        public static TEnum CombineEnumFlags<TEnum>(IEnumerable<TEnum> enums) where TEnum : struct, IConvertible, IComparable, IFormattable => (TEnum)Enum.Parse(typeof(TEnum), string.Join(", ", enums.Select(o => o.ToString())));

        #endregion Enum

        #region Base16

        private static readonly uint[] lookupBase16 = Base16();

        private static uint[] Base16()
        {
            var result = new uint[256];
            for (var i = 0; i < 256; i++)
            {
                var s = i.ToString("X2");
                result[i] = s[0] + ((uint)s[1] << 16);
            }
            return result;
        }

        public static string Base16(byte[] bytes)
        {
            // https://stackoverflow.com/a/24343727/48700 https://stackoverflow.com/a/624379

            var lookup32 = lookupBase16;
            var result = new char[bytes.Length * 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                var val = lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }

        public static byte[] Base16(string base16string)
        {
            var numberChars = base16string.Length;
            var bytes = new byte[numberChars / 2];
            for (var i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(base16string.Substring(i, 2), 16);
            }
            return bytes;
        }

        #endregion Base16

        #region Base64

        public static string Base64(byte[] bytes) => Convert.ToBase64String(bytes);

        public static byte[] Base64(string base64string) => Convert.FromBase64String(base64string);

        #endregion Base64

        #region DefaultValue

        private static readonly IBucketReadOnly<Type, object> GetDefaultValueCache = new DefaultValueCache();

        private sealed class DefaultValueCache : IBucketReadOnly<Type, object>
        {
            private readonly IBucketReadOnly<Type, object> bucket = new BucketCacheCopyOnWrite<Type, object>(type => Activator.CreateInstance(type));
            public IEnumerable<Type> Keys => bucket.Keys;
            public object this[Type key] => key == null ? null : (key.IsValueType ? bucket[key] : null);
        }

        public static object GetDefaultValue(Type type) => GetDefaultValueCache[type];

        public static object GetDefaultValue2(Type type)
        {
            var o = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Default(type), typeof(object))).Compile()();
            return o;
        }

        #endregion DefaultValue

        #region PropertyReaderWriter

        internal sealed class ClassReaderWriter
        {
            private static readonly IBucketReadOnly<Type, ClassReaderWriter> CACHE = new BucketCacheCopyOnWrite<Type, ClassReaderWriter>(o => new ClassReaderWriter(o));
            private readonly IReadOnlyDictionary<string, PropertyReaderWriter> propertiesCaseSensitive;
            private readonly IReadOnlyDictionary<string, PropertyReaderWriter> propertiesCaseInsensitive;
            private readonly Type type;

            private ClassReaderWriter(Type type)
            {
                this.type = type.CheckNotNull(nameof(type));
                var props = PropertyReaderWriter.Create(type);

                var d1 = new Dictionary<string, PropertyReaderWriter>();
                var d2 = new Dictionary<string, PropertyReaderWriter>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in props)
                {
                    d1.Add(prop.PropertyInfo.Name, prop);
                    d2[prop.PropertyInfo.Name] = prop;
                }
                propertiesCaseSensitive = d1.AsReadOnly();
                propertiesCaseInsensitive = d2.AsReadOnly();
            }

            private static ClassReaderWriter Create(Type type) => CACHE[type.CheckNotNull(nameof(type))];

            private PropertyReaderWriter GetProperty(string propertyName)
            {
                propertyName = propertyName.CheckNotNullTrimmed(nameof(propertyName));
                if (propertiesCaseSensitive.TryGetValue(propertyName, out var value)) return value;
                if (propertiesCaseInsensitive.TryGetValue(propertyName, out value)) return value;
                throw new ArgumentException("Public property not found: " + type.FullNameFormatted() + "." + propertyName);
            }

            private void SetPropertyValueInternal(object instance, string propertyName, object propertyValue, TypeConverter converter = null)
            {
                var p = GetProperty(propertyName);

                if (propertyValue != null)
                {
                    var tSource = propertyValue.GetType();
                    var tTarget = p.PropertyInfo.PropertyType;
                    if (tSource != tTarget)
                    {
                        if (!tTarget.IsAssignableFrom(tSource))
                        {
                            if (converter == null)
                            {
                                // throw new ArgumentException($"No converter available for property
                                // {p} to convert {tSource.FullNameFormatted()} to
                                // {tTarget.FullNameFormatted()} value:
                                // {propertyValue.ToStringGuessFormat()}", nameof(converter));
                                propertyValue = Util.ChangeType(propertyValue, tTarget);
                            }
                            else
                            {
                                propertyValue = converter(propertyValue, tTarget);
                            }
                        }
                    }
                }

                p.SetValue(instance, propertyValue);
            }

            private object GetPropertyValueInternal(object instance, string propertyName, TypeConverter converter = null, Type returnType = null)
            {
                if (converter != null && returnType == null) throw new ArgumentNullException(nameof(returnType), "If argument '" + nameof(converter) + "' is provided then argument '" + nameof(returnType) + "' must also be provided");
                if (returnType != null && converter == null) throw new ArgumentNullException(nameof(converter), "If argument '" + nameof(returnType) + "' is provided then argument '" + nameof(converter) + "' must also be provided");

                var p = GetProperty(propertyName);
                var o = p.GetValue(instance);

                if (o != null)
                {
                    if (converter != null)
                    {
                        var tSource = p.PropertyInfo.PropertyType;
                        var tTarget = returnType;
                        if (tSource != tTarget)
                        {
                            if (converter == null) throw new ArgumentException($"No converter available for property {p} to convert {tSource.FullNameFormatted()} to {tTarget.FullNameFormatted()} value: {o.ToStringGuessFormat()}", nameof(converter));
                            o = converter(o, tTarget);
                        }
                    }
                }

                return o;
            }

            public static void SetPropertyValue(object instance, string propertyName, object propertyValue, TypeConverter converter = null)
            {
                instance.CheckNotNull(nameof(instance));
                Create(instance.GetType()).SetPropertyValueInternal(instance, propertyName, propertyValue, converter);
            }

            public static object GetPropertyValue(object instance, string propertyName, TypeConverter converter = null, Type returnType = null)
            {
                instance.CheckNotNull(nameof(instance));
                return Create(instance.GetType()).GetPropertyValueInternal(instance, propertyName, converter, returnType);
            }

            public static void SetPropertyValue(Type type, string propertyName, object propertyValue, TypeConverter converter = null)
            {
                type.CheckNotNull(nameof(type));
                Create(type).SetPropertyValueInternal(null, propertyName, propertyValue, converter);
            }

            public static object GetPropertyValue(Type type, string propertyName, TypeConverter converter = null, Type returnType = null)
            {
                type.CheckNotNull(nameof(type));
                return Create(type).GetPropertyValueInternal(null, propertyName, converter, returnType);
            }
        }

        internal sealed class PropertyReaderWriter
        {
            private static readonly IBucketReadOnly<Type, IReadOnlyList<PropertyReaderWriter>> CACHE = new BucketCacheCopyOnWrite<Type, IReadOnlyList<PropertyReaderWriter>>(o => CreateInternal(o));
            private readonly Action<object, object> propertySetter;
            private readonly Func<object, object> propertyGetter;
            private object DefaultNullValue { get; }
            public bool CanGet => propertyGetter != null;
            public bool CanSet => propertySetter != null;
            public PropertyInfo PropertyInfo { get; }
            public bool IsStatic { get; }

            private PropertyReaderWriter(PropertyInfo propertyInfo, bool isStatic)
            {
                PropertyInfo = propertyInfo.CheckNotNull(nameof(propertyInfo));
                IsStatic = isStatic;

                // https://stackoverflow.com/questions/16436323/reading-properties-of-an-object-with-expression-trees
                var pt = propertyInfo.PropertyType;

                if (pt.IsPrimitive || pt.IsValueType || pt.IsEnum)
                {
                    DefaultNullValue = Activator.CreateInstance(pt);
                }
                else
                {
                    DefaultNullValue = null;
                }

                propertyGetter = CreatePropertyGetter(propertyInfo);
                propertySetter = CreatePropertySetter(propertyInfo);
            }

            private static Func<object, object> CreatePropertyGetter(PropertyInfo propertyInfo)
            {
                if (!propertyInfo.CanRead) return null;
                // https://stackoverflow.com/questions/16436323/reading-properties-of-an-object-with-expression-trees
                var mi = propertyInfo.GetGetMethod();
                if (mi == null) return null;
                //IsStatic = mi.IsStatic;
                var instance = Expression.Parameter(typeof(object), "instance");
                var callExpr = mi.IsStatic   // Is this a static property
                    ? Expression.Call(null, mi)
                    : Expression.Call(Expression.Convert(instance, propertyInfo.DeclaringType), mi);

                var unaryExpression = Expression.TypeAs(callExpr, typeof(object));
                var action = Expression.Lambda<Func<object, object>>(unaryExpression, instance).Compile();
                return action;
            }

            private static Action<object, object> CreatePropertySetter(PropertyInfo propertyInfo)
            {
                if (!propertyInfo.CanWrite) return null;
                // https://stackoverflow.com/questions/16436323/reading-properties-of-an-object-with-expression-trees
                var mi = propertyInfo.GetSetMethod();
                if (mi == null) return null;

                //IsStatic = mi.IsStatic;
                var instance = Expression.Parameter(typeof(object), "instance");
                var value = Expression.Parameter(typeof(object), "value");
                var value2 = Expression.Convert(value, propertyInfo.PropertyType);
                var callExpr = mi.IsStatic   // Is this a static property
                    ? Expression.Call(null, mi, value2)
                    : Expression.Call(Expression.Convert(instance, propertyInfo.DeclaringType), mi, value2);
                var action = Expression.Lambda<Action<object, object>>(callExpr, instance, value).Compile();

                return action;
            }

            private static IReadOnlyList<PropertyReaderWriter> CreateInternal(Type type)
            {
                var list = new List<PropertyReaderWriter>();

                foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    list.Add(new PropertyReaderWriter(propertyInfo, false));
                }

                foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                {
                    list.Add(new PropertyReaderWriter(propertyInfo, true));
                }

                return list.Where(o => o.CanGet || o.CanSet).ToList().AsReadOnly();
            }

            public static IReadOnlyList<PropertyReaderWriter> Create(Type type) => CACHE[type.CheckNotNull(nameof(type))];

            public override string ToString() => PropertyInfo.DeclaringType.FullNameFormatted() + "." + PropertyInfo.Name + "{ " + (CanGet ? "get; " : "") + (CanSet ? "set; " : "") + "}";

            public void SetValue(object instance, object propertyValue)
            {
                if (!CanSet) throw new InvalidOperationException("Cannot SET property " + ToString());
                if (propertyValue == null)
                {
                    propertySetter(instance, DefaultNullValue);
                }
                else
                {
                    propertySetter(instance, propertyValue);
                }
            }

            public object GetValue(object instance)
            {
                if (!CanGet) throw new InvalidOperationException("Cannot GET property " + ToString());
                return propertyGetter(instance);
            }
        }

        public static void SetPropertyValue(object instance, string propertyName, object propertyValue, TypeConverter converter = null) => ClassReaderWriter.SetPropertyValue(instance, propertyName, propertyValue, converter);

        public static object GetPropertyValue(object instance, string propertyName, TypeConverter converter = null, Type returnType = null) => ClassReaderWriter.GetPropertyValue(instance, propertyName, converter, returnType);

        public static void SetPropertyValueStatic(Type type, string propertyName, object propertyValue, TypeConverter converter = null) => ClassReaderWriter.SetPropertyValue(type, propertyName, propertyValue, converter);

        public static object GetPropertyValueStatic(Type type, string propertyName, TypeConverter converter = null, Type returnType = null) => ClassReaderWriter.GetPropertyValue(type, propertyName, converter, returnType);

        public static object CopyPropertyValue(object sourceInstance, object targetInstance, string propertyName)
        {
            var o = GetPropertyValue(sourceInstance, propertyName);
            SetPropertyValue(targetInstance, propertyName, o);
            return o;
        }

        #endregion PropertyReaderWriter

        #region SQL

        public static int ExecuteNonQuery(IDbConnection connection, string sql)
        {
            using (connection)
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public static object ExecuteScalar(IDbConnection connection, string sql)
        {
            using (connection)
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;
                    return cmd.ExecuteScalar();
                }
            }
        }

        #endregion SQL

        #region Path

        private static readonly string[] PathDelimiters = new string[] { "/", "\\", Path.DirectorySeparatorChar.ToString(), Path.AltDirectorySeparatorChar.ToString() };

        public static string[] PathParse(string path) => PathParse(path, PathDelimiters);

        public static string[] PathParse(string path, params string[] pathDelimiters)
        {
            return PathParse(path.Yield(), pathDelimiters);
        }

        public static string[] PathParse(IEnumerable<string> pathParts) => PathParse(pathParts, PathDelimiters);

        public static string[] PathParse(string path, IEnumerable<string> pathDelimiters) => PathParse(path.Yield(), pathDelimiters);

        public static string[] PathParse(IEnumerable<string> pathParts, IEnumerable<string> pathDelimiters)
        {
            var list = (pathParts ?? Enumerable.Empty<string>()).TrimOrNull().WhereNotNull().ToList();

            var pathDelimitersArray = pathDelimiters.CheckNotNull(nameof(pathDelimiters)).ToArray();
            var list2 = new List<string>();
            foreach (var item in list)
            {
                var itemparts = item.Split(pathDelimitersArray, StringSplitOptions.RemoveEmptyEntries).TrimOrNull().WhereNotNull().ToArray();
                foreach (var itempart in itemparts) list2.Add(itempart);
            }
            return list2.ToArray();
        }

        public static string PathToString(IEnumerable<string> pathParts, string pathDelimiter = "/", bool trailingDelimiter = true)
        {
            var s = string.Join(pathDelimiter, pathParts).TrimOrNull() ?? string.Empty;
            s = pathDelimiter + s;
            if (s.Length > 1 && trailingDelimiter) s = s + pathDelimiter;
            return s;
        }

        #endregion Path

        #region Guid

        // https://stackoverflow.com/a/49372627

        public static Guid GuidFromLongs(long a, long b)
        {
            var guidData = new byte[16];
            Array.Copy(BitConverter.GetBytes(a), guidData, 8);
            Array.Copy(BitConverter.GetBytes(b), 0, guidData, 8, 8);
            return new Guid(guidData);
        }

        public static (long, long) ToLongs(this Guid guid)
        {
            var bytes = guid.ToByteArray();
            var long1 = BitConverter.ToInt64(bytes, 0);
            var long2 = BitConverter.ToInt64(bytes, 8);
            return (long1, long2);
        }

        public static Guid GuidFromULongs(ulong a, ulong b)
        {
            var guidData = new byte[16];
            Array.Copy(BitConverter.GetBytes(a), guidData, 8);
            Array.Copy(BitConverter.GetBytes(b), 0, guidData, 8, 8);
            return new Guid(guidData);
        }

        public static (ulong, ulong) ToULongs(this Guid guid)
        {
            var bytes = guid.ToByteArray();
            var ulong1 = BitConverter.ToUInt64(bytes, 0);
            var ulong2 = BitConverter.ToUInt64(bytes, 8);
            return (ulong1, ulong2);
        }

        public static Guid GuidFromInts(int a, int b, int c, int d)
        {
            var guidData = new byte[16];
            Array.Copy(BitConverter.GetBytes(a), guidData, 4);
            Array.Copy(BitConverter.GetBytes(b), 0, guidData, 4, 4);
            Array.Copy(BitConverter.GetBytes(c), 0, guidData, 8, 4);
            Array.Copy(BitConverter.GetBytes(d), 0, guidData, 12, 4);
            return new Guid(guidData);
        }

        public static (int, int, int, int) ToInts(this Guid guid)
        {
            var bytes = guid.ToByteArray();
            var a = BitConverter.ToInt32(bytes, 0);
            var b = BitConverter.ToInt32(bytes, 4);
            var c = BitConverter.ToInt32(bytes, 8);
            var d = BitConverter.ToInt32(bytes, 12);
            return (a, b, c, d);
        }

        public static Guid GuidFromUInts(uint a, uint b, uint c, uint d)
        {
            var guidData = new byte[16];
            Array.Copy(BitConverter.GetBytes(a), guidData, 4);
            Array.Copy(BitConverter.GetBytes(b), 0, guidData, 4, 4);
            Array.Copy(BitConverter.GetBytes(c), 0, guidData, 8, 4);
            Array.Copy(BitConverter.GetBytes(d), 0, guidData, 12, 4);
            return new Guid(guidData);
        }

        public static (uint, uint, uint, uint) ToUInts(this Guid guid)
        {
            var bytes = guid.ToByteArray();
            var a = BitConverter.ToUInt32(bytes, 0);
            var b = BitConverter.ToUInt32(bytes, 4);
            var c = BitConverter.ToUInt32(bytes, 8);
            var d = BitConverter.ToUInt32(bytes, 12);
            return (a, b, c, d);
        }

        #endregion Guid

        #region Random

        public static string Random(int size, char[] characterPool)
        {
            // https://stackoverflow.com/a/1344255

            var data = new byte[4 * size];
            using (var crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(data);
            }
            var result = new StringBuilder(size);
            for (var i = 0; i < size; i++)
            {
                var rnd = BitConverter.ToUInt32(data, i * 4);
                var idx = rnd % characterPool.Length;

                result.Append(characterPool[idx]);
            }
            return result.ToString();
        }

        #endregion Random

        #region Threading / Mutex

        private static readonly char[] illegalMutexChars = new char[] { ':', '/', '\\' }
                                                                .Concat(Path.GetInvalidFileNameChars())
                                                                .Concat(Path.GetInvalidPathChars())
                                                                .Concat(Path.DirectorySeparatorChar)
                                                                .Concat(Path.AltDirectorySeparatorChar)
                                                                .Distinct()
                                                                .ToArray();

        public static MutexLock Mutex(string mutexName, TimeSpan timeout) => new MutexLock(MutexNameFormat(mutexName), timeout);

        public static MutexLock Mutex(string mutexName, double seconds) => Mutex(mutexName, TimeSpan.FromSeconds(seconds));

        public static string MutexNameFormat(string mutexName)
        {
            mutexName = mutexName.CheckNotNullTrimmed(nameof(mutexName));
            for (var i = 0; i < illegalMutexChars.Length; i++)
            {
                mutexName = mutexName.Replace(illegalMutexChars[i], '_');
            }
            while (mutexName.Contains("__")) mutexName = mutexName.Replace("__", "_");
            while (mutexName.StartsWith("_")) mutexName = mutexName.RemoveLeft();
            while (mutexName.EndsWith("_")) mutexName = mutexName.RemoveRight();
            mutexName = mutexName.TrimOrNullUpper();
            if (mutexName == null) return "MUTEX";
            if (mutexName.StartsWith("MUTEX")) return mutexName;
            return "MUTEX_" + mutexName;
        }

        #endregion Threading / Mutex

        #region New

        /// <summary>
        /// High performance new object creation. Type must have a default constructor.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Func<object> CreateNewFactory(Type type)
        {
            // https://stackoverflow.com/a/29972767
            //public static readonly Func<T> New = Expression.Lambda<Func<T>>(Expression.New(typeof(T).GetConstructor(Type.EmptyTypes))).Compile();
            //public static readonly Func<T> New = Expression.Lambda<Func<T>>(Expression.New(typeof(T))).Compile();
            //private static readonly ParameterExpression YCreator_Arg_Param = Expression.Parameter(typeof(int), "z");
            //private static readonly Func<int, X> YCreator_Arg = Expression.Lambda<Func<int, X>>(Expression.New(typeof(Y).GetConstructor(new[] { typeof(int), }), new[] { YCreator_Arg_Param, }), YCreator_Arg_Param).Compile();
            var c = Expression.Lambda<Func<object>>(Expression.New(type)).Compile();
            return c;
        }

        public static List<T> CreateList<T, TEnumerable>(params TEnumerable[] enumerables) where TEnumerable : IEnumerable<T>
        {
            var list = new List<T>();
            foreach (var enumerable in enumerables)
            {
                foreach (var item in enumerable)
                {
                    list.Add(item);
                }
            }
            return list;
        }

        #endregion New

        #region Buckets

        private sealed class BucketFunc<TKey, TValue> : IBucket<TKey, TValue>
        {
            private readonly Func<TKey, TValue> getValue;
            private readonly Action<TKey, TValue> setValue;
            private readonly Func<IEnumerable<TKey>> getKeys;
            public IEnumerable<TKey> Keys => getKeys();

            public BucketFunc(Func<TKey, TValue> getValue, Func<IEnumerable<TKey>> getKeys, Action<TKey, TValue> setValue)
            {
                this.getValue = getValue.CheckNotNull(nameof(getValue));
                this.getKeys = getKeys.CheckNotNull(nameof(getKeys));
                this.setValue = setValue.CheckNotNull(nameof(setValue));
            }

            public TValue this[TKey key] { get => getValue(key); set => setValue(key, value); }
        }

        private sealed class BucketReadOnlyFunc<TKey, TValue> : IBucketReadOnly<TKey, TValue>
        {
            private readonly Func<TKey, TValue> getValue;
            private readonly Func<IEnumerable<TKey>> getKeys;
            public IEnumerable<TKey> Keys => getKeys();

            public BucketReadOnlyFunc(Func<TKey, TValue> getValue, Func<IEnumerable<TKey>> getKeys)
            {
                this.getValue = getValue.CheckNotNull(nameof(getValue));
                this.getKeys = getKeys.CheckNotNull(nameof(getKeys));
            }

            public TValue this[TKey key] => getValue(key);
        }

        /// <summary>
        /// Creates a bucket from a getValue and getKeys function. 
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="getValue"></param>
        /// <param name="getKeys"></param>
        /// <returns>A simple bucket wrapper around 2 functions</returns>
        public static IBucketReadOnly<TKey, TValue> CreateBucket<TKey, TValue>(Func<TKey, TValue> getValue, Func<IEnumerable<TKey>> getKeys) => new BucketReadOnlyFunc<TKey, TValue>(getValue, getKeys);

        /// <summary>
        /// Creates a bucket from a getValue and getKeys and setValue function.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="getValue"></param>
        /// <param name="getKeys"></param>
        /// <param name="setValue"></param>
        /// <returns>A simple bucket wrapper around 3 functions</returns>
        public static IBucket<TKey, TValue> CreateBucket<TKey, TValue>(Func<TKey, TValue> getValue, Func<IEnumerable<TKey>> getKeys, Action<TKey, TValue> setValue) => new BucketFunc<TKey, TValue>(getValue, getKeys, setValue);

        #endregion Buckets

        /// <summary>
        /// Parses an encoding name string to an Encoding. Values allowed are...
        /// ASCII
        /// BIGENDIANUNICODE
        /// DEFAULT
        /// UNICODE
        /// UTF32
        /// UTF8
        /// UTF8BOM
        /// If null is provided then UTF8 encoding is returned.
        /// </summary>
        /// <param name="encoding">The encoding name string</param>
        /// <returns>The Encoding or UTF8 Encoding if null is provided</returns>
        public static Encoding ParseEncoding(string encoding)
        {
            encoding = encoding.TrimOrNull();
            if (encoding == null) encoding = "UTF8";

            switch (encoding.ToUpper())
            {
                case "ASCII": return System.Text.Encoding.ASCII;
                case "BIGENDIANUNICODE": return System.Text.Encoding.BigEndianUnicode;
                case "DEFAULT": return System.Text.Encoding.Default;
                case "UNICODE": return System.Text.Encoding.Unicode;
                case "UTF32": return System.Text.Encoding.UTF32;
                case "UTF8": return Utilities.Constant.ENCODING_UTF8_WITHOUT_BOM;
                case "UTF8BOM": return Utilities.Constant.ENCODING_UTF8_WITH_BOM;
            }

            throw new Exception("Unknown encoding type specified: " + encoding);

        }

        public static string ParseInputFile(string inputFile) => ParseInputFiles(inputFile.Yield()).FirstOrDefault();

        public static List<string> ParseInputFiles(IEnumerable<string> inputFiles, bool recursive = false) => inputFiles.OrEmpty()
            .TrimOrNull()
            .WhereNotNull()
            .SelectMany(o => ParseFileName(o, recursive: recursive))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(o => o, StringComparer.OrdinalIgnoreCase)
            .ToList();

        public static List<string> ParseFileName(string fileName, bool recursive = false)
        {
            var l = new List<string>();
            fileName = fileName.TrimOrNull();
            if (fileName == null) return l;
            if (fileName.IndexOf('*') >= 0 || fileName.IndexOf('?') >= 0) // wildcard
            {
                var idx1 = fileName.LastIndexOf(Path.DirectorySeparatorChar);
                var idx2 = fileName.LastIndexOf(Path.AltDirectorySeparatorChar);
                var workingDirectory = System.Environment.CurrentDirectory;
                var filePattern = fileName;
                if (idx1 == -1 && idx2 == -1)
                {
                    // No directory prefix
                }
                else if (idx1 == idx2 || idx1 > idx2)
                {
                    workingDirectory = fileName.Substring(0, idx1);
                    filePattern = fileName.Substring(idx1 + 1);
                }
                else
                {
                    workingDirectory = fileName.Substring(0, idx2);
                    filePattern = fileName.Substring(idx2 + 1);
                }

                foreach (var f in Util.FileListFiles(workingDirectory))
                {
                    var n = Path.GetFileName(f);
                    if (n.EqualsWildcard(filePattern, true))
                    {
                        l.Add(f);
                    }
                }
            }
            else
            {
                fileName = Path.GetFullPath(fileName);
                if (Util.IsDirectory(fileName))
                {
                    l.AddRange(Util.FileListFiles(fileName, recursive));
                }
                else if (Util.IsFile(fileName))
                {
                    l.Add(fileName);
                }
            }

            return l;
        }

        public static List<string> ParseFileNames(IEnumerable<string> fileNames)
        {
            var l = new List<string>();
            foreach (var fileName in fileNames.OrEmpty())
            {
                l.AddRange(ParseFileName(fileName));
            }
            return l.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        #region Encryption

        public static byte[] EncryptionEncryptAsymetric(string pemPublicKey, byte[] data, RSAEncryptionPadding encryptionPadding)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportFromPem(pemPublicKey.ToCharArray());
                return rsa.Encrypt(data, encryptionPadding);
            }
        }

        public static byte[] EncryptionDecryptAsymetric(string pemPrivateKey, byte[] data, RSAEncryptionPadding encryptionPadding)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportFromPem(pemPrivateKey.ToCharArray());
                return rsa.Decrypt(data, encryptionPadding);
            }
        }

        public static (string publicKey, string privateKey) EncryptionGeneratePublicPrivateKeys(int length)
        {
            using (var rsa = RSA.Create(length))
            {
                //rsa.ExportParameters(true);
                string publicKey = EncryptionGeneratePublicPrivateKeysHelper.ExportPublicKey(rsa);
                string privateKey = EncryptionGeneratePublicPrivateKeysHelper.ExportPrivateKey(rsa);
                return (publicKey, privateKey);
            }
        }

        private static class EncryptionGeneratePublicPrivateKeysHelper
        {
            public static string ExportPrivateKey(RSA rsa)
            {
                var sb = new StringWriter();
                ExportPrivateKey(rsa, sb);
                return sb.ToString();
            }

            public static string ExportPublicKey(RSA rsa)
            {
                var sb = new StringWriter();
                ExportPublicKey(rsa, sb);
                return sb.ToString();
            }

            private static void ExportPrivateKey(RSA csp, TextWriter outputStream)
            {
                // https://stackoverflow.com/a/23739932
                var parameters = csp.ExportParameters(true);
                using (var stream = new MemoryStream())
                {
                    var writer = new BinaryWriter(stream);
                    writer.Write((byte)0x30); // SEQUENCE
                    using (var innerStream = new MemoryStream())
                    {
                        var innerWriter = new BinaryWriter(innerStream);
                        EncodeIntegerBigEndian(innerWriter, new byte[] { 0x00 }); // Version
                        EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
                        EncodeIntegerBigEndian(innerWriter, parameters.Exponent);
                        EncodeIntegerBigEndian(innerWriter, parameters.D);
                        EncodeIntegerBigEndian(innerWriter, parameters.P);
                        EncodeIntegerBigEndian(innerWriter, parameters.Q);
                        EncodeIntegerBigEndian(innerWriter, parameters.DP);
                        EncodeIntegerBigEndian(innerWriter, parameters.DQ);
                        EncodeIntegerBigEndian(innerWriter, parameters.InverseQ);
                        var length = (int)innerStream.Length;
                        EncodeLength(writer, length);
                        writer.Write(innerStream.GetBuffer(), 0, length);
                    }

                    var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
                    outputStream.WriteLine("-----BEGIN RSA PRIVATE KEY-----");
                    // Output as Base64 with lines chopped at 64 characters
                    for (var i = 0; i < base64.Length; i += 64)
                    {
                        outputStream.WriteLine(base64, i, Math.Min(64, base64.Length - i));
                    }
                    outputStream.WriteLine("-----END RSA PRIVATE KEY-----");
                }
            }

            private static void ExportPublicKey(RSA csp, TextWriter outputStream)
            {
                // https://stackoverflow.com/a/28407693
                var parameters = csp.ExportParameters(false);
                using (var stream = new MemoryStream())
                {
                    var writer = new BinaryWriter(stream);
                    writer.Write((byte)0x30); // SEQUENCE
                    using (var innerStream = new MemoryStream())
                    {
                        var innerWriter = new BinaryWriter(innerStream);
                        innerWriter.Write((byte)0x30); // SEQUENCE
                        EncodeLength(innerWriter, 13);
                        innerWriter.Write((byte)0x06); // OBJECT IDENTIFIER
                        var rsaEncryptionOid = new byte[] { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x01 };
                        EncodeLength(innerWriter, rsaEncryptionOid.Length);
                        innerWriter.Write(rsaEncryptionOid);
                        innerWriter.Write((byte)0x05); // NULL
                        EncodeLength(innerWriter, 0);
                        innerWriter.Write((byte)0x03); // BIT STRING
                        using (var bitStringStream = new MemoryStream())
                        {
                            var bitStringWriter = new BinaryWriter(bitStringStream);
                            bitStringWriter.Write((byte)0x00); // # of unused bits
                            bitStringWriter.Write((byte)0x30); // SEQUENCE
                            using (var paramsStream = new MemoryStream())
                            {
                                var paramsWriter = new BinaryWriter(paramsStream);
                                EncodeIntegerBigEndian(paramsWriter, parameters.Modulus); // Modulus
                                EncodeIntegerBigEndian(paramsWriter, parameters.Exponent); // Exponent
                                var paramsLength = (int)paramsStream.Length;
                                EncodeLength(bitStringWriter, paramsLength);
                                bitStringWriter.Write(paramsStream.GetBuffer(), 0, paramsLength);
                            }
                            var bitStringLength = (int)bitStringStream.Length;
                            EncodeLength(innerWriter, bitStringLength);
                            innerWriter.Write(bitStringStream.GetBuffer(), 0, bitStringLength);
                        }
                        var length = (int)innerStream.Length;
                        EncodeLength(writer, length);
                        writer.Write(innerStream.GetBuffer(), 0, length);
                    }

                    var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
                    outputStream.WriteLine("-----BEGIN PUBLIC KEY-----");
                    for (var i = 0; i < base64.Length; i += 64)
                    {
                        outputStream.WriteLine(base64, i, Math.Min(64, base64.Length - i));
                    }
                    outputStream.WriteLine("-----END PUBLIC KEY-----");
                }
            }

            private static void EncodeLength(BinaryWriter stream, int length)
            {
                if (length < 0) throw new ArgumentOutOfRangeException("length", "Length must be non-negative");
                if (length < 0x80)
                {
                    // Short form
                    stream.Write((byte)length);
                }
                else
                {
                    // Long form
                    var temp = length;
                    var bytesRequired = 0;
                    while (temp > 0)
                    {
                        temp >>= 8;
                        bytesRequired++;
                    }
                    stream.Write((byte)(bytesRequired | 0x80));
                    for (var i = bytesRequired - 1; i >= 0; i--)
                    {
                        stream.Write((byte)(length >> (8 * i) & 0xff));
                    }
                }
            }

            private static void EncodeIntegerBigEndian(BinaryWriter stream, byte[] value, bool forceUnsigned = true)
            {
                stream.Write((byte)0x02); // INTEGER
                var prefixZeros = 0;
                for (var i = 0; i < value.Length; i++)
                {
                    if (value[i] != 0) break;
                    prefixZeros++;
                }
                if (value.Length - prefixZeros == 0)
                {
                    EncodeLength(stream, 1);
                    stream.Write((byte)0);
                }
                else
                {
                    if (forceUnsigned && value[prefixZeros] > 0x7f)
                    {
                        // Add a prefix zero to force unsigned if the MSB is 1
                        EncodeLength(stream, value.Length - prefixZeros + 1);
                        stream.Write((byte)0);
                    }
                    else
                    {
                        EncodeLength(stream, value.Length - prefixZeros);
                    }
                    for (var i = prefixZeros; i < value.Length; i++)
                    {
                        stream.Write(value[i]);
                    }
                }
            }

        }

        #endregion Encryption

    }
}
