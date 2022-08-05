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
#if !DEBUG
using System.Diagnostics;
#endif

namespace MaxRunSoftware.Utilities;

public static partial class Constant
{
    private static readonly Queue<LogEventArgs> logQueue = new(1000);
    private static readonly object logLock = new();

    private static readonly IDictionary<int, Action<LogEventArgs>> logListeners = new SortedDictionary<int, Action<LogEventArgs>>();

    public static int LogSubscribe(Action<LogEventArgs> listener)
    {
        if (listener == null) throw new ArgumentNullException(nameof(listener));
        var id = NextInt();

        lock (logLock) { logListeners.Add(id, listener); }

        return id;
    }

    public static bool LogUnsubscribe(int id)
    {
        lock (logLock) { return logListeners.Remove(id); }
    }

    public static void LogUnsubscribeAll()
    {
        lock (logLock) { logListeners.Clear(); }
    }

    private static readonly LogThread logThread = new();
    public static bool LogIsRunning => logThread.IsRunning;

    private static void Log(LogEventArgs args)
    {
        if (args == null) return;
        lock (logLock) { logQueue.Enqueue(args); }
    }

    public static ILogger GetLogger(Type type)
    {
#if !DEBUG
        type ??= new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType;
#endif
        if (type == null) throw new ArgumentNullException(nameof(type));
        return new Logger(type, Log);
    }

    private sealed class LoggerNull : LoggerBase
    {
        public override void Log(string message, Exception exception, LogLevel level) { }
    }

    public static readonly ILogger loggerNull = new LoggerNull();
    public static ILogger GetLoggerNull() => loggerNull;


    private class Logger : LoggerBase
    {
        private readonly Type type;
        private readonly Action<LogEventArgs> onLog;

        public Logger(Type type, Action<LogEventArgs> onLog)
        {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
            this.onLog = onLog ?? throw new ArgumentNullException(nameof(onLog));
        }

        public override void Log(string message, Exception exception, LogLevel level) => onLog(new LogEventArgs(message, exception, level, type));
    }


    private class LogThread : IDisposable
    {
        private readonly Thread thread;

        public bool IsRunning => !IsShutdown && thread.IsAlive;
        private bool IsShutdown { get; set; }

        public LogThread()
        {
            var threadNameId = NextInt();
            thread = new Thread(Work)
            {
                Name = nameof(LogThread) + "[" + threadNameId + "]",
                IsBackground = true
            };
            AppDomain.CurrentDomain.ProcessExit += delegate { Dispose(); };
            thread.Start();
        }

        private void Work()
        {
            while (true)
            {
                var items = new Queue<LogEventArgs>(1000);
                var listeners = new List<Action<LogEventArgs>>(100);
                lock (logLock)
                {
                    if (!IsShutdown && logListeners.Count > 0 && logQueue.Count > 0)
                    {
                        listeners.Clear();
                        listeners.AddRange(logListeners.Values);
                        while (logQueue.Count > 0) { items.Enqueue(logQueue.Dequeue()); }
                    }
                }

                if (!IsShutdown && listeners.Count > 0 && items.Count > 0)
                {
                    while (items.Count > 0)
                    {
                        var o = items.Dequeue();
                        if (o == null) { IsShutdown = true; }
                        else
                        {
                            foreach (var listener in listeners)
                            {
                                try { listener(o); }
                                catch (Exception e) { LogError(e); }
                            }
                        }
                    }
                }

                if (IsShutdown) break;
                Thread.Sleep(50);
            }
        }

        public void Dispose() => IsShutdown = true;
    }
}
