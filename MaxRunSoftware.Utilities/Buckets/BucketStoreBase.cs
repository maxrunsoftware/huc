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

public abstract class BucketStoreBase<TKey, TValue> : IBucketStore<TKey, TValue>
{
    public IBucket<TKey, TValue> this[string name] => new Bucket<TKey, TValue>(this, name.CheckNotNullTrimmed(nameof(name)));

    protected virtual string CleanName(string name) => name.CheckNotNullTrimmed(nameof(name));

    protected virtual TKey CleanKey(TKey key) => key;

    protected virtual TValue CleanValue(TValue value) => value;

    protected abstract TValue GetValue(string bucketName, TKey bucketKey);

    protected abstract IEnumerable<TKey> GetKeys(string bucketName);

    protected abstract void SetValue(string bucketName, TKey bucketKey, TValue bucketValue);

    private sealed class Bucket<TTKey, TTValue> : IBucket<TTKey, TTValue>
    {
        private readonly string bucketName;
        private readonly BucketStoreBase<TTKey, TTValue> bucketStore;

        public Bucket(BucketStoreBase<TTKey, TTValue> bucketStore, string bucketName)
        {
            this.bucketStore = bucketStore.CheckNotNull(nameof(bucketStore));
            this.bucketName = bucketName.CheckNotNullTrimmed(nameof(bucketName));
        }

        public IEnumerable<TTKey> Keys =>
            bucketStore.GetKeys(bucketStore.CleanName(bucketName)) ?? Enumerable.Empty<TTKey>();

        public TTValue this[TTKey key]
        {
            get => bucketStore.CleanValue(bucketStore.GetValue(bucketStore.CleanName(bucketName),
                bucketStore.CleanKey(key)));
            set => bucketStore.SetValue(bucketStore.CleanName(bucketName), bucketStore.CleanKey(key),
                bucketStore.CleanValue(value));
        }
    }
}
