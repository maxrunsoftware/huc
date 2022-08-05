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

using System.Runtime.CompilerServices;

namespace MaxRunSoftware.Utilities;

public static partial class Util
{
    private const int START = 17;
    private const int PRIME = 16777619;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GenerateHashCode<T>(T item) => item is int i ? i : EqualityComparer<T>.Default.Equals(item, default) ? 0 : item.GetHashCode();

    public static int GenerateHashCode<T1, T2>(T1 item1, T2 item2) => GenerateHashCode(item1, item2, 0, 0, 0, 0, 0, 0);

    public static int GenerateHashCode<T1, T2, T3>(T1 item1, T2 item2, T3 item3) => GenerateHashCode(item1, item2, item3, 0, 0, 0, 0, 0);

    public static int GenerateHashCode<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4) => GenerateHashCode(item1, item2, item3, item4, 0, 0, 0, 0);

    public static int GenerateHashCode<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) => GenerateHashCode(item1, item2, item3, item4, item5, 0, 0, 0);

    public static int GenerateHashCode<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) => GenerateHashCode(item1, item2, item3, item4, item5, item6, 0, 0);

    public static int GenerateHashCode<T1, T2, T3, T4, T5, T6, T7>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) => GenerateHashCode(item1, item2, item3, item4, item5, item6, item7, 0);

    public static int GenerateHashCode<T1, T2, T3, T4, T5, T6, T7, T8>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
    {
        // http://stackoverflow.com/a/263416
        unchecked
        {
            var hash = START;
            hash = hash * PRIME + GenerateHashCode(item1);
            hash = hash * PRIME + GenerateHashCode(item2);
            hash = hash * PRIME + GenerateHashCode(item3);
            hash = hash * PRIME + GenerateHashCode(item4);
            hash = hash * PRIME + GenerateHashCode(item5);
            hash = hash * PRIME + GenerateHashCode(item6);
            hash = hash * PRIME + GenerateHashCode(item7);
            hash = hash * PRIME + GenerateHashCode(item8);
            return hash;
        }
    }


    public static int GenerateHashCodeEnumerable<T>(IEnumerable<T> items)
    {
        if (items == null) return START;

        // http://stackoverflow.com/a/263416
        unchecked
        {
            var hash = START;

            foreach (var item in items) hash = hash * PRIME + GenerateHashCode(item);

            return hash;
        }
    }

    public static int GenerateHashCodeReadOnlyCollection<T>(IReadOnlyCollection<T> items) => items == null || items.Count == 0 ? START : GenerateHashCodeEnumerable(items);
    public static int GenerateHashCodeCollection<T>(ICollection<T> items) => items == null || items.Count == 0 ? START : GenerateHashCodeEnumerable(items);
    public static int GenerateHashCodeArray<T>(T[] items) => items == null || items.Length == 0 ? START : GenerateHashCodeEnumerable(items);
}
