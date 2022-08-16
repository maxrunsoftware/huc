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

using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace MaxRunSoftware.Utilities;

public enum CheckType { Argument, Variable, Property, Field }

public static class Check
{
    private static volatile string defaultUndefinedName = "!UNDEFINED!";
    public static string DefaultUndefinedName { get => defaultUndefinedName; set => defaultUndefinedName = value.TrimOrNull() ?? throw new ArgumentNullException(nameof(value)); }

    private static volatile CheckType defaultCheckType = CheckType.Argument;
    public static CheckType DefaultCheckType { get => defaultCheckType; set => defaultCheckType = value; }

    private static volatile bool defaultUseArgumentExpressionForName;
    public static bool DefaultUseArgumentExpressionForName { get => defaultUseArgumentExpressionForName; set => defaultUseArgumentExpressionForName = value; }


    /// <summary>
    /// Checks if value is null, and if it is throw a <see cref="CheckNotNullException" />.
    /// </summary>
    /// <typeparam name="T">Value type</typeparam>
    /// <param name="obj">The value to check</param>
    /// <param name="name">The nameof argument</param>
    /// <param name="type">The value type</param>
    /// <param name="parent">The parent type of the property or field</param>
    /// <param name="callerFilePath">COMPILER GENERATED</param>
    /// <param name="callerLineNumber">COMPILER GENERATED</param>
    /// <param name="callerMemberName">COMPILER GENERATED</param>
    /// <param name="callerArgumentExpression">COMPILER GENERATED</param>
    /// <returns>The value</returns>
    [return: System.Diagnostics.CodeAnalysis.NotNull]
    [ContractAnnotation("obj: null => halt")]
    public static T CheckNotNull<T>(
        [NoEnumeration] this T? obj,
        string? name = null,
        CheckType? type = null,
        Type? parent = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int? callerLineNumber = null,
        [CallerMemberName] string? callerMemberName = null,
        [CallerArgumentExpression("obj")] string? callerArgumentExpression = null
    ) where T : class => obj ?? throw CheckNotNullException.Create(type, name, parent, callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression);


    /// <summary>
    /// Checks if value is null, and if it is throw a <see cref="CheckNotNullException" />.
    /// </summary>
    /// <typeparam name="T">Value type</typeparam>
    /// <param name="obj">The value to check</param>
    /// <param name="name">The nameof argument</param>
    /// <param name="type">The value type</param>
    /// <param name="parent">The parent type of the property or field</param>
    /// <param name="callerFilePath">COMPILER GENERATED</param>
    /// <param name="callerLineNumber">COMPILER GENERATED</param>
    /// <param name="callerMemberName">COMPILER GENERATED</param>
    /// <param name="callerArgumentExpression">COMPILER GENERATED</param>
    /// <returns>The value</returns>
    [ContractAnnotation("obj: null => halt")]
    public static T CheckNotNull<T>(
        [NoEnumeration] this T? obj,
        string? name = null,
        CheckType? type = null,
        Type? parent = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int? callerLineNumber = null,
        [CallerMemberName] string? callerMemberName = null,
        [CallerArgumentExpression("obj")] string? callerArgumentExpression = null
    ) where T : struct => obj ?? throw CheckNotNullException.Create(type, name, parent, callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression);


    /// <summary>
    /// Trims a string and checks if the length is 0, and if it is throw a <see cref="CheckNotNullException" />.
    /// </summary>
    /// <param name="obj">The string to check</param>
    /// <param name="name">The nameof argument</param>
    /// <param name="type">The value type</param>
    /// <param name="parent">The parent type of the property or field</param>
    /// <param name="callerFilePath">COMPILER GENERATED</param>
    /// <param name="callerLineNumber">COMPILER GENERATED</param>
    /// <param name="callerMemberName">COMPILER GENERATED</param>
    /// <param name="callerArgumentExpression">COMPILER GENERATED</param>
    /// <returns>The trimmed string</returns>
    [ContractAnnotation("obj: null => halt")]
    public static string CheckNotNullTrimmed(
        [NoEnumeration] this string? obj,
        string? name = null,
        CheckType? type = null,
        Type? parent = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int? callerLineNumber = null,
        [CallerMemberName] string? callerMemberName = null,
        [CallerArgumentExpression("obj")] string? callerArgumentExpression = null
    ) => obj.TrimOrNull() ?? throw CheckNotNullException.Create(type, name, parent, callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression);


