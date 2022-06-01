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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MaxRunSoftware.Utilities
{
    public static class ExtensionsCheck
    {
        #region CheckFileExists

        public static string CheckFileExists(this string filename)
        {
            if (!File.Exists(filename)) throw new FileNotFoundException("File does not exist " + filename, filename);
            return filename;
        }

        public static string CheckFileExists(this string filename, string argumentName)
        {
            CheckNotNull(filename, argumentName);
            return CheckFileExists(filename);
        }

        #endregion CheckFileExists

        #region CheckMax

        private static ArgumentOutOfRangeException CheckMaxException(object argument, string argumentName, object maximumInclusive) => new ArgumentOutOfRangeException(argumentName, $"Argument {argumentName} with value {argument} cannot be greater than the maximum value of {maximumInclusive}.");

        public static byte CheckMax(this byte argument, string argumentName, byte maximumInclusive)
        {
            if (argument > maximumInclusive) throw CheckMaxException(argument, argumentName, maximumInclusive);
            return argument;
        }

        public static sbyte CheckMax(this sbyte argument, string argumentName, sbyte maximumInclusive)
        {
            if (argument > maximumInclusive) throw CheckMaxException(argument, argumentName, maximumInclusive);
            return argument;
        }

        public static char CheckMax(this char argument, string argumentName, char maximumInclusive)
        {
            if (argument > maximumInclusive) throw CheckMaxException(argument, argumentName, maximumInclusive);
            return argument;
        }

        public static decimal CheckMax(this decimal argument, string argumentName, decimal maximumInclusive)
        {
            if (argument > maximumInclusive) throw CheckMaxException(argument, argumentName, maximumInclusive);
            return argument;
        }

        public static double CheckMax(this double argument, string argumentName, double maximumInclusive)
        {
            if (argument > maximumInclusive) throw CheckMaxException(argument, argumentName, maximumInclusive);
            return argument;
        }

        public static float CheckMax(this float argument, string argumentName, float maximumInclusive)
        {
            if (argument > maximumInclusive) throw CheckMaxException(argument, argumentName, maximumInclusive);
            return argument;
        }

        public static int CheckMax(this int argument, string argumentName, int maximumInclusive)
        {
            if (argument > maximumInclusive) throw CheckMaxException(argument, argumentName, maximumInclusive);
            return argument;
        }

        public static uint CheckMax(this uint argument, string argumentName, uint maximumInclusive)
        {
            if (argument > maximumInclusive) throw CheckMaxException(argument, argumentName, maximumInclusive);
            return argument;
        }

        public static long CheckMax(this long argument, string argumentName, long maximumInclusive)
        {
            if (argument > maximumInclusive) throw CheckMaxException(argument, argumentName, maximumInclusive);
            return argument;
        }

        public static ulong CheckMax(this ulong argument, string argumentName, ulong maximumInclusive)
        {
            if (argument > maximumInclusive) throw CheckMaxException(argument, argumentName, maximumInclusive);
            return argument;
        }

        public static short CheckMax(this short argument, string argumentName, short maximumInclusive)
        {
            if (argument > maximumInclusive) throw CheckMaxException(argument, argumentName, maximumInclusive);
            return argument;
        }

        public static ushort CheckMax(this ushort argument, string argumentName, ushort maximumInclusive)
        {
            if (argument > maximumInclusive) throw CheckMaxException(argument, argumentName, maximumInclusive);
            return argument;
        }

        #endregion CheckMax

        #region CheckMin

        private static ArgumentOutOfRangeException CheckMinException(object argument, string argumentName, object minimumInclusive) => new ArgumentOutOfRangeException(argumentName, $"Argument {argumentName} with value {argument} cannot be less than the minimum value of {minimumInclusive}.");

        public static byte CheckMin(this byte argument, string argumentName, byte minimumInclusive)
        {
            if (argument < minimumInclusive) throw CheckMinException(argument, argumentName, minimumInclusive);
            return argument;
        }

        public static sbyte CheckMin(this sbyte argument, string argumentName, sbyte minimumInclusive)
        {
            if (argument < minimumInclusive) throw CheckMinException(argument, argumentName, minimumInclusive);
            return argument;
        }

        public static char CheckMin(this char argument, string argumentName, char minimumInclusive)
        {
            if (argument < minimumInclusive) throw CheckMinException(argument, argumentName, minimumInclusive);
            return argument;
        }

        public static decimal CheckMin(this decimal argument, string argumentName, decimal minimumInclusive)
        {
            if (argument < minimumInclusive) throw CheckMinException(argument, argumentName, minimumInclusive);
            return argument;
        }

        public static double CheckMin(this double argument, string argumentName, double minimumInclusive)
        {
            if (argument < minimumInclusive) throw CheckMinException(argument, argumentName, minimumInclusive);
            return argument;
        }

        public static float CheckMin(this float argument, string argumentName, float minimumInclusive)
        {
            if (argument < minimumInclusive) throw CheckMinException(argument, argumentName, minimumInclusive);
            return argument;
        }

        public static int CheckMin(this int argument, string argumentName, int minimumInclusive)
        {
            if (argument < minimumInclusive) throw CheckMinException(argument, argumentName, minimumInclusive);
            return argument;
        }

        public static uint CheckMin(this uint argument, string argumentName, uint minimumInclusive)
        {
            if (argument < minimumInclusive) throw CheckMinException(argument, argumentName, minimumInclusive);
            return argument;
        }

        public static long CheckMin(this long argument, string argumentName, long minimumInclusive)
        {
            if (argument < minimumInclusive) throw CheckMinException(argument, argumentName, minimumInclusive);
            return argument;
        }

        public static ulong CheckMin(this ulong argument, string argumentName, ulong minimumInclusive)
        {
            if (argument < minimumInclusive) throw CheckMinException(argument, argumentName, minimumInclusive);
            return argument;
        }

        public static short CheckMin(this short argument, string argumentName, short minimumInclusive)
        {
            if (argument < minimumInclusive) throw CheckMinException(argument, argumentName, minimumInclusive);
            return argument;
        }

        public static ushort CheckMin(this ushort argument, string argumentName, ushort minimumInclusive)
        {
            if (argument < minimumInclusive) throw CheckMinException(argument, argumentName, minimumInclusive);
            return argument;
        }

        #endregion CheckMin

        #region CheckNotNegative

        private static ArgumentOutOfRangeException CheckNotNegativeException(object argument, string argumentName) => new ArgumentOutOfRangeException(argumentName, $"Argument {argumentName} with value {argument} cannot be negative.");

        public static byte CheckNotNegative(this byte argument, string argumentName)
        {
            if (argument < Constant.ZERO_BYTE) throw CheckNotNegativeException(argument, argumentName);
            return argument;
        }

        public static decimal CheckNotNegative(this decimal argument, string argumentName)
        {
            if (argument < Constant.ZERO_DECIMAL) throw CheckNotNegativeException(argument, argumentName);
            return argument;
        }

        public static double CheckNotNegative(this double argument, string argumentName)
        {
            if (argument < Constant.ZERO_DOUBLE) throw CheckNotNegativeException(argument, argumentName);
            return argument;
        }

        public static float CheckNotNegative(this float argument, string argumentName)
        {
            if (argument < Constant.ZERO_FLOAT) throw CheckNotNegativeException(argument, argumentName);
            return argument;
        }

        public static int CheckNotNegative(this int argument, string argumentName)
        {
            if (argument < Constant.ZERO_INT) throw CheckNotNegativeException(argument, argumentName);
            return argument;
        }

        public static long CheckNotNegative(this long argument, string argumentName)
        {
            if (argument < Constant.ZERO_LONG) throw CheckNotNegativeException(argument, argumentName);
            return argument;
        }

        public static short CheckNotNegative(this short argument, string argumentName)
        {
            if (argument < Constant.ZERO_SHORT) throw CheckNotNegativeException(argument, argumentName);
            return argument;
        }

        #endregion CheckNotNegative

        #region CheckNotZero

        private static ArgumentOutOfRangeException CheckNotZeroException(object argument, string argumentName) => new ArgumentOutOfRangeException(argumentName, $"Argument {argumentName} with value {argument} cannot be zero.");

        public static byte CheckNotZero(this byte argument, string argumentName)
        {
            if (argument == Constant.ZERO_BYTE) throw CheckNotZeroException(argument, argumentName);
            return argument;
        }

        public static sbyte CheckNotZero(this sbyte argument, string argumentName)
        {
            if (argument == Constant.ZERO_SBYTE) throw CheckNotZeroException(argument, argumentName);
            return argument;
        }

        public static decimal CheckNotZero(this decimal argument, string argumentName, decimal tolerance = Constant.ZERO_DECIMAL)
        {
            if (Math.Abs(argument) < Math.Abs(tolerance)) throw CheckNotZeroException(argument, argumentName);
            return argument;
        }

        public static double CheckNotZero(this double argument, string argumentName, double tolerance = Constant.ZERO_DOUBLE)
        {
            if (Math.Abs(argument) < Math.Abs(tolerance)) throw CheckNotZeroException(argument, argumentName);
            return argument;
        }

        public static float CheckNotZero(this float argument, string argumentName, float tolerance = Constant.ZERO_FLOAT)
        {
            if (Math.Abs(argument) < Math.Abs(tolerance)) throw CheckNotZeroException(argument, argumentName);
            return argument;
        }

        public static int CheckNotZero(this int argument, string argumentName)
        {
            if (argument == Constant.ZERO_INT) throw CheckNotZeroException(argument, argumentName);
            return argument;
        }

        public static uint CheckNotZero(this uint argument, string argumentName)
        {
            if (argument == Constant.ZERO_UINT) throw CheckNotZeroException(argument, argumentName);
            return argument;
        }

        public static long CheckNotZero(this long argument, string argumentName)
        {
            if (argument == Constant.ZERO_LONG) throw CheckNotZeroException(argument, argumentName);
            return argument;
        }

        public static ulong CheckNotZero(this ulong argument, string argumentName)
        {
            if (argument == Constant.ZERO_ULONG) throw CheckNotZeroException(argument, argumentName);
            return argument;
        }

        public static short CheckNotZero(this short argument, string argumentName)
        {
            if (argument == Constant.ZERO_SHORT) throw CheckNotZeroException(argument, argumentName);
            return argument;
        }

        public static ushort CheckNotZero(this ushort argument, string argumentName)
        {
            if (argument == Constant.ZERO_USHORT) throw CheckNotZeroException(argument, argumentName);
            return argument;
        }

        #endregion CheckNotZero

        #region CheckNotZeroNotNegative

        private static ArgumentOutOfRangeException CheckNotZeroNotNegativeException(object argument, string argumentName) => new ArgumentOutOfRangeException(argumentName, $"Argument {argumentName} with value {argument} cannot be negative or zero.");

        public static byte CheckNotZeroNotNegative(this byte argument, string argumentName)
        {
            if (argument <= Constant.ZERO_BYTE) throw CheckNotZeroNotNegativeException(argument, argumentName);
            return argument;
        }

        public static sbyte CheckNotZeroNotNegative(this sbyte argument, string argumentName)
        {
            if (argument <= Constant.ZERO_SBYTE) throw CheckNotZeroNotNegativeException(argument, argumentName);
            return argument;
        }

        public static decimal CheckNotZeroNotNegative(this decimal argument, string argumentName, decimal tolerance = Constant.ZERO_DECIMAL)
        {
            if (Math.Abs(argument) <= Math.Abs(tolerance)) throw CheckNotZeroException(argument, argumentName);
            return argument;
        }

        public static double CheckNotZeroNotNegative(this double argument, string argumentName, double tolerance = Constant.ZERO_DOUBLE)
        {
            if (Math.Abs(argument) <= Math.Abs(tolerance)) throw CheckNotZeroException(argument, argumentName);
            return argument;
        }

        public static float CheckNotZeroNotNegative(this float argument, string argumentName, float tolerance = Constant.ZERO_FLOAT)
        {
            if (Math.Abs(argument) <= Math.Abs(tolerance)) throw CheckNotZeroException(argument, argumentName);
            return argument;
        }

        public static int CheckNotZeroNotNegative(this int argument, string argumentName)
        {
            if (argument <= Constant.ZERO_INT) throw CheckNotZeroNotNegativeException(argument, argumentName);
            return argument;
        }

        public static uint CheckNotZeroNotNegative(this uint argument, string argumentName)
        {
            if (argument <= Constant.ZERO_UINT) throw CheckNotZeroNotNegativeException(argument, argumentName);
            return argument;
        }

        public static long CheckNotZeroNotNegative(this long argument, string argumentName)
        {
            if (argument <= Constant.ZERO_LONG) throw CheckNotZeroNotNegativeException(argument, argumentName);
            return argument;
        }

        public static ulong CheckNotZeroNotNegative(this ulong argument, string argumentName)
        {
            if (argument <= Constant.ZERO_ULONG) throw CheckNotZeroNotNegativeException(argument, argumentName);
            return argument;
        }

        public static short CheckNotZeroNotNegative(this short argument, string argumentName)
        {
            if (argument <= Constant.ZERO_SHORT) throw CheckNotZeroNotNegativeException(argument, argumentName);
            return argument;
        }

        public static ushort CheckNotZeroNotNegative(this ushort argument, string argumentName)
        {
            if (argument <= Constant.ZERO_USHORT) throw CheckNotZeroNotNegativeException(argument, argumentName);
            return argument;
        }

        #endregion CheckNotZeroNotNegative

        #region CheckNotNull

        /// <summary>
        /// Checks if an argument is null. If it is, throw an ArgumentNullException
        /// </summary>
        /// <typeparam name="T">Argument type</typeparam>
        /// <param name="argument">The argument to check</param>
        /// <param name="argumentName">The nameof argument</param>
        /// <returns>The argument</returns>
        public static T CheckNotNull<T>(this T argument, string argumentName) where T : class
        {
            if (argument == null) throw new ArgumentNullException(argumentName);
            return argument;
        }

        /// <summary>
        /// Checks if an argument is null. If it is, throw an ArgumentNullException
        /// </summary>
        /// <typeparam name="T">Argument type</typeparam>
        /// <param name="argument">The argument to check</param>
        /// <param name="argumentName">The nameof argument</param>
        /// <returns>The argument</returns>
        public static T[] CheckNotNullNotEmpty<T>(this T[] argument, string argumentName)
        {
            if (argument == null) throw new ArgumentNullException(argumentName);
            if (argument.Length == 0) throw new ArgumentNullException(argumentName);
            return argument;
        }

        /// <summary>
        /// Checks if an argument is null. If it is, throw an ArgumentNullException
        /// </summary>
        /// <typeparam name="T">Argument type</typeparam>
        /// <param name="argument">The argument to check</param>
        /// <param name="argumentName">The nameof argument</param>
        /// <returns>The argument</returns>
        public static T? CheckNotNull<T>(this T? argument, string argumentName) where T : struct
        {
            if (argument == null) throw new ArgumentNullException(argumentName);
            return argument;
        }

        /// <summary>
        /// Checks if an argument is null or empty after being trimmed. If it is, throw an ArgumentNullException
        /// </summary>
        /// <param name="argument">The argument to check</param>
        /// <param name="argumentName">The nameof argument</param>
        /// <returns>The trimmed argument</returns>
        public static string CheckNotNullTrimmed(this string argument, string argumentName)
        {
            var s = argument.TrimOrNull();
            if (s == null) throw new ArgumentNullException(argumentName);
            return s;
        }

        #endregion CheckNotNull

        #region CheckNotContains

        public static T[] CheckNotContains<T>(this T[] argument, T argumentToCheckFor, string argumentName) => CheckNotContains(argument, argumentToCheckFor, null, argumentName);

        public static T[] CheckNotContains<T>(this T[] argument, T argumentToCheckFor, IEqualityComparer<T> comparer, string argumentName)
        {
            if (argument == null) return argument;
            if (comparer == null) comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < argument.Length; i++)
            {
                if (comparer.Equals(argument[i], argumentToCheckFor))
                {
                    throw new ArgumentException($"Argument {argumentName}[{i}] cannot contain value {argumentToCheckFor.ToStringGuessFormat()}.");
                }
            }
            return argument;
        }

        public static string[] CheckNotContains(this string[] argument, char[] charactersToCheckFor, string argumentName)
        {
            if (argument == null) return argument;
            for (var i = 0; i < argument.Length; i++)
            {
                var a = argument[i];
                if (a.IndexOfAny(charactersToCheckFor) >= 0)
                {
                    var invalidCharsStr = string.Join("', '", charactersToCheckFor.Select(o => o.ToString()));
                    throw new ArgumentException($"Argument {argumentName}[{i}] cannot contain value {a} which contains one of the invalid characters '{invalidCharsStr}'.");
                }
            }
            return argument;
        }

        #endregion CheckNotContains

        #region CheckOnlyContains

        public static T[] CheckOnlyContains<T>(this T[] argument, T[] validArguments, string argumentName) => CheckOnlyContains(argument, validArguments, null, argumentName);

        public static T[] CheckOnlyContains<T>(this T[] argument, T[] validArguments, IEqualityComparer<T> comparer, string argumentName)
        {
            if (argument == null) return argument;
            if (comparer == null) comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < argument.Length; i++)
            {
                var found = validArguments.Any(o => comparer.Equals(o, argument[i]));
                if (!found)
                {
                    var validArgumentsString = string.Join(", ", validArguments.Select(o => o.ToStringGuessFormat()));
                    throw new ArgumentException($"Argument {argumentName}[{i}] cannot contain value {argument[i].ToStringGuessFormat()} as it is not in the array of valid values [ {validArgumentsString}].");
                }
            }
            return argument;
        }

        public static string[] CheckOnlyContains(this string[] argument, char[] validCharacters, string argumentName)
        {
            if (argument == null) return argument;
            var hashSet = new HashSet<char>(validCharacters);
            for (var i = 0; i < argument.Length; i++)
            {
                var a = argument[i].ToCharArray();
                for (var j = 0; j < a.Length; j++)
                {
                    var c = a[j];
                    if (!hashSet.Contains(c))
                    {
                        var invalidCharsStr = new string(validCharacters);
                        throw new ArgumentException($"Argument {argumentName}[{i}] with value {a} contains invalid character {c} which is not one of the valid characters \"{invalidCharsStr}\".");
                    }
                }
            }
            return argument;
        }

        #endregion CheckOnlyContains

        #region CheckType

        public static Type CheckImplements<T>(this Type type, string argumentName)
        {
            type.CheckNotNull(argumentName);
            var baseClass = typeof(T);
            if (baseClass.IsAssignableFrom(type)) return type;

            var sb = new StringBuilder();
            sb.Append($"Type {type.FullNameFormatted()} does not implement ");
            if (baseClass.IsInterface) sb.Append("interface");
            else if (baseClass.IsAbstract) sb.Append("base abstract class");
            else sb.Append("base class");
            sb.Append(" " + baseClass.FullNameFormatted());
            throw new ArgumentException(sb.ToString(), argumentName);
        }

        public static Type CheckIsEnum(this Type type, string argumentName)
        {
            type.CheckNotNull(argumentName);
            if (type.IsEnum) return type;
            throw new ArgumentException($"Type {type.FullNameFormatted()} is not an enum", argumentName);
        }

        #endregion CheckType

    }
}
