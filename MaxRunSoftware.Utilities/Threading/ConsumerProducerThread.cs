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
/// Thread that consumes an object and produces a new object
/// </summary>
/// <typeparam name="TConsume">Consumed Type</typeparam>
/// <typeparam name="TProduce">Produced Type</typeparam>
public class ConsumerProducerThread<TConsume, TProduce> : ConsumerProducerThreadBase<TConsume, TProduce>
{
    private readonly Func<TConsume, TProduce> func;

    public ConsumerProducerThread(BlockingCollection<TConsume> consumerQueue, BlockingCollection<TProduce> producerQueue, Func<TConsume, TProduce> func) : base(consumerQueue, producerQueue)
    {
        this.func = func.CheckNotNull(nameof(func));
    }

    protected override TProduce WorkConsumeProduce(TConsume item)
    {
        return func(item);
    }
}
