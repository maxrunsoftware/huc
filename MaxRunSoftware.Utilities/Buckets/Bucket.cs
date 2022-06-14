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

public static class Bucket
{
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

    private sealed class BucketFunc<TKey, TValue> : IBucket<TKey, TValue>
    {
        private readonly Func<IEnumerable<TKey>> getKeys;
        private readonly Func<TKey, TValue> getValue;
        private readonly Action<TKey, TValue> setValue;

        public BucketFunc(Func<TKey, TValue> getValue, Func<IEnumerable<TKey>> getKeys, Action<TKey, TValue> setValue)
        {
            this.getValue = getValue.CheckNotNull(nameof(getValue));
            this.getKeys = getKeys.CheckNotNull(nameof(getKeys));
            this.setValue = setValue.CheckNotNull(nameof(setValue));
        }

        public IEnumerable<TKey> Keys => getKeys();

        public TValue this[TKey key]
        {
            get => getValue(key);
            set => setValue(key, value);
        }
    }

    private sealed class BucketReadOnlyFunc<TKey, TValue> : IBucketReadOnly<TKey, TValue>
    {
        private readonly Func<IEnumerable<TKey>> getKeys;
        private readonly Func<TKey, TValue> getValue;

        public BucketReadOnlyFunc(Func<TKey, TValue> getValue, Func<IEnumerable<TKey>> getKeys)
        {
            this.getValue = getValue.CheckNotNull(nameof(getValue));
            this.getKeys = getKeys.CheckNotNull(nameof(getKeys));
        }

        public IEnumerable<TKey> Keys => getKeys();

        public TValue this[TKey key] => getValue(key);
    }
}
