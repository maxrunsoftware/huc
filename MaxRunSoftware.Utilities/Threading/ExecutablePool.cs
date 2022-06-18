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

using System.Threading;

namespace MaxRunSoftware.Utilities;

public interface IExecutable
{
    void Execute();
}

public interface IExecutablePoolConfig
{
    IEnumerator<IExecutable> Enumerator { get; }
    int NumberOfThreads { get; } 
    Action<ExecutablePool> OnComplete { get; }
    object SynchronizationLock { get; }
    bool IsBackground { get; }
    Action<IExecutable, Exception> OnException { get; }
    bool ShouldExitOnException { get; }
    bool IsEventsSynchronous { get; }
    string ThreadPoolName { get; }
}

public sealed class ExecutablePoolConfig : IExecutablePoolConfig
{
    public IEnumerator<IExecutable> Enumerator { get; set; }
    public int NumberOfThreads { get; set; } = 1;
    public Action<ExecutablePool> OnComplete { get; set; }
    public object SynchronizationLock { get; set; } = new();
    public bool IsBackground { get; set; }
    public Action<IExecutable, Exception> OnException { get; set; }
    public bool ShouldExitOnException { get; set; }
    public bool IsEventsSynchronous { get; set; }
    public string ThreadPoolName { get; set; }
    
    public ExecutablePoolConfig() {}

    public ExecutablePoolConfig(IExecutablePoolConfig config)
    {
        Enumerator = config.Enumerator;
        NumberOfThreads = config.NumberOfThreads;
        OnComplete = config.OnComplete;
        SynchronizationLock = config.SynchronizationLock ?? new object();
        IsBackground = config.IsBackground;
        OnException = config.OnException;
        ShouldExitOnException = config.ShouldExitOnException;
        IsEventsSynchronous = config.IsEventsSynchronous;
        ThreadPoolName = config.ThreadPoolName;
    }
}

public sealed class ExecutablePoolState
{
    private static readonly IReadOnlyDictionary<int, IExecutable> empty = new Dictionary<int, IExecutable>().AsReadOnly();
    public ExecutablePool ExecutablePool { get; }
    public int ThreadsTotal { get; }
    public int ThreadsInactive { get; }
    public int ThreadsActive { get; }
    
    /// <summary>
    /// The currently executing items. May be empty even though there actually are executing items if they were not requested.
    /// You can check ExecutingItemsIncluded to see if these items were actually included in the snapshot request.
    /// </summary>
    public IReadOnlyDictionary<int, IExecutable> ExecutingItems { get; }

    public bool ExecutingItemsIncluded { get; }

    public bool IsComplete { get; }
    
    public ExecutablePoolState(ExecutablePool executablePool, int threadsTotal, int threadsInactive, IReadOnlyDictionary<int, IExecutable> executingItems, bool isComplete)
    {
        ExecutablePool = executablePool;
        ThreadsTotal = threadsTotal;
        ThreadsInactive = threadsInactive;
        ThreadsActive = threadsTotal - threadsInactive;
        ExecutingItems = executingItems ?? empty;
        ExecutingItemsIncluded = executingItems != null;
        IsComplete = isComplete;
    }
}

/// <summary>
/// A basic single use thread-pool for executing multiple method calls simultaneously on multiple threads.
/// The items to execute are supplied by the ExecutablePoolConfig.Enumerator provider.
/// As items are supplied from the Enumerator they are processed by multiple threads.
/// Access to the Enumerator is synchronous so the Enumerator does not need to be thread-safe, so any Enumerator will work.
/// If the Enumerator ends, throws an exception, or returns a null element, the ExecutablePool considers that the end of the collection.
/// The supplied IExecutablePoolConfig is copied on creation of the ExecutablePool so changes to the supplied IExecutablePoolConfig after startup are NOT honored.
/// Execution begins immediately. This can be delayed by holding the lock on the IExecutablePoolConfig.SynchronizationLock object.
/// Calling Dispose early on the ExecutablePool will cancel any future items but will not kill the actively processing elements. That needs to be handled by your own IExecutable implementation, or tearing down the whole AppDomain.
/// This is NOT a lightweight implementation, as full System.Threading.Thread objects are created and destroyed on each ExecutablePool implementation.
/// </summary>
public class ExecutablePool : IDisposable
{
    private static readonly int maxNumberOfThreads = 1000; // No one needs more then this many threads
    private static volatile int executablePoolNumCount;
    private readonly string configOriginalTypeName;
    private readonly ILogger log;
    public IExecutablePoolConfig Config { get; }
    private bool isEnumeratorEmpty;
    private volatile int currentItemNum; // Don't think volatile is needed if incrementing in lock but just being extra safe https://stackoverflow.com/a/29544279
    private volatile int threadsExited;
    private readonly Dictionary<int, IExecutable> currentItems = new();
    private readonly object synchronizationLock; // improve lock acquisition performance by copying locally
    private readonly List<ExecutablePoolThread> threads = new();
    
