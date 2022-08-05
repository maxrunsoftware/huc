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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace MaxRunSoftware.Utilities;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public readonly struct Percent : IComparable, IConvertible, ISpanFormattable, IComparable<Percent>, IEquatable<Percent>
{
    /// <summary>
    /// https://stackoverflow.com/a/33697376
    /// </summary>
    private const string DOUBLE_FIXED_POINT = "0.###################################################################################################################################################################################################################################################################################################################################################";

    private readonly double m_value; // Do not rename (binary serialization)

    private const double MIN_VALUE_DOUBLE = 0;
    public static readonly Percent MinValue = (Percent)MIN_VALUE_DOUBLE;

    private const double ONE_VALUE_DOUBLE = 0;
    public static readonly Percent OneValue = (Percent)ONE_VALUE_DOUBLE;

    private const double MAX_VALUE_DOUBLE = 100;
    public static readonly Percent MaxValue = (Percent)MAX_VALUE_DOUBLE;

    // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/operator-overloading
    public static bool operator <(Percent left, Percent right) => left.CompareTo(right) < 0;
    public static bool operator >(Percent left, Percent right) => left.CompareTo(right) > 0;
    public static bool operator <=(Percent left, Percent right) => left.CompareTo(right) <= 0;
    public static bool operator >=(Percent left, Percent right) => left.CompareTo(right) >= 0;
    public static bool operator ==(Percent left, Percent right) => left.Equals(right);
    public static bool operator !=(Percent left, Percent right) => !left.Equals(right);

    public static Percent operator +(Percent left) => left;
    public static Percent operator -(Percent left) => new(MaxValue.m_value - left.m_value);
    public static Percent operator +(Percent left, Percent right) => new(left.m_value + right.m_value);
    public static Percent operator -(Percent left, Percent right) => new(left.m_value - right.m_value);
    public static Percent operator *(Percent left, Percent right) => new(left.m_value * right.m_value);
    public static Percent operator /(Percent left, Percent right) => new(left.m_value / right.m_value);
    public static Percent operator ++(Percent left) => new(left.m_value + OneValue.m_value);
    public static Percent operator --(Percent left) => new(left.m_value - OneValue.m_value);


    public static explicit operator byte(Percent percent) => Convert.ToByte(percent.m_value);
    public static explicit operator sbyte(Percent percent) => Convert.ToSByte(percent.m_value);
    public static explicit operator short(Percent percent) => (byte)percent;
    public static explicit operator ushort(Percent percent) => (byte)percent;
    public static explicit operator int(Percent percent) => (byte)percent;
    public static explicit operator uint(Percent percent) => (byte)percent;
    public static explicit operator ulong(Percent percent) => (byte)percent;
    public static explicit operator long(Percent percent) => (byte)percent;
    public static explicit operator float(Percent percent) => Convert.ToSingle(percent.m_value);
    public static explicit operator double(Percent percent) => percent.m_value;
    public static explicit operator decimal(Percent percent) => Convert.ToDecimal(percent.m_value);

    public static explicit operator Percent(byte value) => new(value);
    public static explicit operator Percent(sbyte value) => new(value);
    public static explicit operator Percent(short value) => new(value);
    public static explicit operator Percent(ushort value) => new(value);
    public static explicit operator Percent(int value) => new(value);
    public static explicit operator Percent(uint value) => new(value);
    public static explicit operator Percent(long value) => new(value);
    public static explicit operator Percent(ulong value) => new(value);
    public static explicit operator Percent(float value) => new(value);
    public static explicit operator Percent(double value) => new(value);
    public static explicit operator Percent(decimal value) => new(Convert.ToDouble(value));

    public Percent() : this(MIN_VALUE_DOUBLE) { }

    private Percent(double value)
    {
        m_value = value switch
        {
            < MIN_VALUE_DOUBLE => MIN_VALUE_DOUBLE,
            > MAX_VALUE_DOUBLE => MAX_VALUE_DOUBLE,
            _ => value
        };
    }


    #region CompareTo

    public int CompareTo(object? value) =>
        value switch
        {
            null => 1,
            Percent p => CompareTo(p),
            _ => throw Exceptions.CompareTo_WrongType(nameof(Percent))
        };

    public int CompareTo(Percent value) => m_value.CompareTo(value.m_value);

    public int CompareTo(Percent value, double tolerance) => Equals(value, tolerance) ? 0 : m_value.CompareTo(value.m_value);

    #endregion CompareTo

    #region Equals

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Percent p && Equals(p);

    public bool Equals(Percent obj) => m_value.Equals(obj.m_value);

    /// <summary>
    /// https://docs.microsoft.com/en-us/dotnet/api/system.double.equals
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public bool Equals(Percent obj, double tolerance) => Math.Abs(m_value - obj.m_value) <= Math.Abs(m_value * tolerance);

    #endregion Equals

    #region GetHashCode

    public override int GetHashCode() => m_value.GetHashCode();

    #endregion GetHashCode

    #region ToString

    // ReSharper disable once SpecifyACultureInStringConversionExplicitly
    public override string ToString() => m_value.ToString(DOUBLE_FIXED_POINT);

    public string ToString(IFormatProvider? provider) => m_value.ToString(provider);

    public string ToString(string? format) => m_value.ToString(format);

    public string ToString(string? format, IFormatProvider? provider) => m_value.ToString(format, provider);

    public string ToString(int decimalPlaces)
    {
        if (decimalPlaces < byte.MinValue) decimalPlaces = byte.MinValue;
        if (decimalPlaces > byte.MinValue) decimalPlaces = byte.MaxValue;

        var rounded = Math.Round(m_value, decimalPlaces, MidpointRounding.AwayFromZero);
        //return rounded.ToString("N" + decimalPlaces);

        return rounded.ToString("0." + new string('#', decimalPlaces));
    }

    #endregion ToString

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null) => m_value.TryFormat(destination, out charsWritten, format, provider);

    #region Parse

    public static Percent Parse(string s) => (Percent)double.Parse(s);

    public static Percent Parse(string s, NumberStyles style) => (Percent)double.Parse(s, style);

    public static Percent Parse(string s, IFormatProvider? provider) => (Percent)double.Parse(s, provider);

    public static Percent Parse(string s, NumberStyles style, IFormatProvider? provider) => (Percent)double.Parse(s, style, provider);

    public static Percent Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null) => (Percent)double.Parse(s, style, provider);

    private static Percent Parse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info) => (Percent)double.Parse(s, style, info);

    #endregion Parse

    #region TryParse

    public static bool TryParse([NotNullWhen(true)] string? s, out Percent result)
    {
        var r = double.TryParse(s, out var o);
        if (r)
        {
            result = (Percent)o;
            return true;
        }

        result = MinValue;
        return false;
    }

    public static bool TryParse(ReadOnlySpan<char> s, out Percent result)
    {
        var r = double.TryParse(s, out var o);
        if (r)
        {
            result = (Percent)o;
            return true;
        }

        result = MinValue;
        return false;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Percent result)
    {
        var r = double.TryParse(s, style, provider, out var o);
        if (r)
        {
            result = (Percent)o;
            return true;
        }

        result = MinValue;
        return false;
    }

    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Percent result)
    {
        var r = double.TryParse(s, style, provider, out var o);
        if (r)
        {
            result = (Percent)o;
            return true;
        }

        result = MinValue;
        return false;
    }

    #endregion TryParse

    #region IConvertible

    public TypeCode GetTypeCode() => TypeCode.Double;

    bool IConvertible.ToBoolean(IFormatProvider? provider) => Convert.ToBoolean(m_value);

    char IConvertible.ToChar(IFormatProvider? provider) => Convert.ToChar(m_value);

    sbyte IConvertible.ToSByte(IFormatProvider? provider) => Convert.ToSByte(m_value);

    byte IConvertible.ToByte(IFormatProvider? provider) => Convert.ToByte(m_value);

    short IConvertible.ToInt16(IFormatProvider? provider) => Convert.ToInt16(m_value);

    ushort IConvertible.ToUInt16(IFormatProvider? provider) => Convert.ToUInt16(m_value);

    int IConvertible.ToInt32(IFormatProvider? provider) => Convert.ToInt32(m_value);

    uint IConvertible.ToUInt32(IFormatProvider? provider) => Convert.ToUInt32(m_value);

    long IConvertible.ToInt64(IFormatProvider? provider) => Convert.ToInt64(m_value);

    ulong IConvertible.ToUInt64(IFormatProvider? provider) => Convert.ToUInt64(m_value);

    float IConvertible.ToSingle(IFormatProvider? provider) => Convert.ToSingle(m_value);

    double IConvertible.ToDouble(IFormatProvider? provider) => m_value;

    decimal IConvertible.ToDecimal(IFormatProvider? provider) => Convert.ToDecimal(m_value);

    DateTime IConvertible.ToDateTime(IFormatProvider? provider) => Convert.ToDateTime(m_value);

    object IConvertible.ToType(Type type, IFormatProvider? provider) => ((IConvertible)m_value).ToType(type, provider);

    #endregion IConvertible
}
