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

namespace MaxRunSoftware.Utilities;

/// <summary>
/// Read-Write Key+Value store
/// </summary>
/// <typeparam name="TKey">Key type</typeparam>
/// <typeparam name="TValue">Value type</typeparam>
public interface IBucket<TKey, TValue> : IBucketReadOnly<TKey, TValue>
{
    /// <summary>
    /// Get or sets a value for a specific key
    /// </summary>
    /// <param name="key">Key</param>
    /// <returns>Value</returns>
    new TValue this[TKey key] { get; set; }
}