    /// <summary>
    /// Gets a snapshot of the state of the current ExecutablePool.
    /// </summary>
    /// <param name="includeExecutingItems">If true, then includes a snapshot of all of the executing items as well.
    /// Getting the items can be an expensive operation if done repeatedly in a tight loop (because it requires a lock) so only use true if you actually need the items.
    /// If you are just checking to see if the ExecutablePool is still executing (like in a loop) then use false.
    /// </param>
    /// <returns>A snapshot of the current ExecutablePool state</returns>
    public ExecutablePoolState GetState(bool includeExecutingItems)
    {
        lock (synchronizationLock)
        {
            return new ExecutablePoolState(
                this,
                Config.NumberOfThreads,
                threadsExited,
                includeExecutingItems ? new Dictionary<int, IExecutable>(currentItems).AsReadOnly() : null,
                threadsExited == Config.NumberOfThreads && currentItems.Count == 0
            );
        }
    }

    public static ExecutablePool Execute(IExecutablePoolConfig config) => new(config);
    
    protected ExecutablePool(IExecutablePoolConfig config)
    {
        config.CheckNotNull(nameof(config));
        log = LogFactory.LogFactoryImpl.GetLogger(GetType());
        configOriginalTypeName = config.GetType().NameFormatted();
        
        var cfg = new ExecutablePoolConfig(config);
        
        cfg.Enumerator.CheckNotNull(configOriginalTypeName + "." + nameof(IExecutablePoolConfig.Enumerator));
        cfg.NumberOfThreads.CheckNotZeroNotNegative(configOriginalTypeName + "." + nameof(IExecutablePoolConfig.NumberOfThreads));
        cfg.NumberOfThreads.CheckMax(configOriginalTypeName + "." + nameof(IExecutablePoolConfig.NumberOfThreads), maxNumberOfThreads);

        cfg.SynchronizationLock ??= new object();
        
        var executablePoolNum = Interlocked.Increment(ref executablePoolNumCount);
        cfg.ThreadPoolName ??= GetType().NameFormatted() + executablePoolNum;
        Config = cfg;

        synchronizationLock = Config.SynchronizationLock;

        lock (synchronizationLock)
        {
            try
            {
                for (var i = 0; i < Config.NumberOfThreads; i++)
                {
                    var thread = new ExecutablePoolThread(this);
                    threads.Add(thread);
                    thread.Start(isBackgroundThread: Config.IsBackground, name: Config.ThreadPoolName + "[" + (i + 1) + "]");
                }
            }
            catch (Exception e)
            {
                log.Error("Error starting threads", e); // TODO: Better exception
                Dispose();
            }
        }
    }

    private void OnThreadException(IExecutable executable, Exception e)
    {
        var logMsg = executable == null ? $"{Config.ThreadPoolName}: Error calling Enumerator" : $"Error calling {executable.GetType().NameFormatted()}.{nameof(IExecutable.Execute)}()";
        var action = Config.OnException;
        
        if (action == null)
        {
            log.Warn(logMsg, e);
            return;
        }

        log.Trace(logMsg, e);
        try
        {
            if (Config.IsEventsSynchronous)
            {
                lock (synchronizationLock) { action(executable, e); }
            }
            else
            {
                action(executable, e); 
            }
        }
        catch (Exception ee)
        {
            log.Error($"{Config.ThreadPoolName}: Error calling {configOriginalTypeName}.{nameof(IExecutablePoolConfig.OnException)}", ee);
            log.Error($"{Config.ThreadPoolName}: Original Error: {logMsg}", e);
        }
    }
    
