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

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace MaxRunSoftware.Utilities;

public abstract class ConsumerThreadBase<T> : ThreadBase
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly ILogger log = LogFactory.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);
    private readonly BlockingCollection<T> queue;
    private readonly object locker = new();
    private readonly CancellationTokenSource cancellation = new();
    private readonly bool cancelAfterCurrent = false;
    private volatile int itemsCompleted;
    public ConsumerThreadState ConsumerThreadState { get; private set; }

    public bool IsCancelled { get; private set; }

    public int ItemsCompleted => itemsCompleted;

    protected ConsumerThreadBase(BlockingCollection<T> queue)
    {
        this.queue = queue.CheckNotNull(nameof(queue));
    }

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

    protected virtual void CancelInternal() { }

    /// <summary>
    /// Do some work on this item
    /// </summary>
    /// <param name="item">item</param>
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
            if (ShouldExitWorkLoop())
            {
                return;
            }

            T t = default;
            try
            {
                stopwatch.Restart();
                ConsumerThreadState = ConsumerThreadState.Waiting;
                var result = queue.TryTake(out t, -1, cancellation.Token);
                stopwatch.Stop();
                log.Trace($"BlockingQueue.TryTake() time spent waiting {stopwatch.Elapsed.ToStringTotalSeconds(3)}s");

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

            if (ShouldExitWorkLoop())
            {
                return;
            }

            try
            {
                stopwatch.Restart();
                ConsumerThreadState = ConsumerThreadState.Working;
                WorkConsume(t);
                
                //itemsCompleted++;
                Interlocked.Increment(ref itemsCompleted);
                
                stopwatch.Stop();
                var timeSpent = stopwatch.Elapsed;
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
            if (IsCancelled)
            {
                return;
            }

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
