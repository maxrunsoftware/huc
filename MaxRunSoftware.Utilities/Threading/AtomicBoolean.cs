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
public class AtomicBoolean : IComparable, IComparable<bool>, IEquatable<bool>, IComparable<AtomicBoolean>, IEquatable<AtomicBoolean>
{
    private int mValue;
    public bool Value => mValue == 1;

    public AtomicBoolean(bool startingValue) { mValue = startingValue ? 1 : 0; }

    /// <summary>
    /// Sets this value to true
    /// </summary>
    /// <returns>True if the current value was changed, else false</returns>
    public bool SetTrue() => Set(true);

    /// <summary>
    /// Sets this value to false
    /// </summary>
    /// <returns>True if the current value was changed, else false</returns>
    public bool SetFalse() => Set(false);

    /// <summary>
    /// Sets the value of this object
    /// </summary>
    /// <param name="value">Value</param>
    /// <returns>True if the current value was changed, else false</returns>
    public bool Set(bool value) => value ? 0 == Interlocked.Exchange(ref mValue, 1) : 1 == Interlocked.Exchange(ref mValue, 0);

    public override int GetHashCode() => ((bool)this).GetHashCode();

    public int CompareTo(object obj) => ((bool)this).CompareTo(obj);

    public int CompareTo(bool other) => ((bool)this).CompareTo(other);

    public int CompareTo(AtomicBoolean other) => ((bool)this).CompareTo(other);

    public bool Equals(bool other) => ((bool)this).Equals(other);

    public bool Equals(AtomicBoolean other) => ((bool)this).Equals(other);

    public override bool Equals(object obj) => ((bool)this).Equals(obj);

    public override string ToString() => ((bool)this).ToString();

    public static implicit operator bool(AtomicBoolean atomicBoolean) => atomicBoolean.Value;

    public static implicit operator AtomicBoolean(bool boolean) => new(boolean);

    public static bool operator ==(AtomicBoolean left, AtomicBoolean right)
    {
        if (ReferenceEquals(left, null) && ReferenceEquals(right, null)) return true;

        if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;

        return left.Equals(right);
    }

    public static bool operator !=(AtomicBoolean left, AtomicBoolean right) => !(left == right);

    public static bool operator <(AtomicBoolean left, AtomicBoolean right) => left.CompareTo(right) < 0;

    public static bool operator <=(AtomicBoolean left, AtomicBoolean right) => left.CompareTo(right) <= 0;

    public static bool operator >(AtomicBoolean left, AtomicBoolean right) => left.CompareTo(right) > 0;

    public static bool operator >=(AtomicBoolean left, AtomicBoolean right) => left.CompareTo(right) >= 0;
}