    /// <summary>
    /// Checks if value is null or length is 0, and if it is throw a <see cref="CheckNotNullException" /> or
    /// <see cref="CheckNotEmptyException" />.
    /// </summary>
    /// <typeparam name="T">Collection type</typeparam>
    /// <typeparam name="TItem">Collection item type</typeparam>
    /// <param name="obj">The value to check</param>
    /// <param name="name">The nameof argument</param>
    /// <param name="type">The value type</param>
    /// <param name="parent">The parent type of the property or field</param>
    /// <param name="callerFilePath">COMPILER GENERATED</param>
    /// <param name="callerLineNumber">COMPILER GENERATED</param>
    /// <param name="callerMemberName">COMPILER GENERATED</param>
    /// <param name="callerArgumentExpression">COMPILER GENERATED</param>
    /// <returns>The value</returns>
    [return: System.Diagnostics.CodeAnalysis.NotNull]
    [ContractAnnotation("obj: null => halt")]
    public static T CheckNotEmpty<T, TItem>(
        this T? obj,
        string? name = null,
        CheckType? type = null,
        Type? parent = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int? callerLineNumber = null,
        [CallerMemberName] string? callerMemberName = null,
        [CallerArgumentExpression("obj")] string? callerArgumentExpression = null
    ) where T : IReadOnlyCollection<TItem?> => obj == null
        ? throw CheckNotNullException.Create(type, name, parent, callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression)
        : obj.Count == 0
            ? throw CheckNotEmptyException.Create(type, name, parent, callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression)
            : obj;


    /// <summary>
    /// Checks if value is not null and greater than or equal to a value, and if it is not throw a
    /// <see cref="CheckNotNullException" /> or
    /// <see cref="CheckMinException" />.
    /// </summary>
    /// <typeparam name="T">Collection type</typeparam>
    /// <param name="obj">The value to check</param>
    /// <param name="minInclusive">Value must be greater than or equal to this</param>
    /// <param name="name">The nameof argument</param>
    /// <param name="type">The value type</param>
    /// <param name="parent">The parent type of the property or field</param>
    /// <param name="callerFilePath">COMPILER GENERATED</param>
    /// <param name="callerLineNumber">COMPILER GENERATED</param>
    /// <param name="callerMemberName">COMPILER GENERATED</param>
    /// <param name="callerArgumentExpression">COMPILER GENERATED</param>
    /// <returns>The value</returns>
    [return: System.Diagnostics.CodeAnalysis.NotNull]
    [ContractAnnotation("obj: null => halt")]
    public static T CheckMin<T>(
        this T? obj,
        T minInclusive,
        string? name = null,
        CheckType? type = null,
        Type? parent = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int? callerLineNumber = null,
        [CallerMemberName] string? callerMemberName = null,
        [CallerArgumentExpression("obj")] string? callerArgumentExpression = null
    ) where T : IComparable<T?> => obj == null
        ? throw CheckNotNullException.Create(type, name, parent, callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression)
        : obj.CompareTo(minInclusive) < 0
            ? throw CheckMinException.Create(type, name, parent, obj, minInclusive, callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression)
            : obj;


