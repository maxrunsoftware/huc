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

namespace MaxRunSoftware.Utilities;

public class LogListenerConsole
{
    public LogLevel ListenerLevel { get; }
    public bool IsColor { get; }

    public LogListenerConsole(LogLevel listenerLevel, bool isColor = true)
    {
        ListenerLevel = listenerLevel;
        IsColor = isColor;
    }

    private class NullDisposable : IDisposable
    {
        public void Dispose() { }
    }

    private static readonly IDisposable nullDisposable = new NullDisposable();

    private IDisposable LogConsoleColor(LogLevel level)
    {
        if (!IsColor) return nullDisposable;
        switch (level)
        {
            case LogLevel.Trace: return Util.ChangeConsoleColor(ConsoleColor.DarkBlue);
            case LogLevel.Debug: return Util.ChangeConsoleColor(ConsoleColor.Blue);
            case LogLevel.Info: return Util.ChangeConsoleColor(ConsoleColor.Gray);
            case LogLevel.Warn: return Util.ChangeConsoleColor(ConsoleColor.DarkYellow);
            case LogLevel.Error: return Util.ChangeConsoleColor(ConsoleColor.Red);
            default: return Util.ChangeConsoleColor();
        }
    }

    public void Log(LogEventArgs args)
    {
        if (args == null) return;

        using (LogConsoleColor(args.Level))
        {
            if (ListenerLevel is LogLevel.Debug or LogLevel.Trace) { Console.WriteLine(args.ToStringDetailed()); }
            else
            {
                if (args.Message != null) Console.WriteLine(args.Message);
                if (args.Exception != null) Console.WriteLine(args.Exception.ToString());
            }
        }
    }
}
