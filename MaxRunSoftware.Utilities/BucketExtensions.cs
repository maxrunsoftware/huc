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

using System.Collections.Generic;

namespace MaxRunSoftware.Utilities
{
    public static class BucketExtensions
    {
        private sealed class BucketDictionaryWrapper<TKey, TValue> : IBucket<TKey, TValue>
        {
            private readonly IDictionary<TKey, TValue> dictionary;

            public BucketDictionaryWrapper(IDictionary<TKey, TValue> dictionary)
            {
                this.dictionary = dictionary;
            }

            public TValue this[TKey key] { get => dictionary[key]; set => dictionary[key] = value; }

            TValue IBucketReadOnly<TKey, TValue>.this[TKey key] => dictionary[key];

            public IEnumerable<TKey> Keys => dictionary.Keys;
        }

        public static IBucket<TKey, TValue> AsBucket<TKey, TValue>(IDictionary<TKey, TValue> dictionary) => new BucketDictionaryWrapper<TKey, TValue>(dictionary);
    }



}