    /// <summary>
    /// Checks if value is not null and less than a value, and if it is not throw a <see cref="CheckNotNullException" /> or
    /// <see cref="CheckMaxException" />.
    /// </summary>
    /// <typeparam name="T">Collection type</typeparam>
    /// <param name="obj">The value to check</param>
    /// <param name="maxInclusive">Value must be greater than or equal to this</param>
    /// <param name="name">The nameof argument</param>
    /// <param name="type">The value type</param>
    /// <param name="parent">The parent type of the property or field</param>
    /// <param name="callerFilePath">COMPILER GENERATED</param>
    /// <param name="callerLineNumber">COMPILER GENERATED</param>
    /// <param name="callerMemberName">COMPILER GENERATED</param>
    /// <param name="callerArgumentExpression">COMPILER GENERATED</param>
    /// <returns>The value</returns>
    [return: System.Diagnostics.CodeAnalysis.NotNull]
    [ContractAnnotation("obj: null => halt")]
    public static T CheckMax<T>(
        this T? obj,
        T maxInclusive,
        string? name = null,
        CheckType? type = null,
        Type? parent = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int? callerLineNumber = null,
        [CallerMemberName] string? callerMemberName = null,
        [CallerArgumentExpression("obj")] string? callerArgumentExpression = null
    ) where T : IComparable<T?> => obj == null
        ? throw CheckNotNullException.Create(type, name, parent, callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression)
        : obj.CompareTo(maxInclusive) >= 0
            ? throw CheckMaxException.Create(type, name, parent, obj, maxInclusive, callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression)
            : obj;


    /// <summary>
    /// Checks if path is not null and is a file that exists, and if it is not throw a <see cref="CheckNotNullException" /> or
    /// <see cref="CheckFileExistsException" />.
    /// </summary>
    /// <param name="obj">The path to check</param>
    /// <param name="name">The nameof argument</param>
    /// <param name="type">The value type</param>
    /// <param name="parent">The parent type of the property or field</param>
    /// <param name="callerFilePath">COMPILER GENERATED</param>
    /// <param name="callerLineNumber">COMPILER GENERATED</param>
    /// <param name="callerMemberName">COMPILER GENERATED</param>
    /// <param name="callerArgumentExpression">COMPILER GENERATED</param>
    /// <returns>The file path</returns>
    [ContractAnnotation("obj: null => halt")]
    public static string CheckFileExists(
        this string? obj,
        string? name = null,
        CheckType? type = null,
        Type? parent = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int? callerLineNumber = null,
        [CallerMemberName] string? callerMemberName = null,
        [CallerArgumentExpression("obj")] string? callerArgumentExpression = null
    ) => obj == null
        ? throw CheckNotNullException.Create(type, name, parent, callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression)
        : !File.Exists(obj)
            ? throw CheckFileExistsException.Create(type, name, parent, obj, callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression)
            : obj;


    /// <summary>
    /// Checks if path is not null and is a directory that exists, and if it is not throw a
    /// <see cref="CheckNotNullException" /> or
    /// <see cref="CheckDirectoryExistsException" />.
    /// </summary>
    /// <param name="obj">The path to check</param>
    /// <param name="name">The nameof argument</param>
    /// <param name="type">The value type</param>
    /// <param name="parent">The parent type of the property or field</param>
    /// <param name="callerFilePath">COMPILER GENERATED</param>
    /// <param name="callerLineNumber">COMPILER GENERATED</param>
    /// <param name="callerMemberName">COMPILER GENERATED</param>
    /// <param name="callerArgumentExpression">COMPILER GENERATED</param>
    /// <returns>The directory path</returns>
    [ContractAnnotation("obj: null => halt")]
    public static string CheckDirectoryExists(
        this string? obj,
        string? name = null,
        CheckType? type = null,
        Type? parent = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int? callerLineNumber = null,
        [CallerMemberName] string? callerMemberName = null,
        [CallerArgumentExpression("obj")] string? callerArgumentExpression = null
    ) => obj == null
        ? throw CheckNotNullException.Create(type, name, parent, callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression)
        : !Directory.Exists(obj)
            ? throw CheckDirectoryExistsException.Create(type, name, parent, obj, callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression)
            : obj;


