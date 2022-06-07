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

using System.Collections.Concurrent;
using System.Threading;

namespace MaxRunSoftware.Utilities;

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
