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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace MaxRunSoftware.Utilities;

public static class ExtensionsCheck
{
    #region CheckFileExists

    public static string CheckFileExists(this string fileName)
    {
        if (!File.Exists(fileName)) throw new FileNotFoundException("File does not exist " + fileName, fileName);

        return fileName;
    }

    public static string CheckFileExists(this string fileName, string argumentName)
    {
        CheckNotNull(fileName, argumentName);
        return CheckFileExists(fileName);
    }

    #endregion CheckFileExists

    #region CheckDirectoryExists

    public static string CheckDirectoryExists(this string directoryName)
    {
        if (!Directory.Exists(directoryName)) throw new DirectoryNotFoundException("Directory does not exist " + directoryName);

        return directoryName;
    }

    public static string CheckDirectoryExists(this string directoryName, string argumentName)
    {
        CheckNotNull(directoryName, argumentName);
        return CheckDirectoryExists(directoryName);
    }

    #endregion CheckDirectoryExists

    #region CheckMax

    private static ArgumentOutOfRangeException CheckMaxException(object argument, string argumentName, object maximumInclusive) => new(argumentName, $"Argument {argumentName} with value {argument} cannot be greater than the maximum value of {maximumInclusive}.");

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

    private static ArgumentOutOfRangeException CheckMinException(object argument, string argumentName, object minimumInclusive) => new(argumentName, $"Argument {argumentName} with value {argument} cannot be less than the minimum value of {minimumInclusive}.");

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

    private static ArgumentOutOfRangeException CheckNotNegativeException(object argument, string argumentName) => new(argumentName, $"Argument {argumentName} with value {argument} cannot be negative.");

    // ReSharper disable once UnusedParameter.Global
    public static byte CheckNotNegative(this byte argument, string argumentName) => argument;
    public static decimal CheckNotNegative(this decimal argument, string argumentName) => Math.Sign(argument) < 0 ? throw CheckNotNegativeException(argument, argumentName) : argument;
    public static double CheckNotNegative(this double argument, string argumentName) => Math.Sign(argument) < 0 ? throw CheckNotNegativeException(argument, argumentName) : argument;
    public static float CheckNotNegative(this float argument, string argumentName) => Math.Sign(argument) < 0 ? throw CheckNotNegativeException(argument, argumentName) : argument;
    public static int CheckNotNegative(this int argument, string argumentName) => Math.Sign(argument) < 0 ? throw CheckNotNegativeException(argument, argumentName) : argument;
    public static long CheckNotNegative(this long argument, string argumentName) => Math.Sign(argument) < 0 ? throw CheckNotNegativeException(argument, argumentName) : argument;
    public static short CheckNotNegative(this short argument, string argumentName) => Math.Sign(argument) < 0 ? throw CheckNotNegativeException(argument, argumentName) : argument;

    #endregion CheckNotNegative

    #region CheckNotZero

    private static ArgumentOutOfRangeException CheckNotZeroException(object argument, string argumentName) => new(argumentName, $"Argument {argumentName} with value {argument} cannot be zero.");

    private const ulong zeroULong = 0;
    public static ulong CheckNotZero(this ulong argument, string argumentName) => argument == zeroULong ? throw CheckNotZeroException(argument, argumentName) : argument;
    public static byte CheckNotZero(this byte argument, string argumentName) => Math.Sign(argument) == 0 ? throw CheckNotZeroException(argument, argumentName) : argument;
    public static sbyte CheckNotZero(this sbyte argument, string argumentName) => Math.Sign(argument) == 0 ? throw CheckNotZeroException(argument, argumentName) : argument;
    public static decimal CheckNotZero(this decimal argument, string argumentName) => Math.Sign(argument) == 0 ? throw CheckNotZeroException(argument, argumentName) : argument;
    public static double CheckNotZero(this double argument, string argumentName) => Math.Sign(argument) == 0 ? throw CheckNotZeroException(argument, argumentName) : argument;
    public static float CheckNotZero(this float argument, string argumentName) => Math.Sign(argument) == 0 ? throw CheckNotZeroException(argument, argumentName) : argument;
    public static int CheckNotZero(this int argument, string argumentName) => Math.Sign(argument) == 0 ? throw CheckNotZeroException(argument, argumentName) : argument;
    public static uint CheckNotZero(this uint argument, string argumentName) => Math.Sign(argument) == 0 ? throw CheckNotZeroException(argument, argumentName) : argument;
    public static long CheckNotZero(this long argument, string argumentName) => Math.Sign(argument) == 0 ? throw CheckNotZeroException(argument, argumentName) : argument;
    public static short CheckNotZero(this short argument, string argumentName) => Math.Sign(argument) == 0 ? throw CheckNotZeroException(argument, argumentName) : argument;
    public static ushort CheckNotZero(this ushort argument, string argumentName) => Math.Sign(argument) == 0 ? throw CheckNotZeroException(argument, argumentName) : argument;

