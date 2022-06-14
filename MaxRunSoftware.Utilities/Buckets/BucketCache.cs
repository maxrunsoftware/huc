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

namespace MaxRunSoftware.Utilities;

/// <summary>
/// Non-thread safe cache implementation using backing dictionary and value generation function
/// </summary>
/// <typeparam name="TKey">Key</typeparam>
/// <typeparam name="TValue">Generated Value</typeparam>
public class BucketCache<TKey, TValue> : IBucketReadOnly<TKey, TValue>
{
    private readonly IDictionary<TKey, TValue> dictionary;
    private readonly Func<TKey, TValue> factory;

    public BucketCache(Func<TKey, TValue> factory, IDictionary<TKey, TValue> dictionary)
    {
        this.factory = factory.CheckNotNull(nameof(factory));
        this.dictionary = dictionary.CheckNotNull(nameof(dictionary));
    }

    public BucketCache(Func<TKey, TValue> factory) : this(factory, new Dictionary<TKey, TValue>()) { }

    public IEnumerable<TKey> Keys => dictionary.Keys;

    public TValue this[TKey key]
    {
        get
        {
            if (!dictionary.TryGetValue(key, out var value)) dictionary[key] = value = factory(key);

            return value;
        }
    }
}
