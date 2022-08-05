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

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using MaxRunSoftware.Utilities;

[assembly: Guid(Constant.Id)]

namespace MaxRunSoftware.Utilities;

// ReSharper disable InconsistentNaming
public static partial class Constant
{
    public const string Id = "461985d6-d681-4a0f-b110-547f3beaf967";
    public static readonly Guid Id_Guid = new(Id);

    private static volatile int nextInt;

    public static int NextInt() => Interlocked.Increment(ref nextInt);

    #region Helpers

    private static ImmutableDictionary<TKey, TValue> CreateDictionary<TKey, TValue>(params (TKey Key, TValue Value)[] items) => CreateDictionary(null, items);

    private static ImmutableDictionary<TKey, TValue> CreateDictionary<TKey, TValue>(IEqualityComparer<TKey> comparer, params (TKey Key, TValue Value)[] items)
    {
        items ??= Array.Empty<(TKey Key, TValue Value)>();
        var b = comparer != null ? ImmutableDictionary.CreateBuilder<TKey, TValue>(comparer) : ImmutableDictionary.CreateBuilder<TKey, TValue>();
        foreach (var item in items) b.TryAdd(item.Key, item.Value);
        return b.ToImmutable();
    }

    private static ImmutableHashSet<T> CreateHashSet<T>(params T[] items) => CreateHashSetInternal(null, items);

    private static ImmutableHashSet<T> CreateHashSet<T>(IEqualityComparer<T> comparer, params T[] items) => CreateHashSetInternal(comparer, items);

    private static ImmutableHashSet<T> CreateHashSetInternal<T>(IEqualityComparer<T> comparer, params T[] items)
    {
        items ??= Array.Empty<T>();
        var b = comparer != null ? ImmutableHashSet.CreateBuilder(comparer) : ImmutableHashSet.CreateBuilder<T>();
        foreach (var item in items) b.Add(item);
        return b.ToImmutable();
    }


    private static void LogError(Exception exception, [CallerMemberName] string memberName = "")
    {
        var msg = nameof(Constant) + "." + memberName + "() failed.";
        if (exception != null)
        {
            try
            {
                var err = exception.ToString();
                msg = msg + " " + err;
            }
            catch (Exception)
            {
                try
                {
                    var err = exception.Message;
                    msg = msg + " " + err;
                }
                catch (Exception)
                {
                    try
                    {
                        var err = exception.GetType().FullName;
                        msg = msg + " " + err;
                    }
                    catch (Exception) { }
                }
            }
        }

        try { Debug.WriteLine(msg); }
        catch (Exception)
        {
            try { Console.Error.WriteLine(msg); }
            catch (Exception)
            {
                try { Console.WriteLine(msg); }
                catch { }
            }
        }
    }

    private static string TrimOrNull(string str)
    {
        if (str == null) return null;

        str = str.Trim();
        return str.Length == 0 ? null : str;
    }

    #endregion Helpers
}