    #endregion CheckNotZero

    #region CheckNotZeroNotNegative

    private static ArgumentOutOfRangeException CheckNotZeroNotNegativeException(object argument, string argumentName) => new(argumentName, argument, $"Argument {argumentName} with value {argument} cannot be negative or zero.");

    public static byte CheckNotZeroNotNegative(this byte argument, string argumentName) => Math.Sign(argument) <= 0 ? throw CheckNotZeroNotNegativeException(argument, argumentName) : argument;
    public static sbyte CheckNotZeroNotNegative(this sbyte argument, string argumentName) => Math.Sign(argument) <= 0 ? throw CheckNotZeroNotNegativeException(argument, argumentName) : argument;
    public static decimal CheckNotZeroNotNegative(this decimal argument, string argumentName) => Math.Sign(argument) <= 0 ? throw CheckNotZeroNotNegativeException(argument, argumentName) : argument;
    public static double CheckNotZeroNotNegative(this double argument, string argumentName) => Math.Sign(argument) <= 0 ? throw CheckNotZeroNotNegativeException(argument, argumentName) : argument;
    public static float CheckNotZeroNotNegative(this float argument, string argumentName) => Math.Sign(argument) <= 0 ? throw CheckNotZeroNotNegativeException(argument, argumentName) : argument;
    public static int CheckNotZeroNotNegative(this int argument, string argumentName) => Math.Sign(argument) <= 0 ? throw CheckNotZeroNotNegativeException(argument, argumentName) : argument;
    public static uint CheckNotZeroNotNegative(this uint argument, string argumentName) => Math.Sign(argument) <= 0 ? throw CheckNotZeroNotNegativeException(argument, argumentName) : argument;
    public static long CheckNotZeroNotNegative(this long argument, string argumentName) => Math.Sign(argument) <= 0 ? throw CheckNotZeroNotNegativeException(argument, argumentName) : argument;
    public static ulong CheckNotZeroNotNegative(this ulong argument, string argumentName) => argument <= zeroULong ? throw CheckNotZeroNotNegativeException(argument, argumentName) : argument;
    public static short CheckNotZeroNotNegative(this short argument, string argumentName) => Math.Sign(argument) <= 0 ? throw CheckNotZeroNotNegativeException(argument, argumentName) : argument;
    public static ushort CheckNotZeroNotNegative(this ushort argument, string argumentName) => Math.Sign(argument) <= 0 ? throw CheckNotZeroNotNegativeException(argument, argumentName) : argument;

    #endregion CheckNotZeroNotNegative

    #region CheckNotNull

    /// <summary>
    /// https://blog.jetbrains.com/dotnet/2021/11/04/caller-argument-expressions-in-csharp-10/
    /// https://weblogs.asp.net/dixin/csharp-10-new-feature-callerargumentexpression-argument-check-and-more
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="message"></param>
    /// <param name="parameterName"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    [return: System.Diagnostics.CodeAnalysis.NotNull]
    public static T NotNull<T>(
        [System.Diagnostics.CodeAnalysis.NotNull] this T? obj,
        string? message = default,
        [CallerArgumentExpression("obj")] string? parameterName = default
    ) where T : class =>
        obj ?? throw new ArgumentNullException(parameterName, message);

