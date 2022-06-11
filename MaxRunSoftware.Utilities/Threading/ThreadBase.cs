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

using System.Threading;

namespace MaxRunSoftware.Utilities;

/// <summary>
/// Base class for implementing custom threads
/// </summary>
public abstract class ThreadBase : IDisposable
{
    private readonly Thread thread;
    protected static ILogFactory LogFactory => MaxRunSoftware.Utilities.LogFactory.LogFactoryImpl;

    private readonly SingleUse isStarted = new SingleUse();
    public bool IsStarted => isStarted.IsUsed;
    private readonly SingleUse isDisposed = new SingleUse();
    public bool IsDisposed => isDisposed.IsUsed;
    public string Name => thread.Name;

    //public int DisposeTimeoutSeconds { get; set; } = 10;
    public bool IsRunning => thread.IsAlive;

    public ThreadState ThreadState => thread.ThreadState;
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
                Console.Error.Write("Exception encountered while trying to dispose. ");
                Console.Error.WriteLine(e);
                LogFactory.GetLogger<ThreadBase>().Error("Exception encountered while trying to dispose", e);
            }
        }
    }

    /// <summary>
    /// Peform work. When this method returns Dispose() is automatically called and the thread shuts down.
    /// </summary>
    protected abstract void Work();

    /// <summary>
    /// Dispose of any resources used. Guarenteed to be only called once.
    /// </summary>
    protected virtual void DisposeInternally() { }

    protected void Join() => thread.Join();

    protected void Join(TimeSpan timeout) => thread.Join(timeout);

    public void Dispose()
    {
        if (!isDisposed.TryUse()) return;

        LogFactory.GetLogger<ThreadBase>().Debug($"Disposing thread \"{thread.Name}\" with IsBackground={thread.IsBackground} of type {GetType().FullNameFormatted()}");

        DisposeInternally();
    }

    public void Start(bool isBackgroundThread = true, string name = null)
    {
        if (IsDisposed) throw new ObjectDisposedException(GetType().FullNameFormatted());
        if (!isStarted.TryUse()) throw new InvalidOperationException("Start() already called");

        thread.IsBackground = isBackgroundThread;
        thread.Name = name ?? GetType().FullNameFormatted();
        if (!(GetType().Equals(typeof(LogBackgroundThread)))) LogFactory.GetLogger<ThreadBase>().Debug($"Starting thread \"{thread.Name}\" with IsBackground={thread.IsBackground} of type {GetType().FullNameFormatted()}");
        thread.Start();
    }
}
