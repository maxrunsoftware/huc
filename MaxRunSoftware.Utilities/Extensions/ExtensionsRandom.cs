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
        items ??= Array.Empty<T>();

        var array = new T[1 + 1 + items.Length];
        array[0] = item1;
        array[1] = item2;
        for (var i = 0; i < items.Length; i++) array[i + 2] = items[i];

        Shuffle(random, array);
        return array;
    }

    public static string NextString(this Random random, int size, params char[] pool) => NextStringInternal(random, size, pool);

    public static string NextString(this Random random, int size, params string[] pool) => NextStringInternal(random, size, pool);

    public static string NextString<T>(this Random random, int size, params T[] pool) => NextStringInternal(random, size, pool);

    private static string NextStringInternal<T>(this Random random, int size, params T[] pool)
    {
        // https://stackoverflow.com/a/1344255

        var data = new byte[4 * size];

        random.NextBytes(data);

        var result = new StringBuilder(size);
        for (var i = 0; i < size; i++)
        {
            var rnd = BitConverter.ToUInt32(data, i * 4);
            var idx = rnd % pool.Length;

            result.Append(pool[idx]);
        }

        return result.ToString();
    }

    public static bool NextBool(this Random random)
    {
        // https://stackoverflow.com/a/28763727
        const double nextBoolHalf = 0.5;
        return random.NextDouble() >= nextBoolHalf;
    }

    public static bool NextBool(this Random random, decimal percentWillBeTrue)
    {
        if (percentWillBeTrue <= 0) return false;
        if (percentWillBeTrue >= 100) return true;
        return random.Next(0, 100) < percentWillBeTrue;
    }


    public static byte NextByte(this Random random, byte minInclusive = byte.MinValue, byte maxInclusive = byte.MaxValue) => (byte)random.Next(minInclusive, maxInclusive + 1);
    public static sbyte NextSByte(this Random random, sbyte minInclusive = sbyte.MinValue, sbyte maxInclusive = sbyte.MaxValue) => (sbyte)random.Next(minInclusive, maxInclusive + 1);
    public static short NextShort(this Random random, short minInclusive = short.MinValue, short maxInclusive = short.MaxValue) => (short)random.Next(minInclusive, maxInclusive + 1);
    public static ushort NextUShort(this Random random, ushort minInclusive = ushort.MinValue, ushort maxInclusive = ushort.MaxValue) => (ushort)random.Next(minInclusive, maxInclusive + 1);

    public static int NextInt(this Random random, int minInclusive = int.MinValue, int maxInclusive = int.MaxValue - 1)
    {
        if (maxInclusive < int.MaxValue) return random.Next(minInclusive, maxInclusive + 1);
        return (int)random.NextInt64(minInclusive, maxInclusive + 1L);
    }

    public static uint NextUInt(this Random random, uint minInclusive = uint.MinValue, uint maxInclusive = uint.MaxValue)
    {
        if (minInclusive < int.MaxValue && maxInclusive < int.MaxValue) return (uint)random.Next((int)minInclusive, (int)maxInclusive + 1);
        return (uint)random.NextInt64(minInclusive, maxInclusive + 1L);
    }

    public static long NextLong(this Random random, long minInclusive = long.MinValue, long maxInclusive = long.MaxValue - 1)
    {
        if (maxInclusive < long.MaxValue) return random.NextInt64(minInclusive, maxInclusive + 1);
        if (minInclusive > long.MinValue) return random.NextInt64(minInclusive - 1L, maxInclusive) + 1L;
        var bytes = new byte[8];
        random.NextBytes(bytes);
        return BitConverter.ToInt64(bytes, 0);
    }

    public static ulong NextULong(this Random random, ulong minInclusive = ulong.MinValue, ulong maxInclusive = ulong.MaxValue - 1UL)
    {
        var min = (decimal)minInclusive;
        var max = (decimal)maxInclusive;
        const decimal minus = long.MinValue;
        var minNew = min + minus;
        var maxNew = max + minus;
        return (ulong)(NextLong(random, (long)minNew, (long)maxNew) - minus);
    }

    public static decimal NextDecimal(this Random random)
    {
        var a = (int)(uint.MaxValue * random.NextDouble());
        var b = (int)(uint.MaxValue * random.NextDouble());
        var c = (int)(uint.MaxValue * random.NextDouble());
        var n = random.NextBool();
        return new decimal(a, b, c, n, 0);
    }

    public static DateTime NextDateTime(this Random random, DateTime? minInclusive = null, DateTime? maxInclusive = null)
    {
        minInclusive ??= DateTime.MinValue.ToUniversalTime();
        var minKind = minInclusive.Value.Kind;

        maxInclusive ??= DateTime.MaxValue.ToUniversalTime();
        var maxKind = maxInclusive.Value.Kind;

        var kind = DateTimeKind.Unspecified;
        if (minKind != DateTimeKind.Unspecified) kind = minKind;
        if (kind == DateTimeKind.Unspecified) kind = maxKind;

        var ticks = random.NextInt64(minInclusive.Value.Ticks, maxInclusive.Value.Ticks + 1L);
        return new DateTime(ticks, kind);
    }

    public static DateOnly NextDateOnly(this Random random, DateOnly? minInclusive = null, DateOnly? maxInclusive = null)
    {
        minInclusive ??= DateOnly.MinValue;
        maxInclusive ??= DateOnly.MaxValue;

        var minDateTime = minInclusive.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var maxDateTime = maxInclusive.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var dt = NextDateTime(random, minDateTime, maxDateTime);
        return new DateOnly(dt.Year, dt.Month, dt.Day);
    }

    public static TimeOnly NextTimeOnly(this Random random, TimeOnly? minInclusive = null, TimeOnly? maxInclusive = null)
    {
        minInclusive ??= TimeOnly.MinValue;
        maxInclusive ??= TimeOnly.MaxValue;

        var dtMin = DateTime.MinValue.Ticks;
        var minDateTime = new DateTime(dtMin + minInclusive.Value.Ticks, DateTimeKind.Utc);
        var maxDateTime = new DateTime(dtMin + maxInclusive.Value.Ticks, DateTimeKind.Utc);

        var dt = NextDateTime(random, minDateTime, maxDateTime);
        return new TimeOnly(dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
    }

    public static byte[] NextBytes(this Random random, int length)
    {
        var bytes = new byte[length];
        random.NextBytes(bytes);
        return bytes;
    }

    public static Guid NextGuid(this Random random) => new(random.NextBytes(16));

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
        } while (result > range);

        return (int)result + fromInclusive;
    }

    public static int Next(this RandomNumberGenerator random) =>
        // ReSharper disable once IntroduceOptionalParameters.Global
        Next(random, 0, int.MaxValue);

    public static int Next(this RandomNumberGenerator random, int maxValueExclusive) => Next(random, 0, maxValueExclusive);

    public static Guid NextGuid(this RandomNumberGenerator random) => new(random.GetBytes(16));

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

    public static string NextString(this RandomNumberGenerator random, int size, params char[] pool) => NextStringInternal(random, size, pool);

    public static string NextString(this RandomNumberGenerator random, int size, params string[] pool) => NextStringInternal(random, size, pool);

    public static string NextString<T>(this RandomNumberGenerator random, int size, params T[] pool) => NextStringInternal(random, size, pool);

    private static string NextStringInternal<T>(this RandomNumberGenerator random, int size, params T[] pool)
    {
        // https://stackoverflow.com/a/1344255

        var data = new byte[4 * size];

        random.GetBytes(data);

        var result = new StringBuilder(size);
        for (var i = 0; i < size; i++)
        {
            var rnd = BitConverter.ToUInt32(data, i * 4);
            var idx = rnd % pool.Length;

            result.Append(pool[idx]);
        }

        return result.ToString();
    }

    public static bool NextBool(this RandomNumberGenerator random) => random.Next(2) == 0;


    public static bool NextBool(this RandomNumberGenerator random, decimal percentWillBeTrue)
    {
        if (percentWillBeTrue <= 0) return false;
        if (percentWillBeTrue >= 100) return true;
        return random.Next(0, 100) < percentWillBeTrue;
    }

    #endregion System.Security.Cryptography.RandomNumberGenerator
}
