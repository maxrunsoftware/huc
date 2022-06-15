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

public class BucketStoreMemory<TKey, TValue> : BucketStoreBase<TKey, TValue>
{
    private static readonly Func<IDictionary<TKey, TValue>> dictionaryFactoryDefault = () => new Dictionary<TKey, TValue>();

    private readonly Dictionary<string, IDictionary<TKey, TValue>> buckets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<IDictionary<TKey, TValue>> dictionaryFactory;
    private readonly TValue nullValue;

    public BucketStoreMemory(TValue nullValue = default, Func<IDictionary<TKey, TValue>> dictionaryFactory = null)
    {
        this.nullValue = nullValue;
        this.dictionaryFactory = dictionaryFactory ?? dictionaryFactoryDefault;
    }

    public IEnumerable<string> Buckets
    {
        get
        {
            lock (buckets) { return buckets.Keys.ToList(); }
        }
    }

    protected override IEnumerable<TKey> GetKeys(string bucketName)
    {
        IDictionary<TKey, TValue> d;
        lock (buckets)
        {
            if (!buckets.TryGetValue(bucketName, out d)) return new TKey[] { };
        }

        lock (d) { return d.Keys.ToArray(); }
    }

    protected override TValue GetValue(string bucketName, TKey bucketKey)
    {
        IDictionary<TKey, TValue> d;
        lock (buckets)
        {
            if (!buckets.TryGetValue(bucketName, out d)) return nullValue;
        }

        lock (d) { return d.TryGetValue(bucketKey, out var val) ? val : nullValue; }
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
            if (bucketValue == null) { d.Remove(bucketKey); }
            else { d[bucketKey] = bucketValue; }
        }
    }
}
