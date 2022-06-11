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

namespace MaxRunSoftware.Utilities;

/// <summary>
/// Pool of consumer threads. Most useful for performing a bunch of actions on multiple threads.
/// </summary>
/// <typeparam name="T">Type of object to process</typeparam>
public class ConsumerThreadPool<T> : IDisposable
{
    private static readonly ILogger log = LogFactory.LogFactoryImpl.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    private readonly List<ConsumerThread<T>> threads = new List<ConsumerThread<T>>();
    private readonly BlockingCollection<T> queue = new BlockingCollection<T>();
    private readonly Action<T> action;
    private readonly object locker = new object();
    private readonly bool isBackgroundThread;
    private volatile int threadCounter = 1;
    public bool IsDisposed { get; private set; }

    public bool IsComplete
    {
        get
        {
            lock (locker)
            {
                if (queue.IsCompleted)
                {
                    if (threads.TrueForAll(o => o.ConsumerThreadState == ConsumerThreadState.Stopped))
                        return true;
                }
                return false;
            }
        }
    }

    public int NumberOfThreads
    {
        get
        {
            lock (locker)
            {
                CleanThreads();
                return threads.Count;
            }
        }
        set
        {
            var newCount = value;
            lock (locker)
            {
                CleanThreads();
                if (IsDisposed) newCount = 0;

                var count = threads.Count;
                if (count == newCount) return;
                if (newCount > count)
                {
                    for (var i = 0; i < (value - count); i++)
                    {
                        var threadName = ThreadPoolName + "(" + threadCounter + ")";
                        log.Debug(threadName + ": Creating thread...");
                        var thread = CreateThread(action);
                        thread.CheckNotNull(nameof(thread));
                        if (!thread.IsStarted)
                        {
                            log.Debug(threadName + ": Starting thread...");
                            thread.Start(isBackgroundThread: isBackgroundThread, name: threadName);
                        }
                        threads.Add(thread);
                        threadCounter++;
                        log.Debug(threadName + ": Created and Started thread");
                    }
                }
                if (newCount < count)
                {
                    for (var i = 0; i < (count - newCount); i++)
                    {
                        var thread = threads.PopAt(0);
                        try
                        {
                            log.Debug(thread.Name + ": Destroying thread...");
                            DestroyThread(thread);
                            log.Debug(thread.Name + ": Destroyed thread");
                        }
                        finally
                        {
                            thread.Dispose();
                        }
                    }
                }
            }
        }
    }

    public string ThreadPoolName { get; }

    public ConsumerThreadPool(Action<T> action, string threadPoolName = null, bool isBackgroundThread = true)
    {
        this.action = action.CheckNotNull(nameof(action));
        ThreadPoolName = threadPoolName.TrimOrNull() ?? GetType().FullNameFormatted();
        this.isBackgroundThread = isBackgroundThread;
    }

    private void CleanThreads()
    {
        lock (locker)
        {
            threads.RemoveAll(o => o.IsCancelled || o.IsDisposed);
        }
    }

    protected virtual ConsumerThread<T> CreateThread(Action<T> action) => new ConsumerThread<T>(queue, action);

    protected virtual void DestroyThread(ConsumerThread<T> consumerThread) => consumerThread.Cancel();

    public void AddWorkItem(T item) => queue.Add(item);

    public void FinishedAddingWorkItems() => queue.CompleteAdding();

    public void Dispose()
    {
        lock (locker)
        {
            IsDisposed = true;
            NumberOfThreads = 0;
        }
    }
}