    /// <summary>
    /// Checks if an argument is null. If it is, throw an ArgumentNullException
    /// </summary>
    /// <typeparam name="T">Argument type</typeparam>
    /// <param name="argument">The argument to check</param>
    /// <param name="argumentName">The nameof argument</param>
    /// <returns>The argument</returns>
    //[ContractAnnotation("argument: null => halt")]
    [return: NotNullIfNotNull("argument")]
    public static T CheckNotNull<T>([NoEnumeration] this T? argument, string argumentName) where T : class
    {
        if (argument == null) throw new ArgumentNullException(argumentName, "Argument " + argumentName + " cannot be null");

        return argument;
    }

    /// <summary>
    /// Checks if an argument is null. If it is, throw an ArgumentNullException
    /// </summary>
    /// <typeparam name="T">Argument type</typeparam>
    /// <param name="argument">The argument to check</param>
    /// <param name="argumentName">The nameof argument</param>
    /// <returns>The argument</returns>
    [ContractAnnotation("argument: null => halt")]
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
    [ContractAnnotation("argument: null => halt")]
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
    [ContractAnnotation("argument: null => halt")]
    public static string CheckNotNullTrimmed(this string argument, string argumentName)
    {
        var s = argument.TrimOrNull();
        if (s == null) throw new ArgumentNullException(argumentName);

        return s;
    }

    #endregion CheckNotNull

    #region CheckPropertyNotNull

    private static string CheckPropertyNotNullFormatException(Type classWithProperty, string propertyName, bool fullTypeName, string suffixMessage)
    {
        var sb = new StringBuilder();

        sb.Append("Property ");

        if (classWithProperty != null)
        {
            sb.Append(fullTypeName ? classWithProperty.FullNameFormatted() : classWithProperty.NameFormatted());
            sb.Append('.');
        }

        propertyName = propertyName.TrimOrNull() ?? "!NotSpecified";
        sb.Append(propertyName);

        sb.Append(" cannot be ");

        sb.Append(suffixMessage);

        return sb.ToString();
    }

    /// <summary>
    /// Checks if a property is null. If it is, throw a NullReferenceException
    /// </summary>
    /// <typeparam name="T">Property type</typeparam>
    /// <param name="property">The property to check</param>
    /// <param name="propertyName">The nameof of the property</param>
    /// <returns>The property value</returns>
    [ContractAnnotation("property: null => halt")]
    public static T CheckPropertyNotNull<T>([NoEnumeration] this T property, string propertyName) where T : class => CheckPropertyNotNull(property, propertyName, null);

    /// <summary>
    /// Checks if a property is null. If it is, throw a NullReferenceException
    /// </summary>
    /// <typeparam name="T">Property type</typeparam>
    /// <param name="property">The property to check</param>
    /// <param name="propertyName">The nameof of the property</param>
    /// <param name="classWithProperty">The class containing the property</param>
    /// <returns>The property value</returns>
    [ContractAnnotation("property: null => halt")]
    public static T CheckPropertyNotNull<T>([NoEnumeration] this T property, string propertyName, Type classWithProperty) where T : class => CheckPropertyNotNull(property, propertyName, classWithProperty, true);

    /// <summary>
    /// Checks if a property is null. If it is, throw a NullReferenceException
    /// </summary>
    /// <typeparam name="T">Property type</typeparam>
    /// <param name="property">The property to check</param>
    /// <param name="propertyName">The nameof of the property</param>
    /// <param name="classWithProperty">The class containing the property</param>
    /// <param name="displayFullTypeName">Whether to display the full class name in the exception</param>
    /// <returns>The property value</returns>
    [ContractAnnotation("property: null => halt")]
    public static T CheckPropertyNotNull<T>([NoEnumeration] this T property, string propertyName, Type classWithProperty, bool displayFullTypeName) where T : class
    {
        if (property == null) throw new NullReferenceException(CheckPropertyNotNullFormatException(classWithProperty, propertyName, displayFullTypeName, "null"));
        return property;
    }

    /// <summary>
    /// Checks if a property is null. If it is, throw a NullReferenceException
    /// </summary>
    /// <typeparam name="T">Property type</typeparam>
    /// <param name="property">The property to check</param>
    /// <param name="propertyName">The nameof of the property</param>
    /// <returns>The property value</returns>
    [ContractAnnotation("property: null => halt")]
    public static T? CheckPropertyNotNull<T>([NoEnumeration] this T? property, string propertyName) where T : struct => CheckPropertyNotNull(property, propertyName, null);

