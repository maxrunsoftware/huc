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

using System.Diagnostics.CodeAnalysis;

namespace MaxRunSoftware.Utilities;

/// <summary>
/// Implments a IReadOnlyDictionary of string keys that are case insensitive. Each time a lookup is done first it is searched by
/// case-insensitive, then if found, it adds that case-sensitive key to a cache so that future lookups will not incur the cost of
/// doing a case-insensitive search.
/// </summary>
/// <typeparam name="TValue">Value Type</typeparam>
[Serializable]
public class DictionaryReadOnlyStringCaseInsensitive<TValue> : IReadOnlyDictionary<string, TValue>, IBucketReadOnly<string, TValue>
{
    private readonly IReadOnlyList<KeyValuePair<string, TValue>> items;
    private readonly Dictionary<string, TValue> dictionaryOriginal;
    private readonly Dictionary<string, TValue> dictionaryCache;
    private readonly IReadOnlyList<string> keys;
    private readonly IReadOnlyList<TValue> values;
    private readonly object locker = new();
    private readonly HashSet<string> invalidKeys = new();

    public DictionaryReadOnlyStringCaseInsensitive(IDictionary<string, TValue> dictionary) : this(dictionary.ToArray()) { }

    public DictionaryReadOnlyStringCaseInsensitive(IEnumerable<(string Key, TValue Value)> items) : this(items.Select(o => new KeyValuePair<string, TValue>(o.Key, o.Value))) { }

    public DictionaryReadOnlyStringCaseInsensitive(IEnumerable<KeyValuePair<string, TValue>> items)
    {
        var keyValuePairs = items.ToList();
        dictionaryOriginal = new(keyValuePairs, StringComparer.Ordinal);
        dictionaryCache = new(keyValuePairs, StringComparer.Ordinal);

        this.items = dictionaryOriginal.ToList().AsReadOnly();
        keys = dictionaryOriginal.Keys.ToList();
        values = dictionaryOriginal.Values.ToList();
    }


    public TValue this[string key] => TryGetValue(key, out var val) ? val : dictionaryOriginal[key];

    public IEnumerable<string> Keys => keys;

    public IEnumerable<TValue> Values => values;

    public int Count => keys.Count;

    public bool ContainsKey(string key) => TryGetValue(key, out var _);

    public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator() => items.GetEnumerator();

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out TValue value)
    {
        lock (locker)
        {
            if (dictionaryCache.TryGetValue(key, out value))
            {
                return true;
            }

            if (invalidKeys.Contains(key)) return false;

            // Do a hard search
            var itemsFound = new List<KeyValuePair<string, TValue>>();
            var itemsCurrentD = new Dictionary<int, KeyValuePair<string, TValue>>();
            for (var i = 0; i < items.Count; i++) itemsCurrentD.Add(i, items[i]);

            foreach (var sc in Constant.LIST_StringComparer)
            {
                foreach (var item in itemsCurrentD.ToArray())
                {
                    if (!sc.Equals(key, item.Value.Key)) continue;
                    itemsFound.Add(item.Value);
                    itemsCurrentD.Remove(item.Key);
                }
            }

            if (itemsFound.Count > 1) throw new ArgumentException($"For key '{key}' found multiple matching items: " + itemsFound.Select(o => o.Key).ToStringDelimited(", "), nameof(key));
            if (itemsFound.Count == 1)
            {
                var v = itemsFound[0].Value;
                dictionaryCache.Add(key, v);
                value = v;
                return true;
            }

            invalidKeys.Add(key);
            return false;


        }
    }

    IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
}

public static class DictionaryReadOnlyStringCaseInsensitiveExtensions
{
    public static DictionaryReadOnlyStringCaseInsensitive<TValue> ToDictionaryReadOnlyStringCaseInsensitive<TValue>(this IEnumerable<TValue> values, Func<TValue, string> keySelector) => new DictionaryReadOnlyStringCaseInsensitive<TValue>(values.ToDictionary(keySelector));
}