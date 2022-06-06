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
    private readonly Dictionary<string, TValue> dictionaryCaseInsensitive;
    private readonly Dictionary<string, TValue> dictionaryCache;
    private readonly IReadOnlyList<string> keys;
    private readonly IReadOnlyList<TValue> values;

    public DictionaryReadOnlyStringCaseInsensitive(IDictionary<string, TValue> dictionary) : this(dictionary.ToArray()) { }

    public DictionaryReadOnlyStringCaseInsensitive(IReadOnlyDictionary<string, TValue> dictionary) : this(dictionary.ToArray()) { }

    public DictionaryReadOnlyStringCaseInsensitive(IEnumerable<KeyValuePair<string, TValue>> dictionary)
    {
        dictionaryCache = new(dictionary, StringComparer.Ordinal);
        dictionaryCaseInsensitive = new(dictionaryCache, StringComparer.OrdinalIgnoreCase);
        items = dictionaryCaseInsensitive.ToList().AsReadOnly();
        keys = dictionaryCaseInsensitive.Keys.ToList();
        values = dictionaryCaseInsensitive.Values.ToList();
    }

    public TValue this[string key]
    {
        get
        {
            if (TryGetValue(key, out var val)) // Call this to search for key and build cache
            {
                return val;
            }
            return dictionaryCache[key]; // Should throw exception
        }
    }

    public IEnumerable<string> Keys => keys;

    public IEnumerable<TValue> Values => values;

    public int Count => keys.Count;

    public bool ContainsKey(string key) => TryGetValue(key, out var _);

    public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator() => items.GetEnumerator();

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out TValue value)
    {
        if (dictionaryCache.TryGetValue(key, out value))
        {
            return true;
        }

        if (dictionaryCaseInsensitive.TryGetValue(key, out value))
        {
            dictionaryCache[key] = value;
            return true;
        }

        return false;
    }

    IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
}