    /// <summary>
    /// Checks if a property is null. If it is, throw a NullReferenceException
    /// </summary>
    /// <typeparam name="T">Property type</typeparam>
    /// <param name="property">The property to check</param>
    /// <param name="propertyName">The nameof of the property</param>
    /// <param name="classWithProperty">The class containing the property</param>
    /// <returns>The property value</returns>
    [ContractAnnotation("property: null => halt")]
    public static T? CheckPropertyNotNull<T>([NoEnumeration] this T? property, string propertyName, Type classWithProperty) where T : struct => CheckPropertyNotNull(property, propertyName, classWithProperty, true);


    /// <summary>
    /// Checks if a property is null. If it is, throw a NullReferenceException
    /// </summary>
    /// <typeparam name="T">Property type</typeparam>
    /// <param name="property">The property to check</param>
    /// <param name="propertyName">The nameof of the property</param>
    /// <param name="classWithProperty">The class containing the property</param>
    /// <param name="displayFullTypeName">Whether to display the full class name in the exception</param>
    /// <returns>The property value</returns>
    [ContractAnnotation("property: null => halt")]
    public static T? CheckPropertyNotNull<T>([NoEnumeration] this T? property, string propertyName, Type classWithProperty, bool displayFullTypeName) where T : struct
    {
        if (property == null) throw new NullReferenceException(CheckPropertyNotNullFormatException(classWithProperty, propertyName, displayFullTypeName, "null"));
        return property;
    }


    /// <summary>
    /// Checks if a property is null or empty. If it is null throw a NullReferenceException. If it is empty, throw an
    /// InvalidOperationException
    /// </summary>
    /// <typeparam name="T">Property type</typeparam>
    /// <param name="property">The property to check</param>
    /// <param name="propertyName">The nameof of the property</param>
    /// <returns>The property value</returns>
    [ContractAnnotation("property: null => halt")]
    public static T[] CheckPropertyNotEmpty<T>(this T[] property, string propertyName) => CheckPropertyNotEmpty(property, propertyName, null);

    /// <summary>
    /// Checks if a property is null or empty. If it is null throw a NullReferenceException. If it is empty, throw an
    /// InvalidOperationException
    /// </summary>
    /// <typeparam name="T">Property type</typeparam>
    /// <param name="property">The property to check</param>
    /// <param name="propertyName">The nameof of the property</param>
    /// <param name="classWithProperty">The class containing the property</param>
    /// <returns>The property value</returns>
    [ContractAnnotation("property: null => halt")]
    public static T[] CheckPropertyNotEmpty<T>(this T[] property, string propertyName, Type classWithProperty) => CheckPropertyNotEmpty(property, propertyName, classWithProperty, true);


    /// <summary>
    /// Checks if a property is null or empty. If it is null throw a NullReferenceException. If it is empty, throw an
    /// InvalidOperationException
    /// </summary>
    /// <typeparam name="T">Property type</typeparam>
    /// <param name="property">The property to check</param>
    /// <param name="propertyName">The nameof of the property</param>
    /// <param name="classWithProperty">The class containing the property</param>
    /// <param name="displayFullTypeName">Whether to display the full class name in the exception</param>
    /// <returns>The property value</returns>
    [ContractAnnotation("property: null => halt")]
    public static T[] CheckPropertyNotEmpty<T>(this T[] property, string propertyName, Type classWithProperty, bool displayFullTypeName)
    {
        if (property == null) throw new NullReferenceException(CheckPropertyNotNullFormatException(classWithProperty, propertyName, displayFullTypeName, "null"));
        if (property.Length == 0) throw new InvalidOperationException(CheckPropertyNotNullFormatException(classWithProperty, propertyName, displayFullTypeName, "empty"));
        return property;
    }


