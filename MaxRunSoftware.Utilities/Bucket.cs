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

using System;
using System.Collections.Generic;

namespace MaxRunSoftware.Utilities
{
    public static class Bucket
    {
        private sealed class BucketFunc<TKey, TValue> : IBucket<TKey, TValue>
        {
            private readonly Func<TKey, TValue> getValue;
            private readonly Action<TKey, TValue> setValue;
            private readonly Func<IEnumerable<TKey>> getKeys;
            public IEnumerable<TKey> Keys => getKeys();

            public BucketFunc(Func<TKey, TValue> getValue, Func<IEnumerable<TKey>> getKeys, Action<TKey, TValue> setValue)
            {
                this.getValue = getValue.CheckNotNull(nameof(getValue));
                this.getKeys = getKeys.CheckNotNull(nameof(getKeys));
                this.setValue = setValue.CheckNotNull(nameof(setValue));
            }

            public TValue this[TKey key] { get => getValue(key); set => setValue(key, value); }
        }

        private sealed class BucketReadOnlyFunc<TKey, TValue> : IBucketReadOnly<TKey, TValue>
        {
            private readonly Func<TKey, TValue> getValue;
            private readonly Func<IEnumerable<TKey>> getKeys;
            public IEnumerable<TKey> Keys => getKeys();

            public BucketReadOnlyFunc(Func<TKey, TValue> getValue, Func<IEnumerable<TKey>> getKeys)
            {
                this.getValue = getValue.CheckNotNull(nameof(getValue));
                this.getKeys = getKeys.CheckNotNull(nameof(getKeys));
            }

            public TValue this[TKey key] => getValue(key);
        }

        /// <summary>
        /// Creates a bucket from a getValue and getKeys function. 
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="getValue"></param>
        /// <param name="getKeys"></param>
        /// <returns>A simple bucket wrapper around 2 functions</returns>
        public static IBucketReadOnly<TKey, TValue> CreateBucket<TKey, TValue>(Func<TKey, TValue> getValue, Func<IEnumerable<TKey>> getKeys) => new BucketReadOnlyFunc<TKey, TValue>(getValue, getKeys);

        /// <summary>
        /// Creates a bucket from a getValue and getKeys and setValue function.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="getValue"></param>
        /// <param name="getKeys"></param>
        /// <param name="setValue"></param>
        /// <returns>A simple bucket wrapper around 3 functions</returns>
        public static IBucket<TKey, TValue> CreateBucket<TKey, TValue>(Func<TKey, TValue> getValue, Func<IEnumerable<TKey>> getKeys, Action<TKey, TValue> setValue) => new BucketFunc<TKey, TValue>(getValue, getKeys, setValue);

    }

    /// <summary>
    /// Readonly Key+Value store
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TValue">Value type</typeparam>
    public interface IBucketReadOnly<TKey, TValue>
    {
        /// <summary>
        /// Keys in bucket
        /// </summary>
        IEnumerable<TKey> Keys { get; }

        /// <summary>
        /// Gets value for a specific key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        TValue this[TKey key] { get; }
    }

    /// <summary>
    /// Read-Write Key+Value store
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TValue">Value type</typeparam>
    public interface IBucket<TKey, TValue> : IBucketReadOnly<TKey, TValue>
    {
        /// <summary>
        /// Get or sets a value for a specific key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        new TValue this[TKey key] { get; set; }
    }

    /// <summary>
    /// Non-threadsafe cache implementation using backing dictionary and value generation function
    /// </summary>
    /// <typeparam name="TKey">Key</typeparam>
    /// <typeparam name="TValue">Generated Value</typeparam>
    public class BucketCache<TKey, TValue> : IBucketReadOnly<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> dictionary;
        private readonly Func<TKey, TValue> factory;
        public IEnumerable<TKey> Keys => dictionary.Keys;

        public BucketCache(Func<TKey, TValue> factory, IDictionary<TKey, TValue> dictionary)
        {
            this.factory = factory.CheckNotNull(nameof(factory));
            this.dictionary = dictionary.CheckNotNull(nameof(dictionary));
        }

        public BucketCache(Func<TKey, TValue> factory) : this(factory, new Dictionary<TKey, TValue>())
        {
        }

        public TValue this[TKey key]
        {
            get
            {
                if (!dictionary.TryGetValue(key, out var value))
                {
                    dictionary[key] = value = factory(key);
                }
                return value;
            }
        }
    }

    /// <summary>
    /// Threadsafe cache implementation using backing dictionary and value generation function. 
    /// </summary>
    /// <typeparam name="TKey">Key</typeparam>
    /// <typeparam name="TValue">Generated Value</typeparam>
    public class BucketCacheThreadSafe<TKey, TValue> : IBucketReadOnly<TKey, TValue>
    {
        private readonly object locker = new object();
        private readonly Func<TKey, TValue> factory;
        private readonly IDictionary<TKey, TValue> dictionary;
        public IEnumerable<TKey> Keys => dictionary.Keys;

        public BucketCacheThreadSafe(Func<TKey, TValue> factory, IDictionary<TKey, TValue> dictionary)
        {
            this.factory = factory.CheckNotNull(nameof(factory));
            this.dictionary = dictionary.CheckNotNull(nameof(dictionary));
        }

        public BucketCacheThreadSafe(Func<TKey, TValue> factory) : this(factory, new Dictionary<TKey, TValue>())
        {
        }

        public TValue this[TKey key]
        {
            get
            {
                lock (locker)
                {
                    if (!dictionary.TryGetValue(key, out var value))
                    {
                        value = factory(key);
                        dictionary.Add(key, value);
                    }
                    return value;
                }
            }
        }

        public void Clear()
        {
            lock (locker)
            {
                dictionary.Clear();
            }
        }
    }

    /// <summary>
    /// Threadsafe cache implementation implementing copy on write using backing dictionary factory and value generation function.
    /// Precache elements using the Cache function
    /// </summary>
    /// <typeparam name="TKey">Key</typeparam>
    /// <typeparam name="TValue">Generated Value</typeparam>
    public class BucketCacheThreadSafeCopyOnWrite<TKey, TValue> : IBucketReadOnly<TKey, TValue>
    {
        private readonly object locker = new object();
        private readonly Func<TKey, TValue> factory;
        private readonly Func<IDictionary<TKey, TValue>> dictionaryFactory;
        private IReadOnlyDictionary<TKey, TValue> dictionary; // shouldn't need volatile because of memory barrier of lock(locker)
        public IEnumerable<TKey> Keys => dictionary.Keys;

        public BucketCacheThreadSafeCopyOnWrite(Func<TKey, TValue> factory, Func<IDictionary<TKey, TValue>> dictionaryFactory)
        {
            this.factory = factory.CheckNotNull(nameof(factory));
            this.dictionaryFactory = dictionaryFactory.CheckNotNull(nameof(dictionaryFactory));
            this.dictionary = dictionaryFactory().AsReadOnly();
        }

        public BucketCacheThreadSafeCopyOnWrite(Func<TKey, TValue> factory) : this(factory, () => new Dictionary<TKey, TValue>()) { }

        public TValue this[TKey key]
        {
            get
            {
                if (dictionary.TryGetValue(key, out var val)) return val;
                lock (locker)
                {
                    if (dictionary.TryGetValue(key, out val)) return val;
                    var d = dictionaryFactory();
                    foreach (var kvp in dictionary) d.Add(kvp.Key, kvp.Value);
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
                foreach (var kvp in dictionary) d.Add(kvp.Key, kvp.Value);
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
                this.dictionary = dictionaryFactory().AsReadOnly();
            }
        }
    }

}
