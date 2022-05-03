// /*
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
// */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace MaxRunSoftware.Utilities
{
    /// <summary>
    /// A dictionary that maintains insertion ordering of keys.
    /// 
    /// This is useful for emitting JSON where it is preferable to keep the key ordering
    /// for various human-friendlier reasons.
    /// 
    /// There is no support to manually re-order keys or to access keys
    /// by index without using Keys/Values or the Enumerator (eg).
    /// </summary>
    /// <see href="https://stackoverflow.com/a/46596033">StackOverflow</see>
    [Serializable]
    public sealed class IndexedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        // Non-generic version only in .NET 4.5
        private readonly OrderedDictionary d;

        private IEnumerable<KeyValuePair<TKey, TValue>> KeyValuePairs => d.OfType<DictionaryEntry>().Select(e => new KeyValuePair<TKey, TValue>((TKey)e.Key, (TValue)e.Value));

        public IndexedDictionary() => d = new OrderedDictionary();

        public IndexedDictionary(IEqualityComparer comparer) => d = new OrderedDictionary(comparer);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => KeyValuePairs.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(KeyValuePair<TKey, TValue> item) => d[item.Key] = item.Value;

        public void Clear() => d.Clear();

        public bool Contains(KeyValuePair<TKey, TValue> item) => d.Contains(item.Key);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => KeyValuePairs.ToList().CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (TryGetValue(item.Key, out var value))
            {
                if (Equals(value, item.Value))
                {
                    Remove(item.Key);
                    return true;
                }
            }
            return false;
        }

        public int Count => d.Count;

        public bool IsReadOnly => d.IsReadOnly;

        public bool ContainsKey(TKey key) => d.Contains(key);

        public void Add(TKey key, TValue value) => d.Add(key, value);

        public bool Remove(TKey key)
        {
            var result = d.Contains(key);
            if (result)
            {
                d.Remove(key);
            }
            return result;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            object foundValue = d[key];
            if (foundValue != null || d.Contains(key))
            {
                // Either found with a non-null value, or contained value is null.
                value = (TValue)foundValue;
                return true;
            }
            value = default(TValue);
            return false;
        }

        public TValue this[TKey key]
        {
            get => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();
            set => d[key] = value;
        }

        public ICollection<TKey> Keys => d.Keys.OfType<TKey>().ToList();

        public ICollection<TValue> Values => d.Values.OfType<TValue>().ToList();
    }
}
