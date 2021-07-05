/*
Copyright (c) 2021 Steven Foster (steven.d.foster@gmail.com)

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

using System;
using System.Collections;
using System.Collections.Generic;

namespace MaxRunSoftware.Utilities
{
    /// <summary>
    /// Makes implementing a comparator easier
    /// </summary>
    /// <typeparam name="T">Type this comparator compares</typeparam>
    public abstract class ComparatorBase<T> : IComparer<T>, IEqualityComparer<T>, IComparer, IEqualityComparer
    {
        public abstract int Compare(T x, T y);

        public abstract int GetHashCode(T obj);

        public int Compare(object x, object y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            if (x is T sa)
            {
                if (y is T sb)
                {
                    return Compare(sa, sb);
                }
            }

            if (x is IComparable ia)
            {
                return ia.CompareTo(y);
            }

            throw new ArgumentException("Argument_ImplementIComparable"); // TODO: Better error
        }

        public virtual bool Equals(T x, T y) => Compare(x, y) == 0;

        public new bool Equals(object x, object y)
        {
            if (x == y) return true;
            if (x == null || y == null) return false;

            if (x is T sa)
            {
                if (y is T sb)
                {
                    return Equals(sa, sb);
                }
            }
            return x.Equals(y);
        }

        public int GetHashCode(object obj)
        {
            obj.CheckNotNull(nameof(obj));

            if (obj is T s)
            {
                return GetHashCode(s);
            }
            return obj.GetHashCode();
        }
    }
}
