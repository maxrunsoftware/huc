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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MaxRunSoftware.Utilities
{
    /// <summary>
    /// System-wide Mutex lock. Good for locking on files using the file name.
    /// </summary>
    public sealed class MutexLock : IDisposable
    {
        private readonly System.Threading.Mutex mutex;
        private readonly SingleUse su = new SingleUse();
        private bool hasHandle;
        public string MutexName { get; }

        private static readonly char[] illegalMutexChars = new char[] { ':', '/', '\\' }
                                                        .Concat(Path.GetInvalidFileNameChars())
                                                        .Concat(Path.GetInvalidPathChars())
                                                        .Concat(Path.DirectorySeparatorChar)
                                                        .Concat(Path.AltDirectorySeparatorChar)
                                                        .Distinct()
                                                        .ToArray();

        private static string MutexNameFormat(string mutexName)
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

        private static readonly object locker = new object();
        private static readonly Dictionary<string, string> mutexNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private MutexLock(string mutexName, TimeSpan timeout)
        {
            mutexName = mutexName.CheckNotNullTrimmed(nameof(mutexName));
            lock (locker)
            {
                if (!mutexNames.TryGetValue(mutexName, out var actualMutexName))
                {
                    actualMutexName = MutexNameFormat(mutexName);
                    mutexNames.Add(mutexName, actualMutexName);
                }
                mutexName = actualMutexName;
            }
            MutexName = mutexName;

            // https://stackoverflow.com/a/229567
            mutex = new System.Threading.Mutex(false, mutexName);
            try
            {
                hasHandle = mutex.WaitOne(timeout, false);
                if (hasHandle == false) throw new MutexLockTimeoutException(mutexName, timeout);
            }
            catch (System.Threading.AbandonedMutexException)
            {
                hasHandle = true;
            }
        }

        public void Dispose()
        {
            if (!su.TryUse()) return;
            if (hasHandle) mutex.ReleaseMutex();
        }

        public static MutexLock Create(TimeSpan timeout, string mutexName) => new MutexLock(mutexName, timeout);
        public static MutexLock Create(TimeSpan timeout, Guid mutexId) => Create(timeout, ParseGuid(mutexId));
        public static MutexLock Create(TimeSpan timeout, FileInfo file) => Create(timeout, ParseFile(file));
        public static MutexLock CreateGlobal(TimeSpan timeout, string mutexName) => new MutexLock(string.Format("Global\\{{{0}}}", mutexName), timeout);
        public static MutexLock CreateGlobal(TimeSpan timeout, Guid mutexId) => CreateGlobal(timeout, ParseGuid(mutexId));
        public static MutexLock CreateGlobal(TimeSpan timeout, FileInfo file) => CreateGlobal(timeout, ParseFile(file));

        private static string ParseGuid(Guid guid) => guid.ToString().Replace("-", "");

        private static string ParseFile(FileInfo file)
        {
            var fn = file.FullName;
            if (fn.Length > 180)
            {
                fn = fn.Left(20) + fn.Right(160); // TODO: Maybe a better way to do this?
            }
            return fn;
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
}
