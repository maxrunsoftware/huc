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

public sealed class EmptyReadOnlyList<T> : IReadOnlyList<T>
{
    /// <summary>
    /// A cached, immutable instance of an empty IReadOnlyList.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static readonly IReadOnlyList<T> Instance = new EmptyReadOnlyList<T>();

    private EmptyReadOnlyList() { }

    public IEnumerator<T> GetEnumerator() => Enumerable.Empty<T>().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public int Count => 0;

    public T this[int index] => throw new IndexOutOfRangeException();
}
