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

namespace MaxRunSoftware.Utilities;

/// <summary>
/// Thread for performing a specified action on a collection
/// </summary>
/// <typeparam name="T">The Type of object to process</typeparam>
public class ConsumerThread<T> : ConsumerThreadBase<T>
{
    private readonly Action<T> action;

    public ConsumerThread(BlockingCollection<T> queue, Action<T> action) : base(queue)
    {
        this.action = action.CheckNotNull(nameof(action));
    }

    protected override void WorkConsume(T item)
    {
        action(item);
    }
}
