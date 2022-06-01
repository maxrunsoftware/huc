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
using System.IO;
using System.Linq;

namespace MaxRunSoftware.Utilities
{
    /// <summary>
    /// Storage for multiple buckets based on a name
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TValue">Value type</typeparam>
    public interface IBucketStore<TKey, TValue>
    {
        IBucket<TKey, TValue> this[string name] { get; }
    }

    public abstract class BucketStoreBase<TKey, TValue> : IBucketStore<TKey, TValue>
    {
        public IBucket<TKey, TValue> this[string name] => new Bucket<TKey, TValue>(this, name.CheckNotNullTrimmed(nameof(name)));

        private sealed class Bucket<TTKey, TTValue> : IBucket<TTKey, TTValue>
        {
            private readonly BucketStoreBase<TTKey, TTValue> bucketStore;
            private readonly string bucketName;

            public IEnumerable<TTKey> Keys => bucketStore.GetKeys(bucketStore.CleanName(bucketName)) ?? Enumerable.Empty<TTKey>();

            public Bucket(BucketStoreBase<TTKey, TTValue> bucketStore, string bucketName)
            {
                this.bucketStore = bucketStore.CheckNotNull(nameof(bucketStore));
                this.bucketName = bucketName.CheckNotNullTrimmed(nameof(bucketName));
            }

            public TTValue this[TTKey key]
            {
                get => bucketStore.CleanValue(bucketStore.GetValue(bucketStore.CleanName(bucketName), bucketStore.CleanKey(key)));
                set => bucketStore.SetValue(bucketStore.CleanName(bucketName), bucketStore.CleanKey(key), bucketStore.CleanValue(value));
            }
        }

        protected virtual string CleanName(string name) => name.CheckNotNullTrimmed(nameof(name));

        protected virtual TKey CleanKey(TKey key) => key;

        protected virtual TValue CleanValue(TValue value) => value;

        protected abstract TValue GetValue(string bucketName, TKey bucketKey);

        protected abstract IEnumerable<TKey> GetKeys(string bucketName);

        protected abstract void SetValue(string bucketName, TKey bucketKey, TValue bucketValue);
    }

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

    public class BucketStoreFile : BucketStoreBase<string, string>
    {
        public StringComparer Comparer;
        public string File { get; }
        public StringComparison Comparison { get; }
        public string BucketNameDelimiter { get; }

        public IEnumerable<string> BucketNames => ReadFile().Keys;

        public BucketStoreFile(string file, StringComparison comparison = StringComparison.OrdinalIgnoreCase, string bucketNameDelimiter = ".")
        {
            File = Path.GetFullPath(file);
            Comparison = comparison;
            Comparer = Constant.MAP_StringComparison_StringComparer[comparison];
            BucketNameDelimiter = bucketNameDelimiter.CheckNotNull(nameof(bucketNameDelimiter));
        }

        protected IDictionary<string, IDictionary<string, string>> ReadFile()
        {
            var props = new Dictionary<string, string>(Comparer);
            var jp = new JavaProperties();

            try
            {
                using (MutexLock.Create(TimeSpan.FromSeconds(10), File))
                {
                    using (var fs = Util.FileOpenRead(File))
                    {
                        jp.Load(fs, Constant.ENCODING_UTF8_WITHOUT_BOM);
                    }
                }
            }
            catch (MutexLockTimeoutException mte)
            {
                throw new IOException("Could not access file " + File, mte);
            }

            var jpd = jp.ToDictionary();
            foreach (var kl in jpd)
            {
                props[kl.Key.TrimOrNull()] = kl.Value.TrimOrNull().WhereNotNull().LastOrDefault();
            }

            var d = new Dictionary<string, IDictionary<string, string>>(Comparer);
            var bucketKeySplit = new string[] { BucketNameDelimiter };
            foreach (var kvp in props)
            {
                var key = kvp.Key;
                var bucketValue = kvp.Value;
                if (key == null || bucketValue == null) continue;
                var keyParts = key.Split(bucketKeySplit, 2, StringSplitOptions.None).TrimOrNull().WhereNotNull().ToArray();
                if (keyParts.Length != 2) continue;
                var bucketName = keyParts.GetAtIndexOrDefault(0).TrimOrNull();
                var bucketKey = keyParts.GetAtIndexOrDefault(1).TrimOrNull();
                if (bucketName == null || bucketKey == null) continue;

                if (!d.TryGetValue(bucketName, out var dd))
                {
                    dd = new Dictionary<string, string>(Comparer);
                    d.Add(bucketName, dd);
                }
                dd[bucketKey] = bucketValue;
            }

            return d;
        }

        protected override string GetValue(string bucketName, string bucketKey)
        {
            bucketName = bucketName.CheckNotNullTrimmed(nameof(bucketName));
            bucketKey = bucketKey.CheckNotNullTrimmed(nameof(bucketKey));

            var d = ReadFile();
            if (d.TryGetValue(bucketName, out var dd))
            {
                if (dd.TryGetValue(bucketKey, out var v))
                {
                    return v;
                }
            }

            return null;
        }

        protected override IEnumerable<string> GetKeys(string bucketName)
        {
            bucketName = bucketName.CheckNotNullTrimmed(nameof(bucketName));
            var d = ReadFile();
            if (d.TryGetValue(bucketName, out var dd))
            {
                return dd.Keys;
            }
            return null;
        }

