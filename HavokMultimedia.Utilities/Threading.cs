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
using System.Threading;

namespace HavokMultimedia.Utilities
{
    public enum ConsumerThreadState
    {
        Waiting,
        Working,
        Stopped
    }

    public abstract class ThreadBase : IDisposable
    {
        private readonly object stateLock = new object();
        private readonly Thread thread;
        protected static ILogFactory LogFactory => HavokMultimedia.Utilities.LogFactory.LogFactoryImpl;


        public bool IsStarted { get; private set; } = false;
        public bool IsDisposed { get; private set; } = false;
        public string Name => thread.Name;

        //public int DisposeTimeoutSeconds { get; set; } = 10;
        public bool IsRunning => thread.IsAlive;

        public System.Threading.ThreadState ThreadState => thread.ThreadState;
        public Exception Exception { get; protected set; }

        protected ThreadBase() => thread = new Thread(new ThreadStart(WorkPrivate));

        private void WorkPrivate()
        {
            try
            {
                Work();
            }
            catch (Exception e)
            {
                Exception = e;
            }

            try
            {
                Dispose();
            }
            catch (Exception e)
            {
                if (Exception == null) Exception = e;
                else
                {
                    System.Console.Error.Write("Exception encountered while trying to dispose. ");
                    System.Console.Error.WriteLine(e);
                    LogFactory.GetLogger<ThreadBase>().Error("Exception encountered while trying to dispose", e);
                }
            }
        }

        protected abstract void Work();

        protected virtual void DisposeInternally()
        {
        }

        protected void Join() => thread.Join();

        protected void Join(TimeSpan timeout) => thread.Join(timeout);

        public void Dispose()
        {
            if (IsDisposed) return;
            lock (stateLock)
            {
                if (IsDisposed) return;
                LogFactory.GetLogger<ThreadBase>().Debug($"Disposing thread \"{thread.Name}\" with IsBackground={thread.IsBackground} of type {GetType().FullNameFormatted()}");
                IsDisposed = true;
            }

            DisposeInternally();
        }

        public void Start(bool isBackgroundThread = true, string name = null)
        {
            lock (stateLock)
            {
                if (IsDisposed) throw new ObjectDisposedException(GetType().FullNameFormatted());
                if (IsStarted) throw new InvalidOperationException("Start() already called");

                thread.IsBackground = isBackgroundThread;
                thread.Name = name ?? GetType().FullNameFormatted();
                if (!(GetType().Equals(typeof(LogBackgroundThread)))) LogFactory.GetLogger<ThreadBase>().Debug($"Starting thread \"{thread.Name}\" with IsBackground={thread.IsBackground} of type {GetType().FullNameFormatted()}");
                thread.Start();
                IsStarted = true;
            }
        }
    }

    public abstract class ConsumerThreadBase<T> : ThreadBase
    {
        private static readonly ILogger log = LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly BlockingCollection<T> queue;
        private readonly object locker = new object();
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly bool cancelAfterCurrent = false;
        private volatile int itemsCompleted;
        public ConsumerThreadState ConsumerThreadState { get; private set; }

        public bool IsCancelled { get; private set; }

        public int ItemsCompleted => itemsCompleted;

        public ConsumerThreadBase(BlockingCollection<T> queue) => this.queue = queue.CheckNotNull(nameof(queue));

        private bool ShouldExitWorkLoop()
        {
            lock (locker)
            {
                if (cancelAfterCurrent || IsDisposed || cancellation.IsCancellationRequested || IsCancelled || queue.IsCompleted)
                {
                    log.Debug($"Exiting work loop for thread {Name}  cancelAfterCurrent={cancelAfterCurrent}, IsDisposed={IsDisposed}, IsCancellationRequested={cancellation.IsCancellationRequested}, IsCancelled={IsCancelled}, IsCompleted={queue.IsCompleted}");
                    Cancel();
                    ConsumerThreadState = ConsumerThreadState.Stopped;
                    return true;
                }
                return false;
            }
        }

        protected virtual void CancelInternal()
        {
        }

        protected abstract void WorkConsume(T item);

        protected override void DisposeInternally()
        {
            Cancel();
            base.DisposeInternally();
        }

