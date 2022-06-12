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
/// Wraps an existing IBucketStore with time delayed caching.
/// </summary>
/// <typeparam name="TKey">Key type</typeparam>
/// <typeparam name="TValue">Value type</typeparam>
public class BucketStoreCachedWrapper<TKey, TValue> : BucketStoreBase<TKey, TValue>
{
    private readonly IBucketStore<TKey, TValue> bucketStore;
    private readonly Dictionary<string, CacheEntry> cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object locker = new();

    public BucketStoreCachedWrapper(IBucketStore<TKey, TValue> bucketStore)
    {
        this.bucketStore = bucketStore.CheckNotNull(nameof(bucketStore));
    }

    public TimeSpan CacheTime { get; set; } = TimeSpan.Zero;

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
            if (DateTime.Now - ce.LastRefresh <= CacheTime)
            {
                return ce.Keys;
            }

            ce.Keys = bucketStore[bucketName].Keys.ToList();
            ce.LastRefresh = DateTime.Now;

            return ce.Keys;
        }
    }

    protected override TValue GetValue(string bucketName, TKey bucketKey)
    {
        lock (locker)
        {
            var bucketValue = GetCacheEntry(bucketName).GetBucketValue(bucketKey);
            if (DateTime.Now - bucketValue.LastRefresh <= CacheTime)
            {
                return bucketValue.Value;
            }

            bucketValue.Value = bucketStore[bucketName][bucketKey];
            bucketValue.LastRefresh = DateTime.Now;

            return bucketValue.Value;
        }
    }

    protected override void SetValue(string bucketName, TKey bucketKey, TValue bucketValue)
    {
        lock (locker)
        {
            bucketStore[bucketName][bucketKey] = bucketValue;
            var bucketValueExisting = GetCacheEntry(bucketName).GetBucketValue(bucketKey);
            bucketValueExisting.Value = bucketValue;
            bucketValueExisting.LastRefresh = DateTime.Now;
        }
    }

    private class CacheEntryBucketValue
    {
        public TValue Value { get; set; }
        public DateTime LastRefresh { get; set; } = DateTime.MinValue;
    }

    private class CacheEntry
    {
        public DateTime LastRefresh { get; set; } = DateTime.MinValue;
        public List<TKey> Keys { get; set; } = new();
        private Dictionary<TKey, CacheEntryBucketValue> Bucket { get; } = new();

        public CacheEntryBucketValue GetBucketValue(TKey key)
        {
            if (Bucket.TryGetValue(key, out var bucketValue))
            {
                return bucketValue;
            }

            bucketValue = new CacheEntryBucketValue();
            Bucket.Add(key, bucketValue);

            return bucketValue;
        }
    }
}
