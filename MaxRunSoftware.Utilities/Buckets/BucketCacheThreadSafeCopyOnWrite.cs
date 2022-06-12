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
/// Thread safe cache implementation implementing copy on write using backing dictionary factory and value generation
/// function.
/// Precache elements using the Cache function
/// </summary>
/// <typeparam name="TKey">Key</typeparam>
/// <typeparam name="TValue">Generated Value</typeparam>
public class BucketCacheThreadSafeCopyOnWrite<TKey, TValue> : IBucketReadOnly<TKey, TValue>
{
    private readonly Func<IDictionary<TKey, TValue>> dictionaryFactory;
    private readonly Func<TKey, TValue> factory;
    private readonly object locker = new();

    private IReadOnlyDictionary<TKey, TValue> dictionary; // shouldn't need volatile because of memory barrier of lock(locker)

    public BucketCacheThreadSafeCopyOnWrite(Func<TKey, TValue> factory, Func<IDictionary<TKey, TValue>> dictionaryFactory)
    {
        this.factory = factory.CheckNotNull(nameof(factory));
        this.dictionaryFactory = dictionaryFactory.CheckNotNull(nameof(dictionaryFactory));
        dictionary = dictionaryFactory().AsReadOnly();
    }

    public BucketCacheThreadSafeCopyOnWrite(Func<TKey, TValue> factory) : this(factory, () => new Dictionary<TKey, TValue>()) { }

    public IEnumerable<TKey> Keys => dictionary.Keys;

    public TValue this[TKey key]
    {
        get
        {
            if (dictionary.TryGetValue(key, out var val))
            {
                return val;
            }

            lock (locker)
            {
                if (dictionary.TryGetValue(key, out val))
                {
                    return val;
                }

                var d = dictionaryFactory();
                foreach (var kvp in dictionary)
                {
                    d.Add(kvp.Key, kvp.Value);
                }

                val = factory(key);
                d.Add(key, val);
                dictionary = d.AsReadOnly();
                return val;
            }
        }
    }

    public void Cache(IEnumerable<TKey> keys)
    {
        lock (locker)
        {
            var d = dictionaryFactory();
            foreach (var kvp in dictionary)
            {
                d.Add(kvp.Key, kvp.Value);
            }

            foreach (var key in keys)
            {
                var val = factory(key);
                d[key] = val;
            }

            dictionary = d.AsReadOnly();
        }
    }

    public void Clear()
    {
        lock (locker)
        {
            dictionary = dictionaryFactory().AsReadOnly();
        }
    }
}
