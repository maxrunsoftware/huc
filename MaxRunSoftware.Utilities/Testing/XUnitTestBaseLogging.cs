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

using System.Threading;

namespace MaxRunSoftware.Utilities;

public abstract partial class XUnitTestBase
{
    private int logCurrentLineNumber;
    protected virtual int LogMaxLines => int.MaxValue;
    protected bool LogNulls { get; set; } = true;
    protected string LogNullsString { get; set; } = "~null~";

    protected bool LogDisablePrefix { get; set; }

    protected virtual string LogPrefix
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append($"[{TestNumber}]");

            var testName = TestName;
            if (testName == null)
                sb.Append(": ");
            else
                sb.Append(" " + testName + ": ");

            return sb.ToString();
        }
    }

    protected void Debug(object o) => Log(o, LogLevel.Debug);
    protected void Info(object o) => Log(o, LogLevel.Info);
    protected void Info() => Log(string.Empty, LogLevel.Info);
    protected void Warn(object o) => Log(o, LogLevel.Warn);
    protected void Error(object o) => Log(o, LogLevel.Error);

    private bool showedLog;
    private static readonly List<string> staticMessagesLog = new();

    protected static void LogStatic(string message)
    {
        lock (lockerStatic) { staticMessagesLog.Add(message); }
    }

    private void Log(object? o, LogLevel level)
    {
        var prefix = LogPrefix;
        if (IsDebug) prefix += "[" + level + "] ";
        else if (level is LogLevel.Trace or LogLevel.Debug) return;
        else
        {
            prefix += level switch
            {
                LogLevel.Warn => "* WARN *  ",
                LogLevel.Error => "*** ERROR ****  ",
                _ => ""
            };
        }

        if (LogDisablePrefix) prefix = string.Empty;

        if (o == null)
        {
            if (LogNulls) Log(prefix + LogNullsString);
            return;
        }

        var s = o.ToStringGuessFormat();
        if (s == null)
        {
            if (LogNulls) Log(prefix + LogNullsString);
            return;
        }


        lock (lockerStatic)
        {
            lock (locker)
            {
                if (!showedLog)
                {
                    foreach (var message in staticMessagesLog) Log(message);
                    showedLog = true;
                }
            }

            Log(prefix + s);
        }
    }

    private void Log(string message)
    {
        if (logCurrentLineNumber > LogMaxLines) return;
        var log = Logger.CheckPropertyNotNull(nameof(Logger), GetType());
        log(message);
        Interlocked.Increment(ref logCurrentLineNumber);
    }
}
