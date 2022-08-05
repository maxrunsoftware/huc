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

using System.Diagnostics;
using System.Threading;

namespace MaxRunSoftware.Utilities;

public sealed class LogEventArgs : EventArgs
{
    public string Message { get; }
    public Exception Exception { get; }
    public LogLevel Level { get; }
    public Type Type { get; }
    public DateTime TimestampUtc { get; }
    public DateTime Timestamp { get; }
    public string ThreadName { get; }
    public int ThreadId { get; }

    public MethodBase CallingMethod { get; }
    public Type CallingType { get; }
    public string CallingFile { get; }
    public string CallingFileName { get; }
    public int? CallingFileLineNumber { get; }

    public LogEventArgs(string message, Exception exception, LogLevel level, Type type, StackFrame frame = null)
    {
        Message = message;
        Exception = exception;
        if (message == null && exception != null) Message = exception.Message;

        Level = level;
        Type = type;
        TimestampUtc = DateTime.UtcNow;
        Timestamp = TimestampUtc.ToLocalTime();
        ThreadName = Thread.CurrentThread.Name.TrimOrNull();
        ThreadId = Thread.CurrentThread.ManagedThreadId;

        if (frame != null)
        {
            try { CallingMethod = frame.GetMethod(); }
            catch (Exception) { }

            if (CallingMethod != null)
            {
                try { CallingType = CallingMethod.DeclaringType; }
                catch (Exception) { }
            }

            try { CallingFile = frame.GetFileName().TrimOrNull(); }
            catch (Exception) { }

            if (CallingFile != null)
            {
                try { CallingFileName = Path.GetFileName(CallingFile).TrimOrNull(); }
                catch (Exception) { }
            }

            try
            {
                var fileLineNumber = frame.GetFileLineNumber();
                if (fileLineNumber < 1) { CallingFileLineNumber = null; }
                else { CallingFileLineNumber = fileLineNumber; }
            }
            catch (Exception) { }
        }
    }

    public override string ToString()
    {
        if (Message != null)
        {
            if (Exception != null) return Message + Environment.NewLine + Exception;

            return Message;
        }

        return Exception?.ToString();
    }

    public string ToStringDetailed(
        bool includeTimestamp = true,
        bool includeLevel = true,
        bool includeThread = true,
        bool includeType = true,
        bool includeCallingFile = true,
        bool includeCallingMethod = true,
        string id = null
    )
    {
        var sb = new StringBuilder();

        if (id != null)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(id);
        }

        if (includeTimestamp)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff"));
        }

        if (includeLevel)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append($"[{Level}]".PadRight(7));
        }

        if (includeThread)
        {
            if (sb.Length > 0) sb.Append(' ');
            var threadName = ThreadName ?? "";
            sb.Append($"{threadName}({ThreadId})");
        }

        if (includeType)
        {
            if (sb.Length > 0) sb.Append(' ');
            var strType = Type?.NameFormatted() ?? "?";
            sb.Append($"[{strType}]");
        }

        if (includeCallingFile)
        {
            if (CallingFileName != null)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(CallingFileName + "[" + CallingFileLineNumber + "]");
            }
        }

        if (includeCallingMethod)
        {
            if (CallingMethod != null)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(CallingMethod.GetSignature(false));
            }
        }

        if (Message != null)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(Message);
        }

        if (Exception != null)
        {
            sb.AppendLine("");
            sb.Append(Exception);
        }

        return sb.ToString();
    }
}
