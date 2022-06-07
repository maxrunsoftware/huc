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

using System.Diagnostics;

namespace MaxRunSoftware.Utilities;

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