    private void OnThreadComplete()
    {
        var action = Config.OnComplete;
        
        var areWeLastOneOut = false;
        lock (synchronizationLock)
        {
            var exitedCount = Interlocked.Increment(ref threadsExited);
            if (exitedCount == Config.NumberOfThreads) areWeLastOneOut = true;
        }

        if (!areWeLastOneOut) return;
        
        log.Trace($"{Config.ThreadPoolName}: Last thread completed");

        if (action == null) return;

        try
        {
            if (Config.IsEventsSynchronous)
            {
                lock (synchronizationLock) {  action(this); }
            }
            else
            {
                action(this);
            }
           
        }
        catch (Exception ee)
        {
            log.Error("Error calling " + configOriginalTypeName + "." + nameof(IExecutablePoolConfig.OnComplete), ee);
        }
        
        
    }

    private void CurrentItemAdd(int itemNum, IExecutable executable)
    {
        lock (synchronizationLock)
        {
            if (!currentItems.TryAdd(itemNum, executable))
            {
                throw new InvalidOperationException($"{Config.ThreadPoolName}: Already executing item {itemNum}");
            }
        }
    }

    private void CurrentItemRemove(int itemNum)
    {
        lock (synchronizationLock)
        {
            if (!currentItems.Remove(itemNum))
            {
                log.Error($"{Config.ThreadPoolName}: No item {itemNum} found that is currently executing");
            }
        }
    }
    
    public void Dispose()
    {
        lock (synchronizationLock)
        {
            isEnumeratorEmpty = true;
            foreach (var thread in threads)
            {
                thread.Dispose();
            }
        }
    }

    private class ExecutablePoolThread : ThreadBase
    {
        private readonly ExecutablePool executablePool;
        private readonly object locker;

        public ExecutablePoolThread(ExecutablePool executablePool)
        {
            this.executablePool = executablePool;
            locker = executablePool.synchronizationLock; // improve lock acquisition performance by copying locally
        }
        
        protected override void Work()
        {
            while (true)
            {
                IExecutable executable;
                int itemNum;
                lock (locker)
                {
                    if (executablePool.isEnumeratorEmpty) break;

                    try
                    {
                        executablePool.isEnumeratorEmpty = !executablePool.Config.Enumerator.TryGetNext(out executable);
                        if (executablePool.isEnumeratorEmpty) break;
                        if (executable == null) // null item shuts everything down as well
                        {
                            executablePool.isEnumeratorEmpty = true;
                            break;
                        }
                        itemNum = Interlocked.Increment(ref executablePool.currentItemNum);
                        executablePool.CurrentItemAdd(itemNum, executable);
                    }
                    catch (Exception e)
                    {
                        executablePool.log.Trace("Error calling enumerator", e);
                        executablePool.isEnumeratorEmpty = true;
                        executablePool.OnThreadException(null, e);
                        break;
                    }
                }
                
                
                // ReSharper disable InconsistentlySynchronizedField
                try
                {
                    executablePool.log.Trace($"Executing item {itemNum}");
                    executable.Execute();
                    lock (locker)
                    {
                        executablePool.CurrentItemRemove(itemNum);
                    }
                }
                catch (Exception e)
                {
                    var shouldExit = false;
                    lock (locker)
                    {
                        executablePool.CurrentItemRemove(itemNum);
                        if (executablePool.Config.ShouldExitOnException)
                        {
                            executablePool.isEnumeratorEmpty = true;
                            shouldExit = true;
                        }
                    }
                    executablePool.OnThreadException(executable, e);
                    if (shouldExit) break;
                }
                // ReSharper restore InconsistentlySynchronizedField
            }
            
                // ReSharper disable InconsistentlySynchronizedField
                executablePool.OnThreadComplete();
                // ReSharper restore InconsistentlySynchronizedField

        }
    }
}
