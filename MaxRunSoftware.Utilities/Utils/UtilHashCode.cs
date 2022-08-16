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
    public static int Hash<T>(T? item)
    {
        if (item is int i) return i;
        if (EqualityComparer<T>.Default.Equals(item, default)) return 0;
        if (item == null) return 0;
        return item.GetHashCode();
    }

    // @formatter:off
    public static int Hash<T1, T2>(T1? item1, T2? item2)
    {
        // http://stackoverflow.com/a/263416
        unchecked
        {
            return (START * PRIME + Hash(item1)) * PRIME + Hash(item2);
        }
    }

    public static int Hash<T1, T2, T3>(T1? item1, T2? item2, T3? item3)
    {
        unchecked
        {
            return ((START * PRIME + Hash(item1)
                ) * PRIME + Hash(item2)
                ) * PRIME + Hash(item3)
                ;
        }
    }

    public static int Hash<T1, T2, T3, T4>(T1? item1, T2? item2, T3? item3, T4? item4)
    {
        unchecked
        {
            return (((START * PRIME + Hash(item1)
                ) * PRIME + Hash(item2)
                ) * PRIME + Hash(item3)
                ) * PRIME + Hash(item4)
                ;
        }
    }


    public static int Hash<T1, T2, T3, T4, T5>(T1? item1, T2? item2, T3? item3, T4? item4, T5? item5)
    {
        unchecked
        {
            return ((((START * PRIME + Hash(item1)
                ) * PRIME + Hash(item2)
                ) * PRIME + Hash(item3)
                ) * PRIME + Hash(item4)
                ) * PRIME + Hash(item5)
                ;
        }
    }

    public static int Hash<T1, T2, T3, T4, T5, T6>(T1? item1, T2? item2, T3? item3, T4? item4, T5? item5, T6? item6)
    {
        unchecked
        {
            return (((((START * PRIME + Hash(item1)
                ) * PRIME + Hash(item2)
                ) * PRIME + Hash(item3)
                ) * PRIME + Hash(item4)
                ) * PRIME + Hash(item5)
                ) * PRIME + Hash(item6)
                ;
        }
    }

    public static int Hash<T1, T2, T3, T4, T5, T6, T7>(T1? item1, T2? item2, T3? item3, T4? item4, T5? item5, T6? item6, T7? item7)
    {
        unchecked
        {
            return ((((((START * PRIME + Hash(item1)
                ) * PRIME + Hash(item2)
                ) * PRIME + Hash(item3)
                ) * PRIME + Hash(item4)
                ) * PRIME + Hash(item5)
                ) * PRIME + Hash(item6)
                ) * PRIME + Hash(item7)
                ;
        }
    }

    public static int Hash<T1, T2, T3, T4, T5, T6, T7, T8>(T1? item1, T2? item2, T3? item3, T4? item4, T5? item5, T6? item6, T7? item7, T8? item8)
    {
        unchecked
        {
            return (((((((START * PRIME + Hash(item1)
                ) * PRIME + Hash(item2)
                ) * PRIME + Hash(item3)
                ) * PRIME + Hash(item4)
                ) * PRIME + Hash(item5)
                ) * PRIME + Hash(item6)
                ) * PRIME + Hash(item7)
                ) * PRIME + Hash(item8)
                ;
        }
    }

    public static int Hash<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1? item1, T2? item2, T3? item3, T4? item4, T5? item5, T6? item6, T7? item7, T8? item8, T9? item9)
    {
        unchecked
        {
            return ((((((((START * PRIME + Hash(item1)
                ) * PRIME + Hash(item2)
                ) * PRIME + Hash(item3)
                ) * PRIME + Hash(item4)
                ) * PRIME + Hash(item5)
                ) * PRIME + Hash(item6)
                ) * PRIME + Hash(item7)
                ) * PRIME + Hash(item8)
                ) * PRIME + Hash(item9)
                ;
        }
    }

    public static int Hash<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1? item1, T2? item2, T3? item3, T4? item4, T5? item5, T6? item6, T7? item7, T8? item8, T9? item9, T10? item10)
    {
        unchecked
        {
            return (((((((((START * PRIME + Hash(item1)
                ) * PRIME + Hash(item2)
                ) * PRIME + Hash(item3)
                ) * PRIME + Hash(item4)
                ) * PRIME + Hash(item5)
                ) * PRIME + Hash(item6)
                ) * PRIME + Hash(item7)
                ) * PRIME + Hash(item8)
                ) * PRIME + Hash(item9)
                ) * PRIME + Hash(item10)
                ;
        }
    }

    public static int Hash<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T1? item1, T2? item2, T3? item3, T4? item4, T5? item5, T6? item6, T7? item7, T8? item8, T9? item9, T10? item10, T11? item11)
    {
        unchecked
        {
            return ((((((((((START * PRIME + Hash(item1)
                ) * PRIME + Hash(item2)
                ) * PRIME + Hash(item3)
                ) * PRIME + Hash(item4)
                ) * PRIME + Hash(item5)
                ) * PRIME + Hash(item6)
                ) * PRIME + Hash(item7)
                ) * PRIME + Hash(item8)
                ) * PRIME + Hash(item9)
                ) * PRIME + Hash(item10)
                ) * PRIME + Hash(item11)
                ;
        }
    }

    public static int Hash<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T1? item1, T2? item2, T3? item3, T4? item4, T5? item5, T6? item6, T7? item7, T8? item8, T9? item9, T10? item10, T11? item11, T12? item12)
    {
        unchecked
        {
            return (((((((((((START * PRIME + Hash(item1)
                ) * PRIME + Hash(item2)
                ) * PRIME + Hash(item3)
                ) * PRIME + Hash(item4)
                ) * PRIME + Hash(item5)
                ) * PRIME + Hash(item6)
                ) * PRIME + Hash(item7)
                ) * PRIME + Hash(item8)
                ) * PRIME + Hash(item9)
                ) * PRIME + Hash(item10)
                ) * PRIME + Hash(item11)
                ) * PRIME + Hash(item12)
                ;
        }
    }

    public static int Hash<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T1? item1, T2? item2, T3? item3, T4? item4, T5? item5, T6? item6, T7? item7, T8? item8, T9? item9, T10? item10, T11? item11, T12? item12, T13? item13)
    {
        unchecked
        {
            return ((((((((((((START * PRIME + Hash(item1)
                ) * PRIME + Hash(item2)
                ) * PRIME + Hash(item3)
                ) * PRIME + Hash(item4)
                ) * PRIME + Hash(item5)
                ) * PRIME + Hash(item6)
                ) * PRIME + Hash(item7)
                ) * PRIME + Hash(item8)
                ) * PRIME + Hash(item9)
                ) * PRIME + Hash(item10)
                ) * PRIME + Hash(item11)
                ) * PRIME + Hash(item12)
                ) * PRIME + Hash(item13)
                ;
        }
    }

    public static int Hash<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T1? item1, T2? item2, T3? item3, T4? item4, T5? item5, T6? item6, T7? item7, T8? item8, T9? item9, T10? item10, T11? item11, T12? item12, T13? item13, T14? item14)
    {
        unchecked
        {
            return (((((((((((((START * PRIME + Hash(item1)
                ) * PRIME + Hash(item2)
                ) * PRIME + Hash(item3)
                ) * PRIME + Hash(item4)
                ) * PRIME + Hash(item5)
                ) * PRIME + Hash(item6)
                ) * PRIME + Hash(item7)
                ) * PRIME + Hash(item8)
                ) * PRIME + Hash(item9)
                ) * PRIME + Hash(item10)
                ) * PRIME + Hash(item11)
                ) * PRIME + Hash(item12)
                ) * PRIME + Hash(item13)
                ) * PRIME + Hash(item14)
                ;
        }
    }

    public static int Hash<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T1? item1, T2? item2, T3? item3, T4? item4, T5? item5, T6? item6, T7? item7, T8? item8, T9? item9, T10? item10, T11? item11, T12? item12, T13? item13, T14? item14, T15? item15)
    {
        unchecked
        {
            return ((((((((((((((START * PRIME + Hash(item1)
                ) * PRIME + Hash(item2)
                ) * PRIME + Hash(item3)
                ) * PRIME + Hash(item4)
                ) * PRIME + Hash(item5)
                ) * PRIME + Hash(item6)
                ) * PRIME + Hash(item7)
                ) * PRIME + Hash(item8)
                ) * PRIME + Hash(item9)
                ) * PRIME + Hash(item10)
                ) * PRIME + Hash(item11)
                ) * PRIME + Hash(item12)
                ) * PRIME + Hash(item13)
                ) * PRIME + Hash(item14)
                ) * PRIME + Hash(item15)
                ;
        }
    }

    public static int Hash<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(T1? item1, T2? item2, T3? item3, T4? item4, T5? item5, T6? item6, T7? item7, T8? item8, T9? item9, T10? item10, T11? item11, T12? item12, T13? item13, T14? item14, T15? item15, T16? item16)
    {
        unchecked
        {
            return (((((((((((((((START * PRIME + Hash(item1)
                ) * PRIME + Hash(item2)
                ) * PRIME + Hash(item3)
                ) * PRIME + Hash(item4)
                ) * PRIME + Hash(item5)
                ) * PRIME + Hash(item6)
                ) * PRIME + Hash(item7)
                ) * PRIME + Hash(item8)
                ) * PRIME + Hash(item9)
                ) * PRIME + Hash(item10)
                ) * PRIME + Hash(item11)
                ) * PRIME + Hash(item12)
                ) * PRIME + Hash(item13)
                ) * PRIME + Hash(item14)
                ) * PRIME + Hash(item15)
                ) * PRIME + Hash(item16)
                ;
        }
    }

    // @formatter:on

    public static int HashEnumerable<T>(IEnumerable<T?>? items)
    {
        switch (items)
        {
            case null:
            case T[] { Length: 0 }:
            //case T?[] { Length: 0 }:
            case ICollection<T> { Count: 0 }:
            //case ICollection<T?> { Count: 0 }:
            case ICollection { Count: 0 }:
                return 0;

            default:
            {
                // http://stackoverflow.com/a/263416
                unchecked
                {
                    var hash = START;

                    foreach (var item in items) hash = hash * PRIME + Hash(item);

                    return hash;
                }
            }
        }
    }
}
