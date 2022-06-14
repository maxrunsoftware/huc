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

public class BucketStoreMemoryString : BucketStoreMemory<string, string>
{
    private static readonly Func<IDictionary<string, string>> dictionaryFactoryDefault = () => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public BucketStoreMemoryString() : base(dictionaryFactory: dictionaryFactoryDefault) { }

    protected override string CleanName(string name) => base.CleanName(name).CheckNotNullTrimmed(nameof(name));

    protected override string CleanKey(string key) => base.CleanKey(key).CheckNotNullTrimmed(nameof(key));

    protected override string CleanValue(string value) => base.CleanValue(value).TrimOrNull();
}