    /// <summary>
    /// Checks if type is not null and is an enum, and if it is not throw a <see cref="CheckNotNullException" /> or
    /// <see cref="CheckIsEnumException" />.
    /// </summary>
    /// <param name="obj">The type to check</param>
    /// <param name="name">The nameof argument</param>
    /// <param name="type">The value type</param>
    /// <param name="parent">The parent type of the property or field</param>
    /// <param name="callerFilePath">COMPILER GENERATED</param>
    /// <param name="callerLineNumber">COMPILER GENERATED</param>
    /// <param name="callerMemberName">COMPILER GENERATED</param>
    /// <param name="callerArgumentExpression">COMPILER GENERATED</param>
    /// <returns>The type which is an enum</returns>
    [ContractAnnotation("obj: null => halt")]
    public static Type CheckIsEnum(
        this Type? obj,
        string? name = null,
        CheckType? type = null,
        Type? parent = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int? callerLineNumber = null,
        [CallerMemberName] string? callerMemberName = null,
        [CallerArgumentExpression("obj")] string? callerArgumentExpression = null
    ) => obj == null
        ? throw CheckNotNullException.Create(type, name, parent, callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression)
        : !obj.IsEnum
            ? throw CheckIsEnumException.Create(type, name, parent, obj, callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression)
            : obj;


    /// <summary>
    /// Checks if type is not null and is assignable to a targetType, and if it is not throw a
    /// <see cref="CheckNotNullException" /> or
    /// <see cref="CheckIsAssignableToException" />.
    /// </summary>
    /// <param name="obj">The type to check</param>
    /// <param name="targetType">The target type to see if obj can be assigned to</param>
    /// <param name="name">The nameof argument</param>
    /// <param name="type">The value type</param>
    /// <param name="parent">The parent type of the property or field</param>
    /// <param name="callerFilePath">COMPILER GENERATED</param>
    /// <param name="callerLineNumber">COMPILER GENERATED</param>
    /// <param name="callerMemberName">COMPILER GENERATED</param>
    /// <param name="callerArgumentExpression">COMPILER GENERATED</param>
    /// <returns>The type which is assignable to the targetType</returns>
    [ContractAnnotation("obj: null => halt")]
    public static Type CheckIsAssignableTo(
        this Type? obj,
        Type targetType,
        string? name = null,
        CheckType? type = null,
        Type? parent = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int? callerLineNumber = null,
        [CallerMemberName] string? callerMemberName = null,
        [CallerArgumentExpression("obj")] string? callerArgumentExpression = null
    ) => obj == null
        ? throw CheckNotNullException.Create(type, name, parent, callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression)
        : !obj.IsAssignableTo(targetType)
            ? throw CheckIsAssignableToException.Create(type, name, parent, obj, targetType, callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression)
            : obj;
}

public static class CheckUtil
{
    internal static (CheckType Type, string Name, CallerInfo CallerInfo, string Message) CreateExceptionDetail(
        CheckType? type,
        string? name,
        Type? parent,
        string message,
        string? callerFilePath,
        int? callerLineNumber,
        string? callerMemberName,
        string? callerArgumentExpression
    )
    {
        type ??= Check.DefaultCheckType;
        var c = new CallerInfo(callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression);

        var n = name.TrimOrNull();
        if (n == null)
        {
            if (Check.DefaultUseArgumentExpressionForName) n = c.ArgumentExpression;
            n ??= Check.DefaultUndefinedName;
        }

        var p = parent?.NameFormatted().TrimOrNull();

        var m = new StringBuilder();
        // {Argument} {'arg' in MyMethod} {cannot be null} {stuff}
        // {Variable} {'arg' in MyMethod} {cannot be null} {stuff}
        // {Property} {Parent.Prop} {cannot be null} {stuff}
        // {Field} {Parent.field} {cannot be null} {stuff}

        m.Append(type.ToString()!);
        m.Append(' ');
        if (type is CheckType.Field or CheckType.Property)
        {
            if (p != null) m.Append(p + ".");
            m.Append(n);
        }
        else
        {
            m.Append("'" + n + "'");
            if (p != null) m.Append(" in " + p);
        }

        m.Append(' ');
        m.Append(message);
        m.Append($"  Name: '{n}'");
        if (p != null) m.Append($", Parent: {p}");

        m.Append(", " + c.GetType().Name + ": ");
        if (c.MemberName != null) m.Append($" {c.MemberName}");
        if (c.ArgumentExpression != null) m.Append($" {c.ArgumentExpression}");
        if (c.FilePath != null) m.Append($" {c.FilePath}");
        if (c.LineNumber != null) m.Append($" [{c.LineNumber.Value}]");

        return ((CheckType)type, n, c, m.ToString());
    }
}