        protected override void Work()
        {
            var stopwatch = new Stopwatch();
            while (true)
            {
                if (ShouldExitWorkLoop()) return;
                T t = default;
                try
                {
                    stopwatch.Restart();
                    ConsumerThreadState = ConsumerThreadState.Waiting;
                    var result = queue.TryTake(out t, -1, cancellation.Token);
                    stopwatch.Stop();
                    var timeSpent =  stopwatch.Elapsed;
                    log.Trace($"BlockingQueue.TryTake() time spent waiting {timeSpent.ToStringTotalSeconds(3)}s");

                    if (!result)
                    {
                        log.Debug($"BlockingQueue.TryTake() returned false, cancelling thread {Name}");
                        Cancel();
                    }
                }
                catch (OperationCanceledException)
                {
                    log.Debug($"Received {nameof(OperationCanceledException)}, cancelling thread {Name}");
                    Cancel();
                }
                catch (Exception e)
                {
                    log.Warn($"Received error requesting next item, cancelling thread {Name}", e);
                    Cancel();
                }

                if (ShouldExitWorkLoop()) return;

                try
                {
                    stopwatch.Restart();
                    ConsumerThreadState = ConsumerThreadState.Working;
                    WorkConsume(t);
                    itemsCompleted++;
                    stopwatch.Stop();
                    var timeSpent =  stopwatch.Elapsed;
                    log.Trace($"WorkConsume() time spent processing {timeSpent.ToStringTotalSeconds(3)}s");
                }
                catch (Exception e)
                {
                    log.Warn($"Received error processing item {t.ToStringGuessFormat()}", e);
                    Cancel();
                }
            }
        }

        public void Cancel()
        {
            lock (locker)
            {
                if (IsCancelled) return;
                IsCancelled = true;
            }

            try
            {
                cancellation.Cancel();
            }
            catch (Exception e)
            {
                log.Warn("CancellationTokenSource.Cancel() request threw exception", e);
            }

            try
            {
                CancelInternal();
            }
            catch (Exception e)
            {
                log.Warn("CancelInternal() request threw exception", e);
            }
        }
    }

    public class ConsumerThread<T> : ConsumerThreadBase<T>
    {
        private readonly Action<T> action;

        public ConsumerThread(BlockingCollection<T> queue, Action<T> action) : base(queue) => this.action = action.CheckNotNull(nameof(action));

        protected override void WorkConsume(T item) => action(item);
    }

    public abstract class ConsumerProducerThreadBase<TConsume, TProduce> : ConsumerThreadBase<TConsume>
    {
        private static readonly ILogger log = LogFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly BlockingCollection<TProduce> producerQueue;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        protected ConsumerProducerThreadBase(BlockingCollection<TConsume> consumerQueue, BlockingCollection<TProduce> producerQueue) : base(consumerQueue) => this.producerQueue = producerQueue.CheckNotNull(nameof(producerQueue));

        protected override void WorkConsume(TConsume item)
        {
            if (IsCancelled) return;
            try
            {
                var produceItem = WorkConsumeProduce(item);
                if (IsCancelled) return;
                producerQueue.Add(produceItem, cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                Cancel();
            }
        }

        protected override void CancelInternal()
        {
            try
            {
                cancellation.Cancel();
            }
            catch (Exception e)
            {
                log.Warn("CancellationTokenSource.Cancel() request threw exception", e);
            }
            base.CancelInternal();
        }

        protected abstract TProduce WorkConsumeProduce(TConsume item);
    }

    public class ConsumerThreadPool<T> : IDisposable
    {
        private static readonly ILogger log = LogFactory.LogFactoryImpl.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

    public class ConsumerProducerThread<TConsume, TProduce> : ConsumerProducerThreadBase<TConsume, TProduce>
    {
        private readonly Func<TConsume, TProduce> func;

        public ConsumerProducerThread(BlockingCollection<TConsume> consumerQueue, BlockingCollection<TProduce> producerQueue, Func<TConsume, TProduce> func) : base(consumerQueue, producerQueue) => this.func = func.CheckNotNull(nameof(func));

        protected override TProduce WorkConsumeProduce(TConsume item) => func(item);
    }

    public abstract class IntervalThread : ThreadBase
    {
        private DateTime lastCheck;
        public TimeSpan SleepInterval { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan SleepIntervalDelay { get; set; } = TimeSpan.FromMilliseconds(50);

        protected override void Work()
        {
            while (true)
            {
                if (IsDisposed) return;
                if ((DateTime.UtcNow - lastCheck) > SleepInterval)
                {
                    WorkInterval();
                    lastCheck = DateTime.UtcNow;
                }

                Thread.Sleep(SleepIntervalDelay);
            }
        }

        protected abstract void WorkInterval();
    }
}
