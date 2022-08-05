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
using System.Runtime.CompilerServices;
using System.Threading;

namespace MaxRunSoftware.Utilities;

public static partial class Util
{
    /// <summary>
    /// Diagnostic disposable token
    /// </summary>
    public sealed class DiagnosticToken : IDisposable
    {
        private static long idCounter;
        private readonly Action<string> log;
        private readonly Stopwatch stopwatch;
        private readonly SingleUse isDisposed = new();

        public long Id { get; }
        public string MemberName { get; }
        public string SourceFilePath { get; }
        public string SourceFileName { get; }
        public int SourceLineNumber { get; }
        public long MemoryStart { get; }
        public int MemoryStartMB => (MemoryStart / (double)Constant.Bytes_Mega).ToString(MidpointRounding.AwayFromZero, 0).ToInt();

        public long MemoryEnd { get; private set; }
        public int MemoryEndMB => (MemoryStart / (double)Constant.Bytes_Mega).ToString(MidpointRounding.AwayFromZero, 0).ToInt();

        public TimeSpan Time { get; private set; }

        internal DiagnosticToken(Action<string> log, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            Id = Interlocked.Increment(ref idCounter);
            this.log = log.CheckNotNull(nameof(log));
            MemberName = memberName.TrimOrNull();
            SourceFilePath = sourceFilePath.TrimOrNull();
            if (SourceFilePath != null)
            {
                try { SourceFileName = Path.GetFileName(SourceFilePath).TrimOrNull(); }
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
    /// With a using statement, logs start and stop time, memory difference, and source line number. Only the log argument
    /// should be supplied
    /// </summary>
    /// <param name="log">Only provide this argument</param>
    /// <param name="memberName">No not provide this argument</param>
    /// <param name="sourceFilePath">No not provide this argument</param>
    /// <param name="sourceLineNumber">No not provide this argument</param>
    /// <returns>Disposable token when logging should end</returns>
    public static IDisposable Diagnostic(Action<string> log, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) => new DiagnosticToken(log, memberName, sourceFilePath, sourceLineNumber);
}
