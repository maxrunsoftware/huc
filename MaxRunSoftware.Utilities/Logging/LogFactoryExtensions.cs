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

namespace MaxRunSoftware.Utilities;

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
            Level = level;
            this.filename = Path.GetFullPath(filename.CheckNotNullTrimmed(nameof(filename)));
            file = new FileInfo(filename);
            id = Guid.NewGuid().ToString().Replace("-", "");
        }

        public void Log(object sender, LogEventArgs e)
        {
            if (e == null) return;
            using (MutexLock.CreateGlobal(TimeSpan.FromSeconds(10), file))
            {
                Util.FileWrite(filename, e.ToStringDetailed(id: id) + Environment.NewLine, Constant.ENCODING_UTF8, append: true);
            }

        }
    }

    private class ConsoleLogger : ILogAppender
    {
        public LogLevel Level { get; init; }
        public ConsoleLogger(LogLevel level)
        {
            Level = level;

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
                    Console.WriteLine(e.ToStringDetailed());
                }
            }
            else
            {
                if (e.Message != null) Console.WriteLine(e.Message);
                if (e.Exception != null) Console.WriteLine(e.Exception.ToString());
            }
        }

    }
}