        protected override string CleanKey(string key) => base.CleanKey(key.TrimOrNull());

        protected override string CleanName(string name) => base.CleanName(name.TrimOrNull());

        protected override string CleanValue(string value) => base.CleanValue(value.TrimOrNull());

        protected override void SetValue(string bucketName, string bucketKey, string bucketValue)
        {
            bucketName = bucketName.CheckNotNullTrimmed(nameof(bucketName));
            bucketKey = bucketKey.CheckNotNullTrimmed(nameof(bucketKey));

            var bucketNameKey = bucketName + BucketNameDelimiter + bucketKey;

            var props = new Dictionary<string, string>(Comparer);
            var jp = new JavaProperties();
            try
            {
                using (MutexLock.Create(TimeSpan.FromSeconds(10), File))
                {
                    using (var fs = Util.FileOpenRead(File))
                    {
                        jp.Load(fs, Constant.ENCODING_UTF8_WITHOUT_BOM);
                    }
                }
            }
            catch (MutexLockTimeoutException mte)
            {
                throw new IOException("Could not access file " + File, mte);
            }

            string jpKey = null;
            string jpVal = null;
            foreach (var jpPropertyName in jp.GetPropertyNames())
            {
                var pn = jpPropertyName.TrimOrNull();
                if (pn == null) continue;
                if (string.Equals(pn, bucketNameKey, Comparison))
                {
                    jpKey = jpPropertyName;
                    jpVal = jp.GetProperty(jpKey);
                    break;
                }
            }

            if (jpKey == null) // new key
            {
                if (bucketValue == null) return;
                jp.SetProperty(bucketNameKey, bucketValue);
            }
            else
            {
                if (string.Equals(bucketValue, jpVal.TrimOrNull(), Comparison)) return;
                if (bucketValue == null)
                {
                    jp.Remove(jpKey);
                }
                else
                {
                    jp.SetProperty(bucketNameKey, bucketValue);
                }
            }
            try
            {
                using (MutexLock.Create(TimeSpan.FromSeconds(10), File))
                {
                    if (System.IO.File.Exists(File)) System.IO.File.Delete(File);
                    using (var fs = Util.FileOpenWrite(File))
                    {
                        jp.Store(fs, null, Constant.ENCODING_UTF8_WITHOUT_BOM);
                        fs.FlushSafe();
                        fs.CloseSafe();
                    }
                }
            }
            catch (MutexLockTimeoutException mte)
            {
                throw new IOException("Could not access file " + File, mte);
            }
        }
    }

    public class BucketStoreMemory<TKey, TValue> : BucketStoreBase<TKey, TValue>
    {
        private static readonly Func<IDictionary<TKey, TValue>> dictionaryFactoryDefault = () => new Dictionary<TKey, TValue>();
        private readonly Dictionary<string, IDictionary<TKey, TValue>> buckets = new Dictionary<string, IDictionary<TKey, TValue>>(StringComparer.OrdinalIgnoreCase);
        private readonly TValue nullValue;
        private readonly Func<IDictionary<TKey, TValue>> dictionaryFactory;

        public IEnumerable<string> Buckets { get { lock (buckets) return buckets.Keys.ToList(); } }

        public BucketStoreMemory(TValue nullValue = default, Func<IDictionary<TKey, TValue>> dictionaryFactory = null)
        {
            this.nullValue = nullValue;
            this.dictionaryFactory = dictionaryFactory ?? dictionaryFactoryDefault;
        }

        protected override IEnumerable<TKey> GetKeys(string bucketName)
        {
            IDictionary<TKey, TValue> d;
            lock (buckets) if (!buckets.TryGetValue(bucketName, out d)) return new TKey[] { };
            lock (d) return d.Keys.ToArray();
        }

        protected override TValue GetValue(string bucketName, TKey bucketKey)
        {
            IDictionary<TKey, TValue> d;
            lock (buckets) if (!buckets.TryGetValue(bucketName, out d)) return nullValue;
            lock (d) return d.TryGetValue(bucketKey, out var val) ? val : nullValue;
        }

        protected override void SetValue(string bucketName, TKey bucketKey, TValue bucketValue)
        {
            IDictionary<TKey, TValue> d;
            lock (buckets)
            {
                if (!buckets.TryGetValue(bucketName, out d))
                {
                    if (bucketValue == null) return;
                    buckets.Add(bucketName, d = dictionaryFactory());
                }
            }

            lock (d)
            {
                if (bucketValue == null) d.Remove(bucketKey);
                else d[bucketKey] = bucketValue;
            }
        }
    }

    public class BucketStoreMemoryString : BucketStoreMemory<string, string>
    {
        private static readonly Func<IDictionary<string, string>> dictionaryFactoryDefault = () => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public BucketStoreMemoryString() : base(dictionaryFactory: dictionaryFactoryDefault)
        {
        }

        protected override string CleanName(string name) => base.CleanName(name).CheckNotNullTrimmed(nameof(name));

        protected override string CleanKey(string key) => base.CleanKey(key).CheckNotNullTrimmed(nameof(key));

        protected override string CleanValue(string value) => base.CleanValue(value).TrimOrNull();
    }
}
