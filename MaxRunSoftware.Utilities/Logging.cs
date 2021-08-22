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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace MaxRunSoftware.Utilities
{
    public enum LogLevel : byte
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Critical = 5
    }

    public interface ILogger
    {
        void Trace(string message);

        void Trace(Exception exception);

        void Trace(string message, Exception exception);

        void Debug(string message);

        void Debug(Exception exception);

        void Debug(string message, Exception exception);

        void Info(string message);

        void Info(Exception exception);

        void Info(string message, Exception exception);

        void Warn(string message);

        void Warn(Exception exception);

        void Warn(string message, Exception exception);

        void Error(string message);

        void Error(Exception exception);

        void Error(string message, Exception exception);

        void Critical(string message);

        void Critical(Exception exception);

        void Critical(string message, Exception exception);
    }

    public interface ILogFactory : IDisposable
    {
        bool IsTraceEnabled { get; }

        bool IsDebugEnabled { get; }

        bool IsInfoEnabled { get; }

        bool IsWarnEnabled { get; }

        bool IsErrorEnabled { get; }

        bool IsCriticalEnabled { get; }

        //event EventHandler<LogEventArgs> Logging;

        ILogger GetLogger<T>();

        ILogger GetLogger(Type type);

        void AddAppender(ILogAppender appender);

    }

    public interface ILogAppender
    {
        LogLevel Level { get; }
        void Log(object sender, LogEventArgs args);
    }


    internal class LogBackgroundThread : IDisposable
    {
        private static int threadCount = 0;

        private readonly Action<LogEventArgs> onLogging;
        private readonly BlockingCollection<LogEventArgs> queue = new BlockingCollection<LogEventArgs>();
        private readonly Thread thread;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        public LogBackgroundThread(Action<LogEventArgs> onLogging)
        {
            this.onLogging = onLogging;
            var threadNameId = Interlocked.Increment(ref threadCount);
            thread = new Thread(new ThreadStart(Work))
            {
                Name = "LogBackgroundThread[" + threadNameId + "]",
                IsBackground = true
            };
            thread.Start();
        }

        private static void LogError(object o) => System.Console.Error.WriteLine(o);

        private void Work()
        {
            while (true)
            {
                if (queue.IsAddingCompleted && queue.IsCompleted) return;
                LogEventArgs t = null;
                try
                {
                    var result = queue.TryTake(out t, Timeout.Infinite, cancellation.Token);
                    if (!result)
                    {
                        LogError("Received unexpected [false] value received from queue.TryTake(), sleeping for 50ms");
                        Thread.Sleep(50);
                    }
                }
                catch (OperationCanceledException)
                {
                    //LogError($"Received unexpected {nameof(OperationCanceledException)}, sleeping for 50ms");
                    Thread.Sleep(50);
                }
                catch (Exception e)
                {
                    LogError($"Received unexpected exception requesting next item, cancelling thread {thread.Name}");
                    LogError(e);
                    return;
                }
                if (t != null)
                {
                    onLogging(t);
                }
            }
        }

        public void AddItem(LogEventArgs logEventArgs)
        {
            try
            {
                queue.Add(logEventArgs);
            }
            catch (Exception e)
            {
                LogError($"Received unexpected exception adding item --> " + logEventArgs.ToStringDetailed());
                LogError(e);
            }
        }

        public void Dispose()
        {
            try
            {
                queue.CompleteAdding();
            }
            catch (Exception e)
            {
                LogError("Received exception on queue.CompleteAdding()");
                LogError(e);
            }

            try
            {
                var timeStart = DateTime.UtcNow;
                var duration = TimeSpan.FromSeconds(5);
                while (!queue.IsCompleted)
                {
                    Thread.Sleep(50);
                    if ((DateTime.UtcNow - timeStart) > duration)
                    {
                        LogError("Waiting for queue.IsCompleted == true (queue: " + queue.Count + ")");
                        timeStart = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception e)
            {
                LogError("Received exception on queue.IsCompleted");
                LogError(e);
            }

            try
            {
                cancellation.Cancel();
            }
            catch (Exception e)
            {
                LogError("Received exception on cancellation.Cancel()");
                LogError(e);
            }

            try
            {
                var timeStart = DateTime.UtcNow;
                var duration = TimeSpan.FromSeconds(5);
                if (queue.Count > 0)
                {
                    while (thread.IsAlive)
                    {
                        Thread.Sleep(50);
                        if ((DateTime.UtcNow - timeStart) > duration)
                        {
                            //LogError("Waiting for thread.IsAlive == true");
                            //timeStart = DateTime.UtcNow;
                            throw new Exception("Timeout waiting for thread exceeded");
                        }
                    }
                }
            }

            catch (Exception)
            {
                //LogError("Received exception on thread.IsAlive");
                //LogError(e);
            }

            try
            {
                thread.Join();
            }
            catch (Exception e)
            {
                LogError("Received exception on thread.Join()");
                LogError(e);
            }
        }
    }

    public static class LogFactoryExtensions
    {
        public static void SetupConsole(this ILogFactory logFactory, LogLevel level) => logFactory.AddAppender(new ConsoleLogger(level));
        public static void SetupFile(this ILogFactory logFactory, LogLevel level, string filename) => logFactory.AddAppender(new FileLogger(level, filename));

        private class FileLogger : ILogAppender
        {
            public LogLevel Level { get; init; }
            private readonly string filename;
            private readonly FileInfo file;
            private readonly string id;
            public FileLogger(LogLevel level, string filename)
            {
                this.Level = level;
                this.filename = Path.GetFullPath(filename.CheckNotNullTrimmed(nameof(filename)));
                this.file = new FileInfo(filename);
                this.id = Guid.NewGuid().ToString().Replace("-", "");
            }

            public void Log(object sender, LogEventArgs e)
            {
                if (e == null) return;
                using (MutexLock.CreateGlobal(TimeSpan.FromSeconds(10), file))
                {
                    Util.FileWrite(filename, e.ToStringDetailed(id: id) + Environment.NewLine, Constant.ENCODING_UTF8_WITHOUT_BOM, append: true);
                }

            }
        }

        private class ConsoleLogger : ILogAppender
        {
            public LogLevel Level { get; init; }
            public ConsoleLogger(LogLevel level)
            {
                this.Level = level;

            }
            private IDisposable LogConsoleColor(LogLevel level)
            {
                switch (level)
                {
                    case LogLevel.Trace: return Util.ChangeConsoleColor(foreground: ConsoleColor.DarkBlue);
                    case LogLevel.Debug: return Util.ChangeConsoleColor(foreground: ConsoleColor.Blue);
                    case LogLevel.Info: return Util.ChangeConsoleColor(foreground: ConsoleColor.Gray);
                    case LogLevel.Warn: return Util.ChangeConsoleColor(foreground: ConsoleColor.DarkYellow);
                    case LogLevel.Error: return Util.ChangeConsoleColor(foreground: ConsoleColor.Red);
                    case LogLevel.Critical: return Util.ChangeConsoleColor(foreground: ConsoleColor.White, background: ConsoleColor.Red);
                    default: return Util.ChangeConsoleColor();
                }
            }

            public void Log(object sender, LogEventArgs e)
            {
                if (e == null) return;
                if (Level == LogLevel.Debug || Level == LogLevel.Trace)
                {
                    using (LogConsoleColor(e.Level))
                    {
                        System.Console.WriteLine(e.ToStringDetailed());
                    }
                }
                else
                {
                    if (e.Message != null) System.Console.WriteLine(e.Message);
                    if (e.Exception != null) System.Console.WriteLine(e.Exception.ToString());
                }
            }

        }
    }

    public class LogFactory : ILogFactory
    {
        public static ILogFactory LogFactoryImpl { get; } = new LogFactory();

        private readonly object locker = new object();
        private readonly LogBackgroundThread thread;
        private volatile Dictionary<Type, ILogger> loggers = new Dictionary<Type, ILogger>();

        public bool IsTraceEnabled { get; private set; }
        public bool IsDebugEnabled { get; private set; }
        public bool IsInfoEnabled { get; private set; }
        public bool IsWarnEnabled { get; private set; }
        public bool IsErrorEnabled { get; private set; }
        public bool IsCriticalEnabled { get; private set; }

        public LogFactory()
        {
            thread = new LogBackgroundThread(OnLogging);
            AppDomain.CurrentDomain.ProcessExit += delegate { Dispose(); };
        }

        private class Logger : LoggerBase
        {
            private readonly LogFactory logFactory;
            private readonly Type type;

            public Logger(LogFactory logFactory, Type type)
            {
                this.logFactory = logFactory;
                this.type = type;
            }

            protected override void Log(string message, Exception exception, LogLevel level)
            {
                if (logFactory.ShouldLog(level)) logFactory.thread.AddItem(new LogEventArgs(message, exception, level, type));
            }
        }

        protected bool ShouldLog(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace: return IsTraceEnabled;
                case LogLevel.Debug: return IsDebugEnabled;
                case LogLevel.Info: return IsInfoEnabled;
                case LogLevel.Warn: return IsWarnEnabled;
                case LogLevel.Error: return IsErrorEnabled;
                case LogLevel.Critical: return IsCriticalEnabled;
                default: throw new NotImplementedException();
            }
        }

        protected virtual ILogger CreateLogger(Type type) => new Logger(this, type);

        public ILogger GetLogger<T>() => GetLogger(typeof(T));

        public ILogger GetLogger(Type type)
        {
            type.CheckNotNull(nameof(type));
            if (loggers.TryGetValue(type, out var logger)) return logger;

            lock (locker)
            {
                if (loggers.TryGetValue(type, out logger)) return logger;

                logger = CreateLogger(type);
                var d = new Dictionary<Type, ILogger>(loggers) { { type, logger } };
                loggers = d;
                return logger;
            }
        }

        public void OnLogging(LogEventArgs args)
        {

            foreach (ILogAppender appender in appenders)
            {
                if (appender == null) continue;
                bool shouldLog = false;
                try
                {
                    var level = args.Level;
                    var levelAppender = appender.Level;
                    if (level == LogLevel.Trace && levelAppender.In(LogLevel.Trace)) shouldLog = true;
                    else if (level == LogLevel.Debug && levelAppender.In(LogLevel.Debug, LogLevel.Trace)) shouldLog = true;
                    else if (level == LogLevel.Info && levelAppender.In(LogLevel.Info, LogLevel.Debug, LogLevel.Trace)) shouldLog = true;
                    else if (level == LogLevel.Warn && levelAppender.In(LogLevel.Warn, LogLevel.Info, LogLevel.Debug, LogLevel.Trace)) shouldLog = true;
                    else if (level == LogLevel.Error && levelAppender.In(LogLevel.Error, LogLevel.Warn, LogLevel.Info, LogLevel.Debug, LogLevel.Trace)) shouldLog = true;
                    else if (level == LogLevel.Critical) shouldLog = true;

                    if (shouldLog)
                    {
                        appender.Log(this, args);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }

        public void Dispose()
        {
            try
            {
                thread.Dispose();
            }
            catch (Exception e)
            {
                System.Console.Error.Write("ERROR DISPOSING OF " + GetType().FullNameFormatted());
                System.Console.Error.WriteLine(e);
            }
        }

        private readonly List<ILogAppender> appenders = new List<ILogAppender>();
        public void AddAppender(ILogAppender appender)
        {

            appenders.Add(appender);

            IsTraceEnabled = false;
            IsDebugEnabled = false;
            IsInfoEnabled = false;
            IsWarnEnabled = false;
            IsErrorEnabled = false;
            IsCriticalEnabled = false;

            foreach (var app in appenders)
            {
                var level = app.Level;
                if (level == LogLevel.Trace)
                {
                    IsTraceEnabled = true;
                    IsDebugEnabled = true;
                    IsInfoEnabled = true;
                    IsWarnEnabled = true;
                    IsErrorEnabled = true;
                    IsCriticalEnabled = true;
                }
                else if (level == LogLevel.Debug)
                {
                    IsDebugEnabled = true;
                    IsInfoEnabled = true;
                    IsWarnEnabled = true;
                    IsErrorEnabled = true;
                    IsCriticalEnabled = true;
                }
                else if (level == LogLevel.Info)
                {
                    IsInfoEnabled = true;
                    IsWarnEnabled = true;
                    IsErrorEnabled = true;
                    IsCriticalEnabled = true;
                }
                else if (level == LogLevel.Warn)
                {
                    IsWarnEnabled = true;
                    IsErrorEnabled = true;
                    IsCriticalEnabled = true;
                }
                else if (level == LogLevel.Error)
                {
                    IsErrorEnabled = true;
                    IsCriticalEnabled = true;
                }
                else if (level == LogLevel.Error)
                {
                    IsCriticalEnabled = true;
                }
            }


        }


    }

    public abstract class LoggerBase : ILogger
    {
        protected abstract void Log(string message, Exception exception, LogLevel level);

        public void Trace(string message) => Trace(message, null);

        public void Trace(Exception exception) => Trace(null, exception);

        public void Trace(string message, Exception exception) => Log(message, exception, LogLevel.Trace);

        public void Debug(string message) => Debug(message, null);

        public void Debug(Exception exception) => Debug(null, exception);

        public void Debug(string message, Exception exception) => Log(message, exception, LogLevel.Debug);

        public void Info(string message) => Info(message, null);

        public void Info(Exception exception) => Info(null, exception);

        public void Info(string message, Exception exception) => Log(message, exception, LogLevel.Info);

        public void Warn(string message) => Warn(message, null);

        public void Warn(Exception exception) => Warn(null, exception);

        public void Warn(string message, Exception exception) => Log(message, exception, LogLevel.Warn);

        public void Error(string message) => Error(message, null);

        public void Error(Exception exception) => Error(null, exception);

        public void Error(string message, Exception exception) => Log(message, exception, LogLevel.Error);

        public void Critical(string message) => Critical(message, null);

        public void Critical(Exception exception) => Critical(null, exception);

        public void Critical(string message, Exception exception) => Log(message, exception, LogLevel.Critical);
    }

    public sealed class LogEventArgs : EventArgs
    {
        public string Message { get; }
        public Exception Exception { get; }
        public LogLevel Level { get; }
        public Type Type { get; }
        public DateTime UtcTimestamp { get; }
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
            UtcTimestamp = DateTime.UtcNow;
            ThreadName = Thread.CurrentThread.Name.TrimOrNull();
            ThreadId = Thread.CurrentThread.ManagedThreadId;

            if (frame != null)
            {
                try { CallingMethod = frame.GetMethod(); } catch (Exception) { }
                if (CallingMethod != null) try { CallingType = CallingMethod.DeclaringType; } catch (Exception) { }
                try { CallingFile = frame.GetFileName().TrimOrNull(); } catch (Exception) { }
                if (CallingFile != null) try { CallingFileName = Path.GetFileName(CallingFile).TrimOrNull(); } catch (Exception) { }
                try
                {
                    var cfln = frame.GetFileLineNumber();
                    if (cfln < 1) CallingFileLineNumber = null;
                    else CallingFileLineNumber = cfln;
                }
                catch (Exception) { }
            }
        }

        public override string ToString()
        {
            if (Message != null)
            {
                if (Exception != null)
                {
                    return Message + Environment.NewLine + Exception;
                }
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
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(id);
            }

            if (includeTimestamp)
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(UtcTimestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff"));
            }

            if (includeLevel)
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append($"[{Level}]".PadRight(7));
            }

            if (includeThread)
            {
                if (sb.Length > 0) sb.Append(" ");
                var threadName = ThreadName ?? "";
                sb.Append($"{threadName}({ThreadId})");
            }

            if (includeType)
            {
                if (sb.Length > 0) sb.Append(" ");
                var strType = Type?.NameFormatted() ?? "?";
                sb.Append($"[{strType}]");
            }

            if (includeCallingFile)
            {
                if (CallingFileName != null)
                {
                    if (sb.Length > 0) sb.Append(" ");
                    sb.Append(CallingFileName + "[" + CallingFileLineNumber + "]");
                }
            }

            if (includeCallingMethod)
            {
                if (CallingMethod != null)
                {
                    if (sb.Length > 0) sb.Append(" ");
                    sb.Append(CallingMethod.GetSignature(false));
                }
            }

            if (Message != null)
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(Message);
            }
            if (Exception != null)
            {
                sb.AppendLine("");
                sb.Append(Exception.ToString());
            }

            return sb.ToString();
        }
    }
}
