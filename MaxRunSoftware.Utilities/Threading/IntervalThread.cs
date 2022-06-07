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
/// Simple polling thread that calls WorkInterval() periodically
/// </summary>
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
