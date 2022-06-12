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
using System.Threading;

namespace MaxRunSoftware.Utilities;

/// <summary>
/// Simple atomic boolean value
/// </summary>
[Serializable]
[ComVisible(true)]
public struct AtomicBoolean : IComparable, IComparable<bool>, IEquatable<bool>, IComparable<AtomicBoolean>, IEquatable<AtomicBoolean>
{
    private int m_value;
    public bool Value => m_value == 1;

    public AtomicBoolean(bool startingValue)
    {
        m_value = startingValue ? 1 : 0;
    }

    /// <summary>
    /// Sets this value to true
    /// </summary>
    /// <returns>True if the current value was changed, else false</returns>
    public bool SetTrue()
    {
        return Set(true);
    }

    /// <summary>
    /// Sets this value to false
    /// </summary>
    /// <returns>True if the current value was changed, else false</returns>
    public bool SetFalse()
    {
        return Set(false);
    }

    /// <summary>
    /// Sets the value of this object
    /// </summary>
    /// <param name="value">Value</param>
    /// <returns>True if the current value was changed, else false</returns>
    public bool Set(bool value)
    {
        return value ? 0 == Interlocked.Exchange(ref m_value, 1) : 1 == Interlocked.Exchange(ref m_value, 0);
    }

    public override int GetHashCode()
    {
        return ((bool)this).GetHashCode();
    }

    public int CompareTo(object obj)
    {
        return ((bool)this).CompareTo(obj);
    }

    public int CompareTo(bool other)
    {
        return ((bool)this).CompareTo(other);
    }

    public int CompareTo(AtomicBoolean other)
    {
        return ((bool)this).CompareTo(other);
    }

    public bool Equals(bool other)
    {
        return ((bool)this).Equals(other);
    }

    public bool Equals(AtomicBoolean other)
    {
        return ((bool)this).Equals(other);
    }

    public override bool Equals(object obj)
    {
        return ((bool)this).Equals(obj);
    }

    public override string ToString()
    {
        return ((bool)this).ToString();
    }

    public static implicit operator bool(AtomicBoolean atomicBoolean)
    {
        return atomicBoolean.Value;
    }

    public static implicit operator AtomicBoolean(bool boolean)
    {
        return new AtomicBoolean(boolean);
    }

    public static bool operator ==(AtomicBoolean left, AtomicBoolean right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AtomicBoolean left, AtomicBoolean right)
    {
        return !(left == right);
    }

    public static bool operator <(AtomicBoolean left, AtomicBoolean right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(AtomicBoolean left, AtomicBoolean right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(AtomicBoolean left, AtomicBoolean right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(AtomicBoolean left, AtomicBoolean right)
    {
        return left.CompareTo(right) >= 0;
    }
}
