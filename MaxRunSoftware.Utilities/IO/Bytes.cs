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

using System.Runtime.InteropServices;

namespace MaxRunSoftware.Utilities;

[Serializable]
public enum BytesType
{
    // ReSharper disable once InconsistentNaming
    SI = 1,

    // ReSharper disable once InconsistentNaming
    IEC = 2
}

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public readonly struct Bytes : IEquatable<Bytes>, IComparable<Bytes>, IComparable
{
    public long Value { get; }
    public BytesType Type { get; }
    public string Name { get; }
    public string Suffix { get; }

    public Bytes(long value, BytesType type, string name, string suffix)
    {
        Value = value;
        Type = type;
        Name = name;
        Suffix = suffix;
    }

    public bool Equals(Bytes other) => Value == other.Value;
    public override bool Equals(object obj) => obj is Bytes other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(Bytes other) => Value.CompareTo(other.Value);

    public int CompareTo(object obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is Bytes other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Bytes)}");
    }

    public override string ToString() => Value.ToString();

    public static bool operator <(Bytes left, Bytes right) => left.CompareTo(right) < 0;
    public static bool operator >(Bytes left, Bytes right) => left.CompareTo(right) > 0;
    public static bool operator <=(Bytes left, Bytes right) => left.CompareTo(right) <= 0;
    public static bool operator >=(Bytes left, Bytes right) => left.CompareTo(right) >= 0;
    public static bool operator ==(Bytes left, Bytes right) => left.Equals(right);
    public static bool operator !=(Bytes left, Bytes right) => !left.Equals(right);

    public static implicit operator ulong(Bytes bytes) => (ulong)bytes.Value;
    public static implicit operator long(Bytes bytes) => bytes.Value;
    public static implicit operator int(Bytes bytes) => (int)bytes.Value;
    public static implicit operator uint(Bytes bytes) => (uint)bytes.Value;
    public static implicit operator float(Bytes bytes) => bytes.Value;
    public static implicit operator double(Bytes bytes) => bytes.Value;
    public static implicit operator decimal(Bytes bytes) => bytes.Value;

    public static readonly Bytes Kilo = new(Constant.Bytes_Kilo, BytesType.SI, nameof(Kilo), "k");
    public static readonly Bytes Kibi = new(Constant.Bytes_Kibi, BytesType.IEC, nameof(Kibi), "ki");
    public static readonly Bytes Mega = new(Constant.Bytes_Mega, BytesType.SI, nameof(Mega), "M");
    public static readonly Bytes Mebi = new(Constant.Bytes_Mebi, BytesType.IEC, nameof(Mebi), "Mi");
    public static readonly Bytes Giga = new(Constant.Bytes_Giga, BytesType.SI, nameof(Giga), "G");
    public static readonly Bytes Gibi = new(Constant.Bytes_Gibi, BytesType.IEC, nameof(Gibi), "Gi");
    public static readonly Bytes Tera = new(Constant.Bytes_Tera, BytesType.SI, nameof(Tera), "T");
    public static readonly Bytes Tebi = new(Constant.Bytes_Tebi, BytesType.IEC, nameof(Tebi), "Ti");
    public static readonly Bytes Peta = new(Constant.Bytes_Peta, BytesType.SI, nameof(Peta), "P");
    public static readonly Bytes Pebi = new(Constant.Bytes_Pebi, BytesType.IEC, nameof(Pebi), "Pi");
    public static readonly Bytes Exa = new(Constant.Bytes_Exa, BytesType.SI, nameof(Exa), "E");
    public static readonly Bytes Exbi = new(Constant.Bytes_Exbi, BytesType.IEC, nameof(Exbi), "Ei");
}
