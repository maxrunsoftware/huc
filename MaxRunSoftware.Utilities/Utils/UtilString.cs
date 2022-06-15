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

using System.Net;
using System.Numerics;

namespace MaxRunSoftware.Utilities;

public static partial class Util
{
    /// <summary>
    /// Tries to guess the best type that is valid to convert a string to
    /// </summary>
    /// <param name="s">The string to guess</param>
    /// <returns>The best found Type or string type if a best guess couldn't be found</returns>
    public static Type GuessType(string s) => GuessType(s.CheckNotNull(nameof(s)).Yield());

    /// <summary>
    /// Tries to guess the best type for a group of strings. All strings provided must be convertable for the match to be made
    /// </summary>
    /// <param name="strings">The strings to guess on</param>
    /// <returns>The best found Type or string type if a best guess couldn't be found</returns>
    public static Type GuessType(IEnumerable<string> strings)
    {
        strings.CheckNotNull(nameof(strings));

        var list = strings.TrimOrNull().ToList();
        var listCount = list.Count;

        list = list.WhereNotNull().ToList();
        var nullable = listCount != list.Count;
        if (list.Count == 0) return typeof(string);

        if (list.All(o => Guid.TryParse(o, out _))) return nullable ? typeof(Guid?) : typeof(Guid);

        if (list.All(o => o.CountOccurrences(".") == 3))
        {
            if (list.All(o => IPAddress.TryParse(o, out _))) { return typeof(IPAddress); }
        }

        if (list.All(o => o.ToBoolTry(out _))) return nullable ? typeof(bool?) : typeof(bool);

        if (list.All(o => o.ToByteTry(out _))) return nullable ? typeof(byte?) : typeof(byte);

        if (list.All(o => o.ToSByteTry(out _))) return nullable ? typeof(sbyte?) : typeof(sbyte);

        if (list.All(o => o.ToShortTry(out _))) return nullable ? typeof(short?) : typeof(short);

        if (list.All(o => o.ToUShortTry(out _))) return nullable ? typeof(ushort?) : typeof(ushort);

        if (list.All(o => o.ToIntTry(out _))) return nullable ? typeof(int?) : typeof(int);

        if (list.All(o => o.ToUIntTry(out _))) return nullable ? typeof(uint?) : typeof(uint);

        if (list.All(o => o.ToLongTry(out _))) return nullable ? typeof(long?) : typeof(long);

        if (list.All(o => o.ToULongTry(out _))) return nullable ? typeof(ulong?) : typeof(ulong);

        if (list.All(o => o.ToDecimalTry(out _))) return nullable ? typeof(decimal?) : typeof(decimal);

        if (list.All(o => o.ToFloatTry(out _))) return nullable ? typeof(float?) : typeof(float);

        if (list.All(o => o.ToDoubleTry(out _))) return nullable ? typeof(double?) : typeof(double);

        if (list.All(o => BigInteger.TryParse(o, out _))) return nullable ? typeof(BigInteger?) : typeof(BigInteger);

        if (list.All(o => o.Length == 1)) return nullable ? typeof(char?) : typeof(char);

        if (list.All(o => DateTime.TryParse(o, out _))) return nullable ? typeof(DateTime?) : typeof(DateTime);

        if (list.All(o => Uri.TryCreate(o, UriKind.Absolute, out _))) return typeof(Uri);

        return typeof(string);
    }

    public static DbType GuessDbType(string s) => GuessDbType(s.CheckNotNull(nameof(s)).Yield());

    public static DbType GuessDbType(IEnumerable<string> strings)
    {
        var type = GuessType(strings);

        if (Constant.TYPE_DBTYPE.TryGetValue(type, out var dbType)) return dbType;

        return DbType.String;
    }
}