    /// <summary>
    /// Checks if a property is null or empty after being trimmed. If it is, throw a NullReferenceException
    /// </summary>
    /// <param name="property">The property to check</param>
    /// <param name="propertyName">The nameof of the property</param>
    /// <returns>The property value trimmed</returns>
    [ContractAnnotation("property: null => halt")]
    public static string CheckPropertyNotNullTrimmed(this string property, string propertyName) => CheckPropertyNotNullTrimmed(property, propertyName, null);

    /// <summary>
    /// Checks if a property is null or empty after being trimmed. If it is, throw a NullReferenceException
    /// </summary>
    /// <param name="property">The property to check</param>
    /// <param name="propertyName">The nameof of the property</param>
    /// <param name="classWithProperty">The class containing the property</param>
    /// <returns>The property value trimmed</returns>
    [ContractAnnotation("property: null => halt")]
    public static string CheckPropertyNotNullTrimmed(this string property, string propertyName, Type classWithProperty) => CheckPropertyNotNullTrimmed(property, propertyName, classWithProperty, true);

    /// <summary>
    /// Checks if a property is null or empty after being trimmed. If it is, throw a NullReferenceException
    /// </summary>
    /// <param name="property">The property to check</param>
    /// <param name="propertyName">The nameof of the property</param>
    /// <param name="classWithProperty">The class containing the property</param>
    /// <param name="displayFullTypeName">Whether to display the full class name in the exception</param>
    /// <returns>The property value trimmed</returns>
    [ContractAnnotation("property: null => halt")]
    public static string CheckPropertyNotNullTrimmed(this string property, string propertyName, Type classWithProperty, bool displayFullTypeName)
    {
        var s = property.TrimOrNull();
        if (s == null) throw new NullReferenceException(CheckPropertyNotNullFormatException(classWithProperty, propertyName, displayFullTypeName, "null or empty after being trimmed"));
        return s;
    }

    #endregion CheckPropertyNotNull

    #region CheckNotContains

    public static T[] CheckNotContains<T>(this T[] argument, T argumentToCheckFor, string argumentName) => CheckNotContains(argument, argumentToCheckFor, null, argumentName);

    public static T[] CheckNotContains<T>(this T[] argument, T argumentToCheckFor, IEqualityComparer<T> comparer, string argumentName)
    {
        if (argument == null) return null;

        comparer ??= EqualityComparer<T>.Default;

        for (var i = 0; i < argument.Length; i++)
        {
            if (comparer.Equals(argument[i], argumentToCheckFor)) throw new ArgumentException($"Argument {argumentName}[{i}] cannot contain value {argumentToCheckFor.ToStringGuessFormat()}.");
        }

        return argument;
    }

    public static string[] CheckNotContains(this string[] argument, char[] charactersToCheckFor, string argumentName)
    {
        if (argument == null) return null;

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
        if (argument == null) return null;

        comparer ??= EqualityComparer<T>.Default;

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
        if (argument == null) return null;

        var hashSet = new HashSet<char>(validCharacters);
        for (var i = 0; i < argument.Length; i++)
        {
            var a = argument[i].ToCharArray();
            foreach (var c in a)
            {
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
        if (baseClass.IsInterface)
            sb.Append("interface");
        else if (baseClass.IsAbstract)
            sb.Append("base abstract class");
        else
            sb.Append("base class");

        sb.Append(" " + baseClass.FullNameFormatted());
        throw new ArgumentException(sb.ToString(), argumentName);
    }

    public static Type CheckIsEnum(this Type type, string argumentName)
    {
        type.CheckNotNull(argumentName);
        if (type.IsEnum) return type;

        throw new ArgumentException($"Type {type.FullNameFormatted()} is not an enum", argumentName);
    }

    public static Type CheckIsAssignableTo<T>(this Type sourceType, string argumentName) => CheckIsAssignableTo(sourceType, typeof(T), argumentName);

    public static Type CheckIsAssignableTo(this Type sourceType, Type targetType, string argumentName)
    {
        sourceType.CheckNotNull(argumentName);
        targetType.CheckNotNull(nameof(targetType));
        if (!sourceType.IsAssignableTo(targetType)) throw new ArgumentException($"{sourceType.FullNameFormatted()} is not a {targetType.FullNameFormatted()}", argumentName);

        return sourceType;
    }

    #endregion CheckType
}
