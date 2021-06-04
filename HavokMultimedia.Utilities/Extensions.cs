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
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HavokMultimedia.Utilities
{
    public delegate object TypeConverter(object inputObject, Type outputType);

    public static class TypeConverterExtensions
    {
        public static TypeConverter AsTypeConverter<TInput, TOutput>(this Converter<TInput, TOutput> converter) => (inputObject, outputType) => converter((TInput)inputObject);

        public static Converter<TInput, TOutput> AsConverter<TInput, TOutput>(this TypeConverter typeConverter) => inputObject => (TOutput)typeConverter(inputObject, typeof(TOutput));
    }

    /// <summary>
    /// Formatting of methods for debugging
    /// https://github.com/kellyelton/System.Reflection.ExtensionMethods/tree/master/System.Reflection.ExtensionMethods
    /// </summary>
    public static class MethodSignatureTools
    {
        public static string GetSignature(this MethodBase method, bool invokable)
        {
            var signatureBuilder = new StringBuilder();

            // Add our method accessors if it's not invokable
            if (!invokable)
            {
                signatureBuilder.Append(GetMethodAccessorSignature(method));
                signatureBuilder.Append(" ");
            }

            // Add method name
            signatureBuilder.Append(method.Name);

            // Add method generics
            if (method.IsGenericMethod)
            {
                signatureBuilder.Append(GetGenericSignature(method));
            }

            // Add method parameters
            signatureBuilder.Append(GetMethodArgumentsSignature(method, invokable));

            return signatureBuilder.ToString();
        }

        public static string GetMethodAccessorSignature(this MethodBase method)
        {
            string signature = null;

            if (method.IsAssembly)
            {
                signature = "internal ";

                if (method.IsFamily)
                    signature += "protected ";
            }
            else if (method.IsPublic)
            {
                signature = "public ";
            }
            else if (method.IsPrivate)
            {
                signature = "private ";
            }
            else if (method.IsFamily)
            {
                signature = "protected ";
            }

            if (method.IsStatic)
                signature += "static ";

            if (method is MethodInfo methodInfo)
            {
                signature += GetSignature(methodInfo.ReturnType);
            }

            return signature;
        }

        public static string GetGenericSignature(this MethodBase method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (!method.IsGenericMethod) throw new ArgumentException($"{method.Name} is not generic.");

            return BuildGenericSignature(method.GetGenericArguments());
        }

        public static string GetMethodArgumentsSignature(this MethodBase method, bool invokable)
        {
            var isExtensionMethod = method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false);
            var methodParameters = method.GetParameters().AsEnumerable();

            // If this signature is designed to be invoked and it's an extension method
            if (isExtensionMethod && invokable)
            {
                // Skip the first argument
                methodParameters = methodParameters.Skip(1);
            }

            var methodParameterSignatures = methodParameters.Select(param =>
            {
                var signature = string.Empty;

                if (param.ParameterType.IsByRef)
                    signature = "ref ";
                else if (param.IsOut)
                    signature = "out ";
                else if (isExtensionMethod && param.Position == 0)
                    signature = "this ";

                if (!invokable)
                {
                    signature += GetSignature(param.ParameterType) + " ";
                }

                signature += param.Name;

                return signature;
            });

            var methodParameterString = "(" + string.Join(", ", methodParameterSignatures) + ")";

            return methodParameterString;
        }

        /// <summary>Get a fully qualified signature for <paramref name="type" /></summary>
        /// <param name="type">Type. May be generic or <see cref="Nullable{T}" /></param>
        /// <returns>Fully qualified signature</returns>
        public static string GetSignature(this Type type)
        {
            var isNullableType = type.IsNullable(out var underlyingNullableType);

            var signatureType = isNullableType
                ? underlyingNullableType
                : type;

            var isGenericType = signatureType.IsGeneric();

            var signature = GetQualifiedTypeName(signatureType);

            if (isGenericType)
            {
                // Add the generic arguments
                signature += BuildGenericSignature(signatureType.GetGenericArguments());
            }

            if (isNullableType)
            {
                signature += "?";
            }

            return signature;
        }

        /// <summary>
        /// Takes an <see cref="IEnumerable{T}" /> and creates a generic type signature (&lt;string,
        /// string&gt; for example)
        /// </summary>
        /// <param name="genericArgumentTypes"></param>
        /// <returns>Generic type signature like &lt;Type, ...&gt;</returns>
        public static string BuildGenericSignature(IEnumerable<Type> genericArgumentTypes)
        {
            var argumentSignatures = genericArgumentTypes.Select(GetSignature);

            return "<" + string.Join(", ", argumentSignatures) + ">";
        }

        /// <summary>
        /// Gets the fully qualified type name of <paramref name="type" />. This will use any
        /// keywords in place of types where possible (string instead of System.String for example)
        /// </summary>
        /// <param name="type"></param>
        /// <returns>The fully qualified name for <paramref name="type" /></returns>
        public static string GetQualifiedTypeName(Type type)
        {
            var n = type.Name;
            if (n != null)
            {
                switch (n.ToUpperInvariant())
                {
                    case "SYSTEM.BOOLEAN":
                    case "BOOLEAN":
                    case "BOOL":
                        return "bool";

                    case "SYSTEM.BYTE":
                    case "BYTE":
                        return "byte";

                    case "SYSTEM.SBYTE":
                    case "SBYTE":
                        return "sbyte";

                    case "SYSTEM.CHAR":
                    case "CHAR":
                        return "char";

                    case "SYSTEM.DECIMAL":
                    case "DECIMAL":
                        return "decimal";

                    case "SYSTEM.DOUBLE":
                    case "DOUBLE":
                        return "double";

                    case "SYSTEM.SINGLE":
                        return "float";

                    case "SYSTEM.INT32":
                    case "INT32":
                    case "SYSTEN.INT":
                    case "INT":
                        return "int";

                    case "SYSTEM.UINT32":
                    case "UINT32":
                    case "SYSTEM.UINT":
                    case "UINT":
                        return "uint";

                    case "SYSTEM.INT64":
                    case "INT64":
                    case "SYSTEM.LONG":
                    case "LONG":
                        return "long";

                    case "SYSTEM.UINT64":
                    case "UINT64":
                    case "SYSTEM.ULONG":
                    case "ULONG":
                        return "ulong";

                    case "SYSTEM.INT16":
                    case "INT16":
                    case "SYSTEM.SHORT":
                    case "SHORT":
                        return "short";

                    case "SYSTEM.UINT16":
                    case "UINT16":
                    case "SYSTEM.USHORT":
                    case "USHORT":
                        return "ushort";

                    case "SYSTEM.OBJECT":
                    case "OBJECT":
                        return "object";

                    case "SYSTEM.STRING":
                    case "STRING":
                        return "string";

                    case "SYSTEM.VOID":
                    case "VOID":
                        return "void";
                }
            }

            var signature = type.FullName.TrimOrNull() ?? type.Name;

            if (type.IsGeneric()) signature = RemoveGenericTypeNameArgumentCount(signature);
            return signature;
        }

        /// <summary>This removes the `{argumentcount} from a the signature of a generic type</summary>
        /// <param name="genericTypeSignature">Signature of a generic type</param>
        /// <returns><paramref name="genericTypeSignature" /> without any argument count</returns>
        public static string RemoveGenericTypeNameArgumentCount(string genericTypeSignature) => genericTypeSignature.Substring(0, genericTypeSignature.IndexOf('`'));
    }

    [System.Diagnostics.DebuggerStepThrough]
    public static class Extensions
    {
        /// <summary>
        /// Attempts to convert the DotNet type to a DbType
        /// </summary>
        /// <param name="type">The DotNet type</param>
        /// <returns>The DbType</returns>
        public static DbType GetDbType(this Type type) => Constant.MAP_Type_DbType.TryGetValue(type, out var dbType) ? dbType : DbType.String;

        /// <summary>
        /// Converts a DbType to a DotNet type
        /// </summary>
        /// <param name="dbType">The DbType</param>
        /// <returns>The DotNet type</returns>
        public static Type GetDotNetType(this DbType dbType) => Constant.MAP_DbType_Type.TryGetValue(dbType, out var type) ? type : typeof(string);

        /// <summary>
        /// Gets a typed service from a IServiceProvider
        /// </summary>
        /// <typeparam name="T">The service type</typeparam>
        /// <param name="serviceProvider">The service provider</param>
        /// <returns>The typed service</returns>
        public static T GetService<T>(this IServiceProvider serviceProvider) => (T)serviceProvider.GetService(typeof(T));

        /// <summary>
        /// Gets a typed value from a serialized object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializationInfo"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetValue<T>(this System.Runtime.Serialization.SerializationInfo serializationInfo, string name) => (T)serializationInfo.GetValue(name, typeof(T));


        #region Attributes

        /*
        public static T GetAttribute<T>(PropertyInfo propertyInfo) where T : Attribute
        {
        }

        public static IEnumerable<T> GetAttributes<T>(PropertyInfo propertyInfo) where T : Attribute
        {
        }
        */

        #endregion Attributes

        #region string

        /// <summary>
        /// Gets the Ordinal hashcode
        /// </summary>
        /// <param name="str">The string</param>
        /// <returns>The hashcode</returns>
        public static int GetHashCodeCaseSensitive(this string str) => str == null ? 0 : StringComparer.Ordinal.GetHashCode(str);

        /// <summary>
        /// Gets the OrdinalIgnoreCase hashcode
        /// </summary>
        /// <param name="str">The string</param>
        /// <returns>The hashcode</returns>
        public static int GetHashCodeCaseInsensitive(this string str) => str == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(str);

        /// <summary>
        /// Removes the first character from a string if there is one
        /// </summary>
        /// <param name="str">The string</param>
        /// <returns>The string without the first character</returns>
        public static string RemoveLeft(this string str) => RemoveLeft(str, out var c);

        /// <summary>
        /// Removes the leftmost character from a string
        /// </summary>
        /// <param name="str"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string RemoveLeft(this string str, out char c)
        {
            if (str.Length == 0)
            {
                c = char.MinValue;
                return string.Empty;
            }
            c = str[0];
            if (str.Length == 1) return string.Empty;
            return str.Substring(1);
        }

        /// <summary>
        /// Removes the leftmost character from a string
        /// </summary>
        /// <param name="str"></param>
        /// <param name="numberOfCharactersToRemove"></param>
        /// <returns></returns>
        public static string RemoveLeft(this string str, int numberOfCharactersToRemove) => numberOfCharactersToRemove.CheckNotNegative(nameof(numberOfCharactersToRemove)) >= str.Length ? string.Empty : str.Substring(numberOfCharactersToRemove);

        /// <summary>
        /// Removes the rightmost characters from a string
        /// </summary>
        /// <param name="str"></param>
        /// <param name="numberOfCharactersToRemove"></param>
        /// <returns></returns>
        public static string RemoveRight(this string str, int numberOfCharactersToRemove) => numberOfCharactersToRemove.CheckNotNegative(nameof(numberOfCharactersToRemove)) >= str.Length ? string.Empty : str.Substring(0, str.Length - numberOfCharactersToRemove);

        /// <summary>
        /// Removes the rightmost character from a string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string RemoveRight(this string str) => RemoveRight(str, out var c);

        /// <summary>
        /// Removes the rightmost character from a string
        /// </summary>
        /// <param name="str"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string RemoveRight(this string str, out char c)
        {
            if (str.Length == 0)
            {
                c = char.MinValue;
                return string.Empty;
            }
            c = str[str.Length - 1];
            if (str.Length == 1) return string.Empty;
            return str.Substring(0, str.Length - 1);
        }

        /// <summary>
        /// Counts the number of occurances of a specific string
        /// </summary>
        /// <param name="str"></param>
        /// <param name="stringToSearchFor"></param>
        /// <param name="comparison"></param>
        /// <returns></returns>
        public static int CountOccurances(this string str, string stringToSearchFor, StringComparison? comparison = null)
        {
            str.Remove(stringToSearchFor, out var num, comparison);
            return num;
        }

        public static string ReplaceAt(this string input, int index, char newChar)
        {
            var chars = input.CheckNotNull(nameof(input)).ToCharArray();
            chars[index] = newChar;
            return new string(chars);
        }

        public static string Remove(this string str, string toRemove, out int itemsRemoved, StringComparison? comparison = null)
        {
            // https://stackoverflow.com/q/541954
            var strRemoved = comparison == null ? str.Replace(toRemove, string.Empty) : str.Replace(toRemove, string.Empty, comparison.Value);
            var countRemoved = (str.Length - strRemoved.Length) / toRemove.Length;

            itemsRemoved = countRemoved;
            return strRemoved;
        }

        public static string Remove(this string str, string toRemove) => Remove(str, toRemove, out var trash);

        public static string Right(this string str, int characterCount)
        {
            characterCount.CheckNotNegative(nameof(characterCount));
            characterCount = Math.Min(characterCount, str.Length);
            return str.Substring(str.Length - characterCount, characterCount);
        }

        public static string Left(this string str, int characterCount)
        {
            characterCount.CheckNotNegative(nameof(characterCount));
            characterCount = Math.Min(characterCount, str.Length);
            return str.Substring(0, characterCount);
        }

        public static string[] SplitIntoParts(this string str, int numberOfParts)
        {
            if (numberOfParts < 1) numberOfParts = 1;
            var arraySize = str.Length;
            var div = arraySize / numberOfParts;
            var remainder = arraySize % numberOfParts;

            var partSizes = new int[numberOfParts];
            for (var i = 0; i < numberOfParts; i++) partSizes[i] = div + (i < remainder ? 1 : 0);

            var newArray = new string[numberOfParts];
            var counter = 0;
            for (var i = 0; i < numberOfParts; i++)
            {
                var partSize = partSizes[i];
                newArray[i] = str.Substring(counter, partSize);
                counter += partSize;
            }
            return newArray;
        }

        // Summary: Reports the zero-based index of the first occurrence in this instance of any
        // character in a specified array of Unicode characters.
        //
        // Parameters: anyOf: A Unicode character array containing one or more characters to seek.
        //
        // Returns: The zero-based index position of the first occurrence in this instance where any
        // character in anyOf was found; -1 if no character in anyOf was found.
        //
        // Exceptions: T:System.ArgumentNullException: anyOf is null.
        public static int IndexOfAny(this string str, params char[] chars) => str.IndexOfAny(chars);

        // Summary: Returns a value indicating whether a specified substring occurs within this string.
        //
        // Parameters: subStrings: The strings to seek.
        //
        // Returns: true if the value parameter occurs within this string, or if value is the empty
        // string (""); otherwise, false.
        //
        // Exceptions: T:System.ArgumentNullException: value is null.
        public static bool ContainsAny(this string str, params string[] subStrings) => ContainsAny(str, default, subStrings);

        // Summary: Returns a value indicating whether a specified substring occurs within this string.
        //
        // Parameters: subStrings: The strings to seek.
        //
        // Returns: true if the value parameter occurs within this string, or if value is the empty
        // string (""); otherwise, false.
        //
        // Exceptions: T:System.ArgumentNullException: value is null.
        public static bool ContainsAny(this string str, StringComparison stringComparison, params string[] subStrings)
        {
            foreach (var subString in subStrings)
            {
                if (str.IndexOf(subString, stringComparison) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool ContainsWhiteSpace(this string str)
        {
            var chars = str.ToCharArray();
            for (var i = 0; i < str.Length; i++)
            {
                if (char.IsWhiteSpace(chars[i])) return true;
            }
            return false;
        }

        public static string[] SplitOnCamelCase(this string str)
        {
            if (str == null) return Array.Empty<string>();
            if (str.Length == 0) return new string[] { str };

            // https://stackoverflow.com/a/37532157
            string[] words = Regex.Matches(str, "(^[a-z]+|[A-Z]+(?![a-z])|[A-Z][a-z]+|[0-9]+|[a-z]+)")
            .OfType<Match>()
            .Select(m => m.Value)
            .ToArray();

            return words;
        }

        public static string EscapeHtml(this string unescaped) => unescaped == null ? null : WebUtility.HtmlEncode(unescaped);

        #region Equals

        public static bool Equals(this string str, string other, StringComparer comparer)
        {
            IEqualityComparer<string> ec = comparer;
            if (ec == null) ec = EqualityComparer<string>.Default;
            return ec.Equals(str, other);
        }

        public static bool Equals(this string str, string[] others, out string match, StringComparer comparer)
        {
            IEqualityComparer<string> ec = comparer;
            if (ec == null) ec = EqualityComparer<string>.Default;
            foreach (var other in others.OrEmpty())
            {
                if (ec.Equals(str, other))
                {
                    match = other;
                    return true;
                }
            }
            match = null;
            return false;
        }

        public static bool EqualsCaseSensitive(this string str, string other) => Equals(str, other, StringComparer.Ordinal);

        public static bool EqualsCaseSensitive(this string str, string[] others, out string match) => Equals(str, others, out match, StringComparer.Ordinal);

        public static bool EqualsCaseSensitive(this string str, string[] others) => Equals(str, others, out _, StringComparer.Ordinal);

        public static bool EqualsCaseInsensitive(this string str, string other) => Equals(str, other, StringComparer.OrdinalIgnoreCase);

        public static bool EqualsCaseInsensitive(this string str, string[] others, out string match) => Equals(str, others, out match, StringComparer.OrdinalIgnoreCase);

        public static bool EqualsCaseInsensitive(this string str, string[] others) => Equals(str, others, out _, StringComparer.OrdinalIgnoreCase);

        public static bool EqualsWildcard(this string text, string wildcardString)
        {
            // https://bitbucket.org/hasullivan/fast-wildcard-matching/src/7457d0dc1aee5ecd373f7c8a7785d5891b416201/FastWildcardMatching/WildcardMatch.cs?at=master&fileviewer=file-view-default

            var isLike = true;
            byte matchCase = 0;
            char[] filter;
            char[] reversedFilter;
            char[] reversedWord;
            char[] word;
            var currentPatternStartIndex = 0;
            var lastCheckedHeadIndex = 0;
            var lastCheckedTailIndex = 0;
            var reversedWordIndex = 0;
            var reversedPatterns = new List<char[]>();

            if (text == null || wildcardString == null)
            {
                return false;
            }

            word = text.ToCharArray();
            filter = wildcardString.ToCharArray();

            //Set which case will be used (0 = no wildcards, 1 = only ?, 2 = only *, 3 = both ? and *
            for (var i = 0; i < filter.Length; i++)
            {
                if (filter[i] == '?')
                {
                    matchCase += 1;
                    break;
                }
            }

            for (var i = 0; i < filter.Length; i++)
            {
                if (filter[i] == '*')
                {
                    matchCase += 2;
                    break;
                }
            }

            if ((matchCase == 0 || matchCase == 1) && word.Length != filter.Length)
            {
                return false;
            }

            switch (matchCase)
            {
                case 0:
                    isLike = (text == wildcardString);
                    break;

                case 1:
                    for (var i = 0; i < text.Length; i++)
                    {
                        if ((word[i] != filter[i]) && filter[i] != '?')
                        {
                            isLike = false;
                        }
                    }
                    break;

                case 2:
                    //Search for matches until first *
                    for (var i = 0; i < filter.Length; i++)
                    {
                        if (filter[i] != '*')
                        {
                            if (filter[i] != word[i])
                            {
                                return false;
                            }
                        }
                        else
                        {
                            lastCheckedHeadIndex = i;
                            break;
                        }
                    }
                    //Search Tail for matches until first *
                    for (var i = 0; i < filter.Length; i++)
                    {
                        if (filter[filter.Length - 1 - i] != '*')
                        {
                            if (filter[filter.Length - 1 - i] != word[word.Length - 1 - i])
                            {
                                return false;
                            }
                        }
                        else
                        {
                            lastCheckedTailIndex = i;
                            break;
                        }
                    }

                    //Create a reverse word and filter for searching in reverse. The reversed word and filter do not include already checked chars
                    reversedWord = new char[word.Length - lastCheckedHeadIndex - lastCheckedTailIndex];
                    reversedFilter = new char[filter.Length - lastCheckedHeadIndex - lastCheckedTailIndex];

                    for (var i = 0; i < reversedWord.Length; i++)
                    {
                        reversedWord[i] = word[word.Length - (i + 1) - lastCheckedTailIndex];
                    }
                    for (var i = 0; i < reversedFilter.Length; i++)
                    {
                        reversedFilter[i] = filter[filter.Length - (i + 1) - lastCheckedTailIndex];
                    }

                    //Cut up the filter into seperate patterns, exclude * as they are not longer needed
                    for (var i = 0; i < reversedFilter.Length; i++)
                    {
                        if (reversedFilter[i] == '*')
                        {
                            if (i - currentPatternStartIndex > 0)
                            {
                                var pattern = new char[i - currentPatternStartIndex];
                                for (var j = 0; j < pattern.Length; j++)
                                {
                                    pattern[j] = reversedFilter[currentPatternStartIndex + j];
                                }
                                reversedPatterns.Add(pattern);
                            }
                            currentPatternStartIndex = i + 1;
                        }
                    }

                    //Search for the patterns
                    for (var i = 0; i < reversedPatterns.Count; i++)
                    {
                        for (var j = 0; j < reversedPatterns[i].Length; j++)
                        {
                            if ((reversedPatterns[i].Length - 1 - j) > (reversedWord.Length - 1 - reversedWordIndex))
                            {
                                return false;
                            }

                            if (reversedPatterns[i][j] != reversedWord[reversedWordIndex + j])
                            {
                                reversedWordIndex += 1;
                                j = -1;
                            }
                            else
                            {
                                if (j == reversedPatterns[i].Length - 1)
                                {
                                    reversedWordIndex = reversedWordIndex + reversedPatterns[i].Length;
                                }
                            }
                        }
                    }
                    break;

                case 3:
                    //Same as Case 2 except ? is considered a match
                    //Search Head for matches util first *
                    for (var i = 0; i < filter.Length; i++)
                    {
                        if (filter[i] != '*')
                        {
                            if (filter[i] != word[i] && filter[i] != '?')
                            {
                                return false;
                            }
                        }
                        else
                        {
                            lastCheckedHeadIndex = i;
                            break;
                        }
                    }
                    //Search Tail for matches until first *
                    for (var i = 0; i < filter.Length; i++)
                    {
                        if (filter[filter.Length - 1 - i] != '*')
                        {
                            if (filter[filter.Length - 1 - i] != word[word.Length - 1 - i] && filter[filter.Length - 1 - i] != '?')
                            {
                                return false;
                            }
                        }
                        else
                        {
                            lastCheckedTailIndex = i;
                            break;
                        }
                    }
                    // Reverse and trim word and filter
                    reversedWord = new char[word.Length - lastCheckedHeadIndex - lastCheckedTailIndex];
                    reversedFilter = new char[filter.Length - lastCheckedHeadIndex - lastCheckedTailIndex];

                    for (var i = 0; i < reversedWord.Length; i++)
                    {
                        reversedWord[i] = word[word.Length - (i + 1) - lastCheckedTailIndex];
                    }
                    for (var i = 0; i < reversedFilter.Length; i++)
                    {
                        reversedFilter[i] = filter[filter.Length - (i + 1) - lastCheckedTailIndex];
                    }

                    for (var i = 0; i < reversedFilter.Length; i++)
                    {
                        if (reversedFilter[i] == '*')
                        {
                            if (i - currentPatternStartIndex > 0)
                            {
                                var pattern = new char[i - currentPatternStartIndex];
                                for (var j = 0; j < pattern.Length; j++)
                                {
                                    pattern[j] = reversedFilter[currentPatternStartIndex + j];
                                }
                                reversedPatterns.Add(pattern);
                            }

                            currentPatternStartIndex = i + 1;
                        }
                    }
                    //Search for the patterns
                    for (var i = 0; i < reversedPatterns.Count; i++)
                    {
                        for (var j = 0; j < reversedPatterns[i].Length; j++)
                        {
                            if ((reversedPatterns[i].Length - 1 - j) > (reversedWord.Length - 1 - reversedWordIndex))
                            {
                                return false;
                            }

                            if (reversedPatterns[i][j] != '?' && reversedPatterns[i][j] != reversedWord[reversedWordIndex + j])
                            {
                                reversedWordIndex += 1;
                                j = -1;
                            }
                            else
                            {
                                if (j == reversedPatterns[i].Length - 1)
                                {
                                    reversedWordIndex = reversedWordIndex + reversedPatterns[i].Length;
                                }
                            }
                        }
                    }
                    break;
            }
            return isLike;
        }

        public static bool EqualsWildcard(this string text, string wildcardString, bool ignoreCase)
        {
            if (text == null) return wildcardString == null;

            // https://bitbucket.org/hasullivan/fast-wildcard-matching/src/7457d0dc1aee5ecd373f7c8a7785d5891b416201/FastWildcardMatching/WildcardMatch.cs?at=master&fileviewer=file-view-default

            if (ignoreCase == true)
            {
                return text.ToLower().EqualsWildcard(wildcardString.ToLower());
            }
            else
            {
                return text.EqualsWildcard(wildcardString);
            }
        }

        #endregion Equals

        #endregion string

        #region System.Type and Reflection


        public static bool IsNullable(this Type type, out Type underlyingType)
        {
            underlyingType = Nullable.GetUnderlyingType(type);
            return underlyingType != null;
        }

        public static bool IsNullable(this Type type) => Nullable.GetUnderlyingType(type) != null;

        /// <summary>Is this type a generic type</summary>
        /// <param name="type"></param>
        /// <returns>True if generic, otherwise False</returns>
        public static bool IsGeneric(this Type type) => type.IsGenericType && type.Name.Contains("`");//TODO: Figure out why IsGenericType isn't good enough and document (or remove) this condition

        public static Type AsNullable(this Type type)
        {
            // https://stackoverflow.com/a/23402284
            type.CheckNotNull(nameof(type));
            if (!type.IsValueType) return type;
            if (Nullable.GetUnderlyingType(type) != null) return type; // Already nullable
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) return type; // Already nullable
            return typeof(Nullable<>).MakeGenericType(type);
        }

        public static string FullNameFormatted(this Type type)
        {
            // https://stackoverflow.com/a/25287378
            if (type.IsGenericType)
            {
                return string.Format(
                    "{0}<{1}>",
                    type.FullName.Substring(0, type.FullName.LastIndexOf("`", StringComparison.InvariantCulture)),
                    string.Join(", ", type.GetGenericArguments().Select(FullNameFormatted)));
            }

            return type.FullName;
        }

        public static string NameFormatted(this Type type)
        {
            // https://stackoverflow.com/a/25287378
            if (type.IsGenericType)
            {
                return string.Format(
                    "{0}<{1}>",
                    type.Name.Substring(0, type.Name.LastIndexOf("`", StringComparison.InvariantCulture)),
                    string.Join(", ", type.GetGenericArguments().Select(NameFormatted)));
            }

            return type.Name;
        }

        public static IEnumerable<Type> GetTypesOf<T>(this Assembly assembly, bool allowAbstract = false, bool allowInterface = false, bool requireNoArgConstructor = false, bool namespaceSystem = false)
        {
            foreach (var t in assembly.GetTypes())
            {
                if (t.Namespace == null) continue;
                if (t.Namespace.StartsWith("System.", StringComparison.OrdinalIgnoreCase) && namespaceSystem == false) continue;
                if (t.IsInterface && allowInterface == false) continue;
                if (t.IsAbstract && t.IsInterface == false && allowAbstract == false) continue;
                if (requireNoArgConstructor == true && t.GetConstructor(Type.EmptyTypes) == null) continue;
                if (typeof(T).IsAssignableFrom(t) == false) continue;

                yield return t;
            }
        }

        public static string GetFileVersion(this Assembly assembly) => System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location)?.FileVersion;

        public static string GetVersion(this Assembly assembly) => assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version;

        #region EqualsAny

        public static bool Equals<T1>(this Type type) => typeof(T1).Equals(type);

        public static bool Equals<T1, T2>(this Type type) => typeof(T1).Equals(type) || typeof(T2).Equals(type);

        public static bool Equals<T1, T2, T3>(this Type type) => typeof(T1).Equals(type) || typeof(T2).Equals(type) || typeof(T3).Equals(type);

        public static bool Equals<T1, T2, T3, T4>(this Type type) => typeof(T1).Equals(type) || typeof(T2).Equals(type) || typeof(T3).Equals(type) || typeof(T4).Equals(type);

        public static bool Equals<T1, T2, T3, T4, T5>(this Type type) => typeof(T1).Equals(type) || typeof(T2).Equals(type) || typeof(T3).Equals(type) || typeof(T4).Equals(type) || typeof(T5).Equals(type);

        public static bool Equals<T1, T2, T3, T4, T5, T6>(this Type type) => typeof(T1).Equals(type) || typeof(T2).Equals(type) || typeof(T3).Equals(type) || typeof(T4).Equals(type) || typeof(T5).Equals(type) || typeof(T6).Equals(type);

        public static bool Equals<T1, T2, T3, T4, T5, T6, T7>(this Type type) => typeof(T1).Equals(type) || typeof(T2).Equals(type) || typeof(T3).Equals(type) || typeof(T4).Equals(type) || typeof(T5).Equals(type) || typeof(T6).Equals(type) || typeof(T7).Equals(type);

        public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8>(this Type type) => typeof(T1).Equals(type) || typeof(T2).Equals(type) || typeof(T3).Equals(type) || typeof(T4).Equals(type) || typeof(T5).Equals(type) || typeof(T6).Equals(type) || typeof(T7).Equals(type) || typeof(T8).Equals(type);

        public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Type type) => typeof(T1).Equals(type) || typeof(T2).Equals(type) || typeof(T3).Equals(type) || typeof(T4).Equals(type) || typeof(T5).Equals(type) || typeof(T6).Equals(type) || typeof(T7).Equals(type) || typeof(T8).Equals(type) || typeof(T9).Equals(type);

        public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this Type type) => typeof(T1).Equals(type) || typeof(T2).Equals(type) || typeof(T3).Equals(type) || typeof(T4).Equals(type) || typeof(T5).Equals(type) || typeof(T6).Equals(type) || typeof(T7).Equals(type) || typeof(T8).Equals(type) || typeof(T9).Equals(type) || typeof(T10).Equals(type);

        public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this Type type) => typeof(T1).Equals(type) || typeof(T2).Equals(type) || typeof(T3).Equals(type) || typeof(T4).Equals(type) || typeof(T5).Equals(type) || typeof(T6).Equals(type) || typeof(T7).Equals(type) || typeof(T8).Equals(type) || typeof(T9).Equals(type) || typeof(T10).Equals(type) || typeof(T11).Equals(type);

        public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this Type type) => typeof(T1).Equals(type) || typeof(T2).Equals(type) || typeof(T3).Equals(type) || typeof(T4).Equals(type) || typeof(T5).Equals(type) || typeof(T6).Equals(type) || typeof(T7).Equals(type) || typeof(T8).Equals(type) || typeof(T9).Equals(type) || typeof(T10).Equals(type) || typeof(T11).Equals(type) || typeof(T12).Equals(type);

        public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this Type type) => typeof(T1).Equals(type) || typeof(T2).Equals(type) || typeof(T3).Equals(type) || typeof(T4).Equals(type) || typeof(T5).Equals(type) || typeof(T6).Equals(type) || typeof(T7).Equals(type) || typeof(T8).Equals(type) || typeof(T9).Equals(type) || typeof(T10).Equals(type) || typeof(T11).Equals(type) || typeof(T12).Equals(type) || typeof(T13).Equals(type);

        public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this Type type) => typeof(T1).Equals(type) || typeof(T2).Equals(type) || typeof(T3).Equals(type) || typeof(T4).Equals(type) || typeof(T5).Equals(type) || typeof(T6).Equals(type) || typeof(T7).Equals(type) || typeof(T8).Equals(type) || typeof(T9).Equals(type) || typeof(T10).Equals(type) || typeof(T11).Equals(type) || typeof(T12).Equals(type) || typeof(T13).Equals(type) || typeof(T14).Equals(type);

        public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this Type type) => typeof(T1).Equals(type) || typeof(T2).Equals(type) || typeof(T3).Equals(type) || typeof(T4).Equals(type) || typeof(T5).Equals(type) || typeof(T6).Equals(type) || typeof(T7).Equals(type) || typeof(T8).Equals(type) || typeof(T9).Equals(type) || typeof(T10).Equals(type) || typeof(T11).Equals(type) || typeof(T12).Equals(type) || typeof(T13).Equals(type) || typeof(T14).Equals(type) || typeof(T15).Equals(type);

        public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this Type type) => typeof(T1).Equals(type) || typeof(T2).Equals(type) || typeof(T3).Equals(type) || typeof(T4).Equals(type) || typeof(T5).Equals(type) || typeof(T6).Equals(type) || typeof(T7).Equals(type) || typeof(T8).Equals(type) || typeof(T9).Equals(type) || typeof(T10).Equals(type) || typeof(T11).Equals(type) || typeof(T12).Equals(type) || typeof(T13).Equals(type) || typeof(T14).Equals(type) || typeof(T15).Equals(type) || typeof(T16).Equals(type);

        #endregion EqualsAny

        #endregion System.Type and Reflection

        #region File

        public static string[] RemoveBase(this FileSystemInfo info, DirectoryInfo baseToRemove, bool caseSensitive = false)
        {
            var sourceParts = info.FullName.Split('/', '\\', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Where(o => o.TrimOrNull() != null).ToArray();
            var baseParts = baseToRemove.FullName.Split('/', '\\', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Where(o => o.TrimOrNull() != null).ToArray();

            var msgInvalidParent = $"{nameof(baseToRemove)} of {baseToRemove.FullName} is not a parent directory of {info.FullName}";

            if (baseParts.Length > sourceParts.Length) throw new ArgumentException(msgInvalidParent, nameof(baseToRemove));

            var list = new List<string>();
            for (var i = 0; i < sourceParts.Length; i++)
            {
                if (i >= baseParts.Length)
                {
                    list.Add(sourceParts[i]);
                }
                else
                {
                    if (caseSensitive)
                    {
                        if (!string.Equals(sourceParts[i], baseParts[i]))
                        {
                            throw new ArgumentException(msgInvalidParent, nameof(baseToRemove));
                        }
                    }
                    else
                    {
                        if (!string.Equals(sourceParts[i], baseParts[i], StringComparison.OrdinalIgnoreCase))
                        {
                            throw new ArgumentException(msgInvalidParent, nameof(baseToRemove));
                        }
                    }
                }
            }

            return list.ToArray();
        }

        #endregion File

        #region System.Net

        public static Attachment AddAttachment(this MailMessage mailMessage, string fileName)
        {
            var attachment = new Attachment(fileName, MediaTypeNames.Application.Octet);

            var disposition = attachment.ContentDisposition;
            var fi = new FileInfo(fileName);
            disposition.CreationDate = fi.CreationTime;
            disposition.ModificationDate = fi.LastWriteTime;
            disposition.ReadDate = fi.LastAccessTime;
            disposition.FileName = fi.Name;
            disposition.Size = fi.Length;
            disposition.DispositionType = DispositionTypeNames.Attachment;
            mailMessage.Attachments.Add(attachment);

            return attachment;
        }

        public static uint ToUInt(this IPAddress ipAddress)
        {
            var ip = ipAddress.ToString().Split('.').Select(s => byte.Parse(s)).ToArray();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(ip);
            }
            var num = BitConverter.ToUInt32(ip, 0);
            return num;
        }

        public static long ToLong(this IPAddress ipaddress) => ToUInt(ipaddress);

        public static IPAddress ToIPAddress(this uint ipAddress)
        {
            var ipBytes = BitConverter.GetBytes(ipAddress);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(ipBytes);
            }
            var address = string.Join(".", ipBytes.Select(n => n.ToString()));
            return IPAddress.Parse(address);
        }

        public static IPAddress ToIPAddress(this long ip) => ToIPAddress((uint)ip);

        public static IEnumerable<IPAddress> Range(this IPAddress startAddressInclusive, IPAddress endAddressInclusive)
        {
            var ui1 = startAddressInclusive.ToUInt();
            var ui2 = endAddressInclusive.ToUInt();

            if (ui2 >= ui1)
            {
                for (var i = ui1; i <= ui2; i++)
                {
                    yield return i.ToIPAddress();
                }
            }
            else
            {
                for (var i = ui1; i >= ui2; i--)
                {
                    yield return i.ToIPAddress();
                }
            }
        }

        #endregion System.Net

        #region System.Data

        private static List<string[]> ToCsvSplit(DataTable dataTable)
        {
            var rows = new List<string[]>();

            var columns = dataTable.Columns.AsList().ToArray();
            var width = columns.Length;

            var columnStrings = new string[width];
            for (var i = 0; i < width; i++)
            {
                columnStrings[i] = columns[i].ColumnName;
            }
            rows.Add(columnStrings);

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var array = dataRow.ItemArray;
                var arrayWidth = array.Length;
                var arrayString = new string[arrayWidth];
                for (var i = 0; i < arrayWidth; i++)
                {
                    arrayString[i] = array[i].ToStringGuessFormat();
                }
                rows.Add(arrayString);
            }

            return rows;
        }

        public static void DisposeSafely(this IDbConnection connection, Action<string, Exception> errorLog = null)
        {
            if (connection == null) return;
            try
            {
                if (connection.State != ConnectionState.Closed) connection.Close();
            }
            catch (Exception e)
            {
                errorLog?.Invoke("Error closing database connection [" + connection.GetType().FullNameFormatted() + "]", e);
            }

            try
            {
                connection.Dispose();
            }
            catch (Exception e)
            {
                errorLog?.Invoke("Error disposing database connection [" + connection.GetType().FullNameFormatted() + "]", e);
            }
        }

        public static IDataParameter SetParameterValue(this IDbCommand command, int parameterIndex, object value)
        {
            var p = (IDbDataParameter)command.Parameters[parameterIndex];
            if (value == null) p.Value = DBNull.Value;
            else p.Value = value;
            return p;
        }

        public static IDataParameter AddParameter(this IDbCommand command,
            DbType? dbType = null,
            ParameterDirection? direction = null,
            string parameterName = null,
            byte? precision = null,
            byte? scale = null,
            int? size = null,
            string sourceColumn = null,
            DataRowVersion? sourceVersion = null,
            object value = null)
        {
            var p = command.CreateParameter();
            if (dbType.HasValue) p.DbType = dbType.Value;
            if (direction.HasValue) p.Direction = direction.Value;
            if (parameterName != null) p.ParameterName = parameterName;
            if (precision.HasValue) p.Precision = precision.Value;
            if (scale.HasValue) p.Scale = scale.Value;
            if (size.HasValue) p.Size = size.Value;
            if (sourceColumn != null) p.SourceColumn = sourceColumn;
            if (sourceVersion.HasValue) p.SourceVersion = sourceVersion.Value;
            p.Value = value ?? DBNull.Value;
            command.Parameters.Add(p);
            return p;
        }

        public static List<string> GetNames(this IDataReader dataReader)
        {
            var list = new List<string>();
            var count = dataReader.FieldCount;
            for (var i = 0; i < count; i++)
            {
                list.Add(dataReader.GetName(i));
            }

            return list;
        }

        public static object[] GetValues(this IDataReader dataReader) => GetValues(dataReader, dataReader.FieldCount);

        public static object[] GetValues(this IDataReader dataReader, int fieldCount)
        {
            var objs = new object[fieldCount];
            var instances = dataReader.GetValues(objs);
            return objs;
        }

        public static List<object[]> GetValuesAll(this IDataReader dataReader)
        {
            var fieldCount = dataReader.FieldCount;
            var list = new List<object[]>();
            while (dataReader.Read())
            {
                list.Add(GetValues(dataReader, fieldCount));
            }
            return list;
        }

        public static List<DataColumn> AsList(this DataColumnCollection dataColumnCollection)
        {
            var list = new List<DataColumn>();
            if (dataColumnCollection == null) return list;
            list.Capacity = dataColumnCollection.Count + 1;
            foreach (DataColumn c in dataColumnCollection)
            {
                list.Add(c);
            }
            return list;
        }

        public static List<object[]> AsList(this DataRowCollection dataRowCollection)
        {
            var list = new List<object[]>();
            if (dataRowCollection == null) return list;
            list.Capacity = dataRowCollection.Count + 1;
            foreach (DataRow row in dataRowCollection)
            {
                list.Add(row.ItemArray.Copy());
            }
            return list;
        }

        public static IDbCommand CreateCommand(this IDbConnection connection, string commandText, CommandType? commandType = null, int? commandTimeout = null)
        {
            var command = connection.CreateCommand();
            if (commandText != null) command.CommandText = commandText;
            if (commandType != null) command.CommandType = commandType.Value;
            if (commandTimeout != null) command.CommandTimeout = commandTimeout.Value;
            return command;
        }

        public static T GetValueOrDefault<T>(this IDataRecord row, string fieldName)
        {
            // http://stackoverflow.com/a/2610220
            var ordinal = row.GetOrdinal(fieldName);
            return row.GetValueOrDefault<T>(ordinal);
        }

        public static T GetValueOrDefault<T>(this IDataRecord row, int ordinal) =>
            // http://stackoverflow.com/a/2610220
            (T)(row.IsDBNull(ordinal) ? default(T) : row.GetValue(ordinal));

        public static string GetStringNullable(this IDataReader reader, int i) => reader.IsDBNull(i) ? null : reader.GetString(i);

        public static string ToCsv(this DataTable dataTable, string delimiter, string escape)
        {
            var list = ToCsvSplit(dataTable);

            var sb = new StringBuilder();

            foreach (var array in list)
            {
                for (var i = 0; i < array.Length; i++)
                {
                    if (i > 0) sb.Append(delimiter);

                    sb.Append(escape);
                    var cell = array[i];
                    if (cell != null && cell.IndexOf("\"") >= 0)
                    {
                        cell = cell.Replace("\"", "\"\"");
                    }
                    sb.Append(cell);
                    sb.Append(escape);
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        #endregion System.Data

        #region System.Security.Cryptography.RandomNumberGenerator

        public static byte[] GetBytes(this RandomNumberGenerator random, int length)
        {
            var key = new byte[length];
            random.GetBytes(key);
            return key;
        }

        public static int Next(this RandomNumberGenerator random, int fromInclusive, int toExclusive)
        {
            if (fromInclusive >= toExclusive) throw new ArgumentException($"Invalid range {nameof(fromInclusive)}:{fromInclusive} {nameof(toExclusive)}:{toExclusive}", nameof(fromInclusive));

            // The total possible range is [0, 4,294,967,295). Subtract one to account for zero
            // being an actual possibility.
            var range = (uint)toExclusive - (uint)fromInclusive - 1;

            // If there is only one possible choice, nothing random will actually happen, so return
            // the only possibility.
            if (range == 0) return fromInclusive;

            // Create a mask for the bits that we care about for the range. The other bits will be
            // masked away.
            var mask = range;
            mask |= mask >> 1;
            mask |= mask >> 2;
            mask |= mask >> 4;
            mask |= mask >> 8;
            mask |= mask >> 16;

            var resultSpan = new byte[4];
            uint result;

            do
            {
                random.GetBytes(resultSpan);
                result = mask & BitConverter.ToUInt32(resultSpan, 0);
            }
            while (result > range);

            return (int)result + fromInclusive;
        }

        public static int Next(this RandomNumberGenerator random) => Next(random, 0, int.MaxValue);

        public static int Next(this RandomNumberGenerator random, int maxValueExclusive) => Next(random, 0, maxValueExclusive);

        public static Guid NextGuid(this RandomNumberGenerator random) => new Guid(random.GetBytes(16));

        public static T Pick<T>(this RandomNumberGenerator random, IList<T> items) => items[random.Next(items.Count)];

        public static T Pick<T>(this RandomNumberGenerator random, params T[] items) => items[random.Next(items.Length)];

        public static void Shuffle<T>(this RandomNumberGenerator random, T[] items)
        {
            // Note i > 0 to avoid final pointless iteration
            for (var i = items.Length - 1; i > 0; i--)
            {
                // Swap element "i" with a random earlier element it (or itself)
                var swapIndex = random.Next(i + 1);
                var tmp = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = tmp;
            }
        }

        public static void Shuffle<T>(this RandomNumberGenerator random, IList<T> items)
        {
            // Note i > 0 to avoid final pointless iteration
            for (var i = items.Count - 1; i > 0; i--)
            {
                // Swap element "i" with a random earlier element it (or itself)
                var swapIndex = random.Next(i + 1);
                var tmp = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = tmp;
            }
        }

        #endregion System.Security.Cryptography.RandomNumberGenerator

        #region System.Random

        public static T Pick<T>(this Random random, IList<T> items) => items[random.Next(items.Count)];

        public static T Pick<T>(this Random random, params T[] items) => items[random.Next(items.Length)];

        public static void Shuffle<T>(this Random random, T[] items)
        {
            // Note i > 0 to avoid final pointless iteration
            for (var i = items.Length - 1; i > 0; i--)
            {
                // Swap element "i" with a random earlier element it (or itself)
                var swapIndex = random.Next(i + 1);
                var tmp = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = tmp;
            }
        }

        public static void Shuffle<T>(this Random random, IList<T> items)
        {
            // Note i > 0 to avoid final pointless iteration
            for (var i = items.Count - 1; i > 0; i--)
            {
                // Swap element "i" with a random earlier element it (or itself)
                var swapIndex = random.Next(i + 1);
                var tmp = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = tmp;
            }
        }

        public static T[] Shuffle<T>(this Random random, T item1, T item2, params T[] items)
        {
            if (items == null) items = Array.Empty<T>();
            var array = new T[1 + 1 + (items.Length)];
            array[0] = item1;
            array[1] = item2;
            for (var i = 0; i < items.Length; i++)
            {
                array[i + 2] = items[i];
            }
            Shuffle(random, array);
            return array;
        }

        #endregion System.Random

        #region Collections

        #region Multidimensional Arrays

        public static List<T[]> ToList<T>(this T[,] array, int listPadding = 0)
        {
            // TODO: I'm sure there are tons of performance tweaks for this such as using Array.Copy somehow.
            var lenRows = array.CheckNotNull(nameof(array)).GetLength(0);
            var lenColumns = array.GetLength(1);
            listPadding.CheckNotNegative(nameof(listPadding));

            var list = new List<T[]>(lenRows + listPadding);

            for (var i = 0; i < lenRows; i++)
            {
                var newRow = new T[lenColumns];
                for (var j = 0; j < lenColumns; j++)
                {
                    newRow[j] = array[i, j];
                }
                list.Add(newRow);
            }

            return list;
        }

        public static T[,] InsertRow<T>(this T[,] array, int index, T[] values)
        {
            var lenRows = array.CheckNotNull(nameof(array)).GetLength(0);
            var lenColumns = array.GetLength(1);

            if (values.Length != lenColumns) throw new ArgumentException(nameof(values), "Values of length " + values.Length + " does not match row size of " + lenColumns + ".");

            var list = array.ToList(1);
            list.Insert(index, values);

            lenRows = list.Count;
            var newArray = new T[lenRows, lenColumns];

            for (var i = 0; i < lenRows; i++)
            {
                var rr = list[i];
                for (var j = 0; j < lenColumns; j++)
                {
                    newArray[i, j] = rr[j];
                }
            }
            return newArray;
        }

        public static T[] GetRow<T>(this T[,] array, int index)
        {
            var lenRows = array.CheckNotNull(nameof(array)).GetLength(0);

            var lenColumns = array.GetLength(1);

            if ((uint)index >= (uint)lenRows) throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range. Must be non-negative and less than the size of the array rows.");

            if (typeof(T).IsPrimitive) // https://stackoverflow.com/a/27440355
            {
                var cols = array.GetUpperBound(1) + 1;
                var result = new T[cols];
                int size;
                if (typeof(T) == typeof(bool)) size = 1;
                else if (typeof(T) == typeof(char)) size = 2;
                else size = System.Runtime.InteropServices.Marshal.SizeOf<T>();
                Buffer.BlockCopy(array, index * cols * size, result, 0, cols * size);
                return result;
            }

            var newArray = new T[lenColumns];
            for (var i = 0; i < lenColumns; i++)
            {
                newArray[i] = array[index, i];
            }
            return newArray;
        }

        public static void SetRow<T>(this T[,] array, int index, T[] values)
        {
            values.CheckNotNull(nameof(values));
            var lenRows = array.CheckNotNull(nameof(array)).GetLength(0);

            var lenColumns = array.GetLength(1);

            if ((uint)index >= (uint)lenRows) throw new ArgumentOutOfRangeException(nameof(index), "Index " + index + " was out of range. Must be non-negative and less than the size of the array rows " + lenRows + ".");
            if (values.Length != lenColumns) throw new ArgumentException(nameof(values), "Values of length " + values.Length + " does not match row size of " + lenColumns + ".");

            for (var i = 0; i < lenColumns; i++)
            {
                array[index, i] = values[i];
            }
        }

        public static T[] GetColumn<T>(this T[,] array, int index)
        {
            var lenRows = array.CheckNotNull(nameof(array)).GetLength(0);

            var lenColumns = array.GetLength(1);

            if ((uint)index >= (uint)lenColumns) throw new ArgumentOutOfRangeException(nameof(index), "Index " + index + " was out of range. Must be non-negative and less than the size of the array columns " + lenColumns + ".");

            var newArray = new T[lenRows];
            for (var i = 0; i < lenRows; i++)
            {
                newArray[i] = array[index, i];
            }
            return newArray;
        }

        public static void SetColumn<T>(this T[,] array, int index, T[] values)
        {
            values.CheckNotNull(nameof(values));
            var lenRows = array.CheckNotNull(nameof(array)).GetLength(0);

            var lenColumns = array.GetLength(1);

            if ((uint)index >= (uint)lenColumns) throw new ArgumentOutOfRangeException(nameof(index), "Index " + index + " was out of range. Must be non-negative and less than the size of the array columns " + lenColumns + ".");
            if (values.Length != lenRows) throw new ArgumentException(nameof(values), "Values of length " + values.Length + " does not match column size of " + lenRows + ".");

            for (var i = 0; i < lenRows; i++)
            {
                array[i, index] = values[i];
            }
        }

        #endregion Multidimensional Arrays

        /// <summary>
        /// Wraps this object instance into an IEnumerable&lt;T&gt; consisting of a single item. https://stackoverflow.com/q/1577822
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="item">The instance that will be wrapped.</param>
        /// <returns>An IEnumerable&lt;T&gt; consisting of a single item.</returns>
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        /// <summary>
        /// Concatenates an item to the end of a sequence
        /// </summary>
        /// <typeparam name="T">The type of the elements of the input sequence</typeparam>
        /// <param name="enumerable">The sequence to add an item to</param>
        /// <param name="item">The item to append to the end of the sequence</param>
        /// <returns>An System.Collections.Generic.IEnumerable`1 that contains the concatenated elements of the enumerable with the item appended to the end</returns>
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> enumerable, T item) => enumerable.Concat(item.Yield());

        public static T[][] SplitIntoParts<T>(this T[] array, int numberOfParts)
        {
            if (numberOfParts < 1) numberOfParts = 1;
            var arraySize = array.Length;
            var div = arraySize / numberOfParts;
            var remainder = arraySize % numberOfParts;

            var partSizes = new int[numberOfParts];
            for (var i = 0; i < numberOfParts; i++) partSizes[i] = div + (i < remainder ? 1 : 0);

            var newArray = new T[numberOfParts][];
            var counter = 0;
            for (var i = 0; i < numberOfParts; i++)
            {
                var partSize = partSizes[i];
                var newArrayItem = new T[partSize];
                Array.Copy(array, counter, newArrayItem, 0, partSize);
                counter += partSize;
                newArray[i] = newArrayItem;
            }
            return newArray;
        }

        public static T[][] SplitIntoPartSizes<T>(this T[] array, int partSize) => SplitIntoParts(array, array.Length / partSize);

        public static bool IsEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable is IReadOnlyCollection<T>) return ((IReadOnlyCollection<T>)enumerable).Count < 1;
            if (enumerable is ICollection<T>) return ((ICollection<T>)enumerable).Count < 1;

            return !enumerable.Any();
        }

        public static T[] RemoveAt<T>(this T[] array, int index)
        {
            var len = array.CheckNotNull(nameof(array)).Length;
            if ((uint)index >= (uint)len)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range. Must be non-negative and less than the size of the collection.");
            }

            var newArray = new T[len - 1];
            var j = 0;
            for (var i = 0; i < len; i++)
            {
                if (i != index)
                {
                    newArray[j] = array[i];
                    j++;
                }
            }

            return newArray;
        }

        public static T[] RemoveHead<T>(this T[] array) => RemoveAt(array, 0);

        public static T[] RemoveHead<T>(this T[] array, int itemsToRemove)
        {
            for (var i = 0; i < itemsToRemove; i++) array = array.RemoveHead();
            return array;
        }

        public static T[] RemoveTail<T>(this T[] array) => RemoveAt(array, array.Length - 1);

        public static T[] RemoveTail<T>(this T[] array, int itemsToRemove)
        {
            for (var i = 0; i < itemsToRemove; i++) array = array.RemoveTail();
            return array;
        }

        public static bool EqualsAt<T>(this T[] array1, T[] array2, int index) => EqualsAt(array1, array2, index, null);

        public static bool EqualsAt<T>(this T[] array1, T[] array2, int index, IEqualityComparer<T> comparer)
        {
            if (array1 == null) return false;
            if (array2 == null) return false;

            if (index >= array1.Length) return false;
            if (index >= array2.Length) return false;

            var item1 = array1[index];
            var item2 = array2[index];

            if (EqualityComparer<T>.Default.Equals(item1, default) && EqualityComparer<T>.Default.Equals(item2, default)) return true;
            if (EqualityComparer<T>.Default.Equals(item1, default)) return false;
            if (EqualityComparer<T>.Default.Equals(item2, default)) return false;

            if (comparer == null) comparer = EqualityComparer<T>.Default;
            return comparer.Equals(item1, item2);
        }

        public static bool EqualsAt<T>(this T[] array, int index, T item) => EqualsAt(array, index, null, item);

        public static bool EqualsAt<T>(this T[] array, int index, IEqualityComparer<T> comparer, T item)
        {
            if (array == null) return false;

            if (index >= array.Length) return false;

            var o = array[index];

            if (EqualityComparer<T>.Default.Equals(o, default) && EqualityComparer<T>.Default.Equals(item, default)) return true;
            if (EqualityComparer<T>.Default.Equals(o, default)) return false;
            if (EqualityComparer<T>.Default.Equals(item, default)) return false;

            if (comparer == null) comparer = EqualityComparer<T>.Default;
            return comparer.Equals(o, item);
        }

        public static bool EqualsAtAny<T>(this T[] array, int index, params T[] items) => EqualsAtAny(array, index, null, items);

        public static bool EqualsAtAny<T>(this T[] array, int index, IEqualityComparer<T> comparer, params T[] items)
        {
            if (array == null) return false;
            foreach (var item in (items ?? Array.Empty<T>()))
            {
                if (EqualsAt(array, index, comparer, item)) return true;
            }

            return false;
        }

        public static T[] Copy<T>(this T[] array)
        {
            if (array == null) return null;

            var len = array.Length;
            var arrayNew = new T[len];

            Array.Copy(array, arrayNew, len);

            return arrayNew;
        }

        public static T[] Append<T>(this T[] array, T[] otherArray)
        {
            T[] result = new T[array.Length + otherArray.Length];

            Buffer.BlockCopy(array, 0, result, 0, array.Length);
            Buffer.BlockCopy(otherArray, 0, result, array.Length, otherArray.Length);

            return result;
        }

        public static (T[], T[]) Split<T>(this T[] array, int index)
        {
            var array1 = new T[index];
            var array2 = new T[array.Length - index];

            Buffer.BlockCopy(array, 0, array1, 0, array1.Length);
            Buffer.BlockCopy(array, index, array2, 0, array2.Length);

            return (array1, array2);
        }

        public static List<T> ToListReversed<T>(this IEnumerable<T> enumerable)
        {
            var l = enumerable.ToList();
            l.Reverse();
            return l;
        }

        /// <summary>
        /// Casts each item to type T
        /// </summary>
        /// <typeparam name="T">The type to cast to</typeparam>
        /// <param name="enumerable">The enumerable to cast</param>
        /// <returns>A list containing all of the casted elements of the enumerable</returns>
        public static List<T> ToList<T>(this CollectionBase enumerable)
        {
            var l = new List<T>();
            foreach (var o in enumerable)
            {
                var t = (T)o;
                l.Add(t);
            }
            return l;

        }

        /// <summary>
        /// Processes an action on each element in an enumerable
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="value">The enumerable to process</param>
        /// <param name="action">The action to execute on each item in enumerable</param>
        public static void ForEach<T>(this IEnumerable<T> value, Action<T> action)
        {
            foreach (var item in value)
            {
                action(item);
            }
        }

        /// <summary>
        /// If the array is null then returns an empty array
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="array">The array to check for null</param>
        /// <returns>The same array or if null an empty array</returns>
        public static T[] OrEmpty<T>(this T[] array) => array ?? Array.Empty<T>();

        /// <summary>
        /// If the enumerable is null then returns an empty enumerable
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="enumerable">The enumerable to check for null</param>
        /// <returns>The same enumerable or if null an empty enumerable</returns>
        public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T> enumerable) => enumerable ?? Enumerable.Empty<T>();

        /// <summary>
        /// If the string is null then returns an empty string
        /// </summary>
        /// <param name="str">The string to check for null</param>
        /// <returns>The same string or an empty string if null</returns>
        public static string OrEmpty(this string str) => str ?? string.Empty;

        public static T DequeueOrDefault<T>(this Queue<T> queue) => queue.Count < 1 ? default : queue.Dequeue();

        /// <summary>
        /// Resizes an array and returns a new array
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="array">The array to resize</param>
        /// <param name="newLength">The length of the new array</param>
        /// <returns>A new array of length newLength</returns>
        public static T[] Resize<T>(this T[] array, int newLength)
        {
            if (array == null) return null;

            var newArray = new T[newLength];

            var width = Math.Min(array.Length, newLength);
            for (var i = 0; i < width; i++)
            {
                newArray[i] = array[i];
            }

            return newArray;
        }
        public static void ResizeAll<T>(this IList<T[]> list, int newLength)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null) list[i] = list[i].Resize(newLength);
            }
        }
        public static void ResizeAll<T>(this T[][] list, int newLength)
        {
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] != null) list[i] = list[i].Resize(newLength);
            }
        }

        /// <summary>
        /// Determines which array is longest and returns that array's length
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="enumerable">The enumerable to search</param>
        /// <returns>The size of the longest array</returns>
        public static int MaxLength<T>(this IEnumerable<T[]> enumerable)
        {
            int len = 0;
            foreach (var item in enumerable)
            {
                if (item != null) len = Math.Max(len, item.Length);
            }
            return len;
        }

        /// <summary>
        /// Determines which string is longest and returns that string's length
        /// </summary>
        /// <param name="enumerable">The enumerable to search</param>
        /// <returns>The size of the longest string</returns>
        public static int MaxLength(this IEnumerable<string> enumerable)
        {
            int len = 0;
            foreach (var item in enumerable)
            {
                if (item != null) len = Math.Max(len, item.Length);
            }
            return len;
        }

        /// <summary>
        /// Determines which collection is longest and returns that collection's length
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <typeparam name="TCollection">The type of collection</typeparam>
        /// <param name="enumerable">The enumerable to search</param>
        /// <returns>The size of the longest collection</returns>
        public static int MaxLength<T, TCollection>(this IEnumerable<TCollection> enumerable) where TCollection : ICollection<T>
        {
            int len = 0;
            foreach (var item in enumerable)
            {
                if (item != null) len = Math.Max(len, item.Count);
            }
            return len;
        }


        public static void Populate<T>(this T[] array, T value)
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }

        public static List<object> ToList(this CollectionBase collection)
        {
            var list = new List<object>();
            foreach (var o in collection) list.Add(o);
            return list;
        }

        public static SortedSet<T> ToSortedSet<T>(this IEnumerable<T> enumerable) => new SortedSet<T>(enumerable);

        public static SortedSet<T> ToSortedSet<T>(this IEnumerable<T> enumerable, IComparer<T> comparer) => new SortedSet<T>(enumerable, comparer);

        public static void AddMany<T>(this System.Collections.Concurrent.BlockingCollection<T> collection, IEnumerable<T> itemsToAdd)
        {
            foreach (var item in itemsToAdd) collection.Add(item);
        }

        public static void AddManyComplete<T>(this System.Collections.Concurrent.BlockingCollection<T> collection, IEnumerable<T> itemsToAdd)
        {
            AddMany(collection, itemsToAdd);
            collection.CompleteAdding();
        }

        public static int GetNumberOfCharacters(this string[] array, int lengthOfNull = 0)
        {
            if (array == null) return 0;
            int size = 0;
            for (int i = 0; i < array.Length; i++)
            {
                var s = array[i];
                if (s == null) size = size + lengthOfNull;
                else size = size + s.Length;
            }
            return size;
        }

        public static IEnumerable<string> ToStringsColumns(this IEnumerable<string> enumerable, int numberOfColumns, string paddingBetweenColumns = "   ", bool rightAlign = false)
        {
            var items = enumerable.ToArray();
            var width = items.MaxLength();

            var itemParts = items.SplitIntoPartSizes(numberOfColumns);
            var lines = new List<string>();
            foreach (var item in itemParts)
            {
                var list = new List<string>();
                for (int i = 0; i < numberOfColumns; i++)
                {
                    var part = item.GetAtIndexOrDefault(i) ?? string.Empty;
                    part = rightAlign ? part.PadLeft(width) : part.PadRight(width);
                    list.Add(part);
                }
                lines.Add(list.ToStringDelimited(paddingBetweenColumns));
            }
            return lines;
        }

        public static Dictionary<TKey, TValue> Copy<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            var d = new Dictionary<TKey, TValue>(dictionary.Count, dictionary.Comparer);
            foreach (var kvp in dictionary)
            {
                d.Add(kvp.Key, kvp.Value);
            }
            return d;
        }

        public static T[] Add<T>(this T[] array, params T[] itemsToAdd) => array.Concat(itemsToAdd).ToArray();

        #region In

        public static bool In<T>(this T value, T possibleValue1) => In(value, EqualityComparer<T>.Default, possibleValue1);
        public static bool In<T>(this T value, T possibleValue1, T possibleValue2) => In(value, EqualityComparer<T>.Default, possibleValue1, possibleValue2);
        public static bool In<T>(this T value, T possibleValue1, T possibleValue2, T possibleValue3) => In(value, EqualityComparer<T>.Default, possibleValue1, possibleValue2, possibleValue3);
        public static bool In<T>(this T value, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4) => In(value, EqualityComparer<T>.Default, possibleValue1, possibleValue2, possibleValue3, possibleValue4);
        public static bool In<T>(this T value, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4, T possibleValue5) => In(value, EqualityComparer<T>.Default, possibleValue1, possibleValue2, possibleValue3, possibleValue4, possibleValue5);
        public static bool In<T>(this T value, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4, T possibleValue5, T possibleValue6) => In(value, EqualityComparer<T>.Default, possibleValue1, possibleValue2, possibleValue3, possibleValue4, possibleValue5, possibleValue6);
        public static bool In<T>(this T value, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4, T possibleValue5, T possibleValue6, T possibleValue7) => In(value, EqualityComparer<T>.Default, possibleValue1, possibleValue2, possibleValue3, possibleValue4, possibleValue5, possibleValue6, possibleValue7);
        public static bool In<T>(this T value, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4, T possibleValue5, T possibleValue6, T possibleValue7, T possibleValue8) => In(value, EqualityComparer<T>.Default, possibleValue1, possibleValue2, possibleValue3, possibleValue4, possibleValue5, possibleValue6, possibleValue7, possibleValue8);
        public static bool In<T>(this T value, IEnumerable<T> possibleValues) => In<T>(value, EqualityComparer<T>.Default, possibleValues);
        public static bool In<T>(this T value, IEqualityComparer<T> equalityComparer, T possibleValue1) => equalityComparer.Equals(value, possibleValue1);
        public static bool In<T>(this T value, IEqualityComparer<T> equalityComparer, T possibleValue1, T possibleValue2) => equalityComparer.Equals(value, possibleValue1) || equalityComparer.Equals(value, possibleValue2);
        public static bool In<T>(this T value, IEqualityComparer<T> equalityComparer, T possibleValue1, T possibleValue2, T possibleValue3) => equalityComparer.Equals(value, possibleValue1) || equalityComparer.Equals(value, possibleValue2) || equalityComparer.Equals(value, possibleValue3);
        public static bool In<T>(this T value, IEqualityComparer<T> equalityComparer, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4) => equalityComparer.Equals(value, possibleValue1) || equalityComparer.Equals(value, possibleValue2) || equalityComparer.Equals(value, possibleValue3) || equalityComparer.Equals(value, possibleValue4);
        public static bool In<T>(this T value, IEqualityComparer<T> equalityComparer, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4, T possibleValue5) => equalityComparer.Equals(value, possibleValue1) || equalityComparer.Equals(value, possibleValue2) || equalityComparer.Equals(value, possibleValue3) || equalityComparer.Equals(value, possibleValue4) || equalityComparer.Equals(value, possibleValue5);
        public static bool In<T>(this T value, IEqualityComparer<T> equalityComparer, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4, T possibleValue5, T possibleValue6) => equalityComparer.Equals(value, possibleValue1) || equalityComparer.Equals(value, possibleValue2) || equalityComparer.Equals(value, possibleValue3) || equalityComparer.Equals(value, possibleValue4) || equalityComparer.Equals(value, possibleValue5) || equalityComparer.Equals(value, possibleValue6);
        public static bool In<T>(this T value, IEqualityComparer<T> equalityComparer, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4, T possibleValue5, T possibleValue6, T possibleValue7) => equalityComparer.Equals(value, possibleValue1) || equalityComparer.Equals(value, possibleValue2) || equalityComparer.Equals(value, possibleValue3) || equalityComparer.Equals(value, possibleValue4) || equalityComparer.Equals(value, possibleValue5) || equalityComparer.Equals(value, possibleValue6) || equalityComparer.Equals(value, possibleValue7);
        public static bool In<T>(this T value, IEqualityComparer<T> equalityComparer, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4, T possibleValue5, T possibleValue6, T possibleValue7, T possibleValue8) => equalityComparer.Equals(value, possibleValue1) || equalityComparer.Equals(value, possibleValue2) || equalityComparer.Equals(value, possibleValue3) || equalityComparer.Equals(value, possibleValue4) || equalityComparer.Equals(value, possibleValue5) || equalityComparer.Equals(value, possibleValue6) || equalityComparer.Equals(value, possibleValue7) || equalityComparer.Equals(value, possibleValue8);
        public static bool In<T>(this T value, IEqualityComparer<T> comparer, IEnumerable<T> possibleValues) => possibleValues.Any(o => comparer.Equals(o, value));

        #endregion In

        #region NotIn

        public static bool NotIn<T>(this T value, T possibleValue1) => !In(value, possibleValue1);
        public static bool NotIn<T>(this T value, T possibleValue1, T possibleValue2) => !In(value, possibleValue1, possibleValue2);
        public static bool NotIn<T>(this T value, T possibleValue1, T possibleValue2, T possibleValue3) => !In(value, possibleValue1, possibleValue2, possibleValue3);
        public static bool NotIn<T>(this T value, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4) => !In(value, possibleValue1, possibleValue2, possibleValue3, possibleValue4);
        public static bool NotIn<T>(this T value, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4, T possibleValue5) => !In(value, possibleValue1, possibleValue2, possibleValue3, possibleValue4, possibleValue5);
        public static bool NotIn<T>(this T value, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4, T possibleValue5, T possibleValue6) => !In(value, possibleValue1, possibleValue2, possibleValue3, possibleValue4, possibleValue5, possibleValue6);
        public static bool NotIn<T>(this T value, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4, T possibleValue5, T possibleValue6, T possibleValue7) => !In(value, possibleValue1, possibleValue2, possibleValue3, possibleValue4, possibleValue5, possibleValue6, possibleValue7);
        public static bool NotIn<T>(this T value, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4, T possibleValue5, T possibleValue6, T possibleValue7, T possibleValue8) => !In(value, possibleValue1, possibleValue2, possibleValue3, possibleValue4, possibleValue5, possibleValue6, possibleValue7, possibleValue8);
        public static bool NotIn<T>(this T value, IEnumerable<T> possibleValues) => !In(value, possibleValues);
        public static bool NotIn<T>(this T value, IEqualityComparer<T> equalityComparer, T possibleValue1) => !In(value, equalityComparer, possibleValue1);
        public static bool NotIn<T>(this T value, IEqualityComparer<T> equalityComparer, T possibleValue1, T possibleValue2) => !In(value, equalityComparer, possibleValue1, possibleValue2);
        public static bool NotIn<T>(this T value, IEqualityComparer<T> equalityComparer, T possibleValue1, T possibleValue2, T possibleValue3) => !In(value, equalityComparer, possibleValue1, possibleValue2, possibleValue3);
        public static bool NotIn<T>(this T value, IEqualityComparer<T> equalityComparer, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4) => !In(value, equalityComparer, possibleValue1, possibleValue2, possibleValue3, possibleValue4);
        public static bool NotIn<T>(this T value, IEqualityComparer<T> equalityComparer, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4, T possibleValue5) => !In(value, equalityComparer, possibleValue1, possibleValue2, possibleValue3, possibleValue4, possibleValue5);
        public static bool NotIn<T>(this T value, IEqualityComparer<T> equalityComparer, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4, T possibleValue5, T possibleValue6) => !In(value, equalityComparer, possibleValue1, possibleValue2, possibleValue3, possibleValue4, possibleValue5, possibleValue6);
        public static bool NotIn<T>(this T value, IEqualityComparer<T> equalityComparer, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4, T possibleValue5, T possibleValue6, T possibleValue7) => !In(value, equalityComparer, possibleValue1, possibleValue2, possibleValue3, possibleValue4, possibleValue5, possibleValue6, possibleValue7);
        public static bool NotIn<T>(this T value, IEqualityComparer<T> equalityComparer, T possibleValue1, T possibleValue2, T possibleValue3, T possibleValue4, T possibleValue5, T possibleValue6, T possibleValue7, T possibleValue8) => !In(value, equalityComparer, possibleValue1, possibleValue2, possibleValue3, possibleValue4, possibleValue5, possibleValue6, possibleValue7, possibleValue8);
        public static bool NotIn<T>(this T value, IEqualityComparer<T> comparer, IEnumerable<T> possibleValues) => !In(value, comparer, possibleValues);

        #endregion NotIn

        #region GetAtIndexOrDefault

        public static T GetAtIndexOrDefault<T>(this T[] array, int index, T defaultValue)
        {
            array.CheckNotNull(nameof(array));

            if (index < 0) return defaultValue;
            if (index >= array.Length) return defaultValue;

            return array[index];
        }

        public static T GetAtIndexOrDefault<T>(this T[] array, int index) => GetAtIndexOrDefault(array, index, default(T));

        public static T GetAtIndexOrDefault<T>(this IList<T> list, int index, T defaultValue)
        {
            list.CheckNotNull(nameof(list));

            if (index < 0) return defaultValue;
            if (index >= list.Count) return defaultValue;

            return list[index];
        }

        public static T GetAtIndexOrDefault<T>(this IList<T> list, int index) => GetAtIndexOrDefault(list, index, default(T));

        public static T GetAtIndexOrDefault<T>(this ICollection<T> collection, int index, T defaultValue)
        {
            collection.CheckNotNull(nameof(collection));

            if (index < 0) return defaultValue;
            if (index >= collection.Count) return defaultValue;

            var i = 0;
            foreach (var item in collection)
            {
                if (i == index) return item;
                i++;
            }

            return defaultValue;
        }

        public static T GetAtIndexOrDefault<T>(this ICollection<T> collection, int index) => GetAtIndexOrDefault(collection, index, default(T));

        public static T GetAtIndexOrDefault<T>(this IEnumerable<T> enumerable, int index, T defaultValue)
        {
            enumerable.CheckNotNull(nameof(enumerable));

            if (index < 0) return defaultValue;

            var i = 0;
            foreach (var item in enumerable)
            {
                if (i == index) return item;
                i++;
            }

            return defaultValue;
        }

        public static T GetAtIndexOrDefault<T>(this IEnumerable<T> enumerable, int index) => GetAtIndexOrDefault(enumerable, index, default(T));

        #endregion GetAtIndexOrDefault

        #region WhereNotNull

        public static T[] WhereNotNull<T>(this T[] array) where T : class
        {
            if (array == null) return null;

            var newArraySize = 0;
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i] != null) newArraySize++;
            }
            var newArray = new T[newArraySize];

            newArraySize = 0;
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i] != null) newArray[newArraySize++] = array[i];
            }

            return newArray;
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> enumerable) where T : class
        {
            foreach (var item in enumerable) if (item != null) yield return item;
        }

        public static T?[] WhereNotNull<T>(this T?[] array) where T : struct
        {
            if (array == null) return null;

            var newArraySize = 0;
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i] != null) newArraySize++;
            }
            var newArray = new T?[newArraySize];

            newArraySize = 0;
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i] != null) newArray[newArraySize++] = array[i];
            }

            return newArray;
        }

        public static IEnumerable<T?> WhereNotNull<T>(this IEnumerable<T?> enumerable) where T : struct
        {
            foreach (var item in enumerable) if (item != null) yield return item;
        }

        #endregion WhereNotNull

        #region Pop

        public static T PopAt<T>(this IList<T> list, int index)
        {
            var o = list[index];
            list.RemoveAt(index);
            return o;
        }

        /// <summary>
        /// Removes the first item in an IList and returns that item
        /// </summary>
        /// <typeparam name="T">List type</typeparam>
        /// <param name="list">list</param>
        /// <returns>The item that was removed</returns>
        public static T PopHead<T>(this IList<T> list) => PopAt(list, 0);

        /// <summary>
        /// Removes the last item in an IList and returns that item
        /// </summary>
        /// <typeparam name="T">List type</typeparam>
        /// <param name="list">list</param>
        /// <returns>The item that was removed</returns>
        public static T PopTail<T>(this IList<T> list) => PopAt(list, list.Count - 1);

        #endregion Pop

        public static IEnumerable<T> MinusHead<T>(this IEnumerable<T> enumerable)
        {
            var first = true;
            foreach (var item in enumerable)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    yield return item;
                }
            }
        }

        public static T? FirstOrNull<T>(this IEnumerable<T> enumerable) where T : struct
        {
            foreach (var o in enumerable) return o;
            return null;
        }

        #region Dictionary

        public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) => new ReadOnlyDictionary<TKey, TValue>(dictionary);

        public static V GetValueNullable<K, V>(this IDictionary<K, V> dictionary, K key) where V : class
        {
            if (!dictionary.TryGetValue(key, out var value)) value = null;
            return value;
        }

        public static V? GetValueNullable<K, V>(this IDictionary<K, V?> dictionary, K key) where V : struct
        {
            if (!dictionary.TryGetValue(key, out var value)) value = null;
            return value;
        }

        public static V GetValueNullable<K, V>(this IDictionary<K, V[]> dictionary, K key, int index) where V : class
        {
            if (!dictionary.TryGetValue(key, out var value)) value = null;
            return value?.GetAtIndexOrDefault(index);
        }

        public static V? GetValueNullable<K, V>(this IDictionary<K, V?[]> dictionary, K key, int index) where V : struct
        {
            if (!dictionary.TryGetValue(key, out var value)) value = null;
            return value?.GetAtIndexOrDefault(index);
        }

        public static V GetValueNullable<K, V>(this IDictionary<K, IList<V>> dictionary, K key, int index) where V : class
        {
            if (!dictionary.TryGetValue(key, out var value)) value = null;
            return value?.GetAtIndexOrDefault(index);
        }

        public static V? GetValueNullable<K, V>(this IDictionary<K, IList<V?>> dictionary, K key, int index) where V : struct
        {
            if (!dictionary.TryGetValue(key, out var value)) value = null;
            return value?.GetAtIndexOrDefault(index);
        }

        public static V GetValueNullable<K, V>(this IDictionary<K, List<V>> dictionary, K key, int index) where V : class
        {
            if (!dictionary.TryGetValue(key, out var value)) value = null;
            return value?.GetAtIndexOrDefault(index);
        }

        public static V? GetValueNullable<K, V>(this IDictionary<K, List<V?>> dictionary, K key, int index) where V : struct
        {
            if (!dictionary.TryGetValue(key, out var value)) value = null;
            return value?.GetAtIndexOrDefault(index);
        }

        public static V GetValueNullable<K, V>(this IDictionary<K, IReadOnlyList<V>> dictionary, K key, int index) where V : class
        {
            if (!dictionary.TryGetValue(key, out var value)) value = null;
            return value?.GetAtIndexOrDefault(index);
        }

        public static V? GetValueNullable<K, V>(this IDictionary<K, IReadOnlyList<V?>> dictionary, K key, int index) where V : struct
        {
            if (!dictionary.TryGetValue(key, out var value)) value = null;
            return value?.GetAtIndexOrDefault(index);
        }

        /// <summary>
        /// Adds an item to a dictionary list, and creates that list if it doesn't already exist.
        /// </summary>
        /// <typeparam name="K">K</typeparam>
        /// <typeparam name="V">V</typeparam>
        /// <param name="dictionary">Dictionary</param>
        /// <param name="key">Key</param>
        /// <param name="values">Values</param>
        /// <returns>True if a new list was created, otherwise false</returns>
        public static bool AddToList<K, V>(this IDictionary<K, List<V>> dictionary, K key, params V[] values)
        {
            if (values == null || values.Length < 1) return false;
            var listCreated = false;
            if (!dictionary.TryGetValue(key, out var list))
            {
                list = new List<V>();
                dictionary.Add(key, list);
                listCreated = true;
            }

            foreach (var value in values) list.Add(value);
            return listCreated;
        }

        public static bool AddToList<K, V>(this IDictionary<K, List<V>> dictionary, K key, IEnumerable<V> values) => AddToList(dictionary, key, values.ToArray());

        #endregion Dictionary

        #endregion Collections

        #region Streams

        public static long Read(this Stream stream, Action<byte[]> action, int bufferSize = (int)(Constant.BYTES_MEGA * 10))
        {
            var buffer = new byte[bufferSize];
            int read;
            long totalRead = 0;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (read != buffer.Length)
                {
                    var buffer2 = new byte[read];
                    Array.Copy(buffer, 0, buffer2, 0, read);
                    action(buffer2);
                }
                else
                {
                    action(buffer);
                }
                totalRead += read;
            }
            return totalRead;
        }

        public static long Read(this StreamReader reader, Action<char[]> action, int bufferSize = (int)(Constant.BYTES_MEGA * 10))
        {
            var buffer = new char[bufferSize];
            int read;
            long totalRead = 0;
            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (read != buffer.Length)
                {
                    var buffer2 = new char[read];
                    Array.Copy(buffer, 0, buffer2, 0, read);
                    action(buffer2);
                }
                else
                {
                    action(buffer);
                }
                totalRead += read;
            }
            return totalRead;
        }

        public static long CopyTo(this Stream source, Stream target) => CopyTo(source, target, Constant.BUFFER_SIZE_OPTIMAL);

        /// <summary>
        /// Reads all the bytes from the current stream and writes them to the destination stream
        /// with the specified buffer size.
        /// </summary>
        /// <param name="source">The current stream.</param>
        /// <param name="target">The stream that will contain the contents of the current stream.</param>
        /// <param name="bufferSize">The size of the buffer to use.</param>
        public static long CopyTo(this Stream source, Stream target, int bufferSize)
        {
            source.CheckNotNull(nameof(source));
            target.CheckNotNull(nameof(target));
            bufferSize.CheckNotZeroNotNegative(nameof(bufferSize));

            var array = new byte[bufferSize];
            long totalCount = 0;
            int count;
            while ((count = source.Read(array, 0, array.Length)) != 0)
            {
                totalCount += count;
                target.Write(array, 0, count);
            }
            return totalCount;
        }

        public static long CopyToWithCount(this Stream source, Stream target) => CopyTo(source, target);

        /// <summary>
        /// Reads all the bytes from the current stream and writes them to the destination stream
        /// with the specified buffer size.
        /// </summary>
        /// <param name="source">The current stream.</param>
        /// <param name="target">The stream that will contain the contents of the current stream.</param>
        /// <param name="bufferSize">The size of the buffer to use.</param>
        public static long CopyToWithCount(this Stream source, Stream target, int bufferSize) => CopyTo(source, target, bufferSize);

        public static void WriteToFile(this Stream stream, string path, int bufferSize)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            //if (File.Exists(path)) File.Delete(path);
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.None))
            {
                CopyTo(stream, fs, bufferSize);
                fs.Flush();
            }
        }

        public static void WriteToFile(this Stream stream, string path) => WriteToFile(stream, path, Constant.BUFFER_SIZE_OPTIMAL);

        public static bool FlushSafe(this Stream stream)
        {
            if (stream == null) return false;
            try
            {
                stream.Flush();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool CloseSafe(this Stream stream)
        {
            if (stream == null) return false;
            try
            {
                stream.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool FlushSafe(this StreamWriter writer)
        {
            if (writer == null) return false;
            try
            {
                writer.Flush();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool CloseSafe(this StreamWriter writer)
        {
            if (writer == null) return false;
            try
            {
                writer.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool FlushSafe(this BinaryWriter writer)
        {
            if (writer == null) return false;
            try
            {
                writer.Flush();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool CloseSafe(this BinaryWriter writer)
        {
            if (writer == null) return false;
            try
            {
                writer.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion Streams

        #region Regex

        public static string[] MatchAll(this Regex regex, string input)
        {
            var matchCollection = regex.Matches(input);
            var list = new List<string>(matchCollection.Count);
            foreach (Match match in matchCollection)
            {
                var val = match.Value;
                if (val != null) list.Add(val);
            }

            return list.ToArray();
        }

        public static string MatchFirst(this Regex regex, string input)
        {
            var matchCollection = regex.Matches(input);
            foreach (Match match in matchCollection)
            {
                var val = match.Value;
                if (val != null) return val;
            }

            return null;
        }

        #endregion Regex

        #region NewLine

        public static string[] SplitOnNewline(this string str) => SplitOnNewline(str, StringSplitOptions.None);

        public static string[] SplitOnNewline(this string str, StringSplitOptions options) => str.Split(new string[] { Constant.NEWLINE_WINDOWS, Constant.NEWLINE_UNIX, Constant.NEWLINE_MAC }, options);

        #endregion NewLine

        #region WhiteSpace

        public static string[] SplitOnWhiteSpace(this string str) => SplitOnWhiteSpace(str, StringSplitOptions.None);

        public static string[] SplitOnWhiteSpace(this string str, StringSplitOptions options)
        {
            var list = new List<string>();

            if (str.Length == 0) return Array.Empty<string>();

            var sb = new StringBuilder();

            for (var i = 0; i < str.Length; i++)
            {
                var c = str[i];
                if (char.IsWhiteSpace(c))
                {
                    if (sb.Length == 0 && options == StringSplitOptions.RemoveEmptyEntries)
                    {
                        ;
                    }
                    else
                    {
                        list.Add(sb.ToString());
                    }
                    sb = new StringBuilder();
                }
                else
                {
                    sb.Append(c);
                }
            }
            if (sb.Length > 0)
            {
                list.Add(sb.ToString());
            }

            return list.ToArray();
        }

        #endregion WhiteSpace

        #region TrimOrNull

        public static string TrimOrNull(this string str)
        {
            if (str == null) return str;
            str = str.Trim();
            if (str.Length == 0) return null;
            return str;
        }

        public static string[] TrimOrNull(this string[] strs)
        {
            if (strs == null) return null;
            var width = strs.Length;

            var strsNew = new string[width];

            for (var i = 0; i < width; i++)
            {
                strsNew[i] = strs[i].TrimOrNull();
            }

            return strsNew;
        }

        public static List<string> TrimOrNull(this List<string> strs)
        {
            var l = new List<string>(strs.Count);
            foreach (var str in strs) l.Add(str.TrimOrNull());
            return l;
        }

        public static IEnumerable<string> TrimOrNull(this IEnumerable<string> strs)
        {
            if (strs != null) foreach (var str in strs) yield return str.TrimOrNull();
        }

        public static string TrimOrNullUpper(this string str) => (str.TrimOrNull())?.ToUpper();

        public static string TrimOrNullLower(this string str) => (str.TrimOrNull())?.ToLower();

        #endregion TrimOrNull

        #region RoundAwayFromZero

        public static decimal Round(this decimal value, MidpointRounding rounding, int decimalPlaces) => Math.Round(value, decimalPlaces, rounding);

        public static double Round(this double value, MidpointRounding rounding, int decimalPlaces) => Math.Round(value, decimalPlaces, rounding);

        public static double Round(this float value, MidpointRounding rounding, int decimalPlaces) => Math.Round(value, decimalPlaces, rounding);

        #endregion RoundAwayFromZero

        #region Equals Floating Point

        public static bool Equals(this double left, double right, byte digitsOfPrecision) => Math.Abs(left - right) < Math.Pow(10d, ((-1d) * digitsOfPrecision));

        public static bool Equals(this float left, float right, byte digitsOfPrecision) => Math.Abs(left - right) < Math.Pow(10f, ((-1f) * digitsOfPrecision));

        #endregion Equals Floating Point

        #region ToStringRoundAwayFromZero

        public static string ToString(this double value, MidpointRounding rounding, int decimalPlaces) => value.Round(rounding, decimalPlaces).ToString("N" + decimalPlaces);

        public static string ToString(this float value, MidpointRounding rounding, int decimalPlaces) => value.Round(rounding, decimalPlaces).ToString("N" + decimalPlaces);

        public static string ToString(this decimal value, MidpointRounding rounding, int decimalPlaces) => value.Round(rounding, decimalPlaces).ToString("N" + decimalPlaces);

        #endregion ToStringRoundAwayFromZero

        #region Color

        public static string ToCSS(this Color color)
        {
            var a = (color.A * (100d / 255d) / 100d).ToString(MidpointRounding.AwayFromZero, 1);
            return $"rgba({color.R}, {color.G}, {color.B}, {a})";
        }

        public static Color Shift(this Color startColor, Color endColor, double percentShifted)
        {
            if (percentShifted > 1d) percentShifted = 1d;
            if (percentShifted < 0d) percentShifted = 0d;

            byte shiftPercent(byte start, byte end, double percent)
            {
                if (start == end) return start;

                var d = (double)start - end;
                d = d * percent;
                var i = int.Parse(d.ToString(MidpointRounding.AwayFromZero, 0));
                i = start - i;
                if (start < end && i > end) i = end;
                if (start > end && i < end) i = end;
                if (i > byte.MaxValue) i = byte.MaxValue;
                if (i < byte.MinValue) i = byte.MinValue;

                var by = (byte)i;
                return by;
            }

            int rStart = startColor.R;
            int rEnd = endColor.R;
            var r = shiftPercent(startColor.R, endColor.R, percentShifted);
            var g = shiftPercent(startColor.G, endColor.G, percentShifted);
            var b = shiftPercent(startColor.B, endColor.B, percentShifted);
            var a = shiftPercent(startColor.A, endColor.A, percentShifted);

            return Color.FromArgb(a, r, g, b);
        }

        #endregion Color

        #region Conversion

        public static T ToNotNull<T>(T? nullable, T ifNull) where T : struct => (nullable == null || !nullable.HasValue) ? ifNull : nullable.Value;

        public static KeyValuePair<TKey, TValue> ToKeyValuePair<TKey, TValue>(this Tuple<TKey, TValue> tuple) => new KeyValuePair<TKey, TValue>(tuple.Item1, tuple.Item2);

        public static ILookup<TKey, TElement> ToLookup<TKey, TEnumerable, TElement>(this IDictionary<TKey, TEnumerable> dictionary) where TEnumerable : IEnumerable<TElement> => dictionary.SelectMany(p => p.Value, Tuple.Create).ToLookup(p => p.Item1.Key, p => p.Item2);

        public static Tuple<TKey, TValue> ToTuple<TKey, TValue>(this KeyValuePair<TKey, TValue> keyValuePair) => Tuple.Create(keyValuePair.Key, keyValuePair.Value);


        public static string ToStringItems(this IEnumerable enumerable)
        {
            var list = new List<string>();

            foreach (var item in enumerable)
            {
                list.Add(item.ToStringGuessFormat());
            }

            return "[" + string.Join(", ", list) + "]";
        }

        public static string ToStringISO8601(this DateTime dateTime) => dateTime.ToString("o"); // ISO 8601

        public static string ToStringGuessFormat(this object obj)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            if (t.IsNullable(out var underlyingType)) t = underlyingType;
            if (t == typeof(string)) return (string)obj;
            if (t == typeof(DateTime)) return ((DateTime)obj).ToString("yyyy-MM-dd HH:mm:ss");
            if (t == typeof(DateTime?)) return ((DateTime?)obj).Value.ToString("yyyy-MM-dd HH:mm:ss");
            if (t == typeof(byte[])) return "0x" + Util.Base16(((byte[])obj));
            if (obj is IEnumerable enumerable) return enumerable.ToStringItems();
            return obj.ToString();
        }
        public static IEnumerable<string> ToStringsGuessFormat(this IEnumerable<object> enumerable)
        {
            foreach (var obj in enumerable.OrEmpty()) yield return obj.ToStringGuessFormat();
        }

        public static string ToStringDelimited<T>(this IEnumerable<T> enumerable, string delimiter) => string.Join(delimiter, enumerable);
        public static string ToStringDelimited(this IEnumerable<object> enumerable, string delimiter) => enumerable.Select(o => o.ToStringGuessFormat()).ToStringDelimited(delimiter);

        public static string ToStringInsecure(this SecureString secureString) => new NetworkCredential("", secureString).Password;

        public static string ToStringTotalSeconds(this TimeSpan timeSpan, int numberOfDecimalDigits = 0) => timeSpan.TotalSeconds.ToString(MidpointRounding.AwayFromZero, Math.Max(0, numberOfDecimalDigits));

        private static readonly string[] ToStringBase16Cache = Enumerable.Range(0, 256).Select(o => BitConverter.ToString(new byte[] { (byte)o })).ToArray();
        public static string ToStringBase16(this byte b) => ToStringBase16Cache[b];

        private static readonly string[] ToStringBase64Cache = Enumerable.Range(0, 256).Select(o => Convert.ToBase64String(new byte[] { (byte)o }).Substring(0, 2)).ToArray();
        public static string ToStringBase64(this byte b) => ToStringBase64Cache[b];

        #region string.To*()

        #region bool

        public static bool ToBool(this string str) => str.ToBoolTry(out var output) ? output : bool.Parse(str);

        public static bool ToBoolTry(this string str, out bool output)
        {
            str = str.TrimOrNull();
            if (str == null)
            {
                output = default;
                return false;
            }

            if (str.Length < 6)
            {
                switch (str.ToUpperInvariant())
                {
                    case "1":
                    case "T":
                    case "TRUE":
                    case "Y":
                    case "YES":
                        output = true;
                        return true;

                    case "0":
                    case "F":
                    case "FALSE":
                    case "N":
                    case "NO":
                        output = false;
                        return true;
                }
            }

            var returnValue = bool.TryParse(str, out var r);
            output = r;
            return returnValue;
        }

        public static bool? ToBoolNullable(this string str) => str == null ? null : (bool?)str.ToBool();

        public static bool ToBoolNullableTry(this string str, out bool? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToBoolTry(out var o);
            output = r ? o : (bool?)null;
            return r;
        }

        #endregion bool

        #region byte

        public static byte ToByte(this string str) => byte.Parse(str);

        public static bool ToByteTry(this string str, out byte output) => byte.TryParse(str, out output);

        public static byte? ToByteNullable(this string str) => str == null ? null : (byte?)str.ToByte();

        public static bool ToByteNullableTry(this string str, out byte? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToByteTry(out var o);
            output = r ? o : (byte?)null;
            return r;
        }

        #endregion byte

        #region sbyte

        public static sbyte ToSByte(this string str) => sbyte.Parse(str);

        public static bool ToSByteTry(this string str, out sbyte output) => sbyte.TryParse(str, out output);

        public static sbyte? ToSByteNullable(this string str) => str == null ? null : (sbyte?)str.ToSByte();

        public static bool ToSByteNullableTry(this string str, out sbyte? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToSByteTry(out var o);
            output = r ? o : (sbyte?)null;
            return r;
        }

        #endregion sbyte

        #region char

        public static char ToChar(this string str) => char.Parse(str);

        public static bool ToCharTry(this string str, out char output) => char.TryParse(str, out output);

        public static char? ToCharNullable(this string str) => str == null ? null : (char?)str.ToChar();

        public static bool ToCharNullableTry(this string str, out char? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToCharTry(out var o);
            output = r ? o : (char?)null;
            return r;
        }

        #endregion char

        #region short

        public static short ToShort(this string str) => short.Parse(str);

        public static bool ToShortTry(this string str, out short output) => short.TryParse(str, out output);

        public static short? ToShortNullable(this string str) => str == null ? null : (short?)str.ToShort();

        public static bool ToShortNullableTry(this string str, out short? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToShortTry(out var o);
            output = r ? o : (short?)null;
            return r;
        }

        #endregion short

        #region ushort

        public static ushort ToUShort(this string str) => ushort.Parse(str);

        public static bool ToUShortTry(this string str, out ushort output) => ushort.TryParse(str, out output);

        public static ushort? ToUShortNullable(this string str) => str == null ? null : (ushort?)str.ToUShort();

        public static bool ToUShortNullableTry(this string str, out ushort? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToUShortTry(out var o);
            output = r ? o : (ushort?)null;
            return r;
        }

        #endregion ushort

        #region int

        public static int ToInt(this string str) => int.Parse(str);

        public static bool ToIntTry(this string str, out int output) => int.TryParse(str, out output);

        public static int? ToIntNullable(this string str) => str == null ? null : (int?)str.ToInt();

        public static bool ToIntNullableTry(this string str, out int? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToIntTry(out var o);
            output = r ? o : (int?)null;
            return r;
        }

        #endregion int

        #region uint

        public static uint ToUInt(this string str) => uint.Parse(str);

        public static bool ToUIntTry(this string str, out uint output) => uint.TryParse(str, out output);

        public static uint? ToUIntNullable(this string str) => str == null ? null : (uint?)str.ToUInt();

        public static bool ToUIntNullableTry(this string str, out uint? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToUIntTry(out var o);
            output = r ? o : (uint?)null;
            return r;
        }

        #endregion uint

        #region long

        public static long ToLong(this string str) => long.Parse(str);

        public static bool ToLongTry(this string str, out long output) => long.TryParse(str, out output);

        public static long? ToLongNullable(this string str) => str == null ? null : (long?)str.ToLong();

        public static bool ToLongNullableTry(this string str, out long? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToLongTry(out var o);
            output = r ? o : (long?)null;
            return r;
        }

        #endregion long

        #region ulong

        public static ulong ToULong(this string str) => ulong.Parse(str);

        public static bool ToULongTry(this string str, out ulong output) => ulong.TryParse(str, out output);

        public static ulong? ToULongNullable(this string str) => str == null ? null : (ulong?)str.ToULong();

        public static bool ToULongNullableTry(this string str, out ulong? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToULongTry(out var o);
            output = r ? o : (ulong?)null;
            return r;
        }

        #endregion ulong

        #region float

        public static float ToFloat(this string str) => float.Parse(str);

        public static bool ToFloatTry(this string str, out float output) => float.TryParse(str, out output);

        public static float? ToFloatNullable(this string str) => str == null ? null : (float?)str.ToFloat();

        public static bool ToFloatNullableTry(this string str, out float? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToFloatTry(out var o);
            output = r ? o : (float?)null;
            return r;
        }

        #endregion float

        #region double

        public static double ToDouble(this string str) => double.Parse(str);

        public static bool ToDoubleTry(this string str, out double output) => double.TryParse(str, out output);

        public static double? ToDoubleNullable(this string str) => str == null ? null : (double?)str.ToDouble();

        public static bool ToDoubleNullableTry(this string str, out double? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToDoubleTry(out var o);
            output = r ? o : (double?)null;
            return r;
        }

        #endregion double

        #region decimal

        public static decimal ToDecimal(this string str) => decimal.Parse(str);

        public static bool ToDecimalTry(this string str, out decimal output) => decimal.TryParse(str, out output);

        public static decimal? ToDecimalNullable(this string str) => str == null ? null : (decimal?)str.ToDecimal();

        public static bool ToDecimalNullableTry(this string str, out decimal? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToDecimalTry(out var o);
            output = r ? o : (decimal?)null;
            return r;
        }

        #endregion decimal

        #region DateTime

        public static DateTime ToDateTime(this string str) => DateTime.Parse(str);

        public static bool ToDateTimeTry(this string str, out DateTime output) => DateTime.TryParse(str, out output);

        public static DateTime? ToDateTimeNullable(this string str) => str == null ? null : (DateTime?)str.ToDateTime();

        public static bool ToDateTimeNullableTry(this string str, out DateTime? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToDateTimeTry(out var o);
            output = r ? o : (DateTime?)null;
            return r;
        }

        #endregion DateTime

        #region Guid

        public static Guid ToGuid(this string str) => Guid.Parse(str);

        public static bool ToGuidTry(this string str, out Guid output) => Guid.TryParse(str, out output);

        public static Guid? ToGuidNullable(this string str) => str == null ? null : (Guid?)str.ToGuid();

        public static bool ToGuidNullableTry(this string str, out Guid? output)
        {
            if (str == null)
            {
                output = null;
                return true;
            }

            var r = str.ToGuidTry(out var o);
            output = r ? o : (Guid?)null;
            return r;
        }

        #endregion Guid

        #region IPAddress

        public static IPAddress ToIPAddress(this string str) => str == null ? null : IPAddress.Parse(str);

        public static bool ToIPAddressTry(this string str, out IPAddress output)
        {
            if (str == null)
            {
                output = null;
                return false;
            }
            return IPAddress.TryParse(str, out output);
        }

        #endregion IPAddress

        #region Uri

        public static Uri ToUri(this string str) => str == null ? null : new Uri(str);

        public static bool ToUriTry(this string str, out Uri output)
        {
            if (str == null)
            {
                output = null;
                return false;
            }
            return Uri.TryCreate(str, UriKind.Absolute, out output);
        }

        #endregion Uri

        #region MailAddress

        public static MailAddress ToMailAddress(this string str) => str == null ? null : new MailAddress(str);

        public static bool ToMailAddressTry(this string str, out MailAddress output)
        {
            if (str == null)
            {
                output = null;
                return false;
            }

            try
            {
                output = new MailAddress(str);
                return true;
            }
            catch (Exception)
            {
                output = null;
                return false;
            }
        }

        #endregion MailAddress

        #region SecureString

        public static SecureString ToSecureString(this string str) => new NetworkCredential("", str).SecurePassword;

        #endregion SecureString

        #endregion string.To*()

        #endregion Conversion

        #region Checks

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

        #region CheckImplements

        public static Type CheckImplements<T>(this Type type, string argumentName)
        {
            var baseClass = typeof(T);
            if (!baseClass.IsAssignableFrom(type))
            {
                var sb = new StringBuilder();
                sb.Append($"Type {type.FullNameFormatted()} does not implement ");
                if (baseClass.IsInterface) sb.Append("interface");
                else if (baseClass.IsAbstract) sb.Append("base abstract class");
                else sb.Append("base class");
                sb.Append(" " + baseClass.FullNameFormatted());
                throw new ArgumentException(sb.ToString(), argumentName);
            }
            return type;
        }

        #endregion CheckImplements

        #endregion Checks

        #region ToStringPadded

        public static string ToStringPadded(this byte value) => value.ToString().PadLeft(byte.MaxValue.ToString().Length, '0');

        public static string ToStringPadded(this sbyte value) => value.ToString().PadLeft(sbyte.MaxValue.ToString().Length, '0');

        public static string ToStringPadded(this decimal value) => value.ToString().PadLeft(decimal.MaxValue.ToString().Length, '0');

        public static string ToStringPadded(this double value) => value.ToString().PadLeft(double.MaxValue.ToString().Length, '0');

        public static string ToStringPadded(this float value) => value.ToString().PadLeft(float.MaxValue.ToString().Length, '0');

        public static string ToStringPadded(this int value) => value.ToString().PadLeft(int.MaxValue.ToString().Length, '0');

        public static string ToStringPadded(this uint value) => value.ToString().PadLeft(uint.MaxValue.ToString().Length, '0');

        public static string ToStringPadded(this long value) => value.ToString().PadLeft(long.MaxValue.ToString().Length, '0');

        public static string ToStringPadded(this ulong value) => value.ToString().PadLeft(ulong.MaxValue.ToString().Length, '0');

        public static string ToStringPadded(this short value) => value.ToString().PadLeft(short.MaxValue.ToString().Length, '0');

        public static string ToStringPadded(this ushort value) => value.ToString().PadLeft(ushort.MaxValue.ToString().Length, '0');

        #endregion ToStringPadded

        #region ToStringCommas

        public static string ToStringCommas(this int value) => string.Format("{0:n0}", value);

        public static string ToStringCommas(this uint value) => string.Format("{0:n0}", value);

        public static string ToStringCommas(this long value) => string.Format("{0:n0}", value);

        public static string ToStringCommas(this ulong value) => string.Format("{0:n0}", value);

        public static string ToStringCommas(this short value) => string.Format("{0:n0}", value);

        public static string ToStringCommas(this ushort value) => string.Format("{0:n0}", value);

        #endregion ToStringCommas
    }
}
