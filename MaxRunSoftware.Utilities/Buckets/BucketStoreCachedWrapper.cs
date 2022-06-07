﻿/*
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

namespace MaxRunSoftware.Utilities;

/// <summary>
/// Wraps an existing IBucketStore with time delayed caching.
/// </summary>
/// <typeparam name="TKey">Key type</typeparam>
/// <typeparam name="TValue">Value type</typeparam>
public class BucketStoreCachedWrapper<TKey, TValue> : BucketStoreBase<TKey, TValue>
{
    private readonly IBucketStore<TKey, TValue> bucketStore;
    private readonly Dictionary<string, CacheEntry> cache = new Dictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);
    private readonly object locker = new object();
    public TimeSpan CacheTime { get; set; } = TimeSpan.Zero;

    public BucketStoreCachedWrapper(IBucketStore<TKey, TValue> bucketStore) => this.bucketStore = bucketStore.CheckNotNull(nameof(bucketStore));

    private class CacheEntryBucketValue
    {
        public TValue Value { get; set; }
        public DateTime LastRefresh { get; set; } = DateTime.MinValue;
    }

    private class CacheEntry
    {
        public DateTime LastRefresh { get; set; } = DateTime.MinValue;
        public List<TKey> Keys { get; set; } = new List<TKey>();
        public Dictionary<TKey, CacheEntryBucketValue> Bucket { get; } = new Dictionary<TKey, CacheEntryBucketValue>();

        public CacheEntryBucketValue GetBucketValue(TKey key)
        {
            if (!Bucket.TryGetValue(key, out var cebv))
            {
                cebv = new CacheEntryBucketValue();
                Bucket.Add(key, cebv);
            }
            return cebv;
        }
    }

    private CacheEntry GetCacheEntry(string bucketName)
    {
        if (!cache.TryGetValue(bucketName, out var ce))
        {
            cache.Add(bucketName, ce = new CacheEntry());
        }
        return ce;
    }

    protected override IEnumerable<TKey> GetKeys(string bucketName)
    {
        lock (locker)
        {
            var ce = GetCacheEntry(bucketName);
            if ((DateTime.Now - ce.LastRefresh) > CacheTime)
            {
                ce.Keys = bucketStore[bucketName].Keys.ToList();
                ce.LastRefresh = DateTime.Now;
            }
            return ce.Keys;
        }
    }

    protected override TValue GetValue(string bucketName, TKey bucketKey)
    {
        lock (locker)
        {
            var cebv = GetCacheEntry(bucketName).GetBucketValue(bucketKey);
            if ((DateTime.Now - cebv.LastRefresh) > CacheTime)
            {
                cebv.Value = bucketStore[bucketName][bucketKey];
                cebv.LastRefresh = DateTime.Now;
            }
            return cebv.Value;
        }
    }

    protected override void SetValue(string bucketName, TKey bucketKey, TValue bucketValue)
    {
        lock (locker)
        {
            bucketStore[bucketName][bucketKey] = bucketValue;
            var cebv = GetCacheEntry(bucketName).GetBucketValue(bucketKey);
            cebv.Value = bucketValue;
            cebv.LastRefresh = DateTime.Now;
        }
    }
}