public class CheckNotNullException : ArgumentNullException
{
    public CallerInfo CallerInfo { get; }
    public CheckType Type { get; }
    public Type? Parent { get; }

    private CheckNotNullException(CheckType type, CallerInfo callerInfo, string name, string message, Type? parent) : base(name, message)
    {
        Type = type;
        CallerInfo = callerInfo;
        Parent = parent;
    }

    internal static CheckNotNullException Create(CheckType? type, string? name, Type? parent, string? callerFilePath, int? callerLineNumber, string? callerMemberName, string? callerArgumentExpression)
    {
        var d = CheckUtil.CreateExceptionDetail(
            type, name, parent,
            "cannot be null",
            callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression);

        return new CheckNotNullException(d.Type, d.CallerInfo, d.Name, d.Message, parent);
    }
}

public abstract class CheckOutOfRangeException : ArgumentOutOfRangeException
{
    public CallerInfo CallerInfo { get; }
    public CheckType Type { get; }
    public Type? Parent { get; }
    public object Value { get; }

    private protected CheckOutOfRangeException(CheckType type, CallerInfo callerInfo, string name, string message, Type? parent, object value) : base(name, 0, message)
    {
        Type = type;
        CallerInfo = callerInfo;
        Parent = parent;
        Value = value;
    }
}

public class CheckNotEmptyException : CheckOutOfRangeException
{
    private CheckNotEmptyException(CheckType type, CallerInfo callerInfo, string name, string message, Type? parent) : base(type, callerInfo, name, message, parent, 0) { }

    internal static CheckNotEmptyException Create(CheckType? type, string? name, Type? parent, string? callerFilePath, int? callerLineNumber, string? callerMemberName, string? callerArgumentExpression)
    {
        var d = CheckUtil.CreateExceptionDetail(
            type, name, parent,
            "cannot be empty",
            callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression);

        return new CheckNotEmptyException(d.Type, d.CallerInfo, d.Name, d.Message, parent);
    }
}

public class CheckMinException : CheckOutOfRangeException
{
    public object Min { get; }

    private CheckMinException(CheckType type, CallerInfo callerInfo, string name, string message, Type? parent, object value, object min) : base(type, callerInfo, name, message, parent, value)
    {
        Min = min;
    }

    internal static CheckMinException Create(CheckType? type, string? name, Type? parent, object value, object min, string? callerFilePath, int? callerLineNumber, string? callerMemberName, string? callerArgumentExpression)
    {
        var d = CheckUtil.CreateExceptionDetail(
            type, name, parent,
            $"must be greater than or equal to {min} but was {value}",
            callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression);

        return new CheckMinException(d.Type, d.CallerInfo, d.Name, d.Message, parent, value, min);
    }
}

public class CheckMaxException : CheckOutOfRangeException
{
    public object Max { get; }

    private CheckMaxException(CheckType type, CallerInfo callerInfo, string name, string message, Type? parent, object value, object max) : base(type, callerInfo, name, message, parent, value)
    {
        Max = max;
    }

    internal static CheckMaxException Create(CheckType? type, string? name, Type? parent, object value, object max, string? callerFilePath, int? callerLineNumber, string? callerMemberName, string? callerArgumentExpression)
    {
        var d = CheckUtil.CreateExceptionDetail(
            type, name, parent,
            $"must be less than or equal to {max} but was {value}",
            callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression);

        return new CheckMaxException(d.Type, d.CallerInfo, d.Name, d.Message, parent, value, max);
    }
}

