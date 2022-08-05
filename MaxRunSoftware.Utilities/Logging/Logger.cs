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
}

public abstract class LoggerBase : ILogger
{
    public abstract void Log(string message, Exception exception, LogLevel level);

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
}
