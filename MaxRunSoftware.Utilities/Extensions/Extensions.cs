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
using System.Drawing;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace MaxRunSoftware.Utilities;

[DebuggerStepThrough]
public static class Extensions
{
    /// <summary>
    /// Attempts to convert the DotNet type to a DbType
    /// </summary>
    /// <param name="type">The DotNet type</param>
    /// <param name="defaultDbType">The default DbType</param>
    /// <returns>The DbType</returns>
    public static DbType GetDbType(this Type type, DbType defaultDbType)
    {
        return Constant.Type_DbType.TryGetValue(type, out var dbType) ? dbType : defaultDbType;
    }

    /// <summary>
    /// Attempts to convert the DotNet type to a DbType
    /// </summary>
    /// <param name="type">The DotNet type</param>
    /// <returns>The DbType</returns>
    public static DbType? GetDbType(this Type type)
    {
        return Constant.Type_DbType.TryGetValue(type, out var dbType) ? dbType : null;
    }

    /// <summary>
    /// Converts a DbType to a DotNet type
    /// </summary>
    /// <param name="dbType">The DbType</param>
    /// <returns>The DotNet type</returns>
    public static Type GetDotNetType(this DbType dbType)
    {
        return Constant.DbType_Type.TryGetValue(dbType, out var type) ? type : typeof(string);
    }

    /// <summary>
    /// Gets a typed service from a IServiceProvider
    /// </summary>
    /// <typeparam name="T">The service type</typeparam>
    /// <param name="serviceProvider">The service provider</param>
    /// <returns>The typed service</returns>
    public static T GetService<T>(this IServiceProvider serviceProvider)
    {
        return (T)serviceProvider.GetService(typeof(T));
    }

    /// <summary>
    /// Gets a typed value from a serialized object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="serializationInfo"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static T GetValue<T>(this SerializationInfo serializationInfo, string name)
    {
        return (T)serializationInfo.GetValue(name, typeof(T));
    }

    #region IDisposable

    public static void DisposeSafely(this IDisposable disposable, Action<string, Exception> onErrorLog)
    {
        onErrorLog.CheckNotNull(nameof(onErrorLog));
        if (disposable == null)
        {
            return;
        }

        try
        {
            disposable.Dispose();
        }
        catch (Exception e)
        {
            onErrorLog($"Error calling {disposable.GetType().FullNameFormatted()}.Dispose() : {e.Message}", e);
        }
    }

    private static readonly IBucketReadOnly<Type, Action<object>> closeSafelyCache = new BucketCacheThreadSafeCopyOnWrite<Type, Action<object>>(CloseSafelyCreate);

    private static Action<object> CloseSafelyCreate(Type type)
    {
        // ReSharper disable once ReplaceWithSingleCallToFirstOrDefault
        var method = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(o => o.GetParameters().Length == 0)
            .Where(o => o.Name.Equals("Close"))
            .FirstOrDefault();


        if (method == null)
        {
            throw new Exception("Close() method not found on object " + type.FullNameFormatted());
        }

        return Util.CreateAction(method);
    }

    public static void CloseSafely(this IDisposable objectWithCloseMethod, Action<string, Exception> onErrorLog)
    {
        if (objectWithCloseMethod == null)
        {
            return;
        }

        var type = objectWithCloseMethod.GetType();
        var closer = closeSafelyCache[type];

        try
        {
            closer(objectWithCloseMethod);
        }
        catch (Exception e)
        {
            onErrorLog($"Error calling {objectWithCloseMethod.GetType().FullNameFormatted()}.Close() : {e.Message}", e);
        }
    }

    #endregion IDisposable

    #region Regex

    public static string[] MatchAll(this Regex regex, string input)
    {
        return regex.Matches(input).Select(o => o.Value).ToArray();
    }

    public static string MatchFirst(this Regex regex, string input)
    {
        return regex.Matches(input).Select(o => o.Value).FirstOrDefault();
    }

    #endregion Regex

    #region RoundAwayFromZero

    public static decimal Round(this decimal value, MidpointRounding rounding, int decimalPlaces)
    {
        return Math.Round(value, decimalPlaces, rounding);
    }

    public static double Round(this double value, MidpointRounding rounding, int decimalPlaces)
    {
        return Math.Round(value, decimalPlaces, rounding);
    }

    public static double Round(this float value, MidpointRounding rounding, int decimalPlaces)
    {
        return Math.Round(value, decimalPlaces, rounding);
    }

    #endregion RoundAwayFromZero

    #region Equals Floating Point

    public static bool Equals(this double left, double right, byte digitsOfPrecision)
    {
        return Math.Abs(left - right) < Math.Pow(10d, -1d * digitsOfPrecision);
    }

    public static bool Equals(this float left, float right, byte digitsOfPrecision)
    {
        return Math.Abs(left - right) < Math.Pow(10f, -1f * digitsOfPrecision);
    }

    #endregion Equals Floating Point

    #region Color

    public static string ToCss(this Color color)
    {
        var a = (color.A * (100d / 255d) / 100d).ToString(MidpointRounding.AwayFromZero, 1);
        return $"rgba({color.R}, {color.G}, {color.B}, {a})";
    }

    public static Color Shift(this Color startColor, Color endColor, double percentShifted)
    {
        if (percentShifted > 1d)
        {
            percentShifted = 1d;
        }

        if (percentShifted < 0d)
        {
            percentShifted = 0d;
        }

        //int rStart = startColor.R;
        //int rEnd = endColor.R;
        var r = ShiftPercent(startColor.R, endColor.R, percentShifted);
        var g = ShiftPercent(startColor.G, endColor.G, percentShifted);
        var b = ShiftPercent(startColor.B, endColor.B, percentShifted);
        var a = ShiftPercent(startColor.A, endColor.A, percentShifted);

        return Color.FromArgb(a, r, g, b);
    }

    private static byte ShiftPercent(byte start, byte end, double percent)
    {
        if (start == end)
        {
            return start;
        }

        var d = (double)start - end;
        d = d * percent;
        var i = int.Parse(d.ToString(MidpointRounding.AwayFromZero, 0));
        i = start - i;
        if (start < end && i > end)
        {
            i = end;
        }

        if (start > end && i < end)
        {
            i = end;
        }

        if (i > byte.MaxValue)
        {
            i = byte.MaxValue;
        }

        if (i < byte.MinValue)
        {
            i = byte.MinValue;
        }

        var by = (byte)i;
        return by;
    }

    #endregion Color

    #region Conversion

    public static T ToNotNull<T>(T? nullable, T ifNull) where T : struct
    {
        return nullable ?? ifNull;
    }

    public static KeyValuePair<TKey, TValue> ToKeyValuePair<TKey, TValue>(this Tuple<TKey, TValue> tuple)
    {
        return new KeyValuePair<TKey, TValue>(tuple.Item1, tuple.Item2);
    }

    public static ILookup<TKey, TElement> ToLookup<TKey, TEnumerable, TElement>(this IDictionary<TKey, TEnumerable> dictionary) where TEnumerable : IEnumerable<TElement>
    {
        return dictionary.SelectMany(p => p.Value, Tuple.Create).ToLookup(p => p.Item1.Key, p => p.Item2);
    }

    public static Tuple<TKey, TValue> ToTuple<TKey, TValue>(this KeyValuePair<TKey, TValue> keyValuePair)
    {
        return Tuple.Create(keyValuePair.Key, keyValuePair.Value);
    }

    #endregion Conversion
}