public class CheckFileExistsException : FileNotFoundException
{
    public string Name { get; }
    public CallerInfo CallerInfo { get; }
    public CheckType Type { get; }
    public Type? Parent { get; }
    public string Path { get; }

    private CheckFileExistsException(CheckType type, CallerInfo callerInfo, string name, string message, Type? parent, string path) : base(message, path)
    {
        Name = name;
        Type = type;
        CallerInfo = callerInfo;
        Parent = parent;
        Path = path;
    }

    internal static CheckFileExistsException Create(CheckType? type, string? name, Type? parent, string path, string? callerFilePath, int? callerLineNumber, string? callerMemberName, string? callerArgumentExpression)
    {
        var d = CheckUtil.CreateExceptionDetail(
            type, name, parent,
            $"file not found {path}",
            callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression);

        return new CheckFileExistsException(d.Type, d.CallerInfo, d.Name, d.Message, parent, path);
    }
}

public class CheckDirectoryExistsException : DirectoryNotFoundException
{
    public string Name { get; }
    public CallerInfo CallerInfo { get; }
    public CheckType Type { get; }
    public Type? Parent { get; }
    public string Path { get; }

    private CheckDirectoryExistsException(CheckType type, CallerInfo callerInfo, string name, string message, Type? parent, string path) : base(message)
    {
        Name = name;
        Type = type;
        CallerInfo = callerInfo;
        Parent = parent;
        Path = path;
    }

    internal static CheckDirectoryExistsException Create(CheckType? type, string? name, Type? parent, string path, string? callerFilePath, int? callerLineNumber, string? callerMemberName, string? callerArgumentExpression)
    {
        var d = CheckUtil.CreateExceptionDetail(
            type, name, parent,
            $"directory not found {path}",
            callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression);

        return new CheckDirectoryExistsException(d.Type, d.CallerInfo, d.Name, d.Message, parent, path);
    }
}

public abstract class CheckTypeException : ArgumentException
{
    public CallerInfo CallerInfo { get; }
    public CheckType Type { get; }
    public Type? Parent { get; }
    public Type Value { get; }

    private protected CheckTypeException(CheckType type, CallerInfo callerInfo, string name, string message, Type? parent, Type value) : base(message, name)
    {
        Type = type;
        CallerInfo = callerInfo;
        Parent = parent;
        Value = value;
    }
}

public class CheckIsEnumException : CheckTypeException
{
    private protected CheckIsEnumException(CheckType type, CallerInfo callerInfo, string name, string message, Type? parent, Type value) : base(type, callerInfo, name, message, parent, value) { }

    internal static CheckTypeException Create(CheckType? type, string? name, Type? parent, Type value, string? callerFilePath, int? callerLineNumber, string? callerMemberName, string? callerArgumentExpression)
    {
        var d = CheckUtil.CreateExceptionDetail(
            type, name, parent,
            $"{value.NameFormatted()} is not an enum",
            callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression);

        return new CheckIsEnumException(d.Type, d.CallerInfo, d.Name, d.Message, parent, value);
    }
}

public class CheckIsAssignableToException : CheckTypeException
{
    public Type TargetType { get; }
    private protected CheckIsAssignableToException(CheckType type, CallerInfo callerInfo, string name, string message, Type? parent, Type value, Type targetType) : base(type, callerInfo, name, message, parent, value)
    {
        TargetType = targetType;
    }

    internal static CheckIsAssignableToException Create(CheckType? type, string? name, Type? parent, Type value, Type targetType, string? callerFilePath, int? callerLineNumber, string? callerMemberName, string? callerArgumentExpression)
    {
        var d = CheckUtil.CreateExceptionDetail(
            type, name, parent,
            $"{value.NameFormatted()} is not assignable to {targetType.NameFormatted()}",
            callerFilePath, callerLineNumber, callerMemberName, callerArgumentExpression);

        return new CheckIsAssignableToException(d.Type, d.CallerInfo, d.Name, d.Message, parent, value, targetType);
    }
}
