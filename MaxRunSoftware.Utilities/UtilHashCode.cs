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

using System.Collections.Generic;

namespace MaxRunSoftware.Utilities
{
    public static partial class Util
    {

        public static int GenerateHashCode<T1>(T1 item1) => (EqualityComparer<T1>.Default.Equals(item1, default) ? 0 : item1.GetHashCode());

        public static int GenerateHashCode<T1, T2>(T1 item1, T2 item2) => GenerateHashCode(item1, item2, false, false, false, false, false, false);

        public static int GenerateHashCode<T1, T2, T3>(T1 item1, T2 item2, T3 item3) => GenerateHashCode(item1, item2, item3, false, false, false, false, false);

        public static int GenerateHashCode<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4) => GenerateHashCode(item1, item2, item3, item4, false, false, false, false);

        public static int GenerateHashCode<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) => GenerateHashCode(item1, item2, item3, item4, item5, false, false, false);

        public static int GenerateHashCode<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) => GenerateHashCode(item1, item2, item3, item4, item5, item6, false, false);

        public static int GenerateHashCode<T1, T2, T3, T4, T5, T6, T7>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) => GenerateHashCode(item1, item2, item3, item4, item5, item6, item7, false);

        public static int GenerateHashCode<T1, T2, T3, T4, T5, T6, T7, T8>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
        {
            // http://stackoverflow.com/a/263416
            unchecked
            {
                const int START = 17;
                const int PRIME = 16777619;

                var hash = START;
                hash = hash * PRIME + (EqualityComparer<T1>.Default.Equals(item1, default) ? 0 : item1.GetHashCode());
                hash = hash * PRIME + (EqualityComparer<T2>.Default.Equals(item2, default) ? 0 : item2.GetHashCode());
                hash = hash * PRIME + (EqualityComparer<T3>.Default.Equals(item3, default) ? 0 : item3.GetHashCode());
                hash = hash * PRIME + (EqualityComparer<T4>.Default.Equals(item4, default) ? 0 : item4.GetHashCode());
                hash = hash * PRIME + (EqualityComparer<T5>.Default.Equals(item5, default) ? 0 : item5.GetHashCode());
                hash = hash * PRIME + (EqualityComparer<T6>.Default.Equals(item6, default) ? 0 : item6.GetHashCode());
                hash = hash * PRIME + (EqualityComparer<T7>.Default.Equals(item7, default) ? 0 : item7.GetHashCode());
                hash = hash * PRIME + (EqualityComparer<T8>.Default.Equals(item8, default) ? 0 : item8.GetHashCode());
                return hash;
            }
        }

        public static int GenerateHashCodeFromCollection<T>(IEnumerable<T> items)
        {
            // http://stackoverflow.com/a/263416
            unchecked
            {
                const int START = 17;
                const int PRIME = 16777619;

                var hash = START;

                foreach (var item in items)
                {
                    hash = hash * PRIME + (EqualityComparer<T>.Default.Equals(item, default) ? 0 : item.GetHashCode());
                }

                return hash;
            }
        }

    }
}
