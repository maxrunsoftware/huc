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

using System.Security.Cryptography;

namespace MaxRunSoftware.Utilities;

public static class ExtensionsRandom
{
    #region System.Random

    public static T Pick<T>(this Random random, IList<T> items) => items[random.Next(items.Count)];

    public static T Pick<T>(this Random random, params T[] items) => items[random.Next(items.Length)];

    public static void Shuffle<T>(this Random random, T[] items)
    {
        // Note i > 0 to avoid final pointless iteration
        for (var i = items.Length - 1; i > 0; i--)
        {
            // Swap element "i" with a random earlier element it (or itself)
            var swapIndex = random.Next(i + 1);
            var tmp = items[i];
            items[i] = items[swapIndex];
            items[swapIndex] = tmp;
        }
    }

    public static void Shuffle<T>(this Random random, IList<T> items)
    {
        // Note i > 0 to avoid final pointless iteration
        for (var i = items.Count - 1; i > 0; i--)
        {
            // Swap element "i" with a random earlier element it (or itself)
            var swapIndex = random.Next(i + 1);
            var tmp = items[i];
            items[i] = items[swapIndex];
            items[swapIndex] = tmp;
        }
    }

    public static T[] Shuffle<T>(this Random random, T item1, T item2, params T[] items)
    {
        if (items == null) items = Array.Empty<T>();
        var array = new T[1 + 1 + (items.Length)];
        array[0] = item1;
        array[1] = item2;
        for (var i = 0; i < items.Length; i++)
        {
            array[i + 2] = items[i];
        }
        Shuffle(random, array);
        return array;
    }

    #endregion System.Random

    #region System.Security.Cryptography.RandomNumberGenerator

    public static byte[] GetBytes(this RandomNumberGenerator random, int length)
    {
        var key = new byte[length];
        random.GetBytes(key);
        return key;
    }

    public static int Next(this RandomNumberGenerator random, int fromInclusive, int toExclusive)
    {
        if (fromInclusive >= toExclusive) throw new ArgumentException($"Invalid range {nameof(fromInclusive)}:{fromInclusive} {nameof(toExclusive)}:{toExclusive}", nameof(fromInclusive));

        // The total possible range is [0, 4,294,967,295). Subtract one to account for zero
        // being an actual possibility.
        var range = (uint)toExclusive - (uint)fromInclusive - 1;

        // If there is only one possible choice, nothing random will actually happen, so return
        // the only possibility.
        if (range == 0) return fromInclusive;

        // Create a mask for the bits that we care about for the range. The other bits will be
        // masked away.
        var mask = range;
        mask |= mask >> 1;
        mask |= mask >> 2;
        mask |= mask >> 4;
        mask |= mask >> 8;
        mask |= mask >> 16;

        var resultSpan = new byte[4];
        uint result;

        do
        {
            random.GetBytes(resultSpan);
            result = mask & BitConverter.ToUInt32(resultSpan, 0);
        }
        while (result > range);

        return (int)result + fromInclusive;
    }

    public static int Next(this RandomNumberGenerator random) => Next(random, 0, int.MaxValue);

    public static int Next(this RandomNumberGenerator random, int maxValueExclusive) => Next(random, 0, maxValueExclusive);

    public static Guid NextGuid(this RandomNumberGenerator random) => new Guid(random.GetBytes(16));

    public static T Pick<T>(this RandomNumberGenerator random, IList<T> items) => items[random.Next(items.Count)];

    public static T Pick<T>(this RandomNumberGenerator random, params T[] items) => items[random.Next(items.Length)];

    public static void Shuffle<T>(this RandomNumberGenerator random, T[] items)
    {
        // Note i > 0 to avoid final pointless iteration
        for (var i = items.Length - 1; i > 0; i--)
        {
            // Swap element "i" with a random earlier element it (or itself)
            var swapIndex = random.Next(i + 1);
            var tmp = items[i];
            items[i] = items[swapIndex];
            items[swapIndex] = tmp;
        }
    }

    public static void Shuffle<T>(this RandomNumberGenerator random, IList<T> items)
    {
        // Note i > 0 to avoid final pointless iteration
        for (var i = items.Count - 1; i > 0; i--)
        {
            // Swap element "i" with a random earlier element it (or itself)
            var swapIndex = random.Next(i + 1);
            var tmp = items[i];
            items[i] = items[swapIndex];
            items[swapIndex] = tmp;
        }
    }

    #endregion System.Security.Cryptography.RandomNumberGenerator

}

