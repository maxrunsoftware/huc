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
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace MaxRunSoftware.Utilities;

public static class ExtensionsString
{
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
    public static string RemoveLeft(this string str) => RemoveLeft(str, out _);

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
    public static string RemoveRight(this string str) => RemoveRight(str, out _);

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

        c = str[^1];
        if (str.Length == 1) return string.Empty;

        //return str.Substring(0, str.Length - 1);
        return str[..^1];
    }

    /// <summary>
    /// Counts the number of occurrences of a specific string
    /// </summary>
    /// <param name="str"></param>
    /// <param name="stringToSearchFor"></param>
    /// <param name="comparison"></param>
    /// <returns></returns>
    public static int CountOccurrences(this string str, string stringToSearchFor, StringComparison? comparison = null)
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

    public static string Remove(this string str, string toRemove) => Remove(str, toRemove, out _);

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


    #region IndexOfAny

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

    #endregion IndexOfAny

    #region ContainsAny

    /// <summary>
    /// Returns a value indicating whether a specified substring occurs within this string.
    /// </summary>
    /// <param name="str">The string to search</param>
    /// <param name="subStrings">The strings to seek</param>
    /// <returns>true if the value parameter occurs within this string, or if value is the empty string (""); otherwise, false.</returns>
    /// <exception cref="T:System.ArgumentNullException">str is null.</exception>
    public static bool ContainsAny(this string str, params string[] subStrings) => ContainsAny(str, default, subStrings);

    /// <summary>
    /// Returns a value indicating whether a specified substring occurs within this string.
    /// </summary>
    /// <param name="str">The string to search</param>
    /// <param name="stringComparison">The comparison to use when checking</param>
    /// <param name="subStrings">The strings to seek</param>
    /// <returns>true if the value parameter occurs within this string, or if value is the empty string (""); otherwise, false.</returns>
    /// <exception cref="T:System.ArgumentNullException">str is null.</exception>
    /// <returns></returns>
    public static bool ContainsAny(this string str, StringComparison stringComparison, params string[] subStrings) => subStrings.Any(subString => str.Contains(subString, stringComparison));

    /// <summary>
    /// Returns a value indicating whether a specified substring occurs within this string.
    /// </summary>
    /// <param name="str">The string to search</param>
    /// <param name="subStrings">The strings to seek</param>
    /// <returns>true if the value parameter occurs within this string, or if value is the empty string (""); otherwise, false.</returns>
    /// <exception cref="T:System.ArgumentNullException">str is null.</exception>
    public static bool ContainsAny(this string str, IEnumerable<string> subStrings) => ContainsAny(str, default, subStrings);

    /// <summary>
    /// Returns a value indicating whether a specified substring occurs within this string.
    /// </summary>
    /// <param name="str">The string to search</param>
    /// <param name="stringComparison">The comparison to use when checking</param>
    /// <param name="subStrings">The strings to seek</param>
    /// <returns>true if the value parameter occurs within this string, or if value is the empty string (""); otherwise, false.</returns>
    /// <exception cref="T:System.ArgumentNullException">str is null.</exception>
    /// <returns></returns>
    public static bool ContainsAny(this string str, StringComparison stringComparison, IEnumerable<string> subStrings) => subStrings.Any(subString => str.Contains(subString, stringComparison));

    /// <summary>
    /// Returns a value indicating whether a specified character occurs within this string.
    /// </summary>
    /// <param name="str">The string to search</param>
    /// <param name="chars">The characters to seek</param>
    /// <returns>true if the value parameter occurs within this string; otherwise, false.</returns>
    /// <exception cref="T:System.ArgumentNullException">str is null.</exception>
    public static bool ContainsAny(this string str, params char[] chars) => ContainsAny(str, (IEnumerable<char>)chars);

    /// <summary>
    /// Returns a value indicating whether a specified character occurs within this string.
    /// </summary>
    /// <param name="str">The string to search</param>
    /// <param name="chars">The characters to seek</param>
    /// <returns>true if the value parameter occurs within this string; otherwise, false.</returns>
    /// <exception cref="T:System.ArgumentNullException">str is null.</exception>
    public static bool ContainsAny(this string str, IEnumerable<char> chars) => ContainsAny(str, chars as ISet<char> ?? new HashSet<char>(chars));

    /// <summary>
    /// Returns a value indicating whether a specified character occurs within this string.
    /// </summary>
    /// <param name="str">The string to search</param>
    /// <param name="chars">The characters to seek</param>
    /// <returns>true if the value parameter occurs within this string; otherwise, false.</returns>
    /// <exception cref="T:System.ArgumentNullException">str is null.</exception>
    public static bool ContainsAny(this string str, ISet<char> chars)
    {
        var cs = str.ToCharArray();
        for (var i = 0; i < cs.Length; i++)
        {
            if (chars.Contains(cs[i])) return true;
        }

        return false;
    }

    #endregion ContainsAny

    #region ContainsAll

    /// <summary>
    /// Returns a value indicating whether all of the specified substrings occurs within this string.
    /// </summary>
    /// <param name="str">The string to search</param>
    /// <param name="subStrings">The strings to seek</param>
    /// <returns>true if the value parameter occurs within this string, or if value is the empty string (""); otherwise, false.</returns>
    /// <exception cref="T:System.ArgumentNullException">str is null.</exception>
    public static bool ContainsAll(this string str, params string[] subStrings) => ContainsAll(str, default, subStrings);

    /// <summary>
    /// Returns a value indicating whether all of the specified substrings occurs within this string.
    /// </summary>
    /// <param name="str">The string to search</param>
    /// <param name="stringComparison">The comparison to use when checking</param>
    /// <param name="subStrings">The strings to seek</param>
    /// <returns>true if the value parameter occurs within this string, or if value is the empty string (""); otherwise, false.</returns>
    /// <exception cref="T:System.ArgumentNullException">str is null.</exception>
    /// <returns></returns>
    public static bool ContainsAll(this string str, StringComparison stringComparison, params string[] subStrings) => subStrings.All(subString => str.Contains(subString, stringComparison));

    /// <summary>
    /// Returns a value indicating whether all of the specified substrings occurs within this string.
    /// </summary>
    /// <param name="str">The string to search</param>
    /// <param name="subStrings">The strings to seek</param>
    /// <returns>true if the value parameter occurs within this string, or if value is the empty string (""); otherwise, false.</returns>
    /// <exception cref="T:System.ArgumentNullException">str is null.</exception>
    public static bool ContainsAll(this string str, IEnumerable<string> subStrings) => ContainsAll(str, default, subStrings);

    /// <summary>
    /// Returns a value indicating whether all of the specified substrings occurs within this string.
    /// </summary>
    /// <param name="str">The string to search</param>
    /// <param name="stringComparison">The comparison to use when checking</param>
    /// <param name="subStrings">The strings to seek</param>
    /// <returns>true if the value parameter occurs within this string, or if value is the empty string (""); otherwise, false.</returns>
    /// <exception cref="T:System.ArgumentNullException">str is null.</exception>
    /// <returns></returns>
    public static bool ContainsAll(this string str, StringComparison stringComparison, IEnumerable<string> subStrings) => subStrings.All(subString => str.Contains(subString, stringComparison));

    /// <summary>
    /// Returns a value indicating whether all of the specified characters occurs within this string.
    /// </summary>
    /// <param name="str">The string to search</param>
    /// <param name="chars">The characters to seek</param>
    /// <returns>true if the value parameter occurs within this string; otherwise, false.</returns>
    /// <exception cref="T:System.ArgumentNullException">str is null.</exception>
    public static bool ContainsAll(this string str, params char[] chars) => ContainsAll(str, (IEnumerable<char>)chars);

    /// <summary>
    /// Returns a value indicating whether all of the specified characters occurs within this string.
    /// </summary>
    /// <param name="str">The string to search</param>
    /// <param name="chars">The characters to seek</param>
    /// <returns>true if the value parameter occurs within this string; otherwise, false.</returns>
    /// <exception cref="T:System.ArgumentNullException">str is null.</exception>
    public static bool ContainsAll(this string str, IEnumerable<char> chars)
    {
        var charsSet = chars.OrEmpty().ToHashSet();
        if (charsSet.Count == 0) return true;
        if (charsSet.Count == 1)
        {
            var c = charsSet.First();
            return str.Contains(c);
        }

        var strArray = str.CheckNotNull(nameof(str)).ToCharArray();
        foreach (var c in strArray)
        {
            charsSet.Remove(c);
            if (charsSet.Count == 0) break;
        }

        return charsSet.Count == 0;
    }

    #endregion ContainsAll

    public static bool ContainsWhiteSpace(this string str)
    {
        var chars = str.ToCharArray();
        for (var i = 0; i < str.Length; i++)
        {
            if (char.IsWhiteSpace(chars[i])) return true;
        }

        return false;
    }


    public static string EscapeHtml(this string unescaped) => unescaped == null ? null : WebUtility.HtmlEncode(unescaped);


    public static string Capitalize(this string str)
    {
        if (str == null) return null;

        if (str.Length == 0) return str;

        if (str.Length == 1) return str.ToUpper();

        return str[0].ToString().ToUpper() + str.Substring(1);
    }

    #region Equals

    public static bool Equals(this string str, string other, StringComparer comparer)
    {
        var ec = comparer ?? (IEqualityComparer<string>)EqualityComparer<string>.Default;

        return ec.Equals(str, other);
    }

    public static bool Equals(this string str, string[] others, out string match, StringComparer comparer)
    {
        var ec = comparer ?? (IEqualityComparer<string>)EqualityComparer<string>.Default;

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

    public static bool EqualsIgnoreCase(this string str, string other) => Equals(str, other, StringComparer.OrdinalIgnoreCase);

    public static bool EqualsIgnoreCase(this string str, string[] others, out string match) => Equals(str, others, out match, StringComparer.OrdinalIgnoreCase);

    public static bool EqualsIgnoreCase(this string str, string[] others) => Equals(str, others, out _, StringComparer.OrdinalIgnoreCase);

    public static bool EqualsWildcard(this string text, string wildcardString)
    {
        // https://bitbucket.org/hasullivan/fast-wildcard-matching/src/7457d0dc1aee5ecd373f7c8a7785d5891b416201/FastWildcardMatching/WildcardMatch.cs?at=master&fileviewer=file-view-default

        var isLike = true;
        byte matchCase = 0;
        char[] reversedFilter;
        char[] reversedWord;
        var currentPatternStartIndex = 0;
        var lastCheckedHeadIndex = 0;
        var lastCheckedTailIndex = 0;
        var reversedWordIndex = 0;
        var reversedPatterns = new List<char[]>();

        if (text == null || wildcardString == null) return false;

        var word = text.ToCharArray();
        var filter = wildcardString.ToCharArray();

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

        if ((matchCase == 0 || matchCase == 1) && word.Length != filter.Length) return false;

        switch (matchCase)
        {
            case 0:
                isLike = text == wildcardString;
                break;

            case 1:
                for (var i = 0; i < text.Length; i++)
                {
                    if (word[i] != filter[i] && filter[i] != '?') isLike = false;
                }

                break;

            case 2:
                //Search for matches until first *
                for (var i = 0; i < filter.Length; i++)
                {
                    if (filter[i] != '*')
                    {
                        if (filter[i] != word[i]) return false;
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
                        if (filter[filter.Length - 1 - i] != word[word.Length - 1 - i]) return false;
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

                for (var i = 0; i < reversedWord.Length; i++) reversedWord[i] = word[word.Length - (i + 1) - lastCheckedTailIndex];

                for (var i = 0; i < reversedFilter.Length; i++) reversedFilter[i] = filter[filter.Length - (i + 1) - lastCheckedTailIndex];

                //Cut up the filter into separate patterns, exclude * as they are not longer needed
                for (var i = 0; i < reversedFilter.Length; i++)
                {
                    if (reversedFilter[i] == '*')
                    {
                        if (i - currentPatternStartIndex > 0)
                        {
                            var pattern = new char[i - currentPatternStartIndex];
                            for (var j = 0; j < pattern.Length; j++) pattern[j] = reversedFilter[currentPatternStartIndex + j];

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
                        if (reversedPatterns[i].Length - 1 - j > reversedWord.Length - 1 - reversedWordIndex) return false;

                        if (reversedPatterns[i][j] != reversedWord[reversedWordIndex + j])
                        {
                            reversedWordIndex += 1;
                            j = -1;
                        }
                        else
                        {
                            if (j == reversedPatterns[i].Length - 1) reversedWordIndex = reversedWordIndex + reversedPatterns[i].Length;
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
                        if (filter[i] != word[i] && filter[i] != '?') return false;
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
                        if (filter[filter.Length - 1 - i] != word[word.Length - 1 - i] && filter[filter.Length - 1 - i] != '?') return false;
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

                for (var i = 0; i < reversedWord.Length; i++) reversedWord[i] = word[word.Length - (i + 1) - lastCheckedTailIndex];

                for (var i = 0; i < reversedFilter.Length; i++) reversedFilter[i] = filter[filter.Length - (i + 1) - lastCheckedTailIndex];

                for (var i = 0; i < reversedFilter.Length; i++)
                {
                    if (reversedFilter[i] == '*')
                    {
                        if (i - currentPatternStartIndex > 0)
                        {
                            var pattern = new char[i - currentPatternStartIndex];
                            for (var j = 0; j < pattern.Length; j++) pattern[j] = reversedFilter[currentPatternStartIndex + j];

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
                        if (reversedPatterns[i].Length - 1 - j > reversedWord.Length - 1 - reversedWordIndex) return false;

                        if (reversedPatterns[i][j] != '?' && reversedPatterns[i][j] != reversedWord[reversedWordIndex + j])
                        {
                            reversedWordIndex += 1;
                            j = -1;
                        }
                        else
                        {
                            if (j == reversedPatterns[i].Length - 1) reversedWordIndex = reversedWordIndex + reversedPatterns[i].Length;
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

        if (ignoreCase) return text.ToLower().EqualsWildcard(wildcardString.ToLower());

        return text.EqualsWildcard(wildcardString);
    }

    #endregion Equals

    #region NewLine

    public static string[] SplitOnNewline(this string str, StringSplitOptions options = StringSplitOptions.None) => str.Split(new[] { Constant.NewLine_Windows, Constant.NewLine_Unix, Constant.NewLine_Mac }, options);

    #endregion NewLine

    #region WhiteSpace

    public static string[] SplitOnWhiteSpace(this string str, StringSplitOptions options = StringSplitOptions.None)
    {
        var list = new List<string>();

        if (str.Length == 0) return Array.Empty<string>();

        var sb = new StringBuilder();

        for (var i = 0; i < str.Length; i++)
        {
            var c = str[i];
            if (char.IsWhiteSpace(c))
            {
                if (sb.Length == 0 && options == StringSplitOptions.RemoveEmptyEntries) { }
                else
                    list.Add(sb.ToString());

                sb = new StringBuilder();
            }
            else
                sb.Append(c);
        }

        if (sb.Length > 0) list.Add(sb.ToString());

        return list.ToArray();
    }

    #endregion WhiteSpace

    #region TrimOrNull

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? TrimOrNull(this string? str)
    {
        if (str == null) return null;
        str = str.Trim();
        if (str.Length == 0) return null;
        return str;
    }

    public static string[] TrimOrNull(this string[] strings)
    {
        if (strings == null) return null;

        var width = strings.Length;

        var stringsNew = new string[width];

        for (var i = 0; i < width; i++) stringsNew[i] = strings[i].TrimOrNull();

        return stringsNew;
    }

    public static List<string> TrimOrNull(this List<string> strings)
    {
        var l = new List<string>(strings.Count);
        foreach (var str in strings) l.Add(str.TrimOrNull());

        return l;
    }

    public static IEnumerable<string> TrimOrNull(this IEnumerable<string> strings) => strings.OrEmpty().Select(o => o.TrimOrNull());

    public static string TrimOrNullUpper(this string str) => str.TrimOrNull()?.ToUpper();

    public static string TrimOrNullLower(this string str) => str.TrimOrNull()?.ToLower();

    /// <summary>
    /// Trims whitespace from a StringBuilder instance.
    /// </summary>
    /// <param name="stringBuilder">The StringBuilder to act on</param>
    /// <returns>A reference to this instance after the TrimOrNull operation has completed.</returns>
    public static StringBuilder TrimOrNull(this StringBuilder stringBuilder)
    {
        // TODO: Not very performant for large strings
        var s = stringBuilder.ToString().TrimOrNull();
        stringBuilder.Clear();
        if (s != null) stringBuilder.Append(s);

        return stringBuilder;
    }

    #endregion TrimOrNull

    public static char FindUnusedCharacter(this string str)
    {
        if (str == null) throw new ArgumentNullException(nameof(str));

        const int threshold = 1000; // TODO: Fine tune this. It determines whether to use a simple loop or hashset.

        var c = 33;
        if (str.Length < threshold)
            while (str.IndexOf((char)c) >= 0) { c++; }
        else
        {
            var hash = new HashSet<char>();
            var chars = str.ToCharArray();
            for (var i = 0; i < chars.Length; i++) hash.Add(chars[i]);

            while (hash.Contains((char)c)) { c++; }
        }

        return (char)c;
    }

    /// <summary>Attempts to identity which NewLine character a string uses.</summary>
    /// <param name="str">String to search</param>
    /// <returns>The newline identified</returns>
    public static string IdentifyNewLine(this string str)
    {
        var nl = Environment.NewLine;
        if (str == null) return nl;

        if (str.Length == 0) return nl;

        str = str.Remove(Constant.NewLine_Windows, out var cWin);
        str = str.Remove(Constant.NewLine_Unix, out var cUnix);
        str.Remove(Constant.NewLine_Mac, out var cMac);

        var d = new SortedDictionary<int, List<string>>();
        d.AddToList(cWin, Constant.NewLine_Windows);
        d.AddToList(cUnix, Constant.NewLine_Unix);
        d.AddToList(cMac, Constant.NewLine_Mac);

        var list = d.ToListReversed().First().Value;

        if (list.Count == 1) return list.First();

        if (list.Count == 3) return nl;

        if (list.Count == 2)
        {
            if (nl.In(list[0], list[1])) return nl;

            if (Constant.NewLine_Windows.In(list[0], list[1])) return Constant.NewLine_Windows;

            if (Constant.NewLine_Unix.In(list[0], list[1])) return Constant.NewLine_Unix;

            if (Constant.NewLine_Mac.In(list[0], list[1])) return Constant.NewLine_Mac;

            return nl;
        }

        throw new NotImplementedException("Should never make it here");
    }


    /// <summary>
    /// Tries to guess the best type that is valid to convert a string to
    /// </summary>
    /// <param name="str">The string to guess</param>
    /// <returns>The best found Type or string type if a best guess couldn't be found</returns>
    public static Type GuessType(this string str) => GuessType(str.CheckNotNull(nameof(str)).Yield());

    /// <summary>
    /// Tries to guess the best type for a group of strings. All strings provided must be convertable for the match to be made
    /// </summary>
    /// <param name="strings">The strings to guess on</param>
    /// <returns>The best found Type or string type if a best guess couldn't be found</returns>
    public static Type GuessType(this IEnumerable<string> strings)
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
            if (list.All(o => IPAddress.TryParse(o, out _)))
                return typeof(IPAddress);
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

    #region EndsWithAny

    public static bool EndsWithAny(this string str, params string[] strings) => EndsWithAny(str, StringComparison.CurrentCulture, strings);

    public static bool EndsWithAny(this string str, StringComparison comparison, params string[] strings) => strings.OrEmpty().Any(s => str.EndsWith(s, comparison));

    public static bool EndsWithAny(this string str, IEnumerable<string> strings) => EndsWithAny(str, StringComparison.CurrentCulture, strings);

    public static bool EndsWithAny(this string str, StringComparison comparison, IEnumerable<string> strings) => strings.OrEmpty().Any(s => str.EndsWith(s, comparison));

    #endregion EndsWithAny

    #region StartsWithAny

    public static bool StartsWithAny(this string str, params string[] strings) => StartsWithAny(str, StringComparison.CurrentCulture, strings);
    public static bool StartsWithAny(this string str, ImmutableArray<string> strings) => StartsWithAny(str, StringComparison.CurrentCulture, strings);
    public static bool StartsWithAny(this string str, StringComparison comparison, params string[] strings) => strings.OrEmpty().Any(s => str.StartsWith(s, comparison));
    public static bool StartsWithAny(this string str, StringComparison comparison, ImmutableArray<string> strings) => strings.OrEmpty().Any(s => str.StartsWith(s, comparison));
    public static bool StartsWithAny(this string str, IEnumerable<string> strings) => StartsWithAny(str, StringComparison.CurrentCulture, strings);
    public static bool StartsWithAny(this string str, StringComparison comparison, IEnumerable<string> strings) => strings.OrEmpty().Any(s => str.StartsWith(s, comparison));

    #endregion StartsWithAny

    #region Split

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

    public static string[] SplitOnDirectorySeparator(this string path) => (path ?? string.Empty).Split(Constant.PathDelimiters).Where(o => o.TrimOrNull() != null).ToArray();

    public static string[] SplitOnCamelCase(this string str)
    {
        if (str == null) return Array.Empty<string>();

        if (str.Length == 0) return new[] { str };

        // https://stackoverflow.com/a/37532157
        var words = Regex.Matches(str, "(^[a-z]+|[A-Z]+(?![a-z])|[A-Z][a-z]+|[0-9]+|[a-z]+)")
            //.OfType<Match>()
            .Select(m => m.Value)
            .ToArray();

        return words;
    }

    public static string[] Split(this string str, IEnumerable<string> stringsToSplitOn) => str.Split(stringsToSplitOn.ToArray(), StringSplitOptions.None);

    public static string[] Split(this string str, ImmutableArray<char> charsToSplitOn) => str.Split(charsToSplitOn.ToArray(), StringSplitOptions.None);

    public static string[] Split(this string str, ImmutableHashSet<char> charsToSplitOn) => str.Split(charsToSplitOn.ToArray(), StringSplitOptions.None);

    #endregion Split

    #region SplitDelimited

    /// <summary>http://stackoverflow.com/a/3776617</summary>
    private static readonly Regex splitDelimitedCommaRegex = new("(?:^|,)(\"(?:[^\"]+|\"\")*\"|[^,]*)", RegexOptions.Compiled);

    private static readonly string[] splitDelimitedTabArray = { "\t" };

    public static List<string[]> SplitDelimitedComma(this string text)
    {
        var result = new List<string[]>();
        if (text == null) return result;

        var lines = text.SplitOnNewline().TrimOrNull().WhereNotNull().ToList();

        foreach (var line in lines)
        {
            var matches = splitDelimitedCommaRegex.Matches(line);
            var items = new List<string>(matches.Count);
            foreach (Match match in matches)
            {
                var item = match.Value.TrimStart(',').Replace("\"", "").TrimOrNull();
                items.Add(item);
            }

            result.Add(items.ToArray());
        }

        return result;
    }

    public static List<string[]> SplitDelimitedTab(this string text)
    {
        var result = new List<string[]>();
        if (text == null) return result;

        var lines = text.SplitOnNewline().Where(o => o.TrimOrNull() != null).ToList();

        foreach (var line in lines)
        {
            var matches = line.Split(splitDelimitedTabArray, StringSplitOptions.None);
            var items = new List<string>(matches.Length);
            foreach (var match in matches)
            {
                var item = match.TrimOrNull();
                items.Add(item);
            }

            result.Add(items.ToArray());
        }

        return result;
    }

    #endregion SplitDelimited
}
